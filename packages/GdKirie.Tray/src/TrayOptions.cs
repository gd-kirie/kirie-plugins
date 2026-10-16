namespace GdKirie.Tray;

/// <summary>An icon source accepted by the native tray and menu implementations.</summary>
public abstract record IconSource
{
    /// <summary>A Godot resource containing a texture.</summary>
    public sealed record Resource(string Path) : IconSource;
    /// <summary>RGBA pixels in row-major order.</summary>
    public sealed record Rgba(byte[] Pixels, uint Width, uint Height) : IconSource;
    /// <summary>An upstream named native icon.</summary>
    public sealed record Native(string Name) : IconSource;
    /// <summary>A macOS system image name.</summary>
    public sealed record NativeName(string Name) : IconSource;
    /// <summary>A Windows stock icon identifier.</summary>
    public sealed record WindowsStock(int Id) : IconSource;
    /// <summary>An icon from a Windows file.</summary>
    public sealed record WindowsPath(string Path, uint? Width = null, uint? Height = null) : IconSource;
    /// <summary>An ordinal icon resource in the Windows executable.</summary>
    public sealed record WindowsResource(ushort Ordinal, uint? Width = null, uint? Height = null) : IconSource;
    /// <summary>A named icon resource in the Windows executable.</summary>
    public sealed record WindowsResourceName(string Name, uint? Width = null, uint? Height = null) : IconSource;
}

/// <summary>A physical or logical menu keyboard accelerator in upstream string syntax.</summary>
public abstract record Accelerator(string Value)
{
    /// <summary>An accelerator referring to a physical key.</summary>
    public sealed record Physical(string Value) : Accelerator(Value);
    /// <summary>An accelerator referring to a logical key.</summary>
    public sealed record Logical(string Value) : Accelerator(Value);
}

/// <summary>Options passed once to the upstream TrayIcon builder.</summary>
public sealed record TrayOptions
{
    /// <summary>An optional upstream object ID.</summary>
    public string? Id { get; init; }
    /// <summary>An optional icon source.</summary>
    public IconSource? Icon { get; init; }
    /// <summary>Whether macOS treats the icon as a template.</summary>
    public bool IconAsTemplate { get; init; }
    /// <summary>The menu or submenu attached to the tray.</summary>
    public IMenuContainer? Menu { get; init; }
    /// <summary>An optional tooltip.</summary>
    public string? Tooltip { get; init; }
    /// <summary>An optional tray title.</summary>
    public string? Title { get; init; }
    /// <summary>Whether a left click opens the menu. Null preserves the upstream default.</summary>
    public bool? MenuOnLeftClick { get; init; }
    /// <summary>Whether a right click opens the menu. Null preserves the upstream default.</summary>
    public bool? MenuOnRightClick { get; init; }
    /// <summary>An optional Windows tray GUID.</summary>
    public string? Guid { get; init; }
    /// <summary>An optional macOS status item autosave name.</summary>
    public string? AutosaveName { get; init; }
    /// <summary>An optional Linux icon directory.</summary>
    public string? TempDirPath { get; init; }
}

/// <summary>Options for a native menu.</summary>
public sealed record MenuOptions
{
    /// <summary>An optional upstream object ID.</summary>
    public string? Id { get; init; }
    /// <summary>The initial children in menu order.</summary>
    public IReadOnlyList<MenuEntry> Items { get; init; } = [];
}

/// <summary>Options for a native submenu.</summary>
public sealed record SubmenuOptions(string Text)
{
    /// <summary>An optional upstream object ID.</summary>
    public string? Id { get; init; }
    /// <summary>The initial enabled state.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>The initial children in menu order.</summary>
    public IReadOnlyList<MenuEntry> Items { get; init; } = [];
    /// <summary>An optional icon source.</summary>
    public IconSource? Icon { get; init; }
    /// <summary>Whether macOS treats the icon as a template.</summary>
    public bool IconAsTemplate { get; init; }
}

/// <summary>Options for a normal native menu item.</summary>
public sealed record MenuItemOptions(string Text)
{
    /// <summary>An optional upstream object ID.</summary>
    public string? Id { get; init; }
    /// <summary>The initial enabled state.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>An optional physical or logical keyboard accelerator.</summary>
    public Accelerator? Accelerator { get; init; }
}

/// <summary>Options for a native check menu item.</summary>
public sealed record CheckMenuItemOptions(string Text)
{
    /// <summary>An optional upstream object ID.</summary>
    public string? Id { get; init; }
    /// <summary>The initial enabled state.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>The initial checked state.</summary>
    public bool Checked { get; init; }
    /// <summary>An optional physical or logical keyboard accelerator.</summary>
    public Accelerator? Accelerator { get; init; }
}

