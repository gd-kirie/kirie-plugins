use crate::types::{Error, IconSource, Result};
use godot::{
    classes::{ResourceLoader, Texture2D, image::Format},
    prelude::*,
};
use tray_icon::{
    Icon, TrayIcon,
    menu::{Icon as MenuIcon, MenuItemKind, NativeIcon},
};

pub enum IconValue {
    Custom(IconSource),
    Native(NativeIcon),
}

pub fn classify(source: IconSource) -> Result<IconValue> {
    match source {
        IconSource::Native { name } => Ok(IconValue::Native(native(&name)?)),
        IconSource::NativeName { name } => {
            #[cfg(not(target_os = "windows"))]
            {
                Ok(IconValue::Native(NativeIcon::from_name(name)))
            }
            #[cfg(target_os = "windows")]
            {
                let _ = name;
                Err(Error::unsupported(
                    "native icon names are unavailable on Windows; use windows-stock",
                ))
            }
        }
        IconSource::WindowsStock { id } => {
            #[cfg(target_os = "windows")]
            {
                Ok(IconValue::Native(NativeIcon::from_id(id)))
            }
            #[cfg(not(target_os = "windows"))]
            {
                let _ = id;
                Err(Error::unsupported("Windows stock icons require Windows"))
            }
        }
        source => Ok(IconValue::Custom(source)),
    }
}

fn pixels(source: IconSource) -> Result<(Vec<u8>, u32, u32)> {
    let (rgba, width, height) = match source {
        IconSource::Rgba {
            rgba,
            width,
            height,
        } => (rgba, width, height),
        IconSource::Resource { path } => {
            let resource = ResourceLoader::singleton()
                .load(path.as_str())
                .ok_or_else(|| {
                    Error::new(
                        "invalid-argument",
                        format!("cannot load texture resource: {path}"),
                    )
                })?;
            let texture = resource
                .try_cast::<Texture2D>()
                .map_err(|_| Error::new("invalid-argument", "icon resource must be Texture2D"))?;
            let mut image = texture
                .get_image()
                .ok_or_else(|| Error::new("invalid-argument", "texture has no image"))?;
            if image.is_compressed() {
                let error = image.decompress();
                if error != godot::global::Error::OK {
                    return Err(Error::new(
                        "native-error",
                        format!("cannot decompress icon texture: {error:?}"),
                    ));
                }
            }
            image.clear_mipmaps();
            image.convert(Format::RGBA8);
            (
                image.get_data().to_vec(),
                image.get_width() as u32,
                image.get_height() as u32,
            )
        }
        _ => {
            return Err(Error::new(
                "invalid-argument",
                "expected RGBA or Godot texture icon",
            ));
        }
    };
    // Upstream multiplies u32 dimensions before validating the pixel count.
    width
        .checked_mul(height)
        .ok_or_else(|| Error::new("invalid-argument", "icon dimensions overflow u32"))?;
    Ok((rgba, width, height))
}

pub fn tray(source: IconSource) -> Result<Icon> {
    match source {
        IconSource::WindowsPath {
            path,
            width,
            height,
        } => {
            #[cfg(target_os = "windows")]
            {
                Icon::from_path(path, size(width, height)?)
                    .map_err(|error| Error::new("native-error", error))
            }
            #[cfg(not(target_os = "windows"))]
            {
                let _ = (path, width, height);
                Err(Error::unsupported("Windows file icons require Windows"))
            }
        }
        IconSource::WindowsResource {
            ordinal,
            width,
            height,
        } => {
            #[cfg(target_os = "windows")]
            {
                Icon::from_resource(ordinal, size(width, height)?)
                    .map_err(|error| Error::new("native-error", error))
            }
            #[cfg(not(target_os = "windows"))]
            {
                let _ = (ordinal, width, height);
                Err(Error::unsupported("Windows resource icons require Windows"))
            }
        }
        IconSource::WindowsResourceName {
            name,
            width,
            height,
        } => {
            #[cfg(target_os = "windows")]
            {
                Icon::from_resource_name(&name, size(width, height)?)
                    .map_err(|error| Error::new("native-error", error))
            }
            #[cfg(not(target_os = "windows"))]
            {
                let _ = (name, width, height);
                Err(Error::unsupported("Windows resource icons require Windows"))
            }
        }
        source => {
            let (rgba, width, height) = pixels(source)?;
            Icon::from_rgba(rgba, width, height)
                .map_err(|error| Error::new("invalid-argument", error))
        }
    }
}

