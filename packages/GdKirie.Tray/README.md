# GdKirie.Tray

`GdKirie.Tray` provides tray and menu objects with Eventa integration for Kirie applications.
It calls the Rust GDExtension, which wraps [`tray-icon`](https://github.com/tauri-apps/tray-icon)
and [`muda`](https://github.com/tauri-apps/muda).
The package includes Eventa contracts and handlers that borrow the application's context.

## Install

Install and configure the plugin as described in the [plugin setup](../tray/README.md).
The installer adds the `GdKirie.Tray` NuGet package and the native `addons/kirie_tray` addon.
Godot loads and exports the platform library from the addon.
The NuGet package contains managed code only.

## Use native objects

Create objects on Godot's main thread after the scene tree starts:

```csharp
using GdKirie.Tray;

using var enabled = new CheckMenuItem(new("Enabled") { Checked = true });
using var quit = new MenuItem(new("Quit"));
using var menu = new Menu(new() { Items = [enabled, quit] });
using var tray = new TrayIcon(new()
{
    Icon = new IconSource.Resource("res://tray.svg"),
    Menu = menu,
    Tooltip = "My application",
});

enabled.Clicked += (_, _) => GD.Print(enabled.Checked);
quit.Clicked += (_, _) => GetTree().Quit();
tray.Event += (_, e) => GD.Print(e.Type);

enabled.Checked = false;
menu.Remove(quit);
menu.Append(quit);
```

The `using` declarations keep these objects alive until the enclosing scope ends.
For application lifetime, store them in application fields and call `Dispose` during shutdown.

`Menu`, `Submenu`, `MenuItem`, `CheckMenuItem`, `IconMenuItem`, and `PredefinedMenuItem` wrap actual native objects.

`Menu` and `Submenu` implement `IMenuContainer`.
Both support `Items`, `Append`, `Prepend`, `Insert`, `Remove`, and `RemoveAt`.
Both can be attached to a `TrayIcon`.

`IconSource` distinguishes Godot textures, RGBA pixels, native icons, and Windows icon sources.
`Accelerator.Physical` and `Accelerator.Logical` preserve the two upstream keyboard accelerator types.
`PredefinedMenuItemKind` selects an upstream action.
`AboutMetadata` supplies the About item metadata.
Platform support follows upstream. Operations the current backend cannot perform
either have no effect or throw `TrayException` with the native error.

## Lifetime and events

Each C# object holds one native Godot reference.
`Dispose` disconnects its callbacks and releases that reference.
It does not remove entries from menus or release references held by native containers.
`Remove` changes menu membership without disposing the item.

`Items` and `RemoveAt` return new, independently disposable C# references.
Dispose each returned reference after use. Wrappers for the same native object
have no managed identity guarantee.
`InstanceId` identifies the native Godot object.
`Id` is its upstream tray or menu ID.
Live tray IDs must be unique among trays, and live menu entry IDs among entries,
so native events can identify their target.

Use `Clicked` and `TrayIcon.Event` to receive native events.
Native signal connections exist only while the event has subscribers. Unsubscribe
or dispose the wrapper to release an active subscription.
Callbacks run on Godot's main thread and can change native menu state directly.
Menu events identify the item. Upstream does not provide the originating tray ID.

All native calls and disposal must run on Godot's main thread.
Native operation errors throw `TrayException`, with the native `Code` and `Message`.
A disposed C# object throws `ObjectDisposedException` on further native access.

## Browser access

Register the JSON contracts before you create the application's Eventa context:

```csharp
using GdKirie.Tray;
using GdKirie.Tray.EventaAdapter;
using GdKirie.EventaAdapter;

var registry = GdKirieTrayEventaAdapter.Register(new KirieEventaJsonRegistry());
using var eventa = kirie.CreateEventaContext(registry);
using var adapter = GdKirieTrayEventaAdapter.Attach(eventa.Context);
using var item = new CheckMenuItem(new("Enabled"));
var resource = adapter.Expose(item);
```

The `GdKirie.Tray` package includes the `GdKirie.Tray.EventaAdapter` namespace.

Send the returned descriptor through an application-defined request or event.
Browser code calls `client.fromResource(resource)` to wrap it locally.
Both languages then operate on the same native state.

The adapter owns one reference and one live handle per native object.
Repeated queries and `Expose` calls reuse that handle without extra retained references or event subscriptions.
Closing the handle invalidates browser wrappers that use it.
The application's original C# wrapper and native parent references remain usable.

Descriptors contain a transport handle, native upstream ID, and object kind.
Creation IDs pass upstream. The adapter provides no aliases, discovery, or reload recovery.
The application owns page lifetime and decides when to replace an adapter or share descriptors.

Dispose the adapter before its Eventa context during application shutdown.
Disposal removes its handlers and subscriptions and releases all of its references.
The application retains ownership of its Eventa context and WebView.
All adapter operations run on Godot's main thread.

See the [browser example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray/README.md), the
[browser API](../tray/README.md), and
[architecture decision](../../docs/decisions/0001-keep-tray-a-thin-forwarding-layer.md).