/// <summary>Options for a native icon menu item.</summary>
public sealed record IconMenuItemOptions(string Text)
{
    /// <summary>An optional upstream object ID.</summary>
    public string? Id { get; init; }
    /// <summary>The initial enabled state.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>An optional physical or logical keyboard accelerator.</summary>
    public Accelerator? Accelerator { get; init; }
    /// <summary>An optional icon source.</summary>
    public IconSource? Icon { get; init; }
    /// <summary>Whether macOS treats the icon as a template.</summary>
    public bool IconAsTemplate { get; init; }
}

/// <summary>Metadata passed to the upstream About menu item.</summary>
public sealed record AboutMetadata
{
    /// <summary>The application name.</summary>
    public string? Name { get; init; }
    /// <summary>The application version.</summary>
    public string? Version { get; init; }
    /// <summary>The short application version.</summary>
    public string? ShortVersion { get; init; }
    /// <summary>The application authors.</summary>
    public IReadOnlyList<string>? Authors { get; init; }
    /// <summary>Comments about the application.</summary>
    public string? Comments { get; init; }
    /// <summary>The application copyright.</summary>
    public string? Copyright { get; init; }
    /// <summary>The application license.</summary>
    public string? License { get; init; }
    /// <summary>The application website.</summary>
    public string? Website { get; init; }
    /// <summary>The label for the application website.</summary>
    public string? WebsiteLabel { get; init; }
    /// <summary>The application credits.</summary>
    public string? Credits { get; init; }
    /// <summary>An optional icon source.</summary>
    public IconSource? Icon { get; init; }
}

/// <summary>Upstream predefined menu item variants.</summary>
public enum PredefinedMenuItemKind
{
    /// <summary>The upstream Separator menu action.</summary>
    Separator,
    /// <summary>The upstream SectionHeader menu action.</summary>
    SectionHeader,
    /// <summary>The upstream About menu action.</summary>
    About,
    /// <summary>The upstream Copy menu action.</summary>
    Copy,
    /// <summary>The upstream Cut menu action.</summary>
    Cut,
    /// <summary>The upstream Paste menu action.</summary>
    Paste,
    /// <summary>The upstream PasteAndMatchStyle menu action.</summary>
    PasteAndMatchStyle,
    /// <summary>The upstream Delete menu action.</summary>
    Delete,
    /// <summary>The upstream SelectAll menu action.</summary>
    SelectAll,
    /// <summary>The upstream Undo menu action.</summary>
    Undo,
    /// <summary>The upstream Redo menu action.</summary>
    Redo,
    /// <summary>The upstream Minimize menu action.</summary>
    Minimize,
    /// <summary>The upstream Maximize menu action.</summary>
    Maximize,
    /// <summary>The upstream Zoom menu action.</summary>
    Zoom,
    /// <summary>The upstream ActualSize menu action.</summary>
    ActualSize,
    /// <summary>The upstream ZoomIn menu action.</summary>
    ZoomIn,
    /// <summary>The upstream ZoomOut menu action.</summary>
    ZoomOut,
    /// <summary>The upstream Fullscreen menu action.</summary>
    Fullscreen,
    /// <summary>The upstream Hide menu action.</summary>
    Hide,
    /// <summary>The upstream HideOthers menu action.</summary>
    HideOthers,
    /// <summary>The upstream ShowAll menu action.</summary>
    ShowAll,
    /// <summary>The upstream CloseWindow menu action.</summary>
    CloseWindow,
    /// <summary>The upstream Quit menu action.</summary>
    Quit,
    /// <summary>The upstream Services menu action.</summary>
    Services,
    /// <summary>The upstream BringAllToFront menu action.</summary>
    BringAllToFront,
    /// <summary>The upstream StartSpeaking menu action.</summary>
    StartSpeaking,
    /// <summary>The upstream StopSpeaking menu action.</summary>
    StopSpeaking,
    /// <summary>The upstream StartDictation menu action.</summary>
    StartDictation,
    /// <summary>The upstream EmojiAndSymbols menu action.</summary>
    EmojiAndSymbols,
}

/// <summary>Options for an upstream predefined menu item.</summary>
public sealed record PredefinedMenuItemOptions(PredefinedMenuItemKind Kind)
{
    /// <summary>An optional predefined item text.</summary>
    public string? Text { get; init; }
    /// <summary>Metadata for the predefined About item.</summary>
    public AboutMetadata? Metadata { get; init; }
}
