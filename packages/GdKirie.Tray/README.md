# GdKirie.Tray

`GdKirie.Tray` provides system tray icons and menus for Kirie Godot applications.
It includes C# APIs and Eventa integration for browser clients.

## Install

Install and configure the plugin as described in the [plugin setup](../tray/README.md#install).
The installer adds this NuGet package and the Godot addon.

## Use

Run tray operations on Godot's main thread after the scene tree starts:

```csharp
using Godot;
using GdKirie.Tray;

using var open = new MenuItem(new("Open"));
using var menu = new Menu(new() { Items = [open] });
using var tray = new TrayIcon(new()
{
    Icon = new IconSource.Resource("res://tray.svg"),
    Menu = menu,
    Tooltip = "My application",
});

using var events = new TrayEvents();
events.Menu += id =>
{
    if (id == open.Id)
    {
        GD.Print("Open");
    }
};
```

Keep `res://tray.svg` in the Godot project.
Keep these objects alive while the application runs.
Dispose them on Godot's main thread during shutdown.

### Sharp macOS icons

`Resource` and `Rgba` icons take an optional `Scale`, in pixels per point.
On macOS, `new IconSource.Resource("res://tray.png", Scale: 2)` draws a 32 pixel icon at 16 points, and the icon stays sharp on Retina displays.
Without `Scale`, the same icon is drawn at 22 points.
Windows and Linux ignore `Scale`. Menu icons accept only the default value of 1.

## Browser access

Register the tray contracts before you create the application's Eventa context:

```csharp
using GdKirie.Tray.EventaAdapter;
using GdKirie.EventaAdapter;

var registry = GdKirieTrayEventaAdapter.Register(new KirieEventaJsonRegistry());
using var eventa = kirie.CreateEventaContext(registry);
using var adapter = GdKirieTrayEventaAdapter.Attach(eventa.Context);
```

Keep the adapter alive while browser clients use the tray API.
Dispose it before its Eventa context.
Create the browser client as described in [@gd-kirie/tray](../tray/README.md#use).

See the [tray example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray/README.md) for a complete application.
