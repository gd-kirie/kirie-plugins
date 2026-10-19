# `@gd-kirie/qr`

This optional Kirie plugin generates QR images and reads QR codes on Android and iOS.
The camera stops after the first result. User cancellation returns `null`.
Permission errors and native errors reject the request.

## Install

Add the plugin to your Kirie configuration:

```ts
import qr from "@gd-kirie/qr";
import { defineKirieConfig } from "kirie";
import core from "kirie/plugin/core";

export default defineKirieConfig({ plugins: [core, qr] });
```

Install the addon and NuGet dependencies:

```sh
pnpm kirie doctor --fix plugin:qr
```

In Godot, enable **Kirie QR** under **Project Settings > Plugins**.
For Android, enable Gradle export. For iOS, enter the camera usage description in the export preset.
The wrapper export plugin installs the native dependencies. Do not enable the upstream editor plugins separately.

Register and attach `GdKirie.Qr` as described in its [README](../GdKirie.Qr/README.md).
The host borrows the existing scene owner, KirieNode, and Eventa context.
Camera preview requires the core version with native KirieNode visibility forwarding.
Published core version `0.14.2` does not include this change.
Camera installation awaits a coordinated core and QR release.

## Browser API

Create the client from the application's Eventa context:

```ts
import { createQrClient } from "@gd-kirie/qr";

const qr = createQrClient(eventa.context);
const png = await qr.generateImage({ text: "Hello", size: 512 });
const imageText = await qr.scanImage(png);
const cameraText = await qr.scan();
```

`generateImage` returns PNG bytes. Its default size is 512 pixels.
The default foreground is black. The default background is white.
Set `foreground` and `background` with HTML color strings.

`scanImage` accepts PNG bytes. It returns the decoded text or `null` when the image contains no QR code.
It does not open a camera or image picker.

`scan` requests camera access and shows a Godot preview with a cancel button.
The host prefers a rear camera and requests 1280 by 720 grayscale frames with automatic orientation correction.
Only one native recognition request runs at a time. New camera analysis frames are discarded while recognition runs.
The host stops capture and restores the previous WebView visibility before it returns the result.
Each call returns one result. The plugin does not expose continuous scan modes or duplicate filters.

The JSON Eventa protocol carries explicit PNG base64 fields. Camera frames remain in the Godot host.
The client does not create another Eventa context.

## Native dependencies

The plugin installs unmodified [godot-qr v1.2](https://github.com/godot-mobile-plugins/godot-qr/tree/v1.2)
and [NativeCamera v3.0](https://github.com/godot-mobile-plugins/godot-native-camera/tree/v3.0) artifacts.
Release URLs and SHA-256 values are fixed in the plugin descriptor.
The package retains both MIT licenses under `addon/kirie_qr/licenses`.

Android uses the bundled ML Kit barcode model. It does not require an installed GMS service or an initial model download.
The bundled package includes official transitive Google client libraries. The plugin does not remove these libraries.
See the [ML Kit installation guide](https://developers.google.com/ml-kit/vision/barcode-scanning/android).

The upstream iOS archives require iOS 16 or later and contain device libraries only.
They do not contain simulator libraries.
The upstream Android scanner does not explicitly close its ML Kit scanner. This package does not patch that native implementation.
NativeCamera does not expose a camera-start error signal. The wrapper cannot report native start failures that upstream only logs.
NativeCamera also queues frames without a session ID. A queued native frame can reach a new scan after cancellation.
The host rejects callbacks from retired subscriptions, but cannot identify frames that upstream emits after the new subscription starts.
See the [host limitations](../GdKirie.Qr/README.md#native-boundaries-and-limitations) for source references.
Desktop and browser-only runtimes do not have these native plugins.

The [implementation plan](../../docs/qr-plugin-plan.md) records the boundaries and validation requirements.
