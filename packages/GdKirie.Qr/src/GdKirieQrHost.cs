using Eventa;
using Godot;
using Dictionary = Godot.Collections.Dictionary;

namespace GdKirie.Qr;

/// <summary>Provides QR operations and owns registrations for a borrowed scene owner.</summary>
public sealed class GdKirieQrHost : IDisposable
{
    private enum ScanKind { Image, Camera }

    private sealed class Operation
    {
        internal readonly ScanKind Kind;
        internal readonly QrNative Native;
        internal readonly TaskCompletionSource<string?> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationTokenRegistration Cancellation;
        // Caller cleanup can finish while native recognition or permission callbacks still drain.
        internal bool Finished;

        internal Operation(GdKirieQrHost host, ScanKind kind)
        {
            Kind = kind;
            Native = new QrNative(text => host.OnDetected(this, text), error => host.OnFailed(this, error),
                () => host.OnPermissionGranted(this), () => host.OnPermissionDenied(this),
                frame => host.OnFrame(this, frame), kind == ScanKind.Camera);
        }
    }

    // Upstream callbacks have no request ID. Keep ownership until every pending result drains.
    private static GdKirieQrHost? _recognitionOwner;
    private readonly Node _owner;
    private readonly SceneTree _tree;
    private readonly Control _kirieNode;
    private readonly IDisposable[] _registrations;
    private Operation? _operation;
    private QrScanView? _view;
    private Dictionary? _latestFrame;
    private bool _wasVisible;
    private bool _wasQuitOnGoBack;
    private bool _recognitionPending;
    private bool _permissionPending;
    private bool _applicationPaused;
    private bool _cameraStarted;
    private bool _disposed;

    internal GdKirieQrHost(IEventContext context, Node owner, Control kirieNode)
    {
        _owner = owner;
        _tree = owner.GetTree();
        _kirieNode = kirieNode;
        _registrations =
        [
            context.RegisterInvokeHandler(QrEvents.GenerateImage, (request, token) =>
            {
                token.ThrowIfCancellationRequested();
                using var image = GenerateImage(request.Text, request.Size,
                    ParseColor(request.Foreground), ParseColor(request.Background));
                return Task.FromResult(new PngPayload(Convert.ToBase64String(image.SavePngToBuffer())));
            }),
            context.RegisterInvokeHandler(QrEvents.ScanImage, async (request, token) =>
                new ScanResult(await ScanImageAsync(Convert.FromBase64String(request.PngBase64), token))),
            context.RegisterInvokeHandler(QrEvents.Scan, async (EmptyPayload _, CancellationToken token) =>
                new ScanResult(await ScanAsync(token))),
        ];
        _owner.TreeExiting += Dispose;
        _kirieNode.TreeExiting += Dispose;
    }

    /// <summary>Generates an RGBA image. The caller owns and disposes the returned image.</summary>
    public Image GenerateImage(string text, int size = 512, Color? foreground = null, Color? background = null)
    {
        RequireReady();
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        return QrNative.GenerateImage(text, size, foreground ?? Colors.Black, background ?? Colors.White);
    }

