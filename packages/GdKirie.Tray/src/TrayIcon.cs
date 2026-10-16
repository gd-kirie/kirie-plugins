using Godot;
using Godot.Collections;

namespace GdKirie.Tray;

/// <summary>A native tray icon, independent of any browser transport.</summary>
public sealed class TrayIcon : TrayObject
{
    private Callable _event;
    private EventHandler<TrayIconEvent>? _handlers;

    /// <summary>Creates a native TrayIcon with the supplied options.</summary>
    public TrayIcon(TrayOptions? options = null) : base(Create("KirieTrayIcon", "initialize", NativeValues.Tray(options ?? new())), "tray") { }
    internal TrayIcon(RefCounted native) : base(native, "tray") { }

    /// <summary>Raised for each native tray mouse event.</summary>
    public event EventHandler<TrayIconEvent>? Event
    {
        add
        {
            var native = Native;
            if (value is null)
            {
                return;
            }

            if (_handlers is null)
            {
                _event = Callable.From<Dictionary>(OnEvent);
                using var name = new StringName("tray_event");
                var error = native.Connect(name, _event);
                if (error != Error.Ok)
                {
                    throw new InvalidOperationException($"Cannot connect the native tray_event signal: {error}");
                }
            }

            _handlers += value;
        }
        remove
        {
            EnsureMainThread();
            if (_handlers is null)
            {
                return;
            }

            _handlers -= value;
            if (_handlers is null)
            {
                using var name = new StringName("tray_event");
                Native.Disconnect(name, _event);
                _event = default;
            }
        }
    }

    /// <summary>Sets or clears the icon. Platform support follows upstream.</summary>
    public void SetIcon(IconSource? icon, bool iconAsTemplate = false) => Set("set_icon", NativeValues.NullableIcon(icon), iconAsTemplate);
    /// <summary>Attaches a menu or submenu, or removes the menu with null.</summary>
    public void SetMenu(IMenuContainer? menu) => Set("set_menu", menu is null ? default(Variant) : ((TrayObject)menu).Native);
    /// <summary>Sets the tooltip, or clears it with null.</summary>
    public void SetTooltip(string? tooltip) => Set("set_tooltip", tooltip is null ? default(Variant) : tooltip);
    /// <summary>Sets the tray title, or clears it with null.</summary>
    public void SetTitle(string? title) => Set("set_title", title is null ? default(Variant) : title);
    /// <summary>Changes native tray visibility.</summary>
    public void SetVisible(bool visible) => Set("set_visible", visible);
    /// <summary>Changes whether a left click opens the native menu.</summary>
    public void SetMenuOnLeftClick(bool enabled) => Set("set_menu_on_left_click", enabled);
    /// <summary>Changes whether a right click opens the native menu.</summary>
    public void SetMenuOnRightClick(bool enabled) => Set("set_menu_on_right_click", enabled);
    /// <summary>Opens the native tray menu.</summary>
    public void ShowMenu() => Set("show_menu");
    /// <summary>Sets the Linux icon directory, or restores the default with null.</summary>
    public void SetTempDirPath(string? path) => Set("set_temp_dir_path", path is null ? default(Variant) : path);

    /// <summary>Reads the native tray rectangle, or returns null if unavailable.</summary>
    public TrayRect? Rect()
    {
        using var result = Call("rect");
        if (result.VariantType == Variant.Type.Nil)
        {
            return null;
        }

        using var dictionary = result.AsGodotDictionary();
        return ReadRect(dictionary);
    }

    /// <summary>Reads the macOS template icon state.</summary>
    public bool IconIsTemplate()
    {
        using var result = Call("icon_is_template");
        return result.AsBool();
    }

    private void OnEvent(Dictionary value)
    {
        var type = Enum.Parse<TrayIconEventKind>(value["type"].AsString().Replace("-", ""), ignoreCase: true);
        using var position = value["position"].AsGodotDictionary();
        using var rect = value["rect"].AsGodotDictionary();
        TrayMouseButton? button = value.TryGetValue("button", out var buttonValue)
            ? Enum.Parse<TrayMouseButton>(buttonValue.AsString(), ignoreCase: true)
            : null;
        TrayMouseButtonState? state = value.TryGetValue("buttonState", out var stateValue)
            ? Enum.Parse<TrayMouseButtonState>(stateValue.AsString(), ignoreCase: true)
            : null;
        _handlers?.Invoke(this, new TrayIconEvent(type, ReadPosition(position), ReadRect(rect), button, state));
    }

    private static TrayPosition ReadPosition(Dictionary value) => new(value["x"].AsDouble(), value["y"].AsDouble());

    private static TrayRect ReadRect(Dictionary value)
    {
        using var position = value["position"].AsGodotDictionary();
        using var size = value["size"].AsGodotDictionary();
        return new TrayRect(ReadPosition(position), size["width"].AsUInt32(), size["height"].AsUInt32());
    }

    /// <summary>Disconnects the native callbacks before this reference is released.</summary>
    protected override void DisconnectSignals()
    {
        if (_handlers is not null)
        {
            using var name = new StringName("tray_event");
            Native.Disconnect(name, _event);
            _handlers = null;
            _event = default;
        }
    }
}
