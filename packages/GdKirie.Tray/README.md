# GdKirie.Tray

`GdKirie.Tray` provides ordinary C# tray and menu objects for Godot .NET applications.
It calls the Rust GDExtension, which wraps [`tray-icon`](https://github.com/tauri-apps/tray-icon)
and [`muda`](https://github.com/tauri-apps/muda).
The SDK has no Eventa or WebView dependency.

## Install

Add the NuGet package to your Godot .NET project:

```sh
dotnet add package GdKirie.Tray
```

Install the native `addons/kirie_tray` addon from the `@gd-kirie/tray` package.
For SDK-only use, copy `node_modules/@gd-kirie/tray/addon/kirie_tray` to your
project's `addons/kirie_tray` directory. No Kirie core addon is required.
The optional [plugin installer](../tray/README.md) installs this addon through
`kirie doctor --fix plugin:tray` and also adds the optional Eventa adapter.
Godot loads and exports the platform library from the addon.
The NuGet SDK contains managed code only.

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

[`GdKirie.Tray.EventaAdapter`](../GdKirie.Tray.EventaAdapter/README.md) provides optional browser access through an existing Eventa context.

See the [standalone C# example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray-native/README.md), the
[browser example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray/README.md), the
[browser API](../tray/README.md), and
[architecture decision](../../docs/decisions/0017-keep-tray-a-thin-forwarding-layer.md).