    /// <summary>Generates a texture. The caller owns and disposes the returned texture.</summary>
    public ImageTexture GenerateTexture(string text, int size = 512, Color? foreground = null, Color? background = null)
    {
        using var image = GenerateImage(text, size, foreground, background);
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>Recognizes one PNG image, or returns null when the image contains no QR code.</summary>
    public Task<string?> ScanImageAsync(byte[] png, CancellationToken cancellationToken = default)
    {
        RequireReady();
        ArgumentNullException.ThrowIfNull(png);
        using var image = new Image();
        var error = image.LoadPngFromBuffer(png);
        if (error != Error.Ok)
            throw new ArgumentException($"The PNG image could not be decoded: {error}.", nameof(png));
        return ScanImageAsync(image, cancellationToken);
    }

    /// <summary>Recognizes a Godot image. The caller retains ownership of the input image.</summary>
    public Task<string?> ScanImageAsync(Image image, CancellationToken cancellationToken = default)
    {
        RequireReady();
        ArgumentNullException.ThrowIfNull(image);
        if (image.IsEmpty())
            throw new ArgumentException("The image is empty.", nameof(image));
        var operation = Begin(ScanKind.Image, cancellationToken);
        if (operation.Finished)
            return operation.Completion.Task;
        _recognitionPending = true;
        operation.Native.Scan(image);
        return operation.Completion.Task;
    }

    /// <summary>Shows a camera preview and returns the first QR code, or null after user cancellation.</summary>
    public Task<string?> ScanAsync(CancellationToken cancellationToken = default)
    {
        RequireReady();
        var operation = Begin(ScanKind.Camera, cancellationToken);
        if (operation.Finished)
            return operation.Completion.Task;

        _wasVisible = _kirieNode.Visible;
        _wasQuitOnGoBack = _tree.QuitOnGoBack;
        _tree.QuitOnGoBack = false;
        _view = new QrScanView();
        _view.CancelRequested += CancelScan;
        _view.ApplicationPausedChanged += OnApplicationPausedChanged;
        _applicationPaused = false;
        _owner.AddChild(_view);
        _kirieNode.Hide();
        if (operation.Native.HasPermission())
            StartCamera();
        else
        {
            _permissionPending = true;
            operation.Native.RequestPermission();
        }

        return operation.Completion.Task;
    }

    /// <summary>Cancels the active camera scan and returns null to its caller.</summary>
    public void CancelScan()
    {
        RequireMainThread();
        if (_operation is { Finished: false, Kind: ScanKind.Camera })
            Finish(null);
    }

    private Operation Begin(ScanKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_recognitionOwner is not null)
            throw new InvalidOperationException("A QR operation or its pending native callback is still active.");
        var operation = new Operation(this, kind);
        _recognitionOwner = this;
        _operation = operation;
        operation.Cancellation = cancellationToken.Register(() =>
        {
            void Cancel()
            {
                if (ReferenceEquals(_operation, operation) && !operation.Finished)
                    Finish(null, cancellationToken: cancellationToken);
            }

            if (GodotThread.IsMainThread())
                Cancel();
            else
                Callable.From(Cancel).CallDeferred();
        });
        if (operation.Finished)
            operation.Cancellation.Dispose();
        return operation;
    }

    private void StartCamera()
    {
        _cameraStarted = true;
        _tree.ProcessFrame += ProcessFrame;
        try
        {
            _operation!.Native.StartCamera();
        }
        catch (InvalidOperationException error)
        {
            Finish(null, error);
        }
    }

    private void OnPermissionGranted(Operation operation)
    {
        if (!ReferenceEquals(_operation, operation) || !_permissionPending)
            return;
        _permissionPending = false;
        if (!operation.Finished && !_applicationPaused)
            StartCamera();
        else
            ReleaseIfDrained();
    }

    private void OnApplicationPausedChanged(bool paused)
    {
        _applicationPaused = paused;
        if (_operation is not { Finished: false, Kind: ScanKind.Camera })
            return;
        // Android's permission activity pauses Godot before the user answers.
        // There is no camera to stop yet. Wait for permission and foreground state.
        if (_permissionPending)
            return;
        if (paused)
        {
            if (_cameraStarted)
                Finish(null);
            return;
        }

        if (!_cameraStarted)
            StartCamera();
    }

    private void OnPermissionDenied(Operation operation)
    {
        if (!ReferenceEquals(_operation, operation) || !_permissionPending)
            return;
        _permissionPending = false;
        if (!operation.Finished)
            Finish(null, new UnauthorizedAccessException("Camera permission was denied."));
        else
            ReleaseIfDrained();
    }

    private void OnFrame(Operation operation, Dictionary data)
    {
        if (!ReferenceEquals(_operation, operation) || operation.Finished || operation.Kind != ScanKind.Camera || !_cameraStarted)
        {
            data.Dispose();
            return;
        }

        // Deferred camera signals can arrive faster than rendering and recognition.
        _latestFrame?.Dispose();
        _latestFrame = data;
    }

