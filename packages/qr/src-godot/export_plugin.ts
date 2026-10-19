@tool
export class _ExportPlugin extends EditorExportPlugin {
  _get_name(): string {
    return "KirieQr";
  }

  _supports_platform(platform: EditorExportPlatform): boolean {
    const os_name = platform.get_os_name().to_lower();
    return os_name === "android" || os_name === "ios";
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

  _export_begin(features: PackedStringArray, is_debug: boolean, _path: string, _flags: int): void {
    if (!features.has("ios")) {
      return;
    }

    const mode = is_debug ? "debug" : "release";

    for (const framework of [
      `res://addons/kirie_qr/vendor/qr-v1.2/ios/QRPlugin.${mode}.xcframework`,
      `res://addons/kirie_qr/vendor/camera-v3.0/ios/NativeCameraPlugin.${mode}.xcframework`,
    ]) {
      assert(DirAccess.dir_exists_absolute(framework), `iOS framework not found: ${framework}`);
      this.add_apple_embedded_platform_framework(framework);
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
    // Preserve Godot's initializer chain for other enabled iOS plugins.
    this.add_apple_embedded_platform_cpp_code(`
extern void qr_plugin_init();
extern void qr_plugin_deinit();
extern void native_camera_plugin_init();
extern void native_camera_plugin_deinit();
void kirie_qr_generated_plugin_initialize();
void kirie_qr_generated_plugin_deinitialize();

void godot_apple_embedded_plugins_initialize() {
  qr_plugin_init();
  native_camera_plugin_init();
  kirie_qr_generated_plugin_initialize();
}

void godot_apple_embedded_plugins_deinitialize() {
  kirie_qr_generated_plugin_deinitialize();
  native_camera_plugin_deinit();
  qr_plugin_deinit();
}

#undef godot_apple_embedded_plugins_initialize
#undef godot_apple_embedded_plugins_deinitialize
#define godot_apple_embedded_plugins_initialize kirie_qr_generated_plugin_initialize
#define godot_apple_embedded_plugins_deinitialize kirie_qr_generated_plugin_deinitialize
`);
  }
}
