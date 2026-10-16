namespace GdKirie.Tray;

/// <summary>A position in physical desktop coordinates.</summary>
public readonly record struct TrayPosition(double X, double Y);

/// <summary>A native tray icon rectangle.</summary>
public readonly record struct TrayRect(TrayPosition Position, uint Width, uint Height);

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
    TrayIconEventKind type,
    TrayPosition position,
    TrayRect rect,
    TrayMouseButton? button = null,
    TrayMouseButtonState? buttonState = null) : EventArgs
{
    /// <summary>The upstream event type.</summary>
    public TrayIconEventKind Type { get; } = type;
    /// <summary>The event position in physical desktop coordinates.</summary>
    public TrayPosition Position { get; } = position;
    /// <summary>Reads the native tray rectangle, or returns null if unavailable.</summary>
    public TrayRect Rect { get; } = rect;
    /// <summary>The mouse button, when present in this event.</summary>
    public TrayMouseButton? Button { get; } = button;
    /// <summary>The mouse button state, when present in this event.</summary>
    public TrayMouseButtonState? ButtonState { get; } = buttonState;
}