    private void ProcessFrame()
    {
        using var data = _latestFrame;
        _latestFrame = null;
        if (data is null)
            return;

        var operation = _operation!;
        Image image;
        try
        {
            image = QrNative.ReadFrame(data);
        }
        catch (InvalidOperationException error)
        {
            Finish(null, error);
            return;
        }

        using var ownedImage = image;
        _view!.ShowFrame(image);
        if (_recognitionPending)
            return;
        _recognitionPending = true;
        operation.Native.Scan(image);
    }

    private void OnDetected(Operation operation, string text)
    {
        if (!ReferenceEquals(_operation, operation) || !_recognitionPending)
            return;
        _recognitionPending = false;
        if (!operation.Finished)
            Finish(text);
        else
            ReleaseIfDrained();
    }

    private void OnFailed(Operation operation, Dictionary error)
    {
        if (!ReferenceEquals(_operation, operation) || !_recognitionPending)
            return;
        _recognitionPending = false;
        if (operation.Finished)
        {
            ReleaseIfDrained();
            return;
        }

        // Android returns a Java enum; iOS returns its numeric ordinal.
        var codeValue = error["code"];
        var code = codeValue.VariantType == Variant.Type.Object
            ? codeValue.AsGodotObject().Call("ordinal").AsInt32()
            : codeValue.AsInt32();
        if (code == 2)
        {
            if (operation.Kind == ScanKind.Image)
                Finish(null);
            return;
        }

        Finish(null, new InvalidOperationException($"QR scan failed ({code}): {error["description"].AsString()}"));
    }

    private void Finish(string? text, Exception? error = null, CancellationToken cancellationToken = default)
    {
        var operation = _operation;
        if (operation is null || operation.Finished)
            return;
        operation.Finished = true;
        operation.Cancellation.Dispose();
        if (_cameraStarted)
        {
            _cameraStarted = false;
            _tree.ProcessFrame -= ProcessFrame;
            operation.Native.StopCamera();
        }

        _latestFrame?.Dispose();
        _latestFrame = null;

        if (_view is not null)
        {
            _view.CancelRequested -= CancelScan;
            _view.ApplicationPausedChanged -= OnApplicationPausedChanged;
            _view.Hide();
            _view.QueueFree();
            _view = null;
            if (GodotObject.IsInstanceValid(_kirieNode))
                _kirieNode.Visible = _wasVisible;
            if (GodotObject.IsInstanceValid(_tree))
                _tree.QuitOnGoBack = _wasQuitOnGoBack;
        }

        ReleaseIfDrained();
        if (error is not null)
            operation.Completion.TrySetException(error);
        else if (cancellationToken.IsCancellationRequested)
            operation.Completion.TrySetCanceled(cancellationToken);
        else
            operation.Completion.TrySetResult(text);
    }

    private void ReleaseIfDrained()
    {
        if (_operation is not { Finished: true } operation || _recognitionPending || _permissionPending)
            return;
        _operation = null;
        operation.Native.Dispose();
        if (ReferenceEquals(_recognitionOwner, this))
            _recognitionOwner = null;
    }

    private static Color ParseColor(string value)
    {
        if (!Color.HtmlIsValid(value))
            throw new ArgumentException("QR colors must use an HTML hexadecimal color value.", nameof(value));
        return Color.FromHtml(value);
    }

    private void RequireReady()
    {
        RequireMainThread();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void RequireMainThread()
    {
        if (!GodotThread.IsMainThread())
            throw new InvalidOperationException("QR operations must run on Godot's main thread.");
    }

    /// <summary>Stops the current scan and removes Eventa handlers. The borrowed scene remains owned by the application.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        RequireMainThread();
        _disposed = true;
        _owner.TreeExiting -= Dispose;
        _kirieNode.TreeExiting -= Dispose;
        Finish(null);
        foreach (var registration in _registrations)
            registration.Dispose();
    }
}
