namespace GdKirie.Tray;

/// <summary>An error returned by the native tray or menu implementation.</summary>
public sealed class TrayException(string code, string message) : Exception(message)
{
    /// <summary>The native error code.</summary>
    public string Code { get; } = code;
}
