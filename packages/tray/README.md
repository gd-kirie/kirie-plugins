# `@gd-kirie/tray`

`@gd-kirie/tray` forwards tray and menu operations to `tray-icon` and `muda`.
It borrows the application's existing Eventa context.

Install the package in a Kirie project:

```sh
pnpm add @gd-kirie/tray
```

Add the plugin to `kirie.config.ts`:

```ts
import tray from "@gd-kirie/tray/plugin";
import { defineKirieConfig } from "kirie";
import core from "kirie/plugin/core";

export default defineKirieConfig({
  plugins: [core, tray],
});
```

Install the plugin dependencies:

```sh
pnpm kirie doctor --fix plugin:tray
```

This command installs `addons/kirie_tray` and the `GdKirie.Tray` NuGet package.
The NuGet package includes the native object wrappers and Eventa integration.
The npm package bundles the desktop GDExtension libraries. Godot selects the
library for the current platform and includes it in application exports.
macOS, Windows, and Linux support x64 and Arm64. Linux requires a
StatusNotifierItem host.

## Native objects

In Godot, [register and attach the adapter](../GdKirie.Tray/README.md#browser-access)
to the application's Eventa context.
Create a client with the application's Eventa context:

```ts
import { createTrayClient } from "@gd-kirie/tray";

const client = createTrayClient(context);
const enabled = await client.createCheckMenuItem({
  id: "enabled",
  text: "Enabled",
  checked: true,
});
const menu = await client.createMenu({ id: "main-menu", items: [enabled] });
const tray = await client.createTray({
  id: "main-tray",
  icon: { kind: "resource", path: "res://tray.svg" },
  menu,
  tooltip: "My application",
});

await enabled.setChecked(false);
const checked = await enabled.isChecked();
const open = await client.createMenuItem({ text: "Open" });
await menu.append(open);
await open.close(); // The menu retains its own reference.
await tray.setTooltip(null);
```

Each object has a transport `handle`, upstream native `id`, and object `kind`.
Getters and setters operate on native state. C# applications can also use the
[native SDK](../GdKirie.Tray/README.md) directly.

| Object | Operations |
| --- | --- |
| `TrayIcon` | Icon, menu, tooltip, title, visibility, click policy, menu display, rectangle, template status, temporary directory |
| `Menu` | Item queries, append, prepend, insert, remove, remove by position |
| `Submenu` | Menu operations, text, enabled state, icon, template status |
| `MenuItem` | Text, enabled state, accelerator |
| `CheckMenuItem` | Menu item operations, checked state |
| `IconMenuItem` | Menu item operations, icon, template status |
| `PredefinedMenuItem` | Upstream predefined actions, text, About metadata at creation |

Tray and regular menu creation methods accept builder options in one call. Tray options include menu,
icon, template mode, tooltip, title, click policy, GUID, autosave name, and temporary directory.
Platform restrictions also apply to creation options.

Icons accept Godot resources, RGBA arrays, named native icons, and Windows icon
sources. `null` clears an optional icon, menu, tooltip, title, or accelerator.
An omitted creation field uses the upstream default. RGBA uses a JSON number
array, not a `Uint8Array` object.

Accelerators use `{ kind: "physical" | "logical", value: string }`. Upstream
`Accelerator` or `KeyAccelerator` parses the string.

Predefined items use the public upstream constructors through `client.predefined`:

```ts
const copy = await client.predefined.copy();
const paste = await client.predefined.paste("Paste");
const separator = await client.predefined.separator();
const section = await client.predefined.sectionHeader("Actions");
const about = await client.predefined.about(null, { name: "My application" });
```

Each constructor has its own Eventa method. No action selector is necessary.
A null or omitted text uses the upstream default. An empty string remains empty.
`sectionHeader` requires text. `separator` has no arguments.
`about` accepts optional text and About metadata.

## Events and ownership

Subscribe to native events:

```ts
const stopEvents = client.onEvent((event) => {
  console.log(event);
});
const stopCheck = enabled.onClick(() => {
  enabled.isChecked().then(console.log).catch(console.error);
});

// Stop subscriptions when the page exits.
stopCheck();
stopEvents();
```

Tray events include click, double-click, enter, move, and leave. They retain
the upstream positions, rectangles, buttons, and button states. Coordinates use physical screen pixels.
Menu events contain the item descriptor. Upstream does not identify the source
tray for a menu event. Native check clicks require `isChecked()`, not local inversion.

The adapter owns browser resources. Repeated queries of the same native object
return the same live handle; querying does not require a cleanup loop.
The optional creation `id` passes to the upstream constructor. Predefined items
use upstream-generated IDs.
Live tray IDs must be unique among trays, and live menu entry IDs among entries,
because upstream events identify objects by ID.

C# applications share an object with `adapter.Expose(item)` and send the returned
descriptor through their own application protocol. Browser code wraps it with
`client.fromResource(resource)`, without another request. This handle owns an
adapter-owned reference; closing it leaves the application's original wrapper alive.

`close()` releases that handle's reference and event forwarding. It does not
remove menu membership or recursively close children. Native containers retain
their attached objects. All browser wrappers sharing the closed handle become
invalid. A later query of an object retained by a native parent opens a new handle.

The adapter releases all of its references on disposal. Applications decide when
to dispose an adapter or retain descriptors across page reloads. Tray provides
no alias registry, discovery methods, or automatic reconnection.

Each native event is forwarded once per adapter with its object's handle.
Browser subscriptions require explicit cleanup, even
after `close()`; browser garbage collection does not release native resources.

Objects belong to one Eventa context. References from another context are
rejected, and closed handles remain invalid.

## Platform behavior

The API forwards operations to `tray-icon` and `muda` and keeps their platform
differences. Operations a backend cannot perform either have no effect or reject
with `TrayError`, which provides a `code` and the original error message.

| Platform | Constraints |
| --- | --- |
| macOS | Template icons, native named icons, autosave names, and template queries are platform-specific |
| Windows | GUID and file, resource, or stock icon sources are platform-specific. Tray title has no effect |
| Linux KSNI | Requires a StatusNotifierItem host. Click events provide left or middle activation, not right-click or pointer tracking |
| Linux KSNI | `rect()` returns `null`. Activation events retain the upstream empty rectangle |
| Linux KSNI | Explicit menu display and click policy have no effect |

Temporary icon directories are forwarded upstream; the KSNI backend does not use them.

Predefined actions keep upstream platform behavior. Linux KSNI can show disabled
predefined entries. Kirie does not emulate their actions. Windows accelerator
execution requires native message-loop integration. Linux KSNI does not export accelerators to its menu host.

Kirie excludes raw native handles, platform pointers, and arbitrary `NSAttributedString`
objects from IPC. The plugin does not expose `muda` application menu bars or window menu bindings.
It does not reproduce deprecated upstream APIs.

See the [`tray` example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray/README.md) for a complete setup.
The design follows [ADR-0001](../../docs/decisions/0001-keep-tray-a-thin-forwarding-layer.md).
Primary API sources are [tray-icon](https://github.com/tauri-apps/tray-icon/tree/17e3196a15aa6174a21588ac4c5a52e322878479)
and [muda](https://github.com/tauri-apps/muda/tree/33dfdf7c6daceed1dddd73b658e0e4c912685f36).
