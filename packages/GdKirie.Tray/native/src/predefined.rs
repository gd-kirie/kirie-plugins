use crate::types::{Error, Result};
use tray_icon::menu::{self, PredefinedMenuItem};

pub fn predefined(
    kind: &str,
    text: Option<&str>,
    metadata: Option<menu::AboutMetadata>,
) -> Result<PredefinedMenuItem> {
    type Constructor = fn(Option<&str>) -> PredefinedMenuItem;
    const CONSTRUCTORS: &[(&str, Constructor)] = &[
        ("copy", PredefinedMenuItem::copy),
        ("cut", PredefinedMenuItem::cut),
        ("paste", PredefinedMenuItem::paste),
        (
            "paste_and_match_style",
            PredefinedMenuItem::paste_and_match_style,
        ),
        ("delete", PredefinedMenuItem::delete),
        ("select_all", PredefinedMenuItem::select_all),
        ("undo", PredefinedMenuItem::undo),
        ("redo", PredefinedMenuItem::redo),
        ("minimize", PredefinedMenuItem::minimize),
        ("maximize", PredefinedMenuItem::maximize),
        ("zoom", PredefinedMenuItem::zoom),
        ("actual_size", PredefinedMenuItem::actual_size),
        ("zoom_in", PredefinedMenuItem::zoom_in),
        ("zoom_out", PredefinedMenuItem::zoom_out),
        ("fullscreen", PredefinedMenuItem::fullscreen),
        ("hide", PredefinedMenuItem::hide),
        ("hide_others", PredefinedMenuItem::hide_others),
        ("show_all", PredefinedMenuItem::show_all),
        ("close_window", PredefinedMenuItem::close_window),
        ("quit", PredefinedMenuItem::quit),
        ("services", PredefinedMenuItem::services),
        ("bring_all_to_front", PredefinedMenuItem::bring_all_to_front),
        ("start_speaking", PredefinedMenuItem::start_speaking),
        ("stop_speaking", PredefinedMenuItem::stop_speaking),
        ("start_dictation", PredefinedMenuItem::start_dictation),
        ("emoji_and_symbols", PredefinedMenuItem::emoji_and_symbols),
    ];
    if kind == "separator" {
        return Ok(PredefinedMenuItem::separator());
    }
    if kind == "section_header" {
        return text
            .map(PredefinedMenuItem::section_header)
            .ok_or_else(|| Error::new("invalid-argument", "section_header requires text"));
    }
    if kind == "about" {
        return Ok(PredefinedMenuItem::about(text, metadata));
    }
    CONSTRUCTORS
        .iter()
        .find(|(candidate, _)| *candidate == kind)
        .map(|(_, constructor)| constructor(text))
        .ok_or_else(|| {
            Error::new(
                "invalid-argument",
                format!("unknown predefined kind: {kind}"),
            )
        })
}
