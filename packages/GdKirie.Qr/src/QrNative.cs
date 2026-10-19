using Godot;
using Dictionary = Godot.Collections.Dictionary;

namespace GdKirie.Qr;

internal sealed class QrNative : IDisposable
{
    private readonly GodotObject _qr;
    private readonly GodotObject? _camera;
    private readonly Callable _detected;
    private readonly Callable _failed;
    private readonly Callable _permissionGranted;
    private readonly Callable _permissionDenied;
    private readonly Callable _frame;

    internal QrNative(Action<string> detected, Action<Dictionary> failed,
        Action granted, Action denied, Action<Dictionary> frame, bool camera)
    {
        _qr = GetSingleton("QRPlugin");
        _camera = camera ? GetSingleton("NativeCameraPlugin") : null;
        _detected = Callable.From(detected);
        _failed = Callable.From(failed);
        _permissionGranted = Callable.From(granted);
        _permissionDenied = Callable.From(denied);
        _frame = Callable.From(frame);
        // NativeCamera emits Android frames on the UI thread, rather than the Godot thread.
        // Deferred connections also preserve signal order for synchronous iOS QR results.
        _qr.Connect("qr_detected", _detected, (uint)GodotObject.ConnectFlags.Deferred);
        _qr.Connect("qr_scan_failed", _failed, (uint)GodotObject.ConnectFlags.Deferred);
        _camera?.Connect("camera_permission_granted", _permissionGranted, (uint)GodotObject.ConnectFlags.Deferred);
        _camera?.Connect("camera_permission_denied", _permissionDenied, (uint)GodotObject.ConnectFlags.Deferred);
        _camera?.Connect("frame_available", _frame, (uint)GodotObject.ConnectFlags.Deferred);
    }

    internal static Image GenerateImage(string text, int size, Color foreground, Color background)
    {
        using var data = GetSingleton("QRPlugin")
            .Call("generate_qr", text, size, unchecked((int)foreground.ToArgb32()), unchecked((int)background.ToArgb32()))
            .AsGodotDictionary();
        return ReadImage(data, false);
    }

    internal void Scan(Image source)
    {
        using var image = new Image();
        image.CopyFrom(source);
        if (image.GetFormat() != Image.Format.Rgba8)
            image.Convert(Image.Format.Rgba8);
        if (image.HasMipmaps())
            image.ClearMipmaps();
        using var request = new Dictionary
        {
            ["width"] = image.GetWidth(),
            ["height"] = image.GetHeight(),
            ["buffer"] = image.GetData(),
            ["format"] = (int)Image.Format.Rgba8,
            ["has_mipmaps"] = false,
        };
        _qr.Call("scan_qr", request);
    }

    internal bool HasPermission() => _camera!.Call("has_camera_permission").AsBool();

    internal void RequestPermission() => _camera!.Call("request_camera_permission");

    internal void StartCamera()
    {
        using var cameras = _camera!.Call("get_all_cameras").AsGodotArray();
        if (cameras.Count == 0)
            throw new InvalidOperationException("NativeCamera did not report an available camera.");
        using var firstCamera = cameras[0].AsGodotDictionary();
        var cameraId = firstCamera["camera_id"].AsString();
        foreach (var entry in cameras)
        {
            using var candidate = entry.AsGodotDictionary();
            if (!candidate["is_front_facing"].AsBool())
            {
                cameraId = candidate["camera_id"].AsString();
                break;
            }
        }

        using var request = new Dictionary
        {
            ["camera_id"] = cameraId,
            ["width"] = 1280,
            ["height"] = 720,
            ["frames_to_skip"] = 0,
            ["is_grayscale"] = true,
            ["auto_upright"] = true,
        };
        _camera.Call("start", request);
    }

    internal void StopCamera() => _camera!.Call("stop");

    internal static Image ReadFrame(Dictionary frame) => ReadImage(frame, frame["is_grayscale"].AsBool());

    private static Image ReadImage(Dictionary data, bool grayscale)
    {
        if (!data.ContainsKey("width") || !data.ContainsKey("height") || !data.ContainsKey("buffer"))
            throw new InvalidOperationException("The native plugin returned incomplete image data.");
        var width = data["width"].AsInt32();
        var height = data["height"].AsInt32();
        var buffer = data["buffer"].AsByteArray();
        if (width <= 0 || height <= 0 || (long)width * height * (grayscale ? 1 : 4) != buffer.LongLength)
            throw new InvalidOperationException("The native plugin returned invalid image dimensions or pixels.");
        var image = Image.CreateFromData(width, height, false, grayscale ? Image.Format.L8 : Image.Format.Rgba8, buffer);
        if (grayscale)
            image.Convert(Image.Format.Rgba8);
        return image;
    }

    private static GodotObject GetSingleton(string name)
    {
        if (!Engine.HasSingleton(name))
            throw new PlatformNotSupportedException($"The {name} native plugin is unavailable. Export for Android or iOS with @gd-kirie/qr enabled.");
        return Engine.GetSingleton(name);
    }

    public void Dispose()
    {
        _qr.Disconnect("qr_detected", _detected);
        _qr.Disconnect("qr_scan_failed", _failed);
        _camera?.Disconnect("camera_permission_granted", _permissionGranted);
        _camera?.Disconnect("camera_permission_denied", _permissionDenied);
        _camera?.Disconnect("frame_available", _frame);
    }
}
