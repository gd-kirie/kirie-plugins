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
    public Menu(MenuOptions? options = null) : base(Create("KirieTrayMenu", "initialize", NativeValues.Menu(options ?? new())), "menu") { }
    internal Menu(RefCounted native) : base(native, "menu") { }

    /// <summary>New references to the current native children in menu order. Dispose each returned reference.</summary>
    public IReadOnlyList<MenuEntry> Items => GetItems();
    /// <summary>Appends these entries in order.</summary>
    public void Append(params MenuEntry[] items) => Set("append", NativeValues.Items(items));
    /// <summary>Prepends these entries in order.</summary>
    public void Prepend(params MenuEntry[] items) => Set("prepend", NativeValues.Items(items));
    /// <summary>Inserts these entries at a zero-based position.</summary>
    public void Insert(int position, params MenuEntry[] items) => Set("insert", NativeValues.Items(items), position);
    /// <summary>Removes an entry from this container without releasing other references.</summary>
    public void Remove(MenuEntry item) => Set("remove", item.Native);
    /// <summary>Removes the entry at a zero-based position and returns a new reference to dispose, or null if absent.</summary>
    public MenuEntry? RemoveAt(int position) => RemoveItemAt(position);
}

/// <summary>A native menu entry with text and a click event.</summary>
public abstract class MenuEntry : TrayObject
{
    private Callable _clicked;
    private EventHandler? _handlers;

    internal MenuEntry(RefCounted native, string kind) : base(native, kind) { }

    /// <summary>The current upstream item text.</summary>
    public string Text
    {
        get
        {
            using var result = Call("text");
            return result.AsString();
        }
        set => Set("set_text", value);
    }

    /// <summary>Raised when upstream reports this item's activation.</summary>
    public event EventHandler? Clicked
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
                _clicked = Callable.From(OnClicked);
                using var name = new StringName("clicked");
                var error = native.Connect(name, _clicked);
                if (error != Error.Ok)
                {
                    throw new InvalidOperationException($"Cannot connect the native clicked signal: {error}");
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
                using var name = new StringName("clicked");
                Native.Disconnect(name, _clicked);
                _clicked = default;
            }
        }
    }
    private void OnClicked() => _handlers?.Invoke(this, EventArgs.Empty);

    /// <summary>Disconnects the native callbacks before this reference is released.</summary>
    protected override void DisconnectSignals()
    {
        if (_handlers is not null)
        {
            using var name = new StringName("clicked");
            Native.Disconnect(name, _clicked);
            _handlers = null;
            _clicked = default;
        }
    }
}

/// <summary>A menu entry with upstream enabled state.</summary>
public abstract class EnabledMenuEntry : MenuEntry
{
    internal EnabledMenuEntry(RefCounted native, string kind) : base(native, kind) { }

    /// <summary>The current upstream enabled state.</summary>
    public bool Enabled
    {
        get
        {
            using var result = Call("is_enabled");
            return result.AsBool();
        }
        set => Set("set_enabled", value);
    }
}

/// <summary>A native submenu that is also a menu entry.</summary>
public sealed class Submenu : EnabledMenuEntry, IMenuContainer
{
    /// <summary>Creates a native Submenu with the supplied options.</summary>
    public Submenu(SubmenuOptions options) : base(Create("KirieTrayItem", "initialize_submenu", NativeValues.Submenu(options)), "submenu") { }
    internal Submenu(RefCounted native) : base(native, "submenu") { }

    /// <summary>New references to the current native children in menu order. Dispose each returned reference.</summary>
    public IReadOnlyList<MenuEntry> Items => GetItems();
    /// <summary>Appends these entries in order.</summary>
    public void Append(params MenuEntry[] items) => Set("append", NativeValues.Items(items));
    /// <summary>Prepends these entries in order.</summary>
    public void Prepend(params MenuEntry[] items) => Set("prepend", NativeValues.Items(items));
    /// <summary>Inserts these entries at a zero-based position.</summary>
    public void Insert(int position, params MenuEntry[] items) => Set("insert", NativeValues.Items(items), position);
    /// <summary>Removes an entry from this container without releasing other references.</summary>
    public void Remove(MenuEntry item) => Set("remove", item.Native);
    /// <summary>Removes the entry at a zero-based position and returns a new reference to dispose, or null if absent.</summary>
    public MenuEntry? RemoveAt(int position) => RemoveItemAt(position);
    /// <summary>Sets or clears the icon. Platform support follows upstream.</summary>
    public void SetIcon(IconSource? icon, bool iconAsTemplate = false) => Set("set_icon", NativeValues.NullableIcon(icon), iconAsTemplate);

    /// <summary>Reads the macOS template icon state.</summary>
    public bool IconIsTemplate()
    {
        using var result = Call("icon_is_template");
        return result.AsBool();
    }
}
