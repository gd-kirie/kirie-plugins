# GdKirie.Qr

This package provides QR generation, image recognition, and one-shot camera scans for Android and iOS.
The browser package [@gd-kirie/qr](../qr/README.md#install) supplies the native dependencies and export configuration.

## Attach the host

Register the JSON contracts before you create the application's Eventa context.
Attach the QR host after the scene owner and KirieNode enter the scene tree.

```csharp
GdKirieQr.Register(registry);

// Reuse the application's existing Eventa context and KirieNode.
var qr = GdKirieQr.Attach(context, owner, kirieNode);
```

Keep the host alive for the scene lifetime.
The host disposes itself when the scene owner or KirieNode exits the scene tree.
Explicit disposal is also safe.
The host does not own the Eventa context, scene owner, or KirieNode.

## Local operations

```csharp
using var image = qr.GenerateImage("https://example.com");
using var texture = qr.GenerateTexture("https://example.com", 256);

string? fromImage = await qr.ScanImageAsync(image);
string? fromPng = await qr.ScanImageAsync(pngBytes);
string? fromCamera = await qr.ScanAsync();
```

Generation defaults are 512 pixels, black foreground, and white background.
The caller owns returned images and textures.
The caller retains ownership of input images.
Image recognition returns `null` when the image contains no QR code.
Camera recognition stops after its first result.
The Cancel button, Android Back, background entry during capture, and scene exit return `null`.
A canceled `CancellationToken` cancels the task.
Permission denial and reported native recognition errors fail the task.
Android's camera permission dialog can pause the application without canceling the scan.
After permission is granted, capture starts when the application is in the foreground.

Call host methods on the Godot main thread.
Only one recognition operation can run in the process.
Concurrent calls fail instead of queuing camera frames or image requests.
After cancellation, the host drains a pending native callback before another operation can start.
This rule prevents a late result from completing a later request.
The host also rejects deferred camera callbacks from an older subscription.

The camera prefers a rear camera and requests 1280 by 720 grayscale frames with automatic orientation correction.
The native plugin controls the actual dimensions.
The host keeps the latest frame for the preview and recognizes one image at a time.
The host hides the borrowed KirieNode during the scan, then restores its previous visibility.
Native WebView visibility requires Kirie core's mobile visibility forwarding.
The host suspends Godot's default Android Back exit behavior during a scan.
It restores the previous policy when the scan ends.

## Native dependencies and limitations

The Eventa contracts carry PNG base64 fields and nullable text results.
Godot images and camera frames stay in the host.

The plugin installs unmodified [godot-qr v1.2](https://github.com/godot-mobile-plugins/godot-qr/tree/v1.2)
and [NativeCamera v3.0](https://github.com/godot-mobile-plugins/godot-native-camera/tree/v3.0) artifacts.
The plugin descriptor pins their release URLs and SHA-256 values.
The npm package retains both MIT licenses under `addon/kirie_qr/licenses`.

Android uses a [bundled ML Kit model](https://developers.google.com/ml-kit/vision/barcode-scanning/android).
Offline scans require neither an installed GMS runtime nor an initial model download.
The upstream Android scanner does not explicitly [close its ML Kit scanner](https://github.com/godot-mobile-plugins/godot-qr/blob/v1.2/android/src/main/java/org/godotengine/plugin/qr/QRPlugin.java).

iOS requires version 16 or later on a physical device.
The upstream archives contain no simulator slices.
The upstream [UTF-8 result bridge](https://github.com/godot-mobile-plugins/godot-qr/blob/v1.2/ios/src/qr_plugin.mm#L50) corrupts Chinese results.
Use English ASCII payloads until upstream fixes this defect. The package applies no native patch.
Desktop and browser-only runtimes do not support the native plugins.

NativeCamera has no camera-start error signal. Some errors reach only native logs, so capture can remain open until cancellation.
Its [Android](https://github.com/godot-mobile-plugins/godot-native-camera/blob/v3.0/android/src/main/java/org/godotengine/plugin/nativecamera/NativeCameraPlugin.java#L335)
and [iOS](https://github.com/godot-mobile-plugins/godot-native-camera/blob/v3.0/ios/src/NativeCamera.swift#L294) frame callbacks have no session IDs or stop acknowledgment.
A queued frame that reaches a new subscription cannot identify its original scan.

## Development

Run the host harness with fake native singletons:

```sh
mise x -- dotnet build packages/GdKirie.Qr/tests/QrRuntimeTests.csproj
mise x -- godot --headless --path packages/GdKirie.Qr/tests
```
