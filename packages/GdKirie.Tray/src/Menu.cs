using Godot;

namespace GdKirie.Tray;

/// <summary>A native menu container.</summary>
public interface IMenuContainer
{
    /// <summary>New references to the current native children in menu order. Dispose each returned reference.</summary>
    IReadOnlyList<MenuEntry> Items { get; }
    /// <summary>Appends these entries in order.</summary>
    void Append(params MenuEntry[] items);
    /// <summary>Prepends these entries in order.</summary>
    void Prepend(params MenuEntry[] items);
    /// <summary>Inserts these entries at a zero-based position.</summary>
    void Insert(int position, params MenuEntry[] items);
    /// <summary>Removes an entry from this container without releasing other references.</summary>
    void Remove(MenuEntry item);
    /// <summary>Removes the entry at a zero-based position and returns a new reference to dispose, or null if absent.</summary>
    MenuEntry? RemoveAt(int position);
}

/// <summary>A native menu, independent of any tray or browser transport.</summary>
/// <remarks>Creates a menu with optional initial children.</remarks>
public sealed class Menu : TrayObject, IMenuContainer
{
    /// <summary>Creates a menu with optional initial children.</summary>
    public Menu(MenuOptions? options = null) : base(Create("KirieTrayMenu", "create", NativeValues.Menu(options ?? new())), "menu") { }
    internal Menu(RefCounted native) : base(native, "menu") { }

    /// <summary>New references to the current native children in menu order. Dispose each returned reference.</summary>
    public IReadOnlyList<MenuEntry> Items => GetItems();
    /// <summary>Appends these entries in order.</summary>
    public void Append(params MenuEntry[] items) => SetItems("append", items);
    /// <summary>Prepends these entries in order.</summary>
    public void Prepend(params MenuEntry[] items) => SetItems("prepend", items);
    /// <summary>Inserts these entries at a zero-based position.</summary>
    public void Insert(int position, params MenuEntry[] items) => SetItems("insert", items, position);
    /// <summary>Removes an entry from this container without releasing other references.</summary>
    public void Remove(MenuEntry item) => Set("remove", item.Native);
    /// <summary>Removes the entry at a zero-based position and returns a new reference to dispose, or null if absent.</summary>
    public MenuEntry? RemoveAt(int position) => RemoveItemAt(position);
}

/// <summary>A native menu entry with text.</summary>
public abstract class MenuEntry : TrayObject
{
    internal MenuEntry(RefCounted native, string kind) : base(native, kind) { }

    /// <summary>The current upstream item text.</summary>
    public string Text
    {
        get => Call<string>("text");
        set => Set("set_text", value);
    }
}

/// <summary>A menu entry with upstream enabled state.</summary>
public abstract class EnabledMenuEntry : MenuEntry
{
    internal EnabledMenuEntry(RefCounted native, string kind) : base(native, kind) { }

    /// <summary>The current upstream enabled state.</summary>
    public bool Enabled
    {
        get => Call<bool>("is_enabled");
        set => Set("set_enabled", value);
    }
}

/// <summary>A native submenu that is also a menu entry.</summary>
public sealed class Submenu : EnabledMenuEntry, IMenuContainer
{
    /// <summary>Creates a native Submenu with the supplied options.</summary>
    public Submenu(SubmenuOptions options) : base(Create("KirieTrayItem", "create_submenu", NativeValues.Submenu(options)), "submenu") { }
    internal Submenu(RefCounted native) : base(native, "submenu") { }

    /// <summary>New references to the current native children in menu order. Dispose each returned reference.</summary>
    public IReadOnlyList<MenuEntry> Items => GetItems();
    /// <summary>Appends these entries in order.</summary>
    public void Append(params MenuEntry[] items) => SetItems("append", items);
    /// <summary>Prepends these entries in order.</summary>
    public void Prepend(params MenuEntry[] items) => SetItems("prepend", items);
    /// <summary>Inserts these entries at a zero-based position.</summary>
    public void Insert(int position, params MenuEntry[] items) => SetItems("insert", items, position);
    /// <summary>Removes an entry from this container without releasing other references.</summary>
    public void Remove(MenuEntry item) => Set("remove", item.Native);
    /// <summary>Removes the entry at a zero-based position and returns a new reference to dispose, or null if absent.</summary>
    public MenuEntry? RemoveAt(int position) => RemoveItemAt(position);
    /// <summary>Sets or clears the icon. Platform support follows upstream.</summary>
    public void SetIcon(IconSource? icon, bool iconAsTemplate = false) => SetIconSource(icon, iconAsTemplate);

    /// <summary>Reads the macOS template icon state.</summary>
    public bool IconIsTemplate() => Call<bool>("icon_is_template");
}
