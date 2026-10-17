using Godot;

namespace GdKirie.Tray;

/// <summary>A managed reference to one native tray or menu object.</summary>
public abstract class TrayObject : IDisposable
{
    private Variant _native;

    internal TrayObject(RefCounted native, string kind)
    {
        _native = Variant.From(native);
        Kind = kind;
        InstanceId = native.GetInstanceId();
        using var id = Call("id");
        Id = id.AsString();
    }

    /// <summary>The upstream object ID.</summary>
    public string Id { get; }
    /// <summary>The native object type.</summary>
    public string Kind { get; }
    /// <summary>The Godot identity of this native object.</summary>
    public ulong InstanceId { get; }
    /// <summary>Whether this managed reference is released.</summary>
    public bool IsDisposed { get; private set; }

    internal RefCounted Native
    {
        get
        {
            EnsureMainThread();
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return (RefCounted)_native.AsGodotObject();
        }
    }

    internal static void EnsureMainThread()
    {
        if (!GodotThread.IsMainThread())
        {
            throw new InvalidOperationException("Tray and menu operations must run on Godot's main thread.");
        }
    }

    internal static RefCounted Create(string className, string factory, params Variant[] arguments)
    {
        try
        {
            EnsureMainThread();
            using var name = new StringName(className);
            if (!ClassDB.ClassExists(name))
            {
                throw new InvalidOperationException("The Kirie Tray GDExtension is not loaded. Install the @gd-kirie/tray plugin assets.");
            }

            using var method = new StringName(factory);
            using var result = ClassDB.ClassCallStatic(name, method, arguments);
            using var value = ReadReply(result);
            return (RefCounted)value.AsGodotObject();
        }
        finally
        {
            foreach (var argument in arguments)
            {
                argument.Dispose();
            }
        }
    }

    internal TrayObject Retain() => this switch
    {
        Menu => new Menu(Native),
        TrayIcon => new TrayIcon(Native),
        _ => WrapEntry(Native),
    };

    private static MenuEntry WrapEntry(RefCounted native)
    {
        using var kind = CallNative(native, "kind");
        return kind.AsString() switch
        {
            "item" => new MenuItem(native),
            "check" => new CheckMenuItem(native),
            "icon" => new IconMenuItem(native),
            "submenu" => new Submenu(native),
            "predefined" => new PredefinedMenuItem(native),
            var unknown => throw new InvalidOperationException($"Unknown native menu item kind: {unknown}"),
        };
    }

    private static Variant CallNative(RefCounted native, string method, params Variant[] arguments)
    {
        using var methodName = new StringName(method);
        using var result = native.Call(methodName, arguments);
        return ReadReply(result);
    }

    private static Variant ReadReply(Variant result)
    {
        using var reply = result.AsGodotDictionary();
        if (!reply["ok"].AsBool())
        {
            using var errorValue = reply["error"];
            using var error = errorValue.AsGodotDictionary();
            throw new TrayException(error["code"].AsString(), error["message"].AsString());
        }

        return reply["value"];
    }

    internal Variant Call(string method, params Variant[] arguments)
    {
        try
        {
            return CallNative(Native, method, arguments);
        }
        finally
        {
            foreach (var argument in arguments)
            {
                argument.Dispose();
            }
        }
    }

    internal void Set(string method, params Variant[] arguments)
    {
        using var result = Call(method, arguments);
    }

    internal IReadOnlyList<MenuEntry> GetItems()
    {
        using var value = Call("items");
        using var array = value.AsGodotArray();
        var items = new List<MenuEntry>(array.Count);
        foreach (var item in array)
        {
            using var ownedItem = item;
            items.Add(WrapEntry((RefCounted)item.AsGodotObject()));
        }

        return items;
    }

    internal MenuEntry? RemoveItemAt(int position)
    {
        using var value = Call("remove_at", position);
        if (value.VariantType == Variant.Type.Nil)
        {
            return null;
        }

        return WrapEntry((RefCounted)value.AsGodotObject());
    }

    /// <summary>Releases this reference. Native menu containers keep their own references to attached objects.</summary>
    public void Dispose()
    {
        EnsureMainThread();
        if (IsDisposed)
        {
            return;
        }

        var native = Native;
        IsDisposed = true;
        native.Dispose();
        _native.Dispose();
    }
}
