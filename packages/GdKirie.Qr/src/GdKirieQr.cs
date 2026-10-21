using Eventa;
using GdKirie.EventaAdapter;
using Godot;

namespace GdKirie.Qr;

/// <summary>Registers and attaches QR operations to an existing Kirie application.</summary>
public static class GdKirieQr
{
    /// <summary>Registers the QR wire contracts with the application's JSON registry.</summary>
    public static KirieEventaJsonRegistry Register(KirieEventaJsonRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry
            .RegisterInvoke(QrEvents.GenerateImage, QrJsonContext.Default.PngPayload,
                QrJsonContext.Default.GenerateImageRequest)
            .RegisterInvoke(QrEvents.ScanImage, QrJsonContext.Default.ScanResult,
                QrJsonContext.Default.PngPayload)
            .RegisterInvoke(QrEvents.Scan, QrJsonContext.Default.ScanResult,
                QrJsonContext.Default.EmptyPayload);
    }

    /// <summary>Attaches QR operations and borrows the scene owner and its KirieNode control.</summary>
    public static GdKirieQrHost Attach(IEventContext context, Node owner, Control kirieNode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(kirieNode);
        if (!GodotThread.IsMainThread())
            throw new InvalidOperationException("GdKirieQr.Attach must run on Godot's main thread.");
        if (!owner.IsInsideTree() || !kirieNode.IsInsideTree())
            throw new ArgumentException("The owner and KirieNode must be inside the scene tree.");
        return new GdKirieQrHost(context, owner, kirieNode);
    }
}
