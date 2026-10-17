using Eventa;
using GdKirie.EventaAdapter;

namespace GdKirie.Tray.EventaAdapter;

/// <summary>Connects native tray objects to an application's existing Eventa context.</summary>
public static class GdKirieTrayEventaAdapter
{
    /// <summary>Registers the tray adapter's method and event JSON contracts.</summary>
    public static KirieEventaJsonRegistry Register(KirieEventaJsonRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        foreach (var method in TrayEvents.Methods.Values)
        {
            registry.RegisterInvoke(method, TrayJsonContext.Default.JsonElement, TrayJsonContext.Default.JsonElement);
        }

        return registry.RegisterEvent(TrayEvents.Event, TrayJsonContext.Default.JsonElement);
    }

    /// <summary>Attaches browser methods while borrowing the application's context.</summary>
    public static TrayEventaAdapter Attach(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        TrayObject.EnsureMainThread();
        return new TrayEventaAdapter(context);
    }
}
