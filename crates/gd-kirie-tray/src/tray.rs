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
    fn read(value: Option<Gd<RefCounted>>) -> Result<Option<Self>> {
        let Some(value) = value else {
            return Ok(None);
        };
        let value = match value.try_cast::<KirieTrayMenu>() {
            Ok(menu) => return Ok(Some(Self::Menu(menu))),
            Err(value) => value,
        };
        let item = value
            .try_cast::<KirieTrayItem>()
            .map_err(|_| Error::new("invalid-argument", "tray menu must be a menu or submenu"))?;
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
    fn create(
        icon: Variant,
        icon_as_template: bool,
        menu: Option<Gd<RefCounted>>,
        options: VarDictionary,
    ) -> VarDictionary {
        reply(|| {
            let icon = IconSource::read(&icon)?;
            let menu = OwnedMenu::read(menu)?;

            if icon_as_template && !cfg!(target_os = "macos") {
                return Err(Error::unsupported("template icons require macOS"));
            }

            let mut builder = TrayIconBuilder::new();
            if let Some(id) = types::optional::<String>(&options, "id")? {
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
            if let Some(tooltip) = types::optional::<String>(&options, "tooltip")? {
                builder = builder.with_tooltip(tooltip);
            }
            if let Some(title) = types::optional::<String>(&options, "title")? {
                builder = builder.with_title(title);
            }
            if let Some(enabled) = types::optional::<bool>(&options, "menuOnLeftClick")? {
                builder = builder.with_menu_on_left_click(enabled);
            }
            if let Some(enabled) = types::optional::<bool>(&options, "menuOnRightClick")? {
                builder = builder.with_menu_on_right_click(enabled);
            }
            if let Some(guid) = types::optional::<String>(&options, "guid")? {
                builder = builder.with_guid(types::guid(&guid)?);
            }
            if let Some(name) = types::optional::<String>(&options, "autosaveName")? {
                builder = builder.with_autosave_name(name);
            }
            if let Some(path) = types::optional::<String>(&options, "tempDirPath")? {
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
    fn set_menu(&mut self, menu: Option<Gd<RefCounted>>) -> VarDictionary {
        reply(|| {
            let menu = OwnedMenu::read(menu)?;
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