pub fn menu(source: IconSource) -> Result<MenuIcon> {
    match source {
        IconSource::WindowsPath {
            path,
            width,
            height,
        } => {
            #[cfg(target_os = "windows")]
            {
                MenuIcon::from_path(path, size(width, height)?)
                    .map_err(|error| Error::new("native-error", error))
            }
            #[cfg(not(target_os = "windows"))]
            {
                let _ = (path, width, height);
                Err(Error::unsupported("Windows file icons require Windows"))
            }
        }
        IconSource::WindowsResource {
            ordinal,
            width,
            height,
        } => {
            #[cfg(target_os = "windows")]
            {
                MenuIcon::from_resource(ordinal, size(width, height)?)
                    .map_err(|error| Error::new("native-error", error))
            }
            #[cfg(not(target_os = "windows"))]
            {
                let _ = (ordinal, width, height);
                Err(Error::unsupported("Windows resource icons require Windows"))
            }
        }
        IconSource::WindowsResourceName {
            name,
            width,
            height,
        } => {
            let _ = (name, width, height);
            Err(Error::unsupported(
                "muda supports ordinal resource icons, not named resource icons",
            ))
        }
        source => {
            let (rgba, width, height) = pixels(source)?;
            MenuIcon::from_rgba(rgba, width, height)
                .map_err(|error| Error::new("invalid-argument", error))
        }
    }
}

#[cfg(target_os = "windows")]
fn size(width: Option<u32>, height: Option<u32>) -> Result<Option<(u32, u32)>> {
    match (width, height) {
        (None, None) => Ok(None),
        (Some(width), Some(height)) => Ok(Some((width, height))),
        _ => Err(Error::new(
            "invalid-argument",
            "icon size requires both width and height",
        )),
    }
}

fn native(name: &str) -> Result<NativeIcon> {
    const ICONS: &[(&str, NativeIcon)] = &[
        ("Add", NativeIcon::Add),
        ("Advanced", NativeIcon::Advanced),
        ("Bluetooth", NativeIcon::Bluetooth),
        ("Bookmarks", NativeIcon::Bookmarks),
        ("Caution", NativeIcon::Caution),
        ("ColorPanel", NativeIcon::ColorPanel),
        ("ColumnView", NativeIcon::ColumnView),
        ("Computer", NativeIcon::Computer),
        ("EnterFullScreen", NativeIcon::EnterFullScreen),
        ("Everyone", NativeIcon::Everyone),
        ("ExitFullScreen", NativeIcon::ExitFullScreen),
        ("FlowView", NativeIcon::FlowView),
        ("Folder", NativeIcon::Folder),
        ("FolderBurnable", NativeIcon::FolderBurnable),
        ("FolderSmart", NativeIcon::FolderSmart),
        ("FollowLinkFreestanding", NativeIcon::FollowLinkFreestanding),
        ("FontPanel", NativeIcon::FontPanel),
        ("GoLeft", NativeIcon::GoLeft),
        ("GoRight", NativeIcon::GoRight),
        ("Home", NativeIcon::Home),
        ("IChatTheater", NativeIcon::IChatTheater),
        ("IconView", NativeIcon::IconView),
        ("Info", NativeIcon::Info),
        (
            "InvalidDataFreestanding",
            NativeIcon::InvalidDataFreestanding,
        ),
        ("LeftFacingTriangle", NativeIcon::LeftFacingTriangle),
        ("ListView", NativeIcon::ListView),
        ("LockLocked", NativeIcon::LockLocked),
        ("LockUnlocked", NativeIcon::LockUnlocked),
        ("MenuMixedState", NativeIcon::MenuMixedState),
        ("MenuOnState", NativeIcon::MenuOnState),
        ("MobileMe", NativeIcon::MobileMe),
        ("MultipleDocuments", NativeIcon::MultipleDocuments),
        ("Network", NativeIcon::Network),
        ("Path", NativeIcon::Path),
        ("PreferencesGeneral", NativeIcon::PreferencesGeneral),
        ("QuickLook", NativeIcon::QuickLook),
        ("RefreshFreestanding", NativeIcon::RefreshFreestanding),
        ("Refresh", NativeIcon::Refresh),
        ("Remove", NativeIcon::Remove),
        ("RevealFreestanding", NativeIcon::RevealFreestanding),
        ("RightFacingTriangle", NativeIcon::RightFacingTriangle),
        ("Share", NativeIcon::Share),
        ("Slideshow", NativeIcon::Slideshow),
        ("SmartBadge", NativeIcon::SmartBadge),
        ("StatusAvailable", NativeIcon::StatusAvailable),
        ("StatusNone", NativeIcon::StatusNone),
        (
            "StatusPartiallyAvailable",
            NativeIcon::StatusPartiallyAvailable,
        ),
        ("StatusUnavailable", NativeIcon::StatusUnavailable),
        (
            "StopProgressFreestanding",
            NativeIcon::StopProgressFreestanding,
        ),
        ("StopProgress", NativeIcon::StopProgress),
        ("TrashEmpty", NativeIcon::TrashEmpty),
        ("TrashFull", NativeIcon::TrashFull),
        ("User", NativeIcon::User),
        ("UserAccounts", NativeIcon::UserAccounts),
        ("UserGroup", NativeIcon::UserGroup),
        ("UserGuest", NativeIcon::UserGuest),
    ];
    ICONS
        .iter()
        .find(|(candidate, _)| *candidate == name)
        .map(|(_, icon)| icon.clone())
        .ok_or_else(|| Error::new("invalid-argument", format!("unknown native icon: {name}")))
}

