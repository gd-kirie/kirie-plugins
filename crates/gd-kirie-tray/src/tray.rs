use crate::{
    KirieTrayItem, KirieTrayMenu, events, icons,
    types::{self, Error, IconSource, Result, reply},
};
use godot::prelude::*;
use tray_icon::{
    TrayIcon, TrayIconBuilder,
    menu::{ContextMenu, MenuItemKind},
};

enum OwnedMenu {
    Menu(Gd<KirieTrayMenu>),
    Submenu(Gd<KirieTrayItem>),
}

impl OwnedMenu {
    fn read(value: &Variant) -> Result<Option<Self>> {
        if value.is_nil() {
            return Ok(None);
        }
        if let Ok(menu) = value.try_to::<Gd<KirieTrayMenu>>() {
            return Ok(Some(Self::Menu(menu)));
        }
        let item = value
            .try_to::<Gd<KirieTrayItem>>()
            .map_err(|error| Error::new("invalid-argument", error))?;
        Ok(Some(Self::Submenu(item)))
    }

    fn context_menu(&self) -> Result<Box<dyn ContextMenu>> {
        match self {
            Self::Menu(menu) => Ok(Box::new(menu.bind().native().clone())),
            Self::Submenu(item) => {
                let item = item.bind();
                let MenuItemKind::Submenu(menu) = item.native() else {
                    return Err(Error::new(
                        "invalid-argument",
                        "tray menu must be a menu or submenu",
                    ));
                };
                Ok(Box::new(menu.clone()))
            }
        }
    }
}

#[derive(GodotClass)]
#[class(no_init, base=RefCounted)]
pub struct KirieTrayIcon {
    tray: TrayIcon,
    // A tray's upstream menu reference also retains its corresponding Godot object.
    menu: Option<OwnedMenu>,
    godot: Base<RefCounted>,
}

#[godot_api]
impl KirieTrayIcon {
    #[func]
    fn create(options: VarDictionary) -> VarDictionary {
        reply(|| {
            let id = types::optional_string(&options, "id")?;
            let icon = IconSource::read(&options.get_or_nil("icon"))?;
            let icon_as_template = types::optional(&options, "iconAsTemplate")?.unwrap_or(false);
            let menu = OwnedMenu::read(&options.get_or_nil("menu"))?;
            let tooltip = types::optional_string(&options, "tooltip")?;
            let title = types::optional_string(&options, "title")?;
            let menu_on_left_click = types::optional(&options, "menuOnLeftClick")?;
            let menu_on_right_click = types::optional(&options, "menuOnRightClick")?;
            let guid = types::optional_string(&options, "guid")?;
            let autosave_name = types::optional_string(&options, "autosaveName")?;
            let temp_dir_path = types::optional_string(&options, "tempDirPath")?;

            if icon_as_template && !cfg!(target_os = "macos") {
                return Err(Error::unsupported("template icons require macOS"));
            }

            let mut builder = TrayIconBuilder::new();
            if let Some(id) = id {
                builder = builder.with_id(id);
            }
            if let Some(icon) = icon {
                builder = match icons::classify(icon)? {
                    icons::IconValue::Native(icon) => builder.with_native_icon(icon),
                    icons::IconValue::Custom(icon) => {
                        let icon = icons::tray(icon)?;
                        #[cfg(target_os = "macos")]
                        {
                            if icon_as_template {
                                builder.with_icon_templated(icon)
                            } else {
                                builder.with_icon(icon)
                            }
                        }
                        #[cfg(not(target_os = "macos"))]
                        {
                            builder.with_icon(icon)
                        }
                    }
                };
            }
            if let Some(menu) = &menu {
                builder = builder.with_menu(menu.context_menu()?);
            }
            if let Some(tooltip) = tooltip {
                builder = builder.with_tooltip(tooltip);
            }
            if let Some(title) = title {
                builder = builder.with_title(title);
            }
            if let Some(enabled) = menu_on_left_click {
                builder = builder.with_menu_on_left_click(enabled);
            }
            if let Some(enabled) = menu_on_right_click {
                builder = builder.with_menu_on_right_click(enabled);
            }
            if let Some(guid) = guid {
                builder = builder.with_guid(types::guid(&guid)?);
            }
            if let Some(name) = autosave_name {
                builder = builder.with_autosave_name(name);
            }
            if let Some(path) = temp_dir_path {
                builder = builder.with_temp_dir_path(path);
            }
            let tray = builder.build()?;
            Ok(Gd::from_init_fn(|godot| Self { tray, menu, godot }))
        })
    }

    #[func]
    fn id(&self) -> VarDictionary {
        reply(|| Ok(GString::from(self.tray.id().0.as_str())))
    }

    #[func]
    fn set_icon(&self, icon: Variant, icon_as_template: bool) -> VarDictionary {
        reply(|| icons::set_tray(&self.tray, IconSource::read(&icon)?, icon_as_template))
    }

    #[func]
    fn set_menu(&mut self, menu: Variant) -> VarDictionary {
        reply(|| {
            let menu = OwnedMenu::read(&menu)?;
            self.tray
                .set_menu(menu.as_ref().map(OwnedMenu::context_menu).transpose()?);
            self.menu = menu;
            Ok(())
        })
    }

    #[func]
    fn set_tooltip(&self, tooltip: Variant) -> VarDictionary {
        reply(|| {
            self.tray.set_tooltip(types::nullable_string(&tooltip)?)?;
            Ok(())
        })
    }

    #[func]
    fn set_title(&self, title: Variant) -> VarDictionary {
        reply(|| {
            let title = types::nullable_string(&title)?;
            self.tray.set_title(title);
            Ok(())
        })
    }

    #[func]
    fn set_visible(&self, visible: bool) -> VarDictionary {
        reply(|| {
            self.tray.set_visible(visible)?;
            Ok(())
        })
    }

    #[func]
    fn set_menu_on_left_click(&self, enabled: bool) -> VarDictionary {
        reply(|| {
            self.tray.set_show_menu_on_left_click(enabled);
            Ok(())
        })
    }

    #[func]
    fn set_menu_on_right_click(&self, enabled: bool) -> VarDictionary {
        reply(|| {
            self.tray.set_show_menu_on_right_click(enabled);
            Ok(())
        })
    }

    #[func]
    fn show_menu(&self) -> VarDictionary {
        reply(|| {
            self.tray.show_menu();
            Ok(())
        })
    }

    #[func]
    fn rect(&self) -> VarDictionary {
        reply(|| {
            Ok(self
                .tray
                .rect()
                .map(|rect| events::rectangle(rect).to_variant())
                .unwrap_or_else(Variant::nil))
        })
    }

    #[func]
    fn icon_is_template(&self) -> VarDictionary {
        reply(|| {
            let tray = &self.tray;
            #[cfg(target_os = "macos")]
            {
                Ok(tray.icon_is_template())
            }
            #[cfg(not(target_os = "macos"))]
            {
                let _ = tray;
                Err::<bool, _>(Error::unsupported("template icons require macOS"))
            }
        })
    }

    #[func]
    fn set_temp_dir_path(&self, path: Variant) -> VarDictionary {
        reply(|| {
            self.tray.set_temp_dir_path(types::nullable_string(&path)?);
            Ok(())
        })
    }
}
