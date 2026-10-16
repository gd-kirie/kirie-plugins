# GdKirie.Tray.EventaAdapter

This package forwards browser operations to GdKirie.Tray through an application's
existing Eventa context. It does not own the context or WebView.

Register the JSON contracts before creating the context, then attach the adapter:

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

Send the returned descriptor through an application-defined request or event.
Browser code calls `client.fromResource(resource)` to wrap it locally.
Both languages then operate on the same native state.

The adapter owns one reference and one live handle per native object. Repeated
queries and Expose calls reuse that handle, without adding retained references or
event subscriptions. Closing it invalidates browser wrappers using that handle.
The application's original C# wrapper and native parent references remain usable.

Descriptors contain a transport handle, native upstream ID, and object kind.
Creation IDs pass upstream. The adapter provides no aliases, discovery, or reload
recovery. The application owns page lifetime and decides when to dispose an old
adapter or pass existing descriptors to a new page.

Disposing the adapter removes its handlers and subscriptions and releases all
of its references. The application retains ownership of its Eventa context.
The application must dispose the adapter during its own shutdown.
All adapter operations run on Godot's main thread.

See [the browser API](../tray/README.md), [native SDK](../GdKirie.Tray/README.md),
and [sharing example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray/README.md).
