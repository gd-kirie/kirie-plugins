using Godot.Collections;

namespace GdKirie.Tray;

/// <summary>A position in physical desktop coordinates.</summary>
public readonly record struct TrayPosition(double X, double Y);

/// <summary>A size in physical desktop pixels.</summary>
public readonly record struct TraySize(uint Width, uint Height);

/// <summary>A native tray icon rectangle.</summary>
public readonly record struct TrayRect(TrayPosition Position, TraySize Size);

/// <summary>A native tray icon event type.</summary>
public enum TrayIconEventKind
{
    /// <summary>The native Click value.</summary>
    Click,
    /// <summary>The native DoubleClick value.</summary>
    DoubleClick,
    /// <summary>The native Enter value.</summary>
    Enter,
    /// <summary>The native Move value.</summary>
    Move,
    /// <summary>The native Leave value.</summary>
    Leave,
}

/// <summary>A mouse button reported by upstream.</summary>
public enum TrayMouseButton
{
    /// <summary>The native Left value.</summary>
    Left,
    /// <summary>The native Right value.</summary>
    Right,
    /// <summary>The native Middle value.</summary>
    Middle,
}

/// <summary>A mouse button state reported by upstream.</summary>
public enum TrayMouseButtonState
{
    /// <summary>The native Up value.</summary>
    Up,
    /// <summary>The native Down value.</summary>
    Down,
}

/// <summary>An event emitted by a native tray icon.</summary>
public sealed class TrayIconEvent(
    string id,
    TrayIconEventKind type,
    TrayPosition position,
    TrayRect rect,
    TrayMouseButton? button = null,
    TrayMouseButtonState? buttonState = null) : EventArgs
{
    /// <summary>The upstream tray ID.</summary>
    public string Id { get; } = id;
    /// <summary>The upstream event type.</summary>
    public TrayIconEventKind Type { get; } = type;
    /// <summary>The event position in physical desktop coordinates.</summary>
    public TrayPosition Position { get; } = position;
    /// <summary>The native tray rectangle supplied by upstream.</summary>
    public TrayRect Rect { get; } = rect;
    /// <summary>The mouse button, when present in this event.</summary>
    public TrayMouseButton? Button { get; } = button;
    /// <summary>The mouse button state, when present in this event.</summary>
    public TrayMouseButtonState? ButtonState { get; } = buttonState;

    internal static TrayIconEvent Read(Dictionary value)
    {
        var type = Enum.Parse<TrayIconEventKind>(value["type"].AsString());
        using var position = value["position"].AsGodotDictionary();
        using var rect = value["rect"].AsGodotDictionary();
        TrayMouseButton? button = value.TryGetValue("button", out var buttonValue)
            ? Enum.Parse<TrayMouseButton>(buttonValue.AsString())
            : null;
        TrayMouseButtonState? state = value.TryGetValue("buttonState", out var stateValue)
            ? Enum.Parse<TrayMouseButtonState>(stateValue.AsString())
            : null;
        return new TrayIconEvent(value["id"].AsString(), type, ReadPosition(position), ReadRect(rect), button, state);
    }

    private static TrayPosition ReadPosition(Dictionary value) => new(value["x"].AsDouble(), value["y"].AsDouble());

    internal static TrayRect ReadRect(Dictionary value)
    {
        using var position = value["position"].AsGodotDictionary();
        using var size = value["size"].AsGodotDictionary();
        return new TrayRect(ReadPosition(position), new TraySize(size["width"].AsUInt32(), size["height"].AsUInt32()));
    }
}
