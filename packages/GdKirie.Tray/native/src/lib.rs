// Godot loads the native object SDK through GDExtension.
mod events;
mod icons;
mod item;
mod menu;
mod ownership;
mod predefined;
mod tray;
mod types;

pub use item::KirieTrayItem;
pub use menu::KirieTrayMenu;
pub use tray::KirieTrayIcon;

use godot::{init::InitStage, prelude::*};

struct KirieTrayExtension;

#[gdextension]
unsafe impl ExtensionLibrary for KirieTrayExtension {
    fn on_stage_init(stage: InitStage) {
        if stage == InitStage::Scene {
            events::install_handlers();
        }
    }

    fn on_main_loop_frame() {
        events::pump();
    }
}
