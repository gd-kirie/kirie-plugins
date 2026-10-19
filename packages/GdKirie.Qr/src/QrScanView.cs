using Godot;

namespace GdKirie.Qr;

internal sealed partial class QrScanView : CanvasLayer
{
    private readonly TextureRect _preview = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
    };
    private ImageTexture? _texture;
    private Window? _window;

    internal event Action? CancelRequested;
    internal event Action<bool>? ApplicationPausedChanged;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 100;
        _window = GetWindow();
        _window.GoBackRequested += OnBack;
        var background = new PanelContainer();
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var column = new VBoxContainer();
        background.AddChild(column);
        column.AddChild(new Label { Text = "Point the camera at a QR code", HorizontalAlignment = HorizontalAlignment.Center });
        column.AddChild(_preview);
        var cancel = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(0, 64) };
        cancel.Pressed += () => CancelRequested?.Invoke();
        column.AddChild(cancel);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationPaused)
            ApplicationPausedChanged?.Invoke(true);
        else if (what == NotificationApplicationResumed)
            ApplicationPausedChanged?.Invoke(false);
    }

    private void OnBack() => CancelRequested?.Invoke();

    internal void ShowFrame(Image image)
    {
        if (_texture is null)
        {
            _texture = ImageTexture.CreateFromImage(image);
            _preview.Texture = _texture;
            return;
        }

        if (_texture.GetWidth() == image.GetWidth() && _texture.GetHeight() == image.GetHeight())
            _texture.Update(image);
        else
            _texture.SetImage(image);
    }

    public override void _ExitTree()
    {
        if (_window is not null)
            _window.GoBackRequested -= OnBack;
        _window = null;
        _preview.Texture = null;
        _texture?.Dispose();
        _texture = null;
    }
}
