using Godot;

namespace GdKirie.Tray;

/// <summary>A native tray icon, independent of any browser transport.</summary>
public sealed class TrayIcon : TrayObject
{
    /// <summary>Creates a native TrayIcon with the supplied options.</summary>
    public TrayIcon(TrayOptions? options = null) : base(Create("KirieTrayIcon", "create", NativeValues.Tray(options ?? new())), "tray") { }
    internal TrayIcon(RefCounted native) : base(native, "tray") { }

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
        return TrayIconEvent.ReadRect(dictionary);
    }

    /// <summary>Reads the macOS template icon state.</summary>
    public bool IconIsTemplate()
    {
        using var result = Call("icon_is_template");
        return result.AsBool();
    }
}
