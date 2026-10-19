using System.Text.Json;
using GdKirie.EventaAdapter;
using GdKirie.Qr;
using Godot;

public sealed partial class RuntimeTests : Node
{
    private SceneTree? _testTree;
    private bool _succeeded;

    private sealed class TestTransport : IKirieTextTransport
    {
        public event Action<string>? TextReceived;
        internal TaskCompletionSource<string> Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void SendText(string message) => Response.TrySetResult(message);
        internal void Receive(string message)
        {
            Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TextReceived?.Invoke(message);
        }
    }

    public override async void _Ready()
    {
        _testTree = GetTree();
        try
        {
            await Run();
            _succeeded = true;
            GD.Print("QR runtime tests passed: JSON Eventa PNG request/response, generation, synchronous signals, misses, backpressure, cancellation, late callbacks, permissions, Android Back, background, scene exit.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    public override void _ExitTree()
    {
        if (!_succeeded)
            _testTree?.Quit(1);
    }

    private async Task Run()
    {
        var qr = new FakeQr();
        var camera = new FakeCamera();
        AddChild(qr);
        AddChild(camera);
        Engine.RegisterSingleton("QRPlugin", qr);
        Engine.RegisterSingleton("NativeCameraPlugin", camera);
        try
        {
            var owner = new Node();
            var web = new Control();
            AddChild(owner);
            owner.AddChild(web);
            var transport = new TestTransport();
            using var context = KirieEventa.CreateContext(transport, GdKirieQr.Register(new KirieEventaJsonRegistry()));
            using var host = GdKirieQr.Attach(context.Context, owner, web);
            transport.Receive("""
                {"type":"kirie:qr:generate-image-send","payload":{"body":{"invokeId":"generate-1","content":{"text":"wire test","size":18,"foreground":"#ff0000","background":"#ffffff"}}}}
                """);
            using var generationReply = JsonDocument.Parse(await transport.Response.Task);
            var pngBase64 = generationReply.RootElement.GetProperty("payload").GetProperty("body").GetProperty("content").GetProperty("pngBase64").GetString()!;
            using var wireImage = new Image();
            Assert(wireImage.LoadPngFromBuffer(Convert.FromBase64String(pngBase64)) == Error.Ok
                && wireImage.GetWidth() == 18 && wireImage.GetPixel(0, 0) == Colors.Red,
                "The Eventa JSON request did not return the generated PNG.");
            qr.ImmediateResult = "wire scan result ✓";
            transport.Receive("""
                {"type":"kirie:qr:scan-image-send","payload":{"body":{"invokeId":"scan-image-1","content":{"pngBase64":"__PNG__"}}}}
                """.Replace("__PNG__", pngBase64));
            using var scanReply = JsonDocument.Parse(await transport.Response.Task);
            Assert(scanReply.RootElement.GetProperty("payload").GetProperty("body").GetProperty("content").GetProperty("text").GetString() == qr.ImmediateResult,
                "The Eventa JSON image request did not return recognized text.");
            qr.ImmediateResult = null;
            using var image = host.GenerateImage("test", 24, Colors.Red);
            Assert(image.GetWidth() == 24 && image.GetPixel(0, 0) == Colors.Red, "Generation did not preserve the native image.");
            using var texture = host.GenerateTexture("texture", 16);
            Assert(texture.GetWidth() == 16, "Texture conversion failed.");

            qr.ImmediateMiss = true;
            Assert(await host.ScanImageAsync(image) is null, "An image miss must return null.");
            qr.ImmediateMiss = false;
            qr.ImmediateResult = "同步结果 ✓";
            Assert(await host.ScanImageAsync(image.SavePngToBuffer()) == qr.ImmediateResult, "Synchronous native results were lost.");
            qr.ImmediateResult = null;

            using var cancellation = new CancellationTokenSource();
            var canceledImage = host.ScanImageAsync(image, cancellation.Token);
            cancellation.Cancel();
            Assert(canceledImage.IsCanceled, "Token cancellation did not cancel image recognition.");
            ExpectBusy(() => host.ScanImageAsync(image));
            qr.Detect("late canceled image");
            await Flush();

            var scan = host.ScanAsync();
            Assert(camera.Started && !web.Visible, "The camera did not start above the hidden WebView.");
            using (var competingContext = Eventa.EventContext.Create())
            using (var competingHost = GdKirieQr.Attach(competingContext, owner, web))
                ExpectBusy(() => competingHost.ScanImageAsync(image));
            var before = qr.ScanCalls;
            camera.Frame(image);
            camera.Frame(image);
            await Flush();
            Assert(qr.ScanCalls == before + 1, "Busy recognition accepted another camera frame.");
            qr.Miss();
            await Flush();
            Assert(!scan.IsCompleted && camera.Started, "A camera miss ended the scan.");
            camera.Frame(image);
            await Flush();
            qr.Detect("camera result");
            Assert(await scan == "camera result", "The camera result was not returned.");
            Assert(!camera.Started && web.Visible, "Success did not stop capture and restore the WebView.");

            var canceledScan = host.ScanAsync();
            camera.Frame(image);
            await Flush();
            host.CancelScan();
            Assert(await canceledScan is null && web.Visible && !camera.Started, "Camera cancellation did not clean up.");
            ExpectBusy(() => host.ScanAsync());
            qr.Detect("late canceled camera");
            await Flush();

            var oldSession = host.ScanAsync();
            camera.Frame(image);
            host.CancelScan();
            Assert(await oldSession is null, "The old camera session did not cancel.");
            var nextSession = host.ScanAsync();
            var callsBeforeStaleFrame = qr.ScanCalls;
            await Flush();
            Assert(!GetTree().QuitOnGoBack, "The previous session restored the back policy during the next scan.");
            Assert(qr.ScanCalls == callsBeforeStaleFrame,
                "A deferred frame from the previous session entered the next session.");
            host.CancelScan();
            Assert(await nextSession is null, "The next camera session did not cancel.");

            var backScan = host.ScanAsync();
            GetTree().Root.EmitSignal(Window.SignalName.GoBackRequested);
            Assert(await backScan is null && !camera.Started && web.Visible,
                "The window Back signal did not cancel the scan.");
            await Flush();
            Assert(GetTree().QuitOnGoBack, "The original Android Back policy was not restored.");
            GetTree().QuitOnGoBack = false;
            var customBackPolicy = host.ScanAsync();
            host.CancelScan();
            Assert(await customBackPolicy is null, "The custom-policy scan did not cancel.");
            await Flush();
            Assert(!GetTree().QuitOnGoBack, "The host changed the application's custom Back policy.");
            GetTree().QuitOnGoBack = true;

            var failedScan = host.ScanAsync();
            camera.Frame(image);
            await Flush();
            qr.Fail();
            await ExpectFailure<InvalidOperationException>(failedScan);
            Assert(web.Visible && !camera.Started, "A native recognition error did not clean up the camera.");

            camera.Permission = false;
            var firstPermission = host.ScanAsync();
            var permissionView = owner.GetChildren().OfType<CanvasLayer>().Last();
            permissionView.Notification((int)NotificationApplicationPaused);
            Assert(!firstPermission.IsCompleted && !camera.Started,
                "The Android permission activity canceled the pending scan.");
            var permissionStarts = camera.Starts;
            camera.Grant();
            await Flush();
            Assert(!firstPermission.IsCompleted && camera.Starts == permissionStarts,
                "A permission callback started capture while the application was paused.");
            permissionView.Notification((int)NotificationApplicationResumed);
            Assert(camera.Started && camera.Starts == permissionStarts + 1,
                "Granted permission did not start capture after the application resumed.");
            host.CancelScan();
            Assert(await firstPermission is null, "The first permission scan did not cancel.");

            var resumedPermission = host.ScanAsync();
            permissionView = owner.GetChildren().OfType<CanvasLayer>().Last();
            permissionView.Notification((int)NotificationApplicationPaused);
            permissionView.Notification((int)NotificationApplicationResumed);
            camera.Grant();
            await Flush();
            Assert(camera.Started, "Permission arriving after resume did not start capture.");
            host.CancelScan();
            Assert(await resumedPermission is null, "The resumed permission scan did not cancel.");

            var permission = host.ScanAsync();
            camera.Deny();
            await ExpectFailure<UnauthorizedAccessException>(permission);
            Assert(web.Visible && !camera.Started, "Permission denial left the preview active.");
            var pendingPermission = host.ScanAsync();
            host.CancelScan();
            Assert(await pendingPermission is null, "Permission cancellation did not return null.");
            ExpectBusy(() => host.ScanAsync());
            var starts = camera.Starts;
            camera.Grant();
            await Flush();
            Assert(camera.Starts == starts, "A late permission callback restarted capture.");
            camera.Permission = true;

            web.Hide();
            var background = host.ScanAsync();
            var view = owner.GetChildren().OfType<CanvasLayer>().Last();
            view.Notification((int)NotificationApplicationPaused);
            Assert(await background is null && !web.Visible && !camera.Started, "Background cleanup changed the original hidden state.");
            web.Show();

            var exiting = host.ScanAsync();
            RemoveChild(owner);
            Assert(await exiting is null && !camera.Started, "Scene exit did not stop the camera.");
            owner.Free();
            Assert(camera.Stops >= 4, "Camera lifecycle did not stop each capture session.");
        }
        finally
        {
            Engine.UnregisterSingleton("NativeCameraPlugin");
            Engine.UnregisterSingleton("QRPlugin");
        }
    }

    private async Task Flush()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void ExpectBusy(Func<Task<string?>> start)
    {
        try
        {
            start();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("A pending native callback did not preserve recognition ownership.");
    }

    private static async Task ExpectFailure<T>(Task<string?> task) where T : Exception
    {
        try
        {
            await task;
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Assert(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException(description);
    }
}