pub fn set_tray(tray: &TrayIcon, source: Option<IconSource>, template: bool) -> Result<()> {
    if template && !cfg!(target_os = "macos") {
        return Err(Error::unsupported("template icons require macOS"));
    }
    match source {
        None => apply_tray_icon(tray, None, template),
        Some(source) => match classify(source)? {
            IconValue::Native(icon) => tray.set_native_icon(Some(icon)).map_err(Into::into),
            IconValue::Custom(source) => {
                apply_tray_icon(tray, Some(crate::icons::tray(source)?), template)
            }
        },
    }
}

fn apply_tray_icon(tray: &TrayIcon, icon: Option<Icon>, template: bool) -> Result<()> {
    #[cfg(target_os = "macos")]
    if template {
        return tray.set_icon_templated(icon).map_err(Into::into);
    }
    #[cfg(not(target_os = "macos"))]
    let _ = template;
    tray.set_icon(icon).map_err(Into::into)
}

pub fn set_item(item: &MenuItemKind, source: Option<IconSource>, template: bool) -> Result<()> {
    if !matches!(item, MenuItemKind::Icon(_) | MenuItemKind::Submenu(_)) {
        return Err(Error::new(
            "invalid-argument",
            "item must be an icon item or submenu",
        ));
    }
    if template && !cfg!(target_os = "macos") {
        return Err(Error::unsupported("template icons require macOS"));
    }
    match source {
        None => {
            apply_item_icon(item, None, template);
            Ok(())
        }
        Some(source) => match classify(source)? {
            IconValue::Native(icon) => {
                match item {
                    MenuItemKind::Icon(item) => item.set_native_icon(Some(icon)),
                    MenuItemKind::Submenu(item) => item.set_native_icon(Some(icon)),
                    _ => unreachable!(),
                }
                Ok(())
            }
            IconValue::Custom(source) => {
                apply_item_icon(item, Some(crate::icons::menu(source)?), template);
                Ok(())
            }
        },
    }
}

fn apply_item_icon(item: &MenuItemKind, icon: Option<MenuIcon>, template: bool) {
    #[cfg(target_os = "macos")]
    if template {
        match item {
            MenuItemKind::Icon(item) => item.set_icon_templated(icon),
            MenuItemKind::Submenu(item) => item.set_icon_templated(icon),
            _ => unreachable!(),
        }
        return;
    }
    #[cfg(not(target_os = "macos"))]
    let _ = template;
    match item {
        MenuItemKind::Icon(item) => item.set_icon(icon),
        MenuItemKind::Submenu(item) => item.set_icon(icon),
        _ => unreachable!(),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn protects_upstream_arithmetic_and_preserves_pixel_errors() {
        assert!(
            pixels(IconSource::Rgba {
                rgba: vec![],
                width: u32::MAX,
                height: u32::MAX,
            })
            .is_err()
        );

        let error = tray(IconSource::Rgba {
            rgba: vec![0; 3],
            width: 1,
            height: 1,
        })
        .unwrap_err();
        assert_eq!(
            error.message,
            Icon::from_rgba(vec![0; 3], 1, 1).unwrap_err().to_string()
        );
    }
}
