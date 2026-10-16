---
status: "superseded"
date: 2026-10-14
decision-makers: "Doji"
consulted: "Codex"
informed: "Kirie contributors"
---

# Forward native Tray and menu objects

Superseded by [ADR-0017](0017-keep-tray-a-thin-forwarding-layer.md).

## Context

The original Tray interface exposed one implicit tray and rebuilt menu snapshots.
It omitted most upstream operations. The user requested broad tray-icon and muda
coverage through a thin wrapper, usable from both C# and browser TypeScript.

C# must call registered GDExtension functions directly. Browser IPC must not shape
the native object API or become a dependency of the C# SDK.
This decision supersedes the API and lifecycle choices in
[ADR-0015](https://github.com/gd-kirie/gd-kirie/blob/main/docs/decisions/0015-load-tray-through-a-gdextension.md).
It retains GDExtension loading, addon distribution, and native dependencies.

## Decision

Use three boundaries:

```text
C# application -> GdKirie.Tray -> Rust GDExtension objects -> tray-icon / muda
Browser objects -> Eventa -> GdKirie.Tray.EventaAdapter -> GdKirie.Tray
```

The C# SDK exposes ordinary TrayIcon, Menu, Submenu, MenuItem, CheckMenuItem,
IconMenuItem, and PredefinedMenuItem objects. It has no Eventa or JSON dependency.
Constructors create upstream objects. Methods and properties call registered
native methods. Getters read upstream state.

Rust registers KirieTrayIcon, KirieTrayMenu, and KirieTrayItem as RefCounted
Godot classes. Each wraps one upstream object. Menu containers and trays retain
Godot references to their attached items or menu. They query upstream membership
for its current order. They do not store a second menu model.

Godot arguments carry values and actual object references. Options use Godot
dictionaries. A native function returns a dictionary containing a success value
or an error code and message. C# converts errors to TrayException.
This representation carries no command and selects no operation.
Do not add a command dispatcher, native resource registry, or native browser handles.

One private scene-tree node consumes upstream event queues. It routes events to
object signals through weak Godot instance identities. Signal emission holds no
Rust object binding or router borrow, so callbacks can call object methods.
C# exposes these signals as ordinary events. Event delivery works without Eventa.

The separate Eventa adapter borrows the application's existing context.
It registers a named Eventa method for each browser operation.
Its handlers call the same C# SDK used by native applications.
Only this adapter converts JSON values and maintains browser object bindings.
TypeScript exposes ordinary classes with explicit methods. It does not use
JavaScript Proxy.

The adapter owns objects created through browser constructors. Expose(id, object)
opens a binding to an application-owned C# object. Closing a borrowed binding
does not dispose that object. Adapter shutdown disposes its own objects and
removes subscriptions to borrowed objects.
The adapter also releases managed references newly allocated by menu queries.
Cached application references remain borrowed.

Browser IDs identify live adapter bindings. Handles are local to one adapter and
never reused there. list, get, and find query those bindings.
They are not native upstream APIs. Closing a handle leaves it invalid.
Menu queries can open a fresh binding to an object retained by its parent.
There are no permanent logical-ID reservations or recursive child disposal.

C# Dispose releases its managed reference and subscriptions.
Native objects are released when their last Godot reference goes away.
An attached item can outlive its original C# wrapper. A later menu query can
create a new wrapper over the same native object.

Keep creation, mutation, events, and cleanup on Godot's main thread.
Preserve tray-icon 0.26.1, muda 0.21.2, and godot 0.5.5 with api-4-7.
No dependency workaround forms part of this decision.

Cover public, non-deprecated tray and menu operations suitable for these language
boundaries. Preserve upstream platform differences and errors.
Do not emulate unsupported behavior. Raw native pointers, arbitrary
NSAttributedString objects, application menu bars, and window menu bindings
remain outside this Tray API.

Icons are values: Godot resources, RGBA pixels, native names, or Windows sources.
Physical and logical accelerators use upstream parsers.
Predefined items preserve upstream platform behavior.

## Consequences

- C# applications can use the SDK without Kirie WebView or Eventa.
- Browser code and C# code can operate on the same native object.
- Browser binding lifecycle stays in the optional adapter.
- The GDExtension loader and native addon packaging remain unchanged.
- Platform support remains uneven because upstream behavior is preserved.
- Linux KSNI multi-tray menu updates need platform testing. A source-based
  suspicion about its shared change queue does not justify a local workaround.

## Implementation

- [Native object classes and event pump](../../packages/GdKirie.Tray/native/src/).
- [C# SDK](../../packages/GdKirie.Tray/src/).
- [Browser adapter](../../packages/GdKirie.Tray.EventaAdapter/src/).
- [TypeScript objects and Eventa methods](../../packages/tray/src/).
- [Independent C# example](https://github.com/gd-kirie/kirie-examples/tree/main/examples/tray-native).
- [Shared C# and browser example](https://github.com/gd-kirie/kirie-examples/tree/main/examples/tray).

## Verification

Run native tests and lint, compile the .NET SDK and adapter, and check TypeScript.
In kirie-examples, use test:tray-sdk to exercise C# without IPC.
Use test:tray-desktop there to exercise browser forwarding and a shared C# object.
These probes cover mutation, membership, retained references, stale handles,
icon validation, and cleanup.
Native user clicks and Windows/Linux behavior require platform acceptance tests.

## Rejected Alternatives

A singleton and rebuilt snapshots cannot express upstream object lifetimes or
independent trays and menus.

A second command protocol between C# and Rust duplicates Godot's registered
method calls.

A native browser registry makes the SDK depend on transport-specific concepts.
The optional adapter already has the information needed to own those bindings.

## References

- [tray-icon public API](https://github.com/tauri-apps/tray-icon/blob/17e3196a15aa6174a21588ac4c5a52e322878479/src/lib.rs)
- [muda menu objects](https://github.com/tauri-apps/muda/tree/33dfdf7c6daceed1dddd73b658e0e4c912685f36/src)
- [Godot RefCounted ownership](https://docs.godotengine.org/en/stable/classes/class_refcounted.html)
- [Godot GDExtension loading](https://docs.godotengine.org/en/stable/engine_details/engine_api/gdextension/gdextension_file.html)
- [Godot Object method calls](https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-call)

## More Information

### 2026-10-15: Event pump implementation

The implementation connects the event pump to SceneTree.process_frame instead
of adding the private node described in the decision. This keeps one event
consumer on Godot's main thread without another node lifecycle.
See the [Godot signal contract](https://docs.godotengine.org/en/stable/classes/class_scenetree.html#class-scenetree-signal-process-frame).
