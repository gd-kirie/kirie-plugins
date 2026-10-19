# QR plugin plan

## Contract

The optional QR plugin supports Android and iOS. It reuses upstream godot-qr and NativeCamera binaries without source patches.
The browser and C# clients support QR generation, image recognition, and one camera scan per call.
The camera stops after the first decoded QR code. User cancellation returns no result. Permission and native errors remain errors.
The plugin borrows the application's Eventa context, scene owner, and KirieNode. It does not create another WebView.

## Dependencies

Use godot-qr v1.2 and NativeCamera v3.0. Pin release URLs and SHA-256 values in the plugin descriptor.
Install only the native artifact directories under `addons/kirie_qr/vendor`. Keep upstream licenses in the package.
The wrapper export plugin supplies the required Android dependencies and iOS initialization code.
Enable the wrapper editor plugin explicitly through Godot. The existing doctor command installs its files and NuGet package.

Android uses only `com.google.mlkit:barcode-scanning:17.3.0`, which includes the recognition model.
The bundled package retains its official transitive API dependencies. Do not exclude those dependencies.
The original QR AAR remains unchanged. The application does not offer an unbundled model option or model download management.

The upstream QR and NativeCamera iOS binaries require iOS 16 or later and contain device slices only.
Device validation is required. Simulator packaging is outside this change.
The upstream Android scanner has no explicit `close()` call. Record this limitation without adding an unapproved native patch.

## Packages

`packages/qr` contains the browser client, plugin descriptor, and generated Godot export integration.
`packages/GdKirie.Qr` contains the typed C# operations, native singleton adapter, scan UI, and Eventa registration.
`kirie-examples/examples/qr` demonstrates the public browser and C# APIs.
The example uses published core packages. It does not add an implicit dependency on a sibling checkout.

The browser API is:

```ts
interface QrGenerateOptions {
  text: string;
  size?: number;
  foreground?: string;
  background?: string;
}

interface QrClient {
  generateImage: (options: QrGenerateOptions) => Promise<Uint8Array>;
  scanImage: (png: Uint8Array) => Promise<string | null>;
  scan: () => Promise<string | null>;
}
```

Generation defaults are 512 pixels, black foreground, and white background.
The wire protocol uses explicit PNG base64 fields over the existing JSON Eventa context.
C# local operations also accept Godot Image values and return Image or ImageTexture values.
No engine object or live camera frame crosses browser IPC.

Wire IDs are `kirie:qr:generate-image`, `kirie:qr:scan-image`, and `kirie:qr:scan`.
Generation requests contain `text`, `size`, `foreground`, and `background` with explicit defaults.
Generation responses contain `pngBase64`. Image scan requests contain `pngBase64`.
Both scan responses contain nullable `text`. Camera scans use an empty request object.

## Camera behavior

Keep the upstream 1280 by 720 request and grayscale capture. Prefer a rear camera and enable automatic orientation correction.
Use one image recognition operation at a time. Discard new analysis frames while the recognizer is busy.
Start with no fixed analysis timer. Measure frame processing before introducing a rate limit.
The preview can update independently of recognition. Camera frame skipping remains an internal tuning value.
Do not add continuous scanning, duplicate filters, cooldowns, or a queue of camera frames.

Register pending scan state before native calls. The iOS native scanner can emit its result synchronously.
After cancellation, ignore and drain an outstanding native result before another operation starts.
Stop capture on success, cancellation, error, background entry, and scene exit.
Finish each scan exactly once. Restore the previous WebView visibility before returning the result.

Use a small Godot preview with a cancel button as the initial UI.
Core visibility forwarding makes KirieNode's effective visibility control its existing native WebView.
This forwarding includes initial state and ancestor visibility. It preserves the page and Eventa context.
No QR or camera capability enters the Kirie core API.

## Execution and validation

1. Implement effective WebView visibility in the core addon and native backends.
2. Implement the QR browser package and native export configuration.
3. Implement C# generation, image recognition, and the camera scan lifecycle.
4. Add the example and package documentation.
5. Run the relevant format, lint, build, type, and unit checks.
6. Build the core Android and iOS native artifacts.
7. Exercise the actual image request and response path through Godot and Eventa.
8. Verify camera scanning on Android without GMS and without a network connection.
9. Verify the iOS camera flow on a device.

