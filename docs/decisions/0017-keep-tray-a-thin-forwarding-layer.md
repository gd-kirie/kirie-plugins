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

This supersedes [ADR-0016](0016-forward-native-tray-and-menu-objects.md).
It retains the independent C# SDK, optional Eventa adapter, registered Godot
objects, native dependencies, and GDExtension distribution described there.

## Decision

Keep explicit methods that convert arguments and call upstream operations.
Native objects remain the source of state. Preserve upstream defaults, errors,
and platform differences; do not emulate unsupported behavior.

Each C# wrapper owns an independent native reference. Menu queries return new
references, without a managed identity cache. Dispose releases only that
reference and its subscriptions. Native containers retain their attached objects.
Subscribe to native signals only while a C# event has subscribers.

The Eventa adapter borrows the application's context and owns one C# reference
per exposed native object. Repeated queries return its existing live handle and
do not add retained references or event connections. Expose retains a reference
independent of the application's original C# wrapper. Closing a handle releases
that adapter reference; querying a parent-retained object later opens a new handle.

Creation IDs are upstream IDs, not browser aliases. Remove list, get, find,
alias registration, and automatic reconnection. Application protocols decide
which descriptors to share and when to replace a page's adapter. Browser garbage
collection does not release native resources; close and adapter disposal do.

Keep only the machinery required by language and runtime boundaries: value
conversion, handle lookup, native reference ownership, and main-thread event
delivery. Queued events retain the Godot identity registered at receipt so that
later native ID reuse cannot redirect already-received events.

## Consequences

The adapter owns browser resources until explicit close or adapter disposal.
Reading a menu does not require browser callers to track and release each query.
C# callers use ordinary disposable native references. Applications that need
discovery or reload persistence own that policy.

## References

- [tray-icon API](https://github.com/tauri-apps/tray-icon/blob/17e3196a15aa6174a21588ac4c5a52e322878479/src/lib.rs)
- [muda objects](https://github.com/tauri-apps/muda/tree/33dfdf7c6daceed1dddd73b658e0e4c912685f36/src)
- [Godot reference ownership](https://docs.godotengine.org/en/stable/classes/class_refcounted.html)
