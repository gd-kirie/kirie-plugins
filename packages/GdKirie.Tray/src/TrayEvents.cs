using Godot;
using Godot.Collections;

namespace GdKirie.Tray;

/// <summary>A subscription to global upstream menu and tray events on Godot's main thread.</summary>
public sealed class TrayEvents : IDisposable
{
    private readonly GodotObject _native;
    private readonly Callable _menu;
    private readonly Callable _tray;
    private bool _disposed;

    /// <summary>Connects to the KirieTrayEvents Godot singleton.</summary>
    public TrayEvents()
    {
        TrayObject.EnsureMainThread();
        _native = Engine.GetSingleton("KirieTrayEvents")
            ?? throw new InvalidOperationException("The Kirie Tray GDExtension is not loaded.");
        _menu = Callable.From<string>(id => Menu?.Invoke(id));
        _tray = Callable.From<Dictionary>(value => Tray?.Invoke(TrayIconEvent.Read(value)));
        try
        {
            Connect("menu_event", _menu);
            Connect("tray_event", _tray);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Reports the upstream menu item ID.</summary>
    public event Action<string>? Menu;
    /// <summary>Reports the upstream tray ID and mouse event data.</summary>
    public event Action<TrayIconEvent>? Tray;

    private void Connect(string signal, Callable callback)
    {
        using var name = new StringName(signal);
        var error = _native.Connect(name, callback);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Cannot connect the native {signal} signal: {error}");
        }
    }

    /// <summary>Disconnects this subscription without changing native tray or menu objects.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        TrayObject.EnsureMainThread();
        _disposed = true;
        using var menu = new StringName("menu_event");
        using var tray = new StringName("tray_event");
        if (_native.IsConnected(menu, _menu))
        {
            _native.Disconnect(menu, _menu);
        }
        if (_native.IsConnected(tray, _tray))
        {
            _native.Disconnect(tray, _tray);
        }
        Menu = null;
        Tray = null;
    }
}
