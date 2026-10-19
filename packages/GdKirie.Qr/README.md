# GdKirie.Qr

This package provides QR generation, image recognition, and one-shot camera scans for Android and iOS.
It uses the original godot-qr v1.2 and NativeCamera v3.0 native plugins.
The browser package `@gd-kirie/qr` supplies their install and export configuration.

## Attach the host

Register the JSON contracts before you create the application's Eventa context.
Attach the QR host after the scene owner and KirieNode enter the scene tree.

```csharp
GdKirieQr.Register(registry);

// Reuse the application's existing Eventa context and KirieNode.
using var qr = GdKirieQr.Attach(context, owner, kirieNode);
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

The camera uses a rear camera when available, 1280 by 720 capture, grayscale pixels, and automatic orientation correction.
The preview updates while the recognizer processes one image.
The host skips analysis frames while the recognizer is busy.
The host hides the borrowed KirieNode during the scan, then restores its previous visibility.
Native WebView visibility requires Kirie core's mobile visibility forwarding.
The host suspends Godot's default Android Back exit behavior during a scan.
It restores the previous policy when the scan ends.
Godot connects its default Back handler before the scan view subscribes to the window signal.
The default handler sees the suspended policy before the scan view cancels.
[Godot window setup](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/main/scene_tree.cpp#L2052)
[Godot Back handler](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/main/scene_tree.cpp#L837)

## Native boundaries and limitations

The Eventa contracts carry PNG base64 fields and nullable text results.
Godot images and camera frames stay in the host.

Android uses the bundled ML Kit model.
It does not offer a model download mode or require an installed GMS runtime.
Its official transitive `play-services-*` client libraries remain dependencies.
Device tests must verify offline scans on a device without GMS.
[ML Kit documentation](https://developers.google.com/ml-kit/vision/barcode-scanning/android)

The upstream Android error dictionary contains a Java enum.
The host reads its `ordinal()` through Godot's JavaObject wrapper.
The iOS error dictionary contains an integer.
[Upstream error type](https://github.com/godot-mobile-plugins/godot-qr/blob/v1.2/android/src/main/java/org/godotengine/plugin/qr/model/ScanError.java)
[Godot JNI conversion](https://github.com/godotengine/godot/blob/4.7/platform/android/jni_utils.cpp#L447)

The upstream Android QR scanner does not explicitly close each ML Kit scanner.
This package preserves the upstream binary and does not fix that limitation.
NativeCamera does not provide a camera-start error signal.
Some capture errors only reach native logs, so the scan can remain open until cancellation.
The upstream iOS release contains device slices only.
Camera behavior requires device validation.

NativeCamera queues frame signals on each platform's UI thread without a session ID.
A frame can remain in that native queue after `stop()` returns.
If the native plugin emits that frame after another scan subscribes, the host cannot identify its original session.
This residual restart race requires an upstream session ID or a stop acknowledgment to eliminate.
[Android frame dispatch](https://github.com/godot-mobile-plugins/godot-native-camera/blob/v3.0/android/src/main/java/org/godotengine/plugin/nativecamera/NativeCameraPlugin.java#L335)
[iOS frame dispatch](https://github.com/godot-mobile-plugins/godot-native-camera/blob/v3.0/ios/src/NativeCamera.swift#L294)

## Sources

- [godot-qr v1.2](https://github.com/godot-mobile-plugins/godot-qr/tree/v1.2)
- [NativeCamera v3.0](https://github.com/godot-mobile-plugins/godot-native-camera/tree/v3.0)

## Runtime tests

The headless harness registers fake native singletons in Godot.
It verifies the JSON Eventa PNG request and response path through the published core adapter.
It verifies generation, synchronous callbacks, camera misses, frame backpressure, cancellation, late callbacks, permission-dialog pause and resume, background entry, and scene exit.
It does not verify native camera capture or ML Kit recognition.

```sh
mise x dotnet@10.0.300 -- dotnet build packages/GdKirie.Qr/tests/QrRuntimeTests.csproj
mise x godot@4.7.2-stable_mono -- godot --headless --path packages/GdKirie.Qr/tests
```
