# `@gd-kirie/qr`

This optional Kirie plugin generates QR images and reads QR codes on Android and iOS.
The camera stops after the first result. User cancellation returns `null`.
Permission errors and native errors reject the request.

## Install

The QR packages and required core visibility change await a coordinated release.
Published core `0.14.2` does not provide the camera preview's visibility behavior.

After the coordinated release, install the browser package:

```sh
pnpm add @gd-kirie/qr
```

Add the plugin to `kirie.config.ts`:

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
Set `foreground` and `background` with HTML hexadecimal color strings.

`scanImage` accepts PNG bytes. It returns the decoded text or `null` when the image contains no QR code.
It does not open a camera or image picker.

`scan` requests camera access and shows a Godot preview with a cancel button.
The host stops capture and restores the previous WebView visibility before it returns the result.
Concurrent recognition calls reject instead of queuing.

The JSON Eventa protocol carries explicit PNG base64 fields. Camera frames remain in the Godot host.
The client does not create another Eventa context.

See the [native dependencies and limitations](../GdKirie.Qr/README.md#native-dependencies-and-limitations) before deployment.
