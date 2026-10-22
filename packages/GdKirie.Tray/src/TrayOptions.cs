namespace GdKirie.Tray;

/// <summary>An icon source accepted by the native tray and menu implementations.</summary>
public abstract record IconSource
{
    /// <summary>A Godot resource containing a texture.</summary>
    /// <param name="Path">The texture resource path.</param>
    /// <param name="Scale">Texture pixels per point. On macOS, a 32 pixel tray icon with scale 2 is drawn at 16 points. Windows and Linux ignore it. Menu icons require 1.</param>
    public sealed record Resource(string Path, double Scale = 1) : IconSource;
    /// <summary>RGBA pixels in row-major order.</summary>
    /// <param name="Pixels">The RGBA bytes.</param>
    /// <param name="Width">The width in pixels.</param>
    /// <param name="Height">The height in pixels.</param>
    /// <param name="Scale">Pixels per point. On macOS, a 32 pixel tray icon with scale 2 is drawn at 16 points. Windows and Linux ignore it. Menu icons require 1.</param>
    public sealed record Rgba(byte[] Pixels, uint Width, uint Height, double Scale = 1) : IconSource;
    /// <summary>A public upstream NativeIcon variant name, such as Folder or StatusAvailable.</summary>
    public sealed record Native(string Name) : IconSource;
    /// <summary>A macOS system image name or a Linux theme icon name.</summary>
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
