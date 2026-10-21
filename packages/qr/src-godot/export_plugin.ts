interface QrExportOptionOverrides {
  "plugins/QRPlugin"?: boolean;
  "plugins/NativeCameraPlugin"?: boolean;
}

@tool
export class _ExportPlugin extends EditorExportPlugin {
  _get_name(): string {
    return "KirieQr";
  }

  _supports_platform(platform: EditorExportPlatform): boolean {
    const os_name = platform.get_os_name().to_lower();
    return os_name === "android" || os_name === "ios";
  }

  _get_export_options_overrides(platform: EditorExportPlatform): QrExportOptionOverrides {
    if (platform.get_os_name().to_lower() !== "ios") {
      return {};
    }

    return {
      "plugins/QRPlugin": true,
      "plugins/NativeCameraPlugin": true,
    };
  }

  _get_android_libraries(_platform: EditorExportPlatform | null, debug: boolean): PackedStringArray {
    const mode = debug ? "debug" : "release";

    const qr_library = `kirie_qr/vendor/qr-v1.2/android/${mode}/QRPlugin-${mode}.aar`;
    const camera_library = `kirie_qr/vendor/camera-v3.0/android/${mode}/NativeCameraPlugin-${mode}.aar`;
    assert(FileAccess.file_exists(`res://addons/${qr_library}`), `Android library not found: ${qr_library}`);
    assert(FileAccess.file_exists(`res://addons/${camera_library}`), `Android library not found: ${camera_library}`);
    return PackedStringArray([qr_library, camera_library]);
  }

  _get_android_dependencies(_platform: EditorExportPlatform | null, _debug: boolean): PackedStringArray {
    return PackedStringArray([
      "androidx.appcompat:appcompat:1.7.1",
      "com.google.mlkit:barcode-scanning:17.3.0",
      "com.google.zxing:core:3.5.4",
    ]);
  }

  _export_begin(features: PackedStringArray, _is_debug: boolean, _path: string, _flags: int): void {
    const notices_path = "res://addons/kirie_qr/THIRD_PARTY_NOTICES.txt";
    this.add_file(notices_path, FileAccess.get_file_as_bytes(notices_path), false);

    if (!features.has("ios")) {
      return;
    }

    for (const system_framework of [
      "Foundation.framework",
      "UIKit.framework",
      "AudioToolbox.framework",
      "AVFoundation.framework",
      "CoreImage.framework",
      "CoreMedia.framework",
      "CoreVideo.framework",
    ]) {
      this.add_apple_embedded_platform_framework(system_framework);
    }

    this.add_apple_embedded_platform_linker_flags("-ObjC");
  }
}
