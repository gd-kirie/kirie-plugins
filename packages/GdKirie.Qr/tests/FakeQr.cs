using Godot;
using Dictionary = Godot.Collections.Dictionary;

public sealed partial class FakeQr : Node
{
    [Signal] public delegate void qr_detectedEventHandler(string text);
    [Signal] public delegate void qr_scan_failedEventHandler(Dictionary error);

    public int ScanCalls { get; private set; }

    public Dictionary generate_qr(string text, int size, int foreground, int background)
    {
        using var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(new Color(((foreground >> 16) & 255) / 255f,
            ((foreground >> 8) & 255) / 255f, (foreground & 255) / 255f,
            (unchecked((uint)foreground) >> 24) / 255f));
        return new Dictionary { ["width"] = size, ["height"] = size, ["buffer"] = image.GetData() };
    }

    public void scan_qr(Dictionary image)
    {
        using var pixels = image["buffer"];
        if (image["format"].AsInt32() != (int)Image.Format.Rgba8
            || image["has_mipmaps"].AsBool()
            || pixels.AsByteArray().Length != image["width"].AsInt32() * image["height"].AsInt32() * 4)
            throw new InvalidOperationException("The native QR scanner requires RGBA8 pixels without mipmaps.");

        ScanCalls++;
    }

    public void Detect(string text) => EmitSignal(SignalName.qr_detected, text);

    public void Miss() => EmitSignal(SignalName.qr_scan_failed,
        new Dictionary { ["code"] = 2, ["description"] = "No QR code detected" });

    public void Fail() => EmitSignal(SignalName.qr_scan_failed,
        new Dictionary { ["code"] = 3, ["description"] = "Recognition failed" });
}
