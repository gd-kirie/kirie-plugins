import type { KirieEventaContext } from "@gd-kirie/ipc-eventa";
import type { KiriePlugin } from "kirie/plugin";
import { defineInvokeEventa, defineInvokes } from "@moeru/eventa";
import { defineKiriePlugin } from "kirie/plugin";

import packageJson from "../package.json" with { type: "json" };

export interface QrGenerateOptions {
  text: string;
  size?: number;
  foreground?: string;
  background?: string;
}

export interface QrClient {
  generateImage: (options: QrGenerateOptions) => Promise<Uint8Array>;
  scanImage: (png: Uint8Array) => Promise<string | null>;
  scan: () => Promise<string | null>;
}

interface PngPayload {
  pngBase64: string;
}

interface ScanResult {
  text: string | null;
}

const generateImage = defineInvokeEventa<PngPayload, Required<QrGenerateOptions>>("kirie:qr:generate-image");
const scanImage = defineInvokeEventa<ScanResult, PngPayload>("kirie:qr:scan-image");
const scan = defineInvokeEventa<ScanResult, Record<string, never>>("kirie:qr:scan");

function encodePng(bytes: Uint8Array): string {
  let binary = "";
  for (let offset = 0; offset < bytes.length; offset += 0x8000) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + 0x8000));
  }
  return btoa(binary);
}

function decodePng(base64: string): Uint8Array {
  return Uint8Array.from(atob(base64), (character) => character.charCodeAt(0));
}

/** Creates a QR client that borrows the application's Eventa context. */
export function createQrClient(context: KirieEventaContext): QrClient {
  const invoke = defineInvokes(context, { generateImage, scanImage, scan });
  return {
    async generateImage(options) {
      const result = await invoke.generateImage({
        text: options.text,
        size: options.size ?? 512,
        foreground: options.foreground ?? "#000000",
        background: options.background ?? "#ffffff",
      });
      return decodePng(result.pngBase64);
    },
    async scanImage(png) {
      return (await invoke.scanImage({ pngBase64: encodePng(png) })).text;
    },
    async scan() {
      return (await invoke.scan({})).text;
    },
  };
}

const qrArchive = {
  sha256: "be220c740f152ee8d21865b3d83ad11b6066cb8abb1e542ffb85f28a2c0d37ad",
  url: "https://github.com/godot-mobile-plugins/godot-qr/releases/download/v1.2/QRPlugin-Multi-v1.2.zip",
};

const cameraArchive = {
  sha256: "6aa946cbe5af269d107aa0069a3291a64522fb78a2e15e17180bb83d6cfebc32",
  url: "https://github.com/godot-mobile-plugins/godot-native-camera/releases/download/v3.0/NativeCameraPlugin-Multi-v3.0.zip",
};

const qr: KiriePlugin = defineKiriePlugin({
  id: "qr",
  dotnetPackages: [{ id: "GdKirie.Qr", version: packageJson.version }],
  godotAddons: [
    {
      id: "kirie-qr",
      path: "addons/kirie_qr",
      source: { type: "bundled", url: new URL("../addon/kirie_qr", import.meta.url).href },
      version: packageJson.version,
    },
    {
      id: "godot-qr-android",
      path: "addons/kirie_qr/vendor/qr-v1.2/android",
      source: {
        ...qrArchive,
        type: "remote",
        archivePath: "res/addons/QRPlugin/bin",
      },
      version: "1.2",
    },
    {
      id: "godot-qr-ios",
      path: "addons/kirie_qr/vendor/qr-v1.2/ios",
      source: {
        ...qrArchive,
        type: "remote",
        archivePath: "res/ios/plugins",
      },
      version: "1.2",
    },
    {
      id: "godot-native-camera-android",
      path: "addons/kirie_qr/vendor/camera-v3.0/android",
      source: {
        ...cameraArchive,
        type: "remote",
        archivePath: "res/addons/NativeCameraPlugin/bin",
      },
      version: "3.0",
    },
    {
      id: "godot-native-camera-ios",
      path: "addons/kirie_qr/vendor/camera-v3.0/ios",
      source: {
        ...cameraArchive,
        type: "remote",
        archivePath: "res/ios/plugins",
      },
      version: "3.0",
    },
  ],
});

export default qr;
