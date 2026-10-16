using Godot;

namespace GdKirie.Tray;

/// <summary>A normal upstream menu item.</summary>
public sealed class MenuItem : EnabledMenuEntry
{
    /// <summary>Creates a native MenuItem with the supplied options.</summary>
    public MenuItem(MenuItemOptions options) : base(Create("KirieTrayItem", "initialize_item", NativeValues.Item(options)), "item") { }
    internal MenuItem(RefCounted native) : base(native, "item") { }
    /// <summary>Sets a keyboard accelerator, or clears it with null.</summary>
    public void SetAccelerator(Accelerator? accelerator) => Set("set_accelerator", NativeValues.Accelerator(accelerator));
}

/// <summary>An upstream check menu item.</summary>
public sealed class CheckMenuItem : EnabledMenuEntry
{
    /// <summary>Creates a native CheckMenuItem with the supplied options.</summary>
    public CheckMenuItem(CheckMenuItemOptions options) : base(Create("KirieTrayItem", "initialize_check", NativeValues.Check(options)), "check") { }
    internal CheckMenuItem(RefCounted native) : base(native, "check") { }

    /// <summary>The current upstream checked state.</summary>
    public bool Checked
    {
        get
        {
            using var result = Call("is_checked");
            return result.AsBool();
        }
        set => Set("set_checked", value);
    }

    /// <summary>Sets a keyboard accelerator, or clears it with null.</summary>
    public void SetAccelerator(Accelerator? accelerator) => Set("set_accelerator", NativeValues.Accelerator(accelerator));
}

/// <summary>An upstream menu item with an icon.</summary>
public sealed class IconMenuItem : EnabledMenuEntry
{
    /// <summary>Creates a native IconMenuItem with the supplied options.</summary>
    public IconMenuItem(IconMenuItemOptions options) : base(Create("KirieTrayItem", "initialize_icon", NativeValues.IconItem(options)), "icon") { }
    internal IconMenuItem(RefCounted native) : base(native, "icon") { }
    /// <summary>Sets a keyboard accelerator, or clears it with null.</summary>
    public void SetAccelerator(Accelerator? accelerator) => Set("set_accelerator", NativeValues.Accelerator(accelerator));
    /// <summary>Sets or clears the icon. Platform support follows upstream.</summary>
    public void SetIcon(IconSource? icon, bool iconAsTemplate = false) => Set("set_icon", NativeValues.NullableIcon(icon), iconAsTemplate);

    /// <summary>Reads the macOS template icon state.</summary>
    public bool IconIsTemplate()
    {
        using var result = Call("icon_is_template");
        return result.AsBool();
    }
}

/// <summary>A menu item with behavior supplied by upstream and the operating system.</summary>
public sealed class PredefinedMenuItem : MenuEntry
{
    /// <summary>Creates a native PredefinedMenuItem with the supplied options.</summary>
    public PredefinedMenuItem(PredefinedMenuItemOptions options) : base(Create("KirieTrayItem", "initialize_predefined", NativeValues.Predefined(options)), "predefined") { }
    internal PredefinedMenuItem(RefCounted native) : base(native, "predefined") { }
}
