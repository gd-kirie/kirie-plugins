using Godot;

namespace GdKirie.Tray;

/// <summary>A normal upstream menu item.</summary>
public sealed class MenuItem : EnabledMenuEntry
{
    /// <summary>Creates a native MenuItem with the supplied options.</summary>
    public MenuItem(MenuItemOptions options) : base(Create("KirieTrayItem", "create_item", NativeValues.Item(options)), "item") { }
    internal MenuItem(RefCounted native) : base(native, "item") { }
    /// <summary>Sets a keyboard accelerator, or clears it with null.</summary>
    public void SetAccelerator(Accelerator? accelerator) => Set("set_accelerator", NativeValues.Accelerator(accelerator));
}

/// <summary>An upstream check menu item.</summary>
public sealed class CheckMenuItem : EnabledMenuEntry
{
    /// <summary>Creates a native CheckMenuItem with the supplied options.</summary>
    public CheckMenuItem(CheckMenuItemOptions options) : base(Create("KirieTrayItem", "create_check", NativeValues.Check(options)), "check") { }
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
    public IconMenuItem(IconMenuItemOptions options) : base(Create("KirieTrayItem", "create_icon", NativeValues.IconItem(options)), "icon") { }
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
    internal PredefinedMenuItem(RefCounted native) : base(native, "predefined") { }

    /// <summary>Creates an upstream separator.</summary>
    public static PredefinedMenuItem Separator() => new(Create("KirieTrayItem", "separator"));

    /// <summary>Creates an upstream section header with the supplied text.</summary>
    public static PredefinedMenuItem SectionHeader(string text) => new(Create("KirieTrayItem", "section_header", text));

    /// <summary>Calls the upstream copy constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Copy(string? text = null) =>
        new(Create("KirieTrayItem", "copy", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream cut constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Cut(string? text = null) =>
        new(Create("KirieTrayItem", "cut", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream paste constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Paste(string? text = null) =>
        new(Create("KirieTrayItem", "paste", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream paste_and_match_style constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem PasteAndMatchStyle(string? text = null) =>
        new(Create("KirieTrayItem", "paste_and_match_style", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream delete constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Delete(string? text = null) =>
        new(Create("KirieTrayItem", "delete", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream select_all constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem SelectAll(string? text = null) =>
        new(Create("KirieTrayItem", "select_all", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream undo constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Undo(string? text = null) =>
        new(Create("KirieTrayItem", "undo", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream redo constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Redo(string? text = null) =>
        new(Create("KirieTrayItem", "redo", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream minimize constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Minimize(string? text = null) =>
        new(Create("KirieTrayItem", "minimize", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream maximize constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Maximize(string? text = null) =>
        new(Create("KirieTrayItem", "maximize", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream zoom constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Zoom(string? text = null) =>
        new(Create("KirieTrayItem", "zoom", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream actual_size constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem ActualSize(string? text = null) =>
        new(Create("KirieTrayItem", "actual_size", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream zoom_in constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem ZoomIn(string? text = null) =>
        new(Create("KirieTrayItem", "zoom_in", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream zoom_out constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem ZoomOut(string? text = null) =>
        new(Create("KirieTrayItem", "zoom_out", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream fullscreen constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Fullscreen(string? text = null) =>
        new(Create("KirieTrayItem", "fullscreen", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream hide constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Hide(string? text = null) =>
        new(Create("KirieTrayItem", "hide", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream hide_others constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem HideOthers(string? text = null) =>
        new(Create("KirieTrayItem", "hide_others", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream show_all constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem ShowAll(string? text = null) =>
        new(Create("KirieTrayItem", "show_all", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream close_window constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem CloseWindow(string? text = null) =>
        new(Create("KirieTrayItem", "close_window", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream quit constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Quit(string? text = null) =>
        new(Create("KirieTrayItem", "quit", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream services constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem Services(string? text = null) =>
        new(Create("KirieTrayItem", "services", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream bring_all_to_front constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem BringAllToFront(string? text = null) =>
        new(Create("KirieTrayItem", "bring_all_to_front", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream start_speaking constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem StartSpeaking(string? text = null) =>
        new(Create("KirieTrayItem", "start_speaking", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream stop_speaking constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem StopSpeaking(string? text = null) =>
        new(Create("KirieTrayItem", "stop_speaking", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream start_dictation constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem StartDictation(string? text = null) =>
        new(Create("KirieTrayItem", "start_dictation", text is null ? default(Variant) : text));

    /// <summary>Calls the upstream emoji_and_symbols constructor. Null keeps the upstream text.</summary>
    public static PredefinedMenuItem EmojiAndSymbols(string? text = null) =>
        new(Create("KirieTrayItem", "emoji_and_symbols", text is null ? default(Variant) : text));

    /// <summary>Creates an upstream About item with optional text and metadata.</summary>
    public static PredefinedMenuItem About(string? text = null, AboutMetadata? metadata = null) =>
        new(Create("KirieTrayItem", "about", text is null ? default(Variant) : text,
            metadata is null ? default : NativeValues.Metadata(metadata)));
}