Report unavailable device checks explicitly. Build success does not establish camera or model availability on a device.
Verify cancellation, late native callbacks, repeated scans, and cleanup during scene exit.
Verify that QR and Notifications compose their iOS initialization code.

## Implementation status

The browser package, C# host, export integration, core visibility forwarding, and example are implemented.
The changes are local. No package publication or release is part of this work.
Published core `0.14.2` lacks the new visibility behavior. Camera installation awaits a coordinated core and QR release.

Completed checks cover TypeScript types, ESLint, browser unit tests, .NET builds, and .NET analyzers.
The generated QR export script passes Godot's parser.
The core passes its language checks, Android AAR build, and five desktop integration tests.
Compiled C++ tests verify core, Notifications, and QR initialization in all six plugin orders.
The headless Godot harness verifies JSON Eventa PNG requests, synchronous results, cancellation, backpressure, permissions, Android Back, and scene exit.
These host tests use fake native singletons. They do not verify native QR recognition or camera capture.

The Android emulator and physical Android checks in the next sections pass with real native plugins.
The [iOS device results](#ios-device-preparation-2026-10-19) record camera recognition, cancellation, and background cleanup.
The iOS Debug and Release XCFramework builds pass with Xcode 27.1 and XcodeGen 2.46.0.
The example build also awaits publication of the new QR packages and updated core package pins.

NativeCamera queues frames without a session ID or a stop acknowledgment.
The host rejects callbacks from retired subscriptions, but cannot identify old frames emitted after a new subscription starts.
Eliminating that native restart race requires an upstream change.
The [host README](../packages/GdKirie.Qr/README.md#native-boundaries-and-limitations) records the native source references and other limitations.

## Android emulator validation: 2026-10-19

The test uses an AOSP API 36 ARM64 image, revision 2, with Android Emulator 37.1.11.
The system WebView version is `133.0.6943.137`.
`com.google.android.gms` is absent. Airplane mode is enabled, and Android reports no active default network.
The first camera test starts after application data is cleared, before the recognizer runs.

An isolated test application consumes the locally packed QR NuGet package and current core addon.
Its browser page uses the actual `createQrClient`, Eventa context, and native Android plugins.
The packaged, restored, and exported QR DLL hashes match.
The test leaves repository dependency pins and upstream AARs unchanged.
The application includes the bundled ML Kit model files.

| Check | Observed result |
| --- | --- |
| Core Android integration suite | All five tests pass, including WebView visibility and retained IPC |
| Fresh camera authorization | The request remains pending during the system dialog and starts capture after approval |
| Offline camera recognition without GMS | Returns `kirie-qr-emulator-offline-2026` and closes capture after the first result |
| PNG generation and recognition | Returns the original text, including Chinese characters; generated PNG contains 2742 bytes |
| Blank PNG | Returns `null` |
| Invalid PNG | Rejects with the host's readable PNG decode error |
| Repeated and concurrent camera requests | A later scan succeeds; a concurrent request rejects while the first scan completes |
| Android Back during capture | Returns `null`, restores the browser page, and keeps the application process alive |
| Home during capture | Closes capture while the application remains in the background; the scan returns `null` |
| Camera permission denial | Rejects with `Camera permission was denied.` and does not open a camera |
| Permission restored after denial | A new scan succeeds in the same application process |

Android's camera service reports an empty active-client list after successful scans, Back cancellation, and background cancellation.
The camera input uses the emulator's image-file camera with a generated QR test chart and a blank chart.
The stock camera also crops the image-file input. The QR chart repeats the same code so that complete codes remain visible.
These checks do not establish autofocus, lighting tolerance, or physical device compatibility.

The first test run exposed a permission lifecycle defect: the system permission activity caused premature scan cancellation.
The host now distinguishes a pending permission request from active capture and waits for foreground state before capture starts.
Regression tests cover permission and resume callbacks in both orders. Independent review found no further issue in this change.
The example also reads serialized Eventa error messages, which prevents `[object Object]` in its status field.

The QA application is temporary because the QR packages and core visibility change are not yet published.
Android exports produced working APKs but also emitted Godot's shutdown `EditorSettings` diagnostics.
The GDScript core integration fixture additionally reported unused C# addon files without a solution.
The camera tests use a real .NET application and the packaged QR library.

## Physical Android validation: 2026-10-19

The physical test uses a Xiaomi `25098PN5AC` device with Android 17, API 37, and ARM64.
The system WebView version is `155.0.8059.43`. Google Play services are installed on this device.
The test does not change network settings or disable Google Play services.
The AOSP emulator remains the evidence for offline operation without GMS.

The device installs an isolated QA application with the same packaged QR library and current core addon.
The physical-device APK removes the emulator's camera-frame capture diagnostic. It does not save camera frames.
The user points the rear camera at a computer screen with a generated QR image.
ZXing independently verifies the fixture text before the camera test.

| Check | Observed result |
| --- | --- |
| Initial camera authorization | The system grants permission and capture starts without premature cancellation |
| Physical rear-camera recognition | Returns `kirie-qr-real-device-2026-10-19`; later scans return the same fixture text |
| First-result cleanup | The camera service reports no active camera client after the result |
| Concurrent requests | The second request rejects while the first completes with the fixture text |
| PNG generation and recognition | Returns `Kirie QR device 中文 2026`; generated PNG contains 2738 bytes |
| Blank and invalid PNG inputs | The blank image returns `null`; invalid bytes reject with a readable PNG decode error |
| Android Back during active capture | Returns `null`, restores the page, releases the camera, and keeps the process alive |
| Home during active capture | Releases the camera before the application returns to the foreground; the scan returns `null` |
| Permission denial | Selecting **拒绝** returns `Camera permission was denied.` and leaves the camera closed |
| Permission restored after denial | Capture opens again in the same process; Back cancels it and releases the camera |

The test restores the QA application's camera permission after the denial check.
No source changes are necessary for this device. Other Android devices and iOS remain outside this physical test.

## Simplification experiment: 2026-10-19

The host now stores the native adapter in one operation object. It no longer synchronizes separate native-operation references.
NativeCamera requests omit five values that match both upstream platform defaults: rotation, horizontal mirror, vertical mirror, and two scale dimensions.
The request keeps zero frame skipping because Android defaults to one skipped frame.
The browser plugin descriptor defines each pinned archive URL and checksum once.
The example passes its ready client into each operation instead of repeating readiness checks.

Android Back now restores the saved policy immediately. The global handoff owner and deferred restoration callback were removed.
Godot connects its built-in Back handler before the scan view's signal handler.
The physical test confirms cancellation returns `null`, releases the camera, and preserves the process.
A later PNG request succeeds through the retained WebView.

Temporary copies remove one existing guard at a time. The original assertions fail when permission foreground handling,
frame backpressure, retired-subscription checks, or pending-native-result ownership is removed.
Removing chunked base64 encoding also fails with a stack overflow for a 1,000,000-byte image input.
These experiments support keeping those specific protections. They do not establish native device behavior by themselves.

The experiments add no permanent host test cases. Two proposed cases outside the current example's usage were discarded.
The existing large-image browser case now uses 1,000,000 bytes to exercise the actual encoding limit.

The final package passes the existing host harness, browser tests, language checks, and independent review.
The NuGet package, restored library, and exported Android library have matching hashes.
On the physical device, the final APK passes PNG generation and recognition, Back cancellation, and background cancellation.
Both cancellation paths release the camera and retain the application process and WebView.
This simplification adds no iOS camera evidence.

## iOS device preparation: 2026-10-19

The native core, integration fixture, and application examples now require iOS 16 or later.
The upstream QR and NativeCamera binaries also target iOS 16.
XcodeGen 2.46.0 is pinned through mise. The local .NET iOS workload is installed.

An isolated QA application passes NativeAOT compilation for device arm64 and both simulator architectures.
This managed compilation does not add simulator support to the upstream native plugins.
Xcode 27.1 builds and signs the device application through a free Personal Team.
The application installs and starts on an iPhone 16 Pro running iOS 27.0.1.
The native WebView loads the packaged browser page.
The QA application saves no camera frames.

The temporary QA page initially omitted its UTF-8 declaration. Adding `meta charset="UTF-8"` fixes its visible arrow characters.
The first device PNG roundtrip then exposes a separate upstream result-conversion defect.
For input `Kirie QR device 中文 2026`, the returned Chinese text is mojibake; the ASCII prefix and suffix remain intact.
The [iOS result bridge](https://github.com/godot-mobile-plugins/godot-qr/blob/v1.2/ios/src/qr_plugin.mm#L50) passes UTF-8 bytes directly into a Godot Variant.
Godot's `const char *` conversion uses Latin-1; this bridge needs explicit UTF-8 decoding.
The upstream source and packaged binaries remain unchanged. The proposed local patch was discarded at the user's request.
Continue device validation with English ASCII payloads. Unicode result handling remains a known upstream limitation.

The unmodified iOS libraries pass PNG generation and recognition for `Kirie QR device test 2026` (2975 PNG bytes).
A blank PNG returns `null`; invalid PNG bytes reject with a readable decode error.
The first camera permission request succeeds, but the preview is black and no code is returned.
The cancel button returns `null` and restores the page.
One diagnostic run terminates with signal 9 without user action. The termination cause remains unconfirmed.

A temporary application calls NativeCamera directly, without WebView, preview, or QR recognition.
It receives 82 frames in three seconds and stops through a Godot timer.
The actual output is 1080 × 1920, despite the 1280 × 720 request.
With Device Hub mirroring active, every pixel has value 16, even with an unobstructed lens.
After Device Hub exits, the same application receives normal pixels and recognizes `kirie-qr-real-device-2026-10-19` in approximately 51 milliseconds.
The application stops capture before this single recognition. It saves no camera images.
Keep Device Hub mirroring closed during physical camera tests on this setup.

The adapter also replaces obsolete queued frames before image conversion and preview upload.
It processes the latest frame once per Godot render iteration and releases owned dictionaries and buffer Variants deterministically.
The [Godot 4.7.2 Variant conversion](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/VariantUtils.cs#L573-L580)
creates an independently owned dictionary wrapper for each callback.
Release and Debug builds, C# analyzers, and the existing headless QR runtime suite pass after this change.
With mirroring closed, the complete Kirie flow returns `kirie-qr-real-device-2026-10-19` through browser IPC.
The scanner closes after the first result and restores the WebView. The application remains alive.
This device build contains no frame observer or pixel-statistics diagnostic.
The user also confirms normal preview during approximately 30 seconds without a QR code.
Selecting **Cancel** returns `null` through IPC and restores the WebView without application exit.
Moving the application to the background also returns `null` and restores the WebView after foreground return.
The user confirms that the camera indicator disappears. The device process keeps the same PID throughout these tests.

## Sources

- [godot-qr v1.2](https://github.com/godot-mobile-plugins/godot-qr/tree/v1.2)
- [NativeCamera v3.0](https://github.com/godot-mobile-plugins/godot-native-camera/tree/v3.0)
- [NativeCamera Android request defaults](https://github.com/godot-mobile-plugins/godot-native-camera/blob/v3.0/android/src/main/java/org/godotengine/plugin/nativecamera/model/FeedRequest.java)
- [NativeCamera iOS request defaults](https://github.com/godot-mobile-plugins/godot-native-camera/blob/v3.0/ios/src/model/frame_request.mm)
- [Godot SceneTree Back handler and registration](https://github.com/godotengine/godot/blob/4.7.2-stable/scene/main/scene_tree.cpp)
- [ML Kit barcode scanning](https://developers.google.com/ml-kit/vision/barcode-scanning/android)
- [Bundled dependency metadata](https://dl.google.com/dl/android/maven2/com/google/mlkit/barcode-scanning/17.3.0/barcode-scanning-17.3.0.pom)
- [quickie scanner lifecycle](https://github.com/G00fY2/quickie)
- [AOSP system images](https://developer.android.com/studio/run/managing-avds)
- [Emulator camera inputs](https://developer.android.com/studio/run/emulator-commandline)
