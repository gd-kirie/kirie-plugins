---
status: "accepted"
date: 2026-10-15
decision-makers: "Doji"
---

# Keep Tray a thin forwarding layer

## Context

Tray needs to expose tray-icon and muda to C# and browser applications. Wrapper
identity caching, browser aliases, discovery, and reload recovery add contracts
that these upstream libraries do not require. The user chose to remove those
contracts and keep Kirie responsible for forwarding operations and events.

The implementation uses C# wrappers, Eventa integration, and registered Godot
objects. Kirie applications use these components together. GdKirie.Tray includes
the wrappers and Eventa integration in one NuGet package, like GdKirie.Notifications.
It retains the native dependencies and GDExtension distribution.

## Decision

Keep explicit methods that convert arguments and call upstream operations.
Expose each public upstream predefined-item constructor as a separate operation.
Do not expose the private upstream action type through a Kirie enum or a `kind` selector.
C# static methods, browser methods, and native factories call the corresponding upstream constructor.
Use the upstream Serde implementation for public `NativeIcon` variant names instead of a separate name table.

Native objects remain the source of state. Preserve upstream defaults, errors,
and platform differences; do not emulate unsupported behavior.

Construct each upstream object before its Godot wrapper.
`KirieTrayItem`, `KirieTrayMenu`, and `KirieTrayIcon` use `#[class(no_init)]` and expose static factories.
Their native fields hold complete values directly, without an uninitialized `Option` state.
C# constructors call these factories through `ClassDB.ClassCallStatic`.
Construction errors return through the existing reply format before a wrapper exists.

Use explicit native parameters for types that Godot bindings can declare.
Declare strings, booleans, menu item arrays, and positions with their Godot binding types.
Let the binding check those types; do not repeat these checks inside operations.
C# keeps its public options records and supplies their defaults as native arguments.
C# fills a native-created `Array[KirieTrayItem]` through an `Array<RefCounted>` view.
The native factory preserves the runtime element type, which C# cannot name as a generic argument.
Keep conversions for nullable scalars and union values that Godot cannot declare directly.
Group the tray's remaining nullable options in one dictionary instead of a long `Variant` parameter list.
Share predefined-item text conversion and wrapper construction; each entry still names its upstream constructor.
Keep upstream error forwarding and checks required to prevent invalid native access.
C# conversion helpers return concrete Godot collections. Convert arguments to `Variant` at dynamic calls and read known results through typed calls.
Each C# wrapper retains one owning `Variant` for its independent native reference.

Each C# wrapper owns an independent native reference. Menu queries return new
references, without a managed identity cache. Dispose releases only that
reference. Native containers retain their attached objects.
Events use the global upstream ID stream instead of per-object subscriptions.

The Eventa adapter is part of GdKirie.Tray. It borrows the application's context
and owns one C# reference per exposed native object.
Repeated queries return its existing live handle and do not add retained references.
Expose retains a reference
independent of the application's original C# wrapper. Closing a handle releases
that adapter reference; querying a parent-retained object later opens a new handle.

Creation IDs are upstream IDs, not browser aliases. Remove list, get, find,
alias registration, and automatic reconnection. Application protocols decide
which descriptors to share and when to replace a page's adapter. Browser garbage
collection does not release native resources; close and adapter disposal do.

Keep only the machinery required by language and runtime boundaries: value
conversion, handle lookup, native reference ownership, and main-thread event delivery.
Read `MenuEvent::receiver()` and `TrayIconEvent::receiver()` from the Godot main-loop callback.
The `KirieTrayEvents` Godot singleton emits global signals with upstream IDs.
Do not add a separate event queue, an ID-to-object registry, or ID reuse tracking.

C# `TrayEvents` owns a subscription to these signals.
Each Eventa adapter owns one subscription and forwards global events with IDs, independent of browser handles.
Browser callers subscribe through `client.onEvent()` and handle IDs directly.
Keep upstream enum names and the `position` plus `size` rectangle structure across all event boundaries.
Eventa serializes the C# tray event directly without a separate event or rectangle model.
Object disposal and handle closure do not stop the global event stream.

## Consequences

The adapter owns browser resources until explicit close or adapter disposal.
Reading a menu does not require browser callers to track and release each query.
C# callers use ordinary disposable native references. Applications that need
discovery or reload persistence own that policy.

Tray and Notifications each publish one managed plugin package with Eventa integration.
The Tray installer adds GdKirie.Tray directly.
The adapter keeps its public types and namespace within that assembly.

## Implementation

- [Rust GDExtension](../../crates/gd-kirie-tray/) belongs to the root Cargo workspace.
- [C# wrappers](../../packages/GdKirie.Tray/src/) expose native tray and menu objects.
- [Eventa integration](../../packages/GdKirie.Tray/src/EventaAdapter/) registers browser methods and manages browser references.
- [Plugin descriptor](../../packages/tray/src/plugin.ts) installs GdKirie.Tray and the native addon.

## Verification

- [x] Build the .NET solution with `mise run build:dotnet`.
- [x] Pack the plugins with `mise run pack:nuget`.
- [x] Verify that GdKirie.Tray includes adapter types and depends on the published GdKirie.EventaAdapter package.
- [x] Run `mise run typecheck`.
- [x] Run `mise run test:unit` to verify the browser contracts.

## References

- [tray-icon API](https://github.com/tauri-apps/tray-icon/blob/17e3196a15aa6174a21588ac4c5a52e322878479/src/lib.rs)
- [muda objects](https://github.com/tauri-apps/muda/tree/33dfdf7c6daceed1dddd73b658e0e4c912685f36/src)
- [Godot reference ownership](https://docs.godotengine.org/en/stable/classes/class_refcounted.html)
