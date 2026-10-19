using Godot;
using Dictionary = Godot.Collections.Dictionary;

public sealed partial class FakeCamera : Node
{
    [Signal] public delegate void camera_permission_grantedEventHandler();
    [Signal] public delegate void camera_permission_deniedEventHandler();
    [Signal] public delegate void frame_availableEventHandler(Dictionary frame);

    public bool Permission { get; set; } = true;
    public bool Started { get; private set; }
    public int Starts { get; private set; }
    public int Stops { get; private set; }

    public bool has_camera_permission() => Permission;
    public void request_camera_permission() { }
    public Godot.Collections.Array get_all_cameras() =>
    [
        new Dictionary { ["camera_id"] = "front", ["is_front_facing"] = true },
        new Dictionary { ["camera_id"] = "rear", ["is_front_facing"] = false },
    ];

    public void start(Dictionary request)
    {
        if (request["camera_id"].AsString() != "rear")
            throw new InvalidOperationException("The host did not select the rear camera.");
        Started = true;
        Starts++;
    }

    public void stop()
    {
        Started = false;
        Stops++;
    }

    public void Frame(Image image) => EmitSignal(SignalName.frame_available, new Dictionary
    {
        ["width"] = image.GetWidth(),
        ["height"] = image.GetHeight(),
        ["buffer"] = image.GetData(),
        ["is_grayscale"] = false,
    });

    public void Grant() => EmitSignal(SignalName.camera_permission_granted);
    public void Deny() => EmitSignal(SignalName.camera_permission_denied);
}
