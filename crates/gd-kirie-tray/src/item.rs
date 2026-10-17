use crate::{
    icons,
    ownership::{self, Children},
    types::{self, Accelerator, Error, IconSource, Result, reply},
};
use godot::prelude::*;
use tray_icon::menu::{
    CheckMenuItem, IconMenuItem, MenuItem, MenuItemKind, PredefinedMenuItem, Submenu,
};

#[derive(GodotClass)]
#[class(no_init, base=RefCounted)]
pub struct KirieTrayItem {
    item: MenuItemKind,
    children: Children,
    godot: Base<RefCounted>,
}

impl KirieTrayItem {
    pub fn native(&self) -> &MenuItemKind {
        &self.item
    }

    fn wrap(item: MenuItemKind, children: Children) -> Gd<Self> {
        Gd::from_init_fn(|godot| Self {
            item,
            children,
            godot,
        })
    }

    fn submenu(&self) -> Result<&Submenu> {
        match self.native() {
            MenuItemKind::Submenu(menu) => Ok(menu),
            _ => Err(Error::new("invalid-argument", "item must be a submenu")),
        }
    }

    fn finish(item: MenuItemKind, accelerator: Variant) -> Result<Gd<Self>> {
        let accelerator = Accelerator::read(&accelerator)?;
        if accelerator.is_some() {
            set_accelerator(&item, accelerator)?;
        }
        Ok(Self::wrap(item, Children::default()))
    }

    fn with_icon(item: MenuItemKind, icon: Variant, template: bool) -> Result<MenuItemKind> {
        let icon = IconSource::read(&icon)?;
        if icon.is_some() || template {
            icons::set_item(&item, icon, template)?;
        }
        Ok(item)
    }

    fn predefined(
        text: Variant,
        constructor: fn(Option<&str>) -> PredefinedMenuItem,
    ) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(constructor(text.as_deref())),
                Children::default(),
            ))
        })
    }
}

#[godot_api]
impl KirieTrayItem {
    // C# cannot name a GDExtension class as a generic Array<T> element type.
    #[func]
    fn item_array() -> Array<Gd<Self>> {
        Array::new()
    }

    #[func]
    fn create_item(
        id: Variant,
        text: GString,
        enabled: bool,
        accelerator: Variant,
    ) -> VarDictionary {
        reply(|| {
            let id = types::nullable_string(&id)?;
            let text = text.to_string();
            let item = MenuItemKind::MenuItem(match id {
                Some(id) => MenuItem::with_id(id, &text, enabled, None),
                None => MenuItem::new(&text, enabled, None),
            });
            Self::finish(item, accelerator)
        })
    }

    #[func]
    fn create_check(
        id: Variant,
        text: GString,
        enabled: bool,
        checked: bool,
        accelerator: Variant,
    ) -> VarDictionary {
        reply(|| {
            let id = types::nullable_string(&id)?;
            let text = text.to_string();
            let item = MenuItemKind::Check(match id {
                Some(id) => CheckMenuItem::with_id(id, &text, enabled, checked, None),
                None => CheckMenuItem::new(&text, enabled, checked, None),
            });
            Self::finish(item, accelerator)
        })
    }

    #[func]
    fn create_icon(
        id: Variant,
        text: GString,
        enabled: bool,
        accelerator: Variant,
        icon: Variant,
        icon_as_template: bool,
    ) -> VarDictionary {
        reply(|| {
            let id = types::nullable_string(&id)?;
            let text = text.to_string();
            let item = MenuItemKind::Icon(match id {
                Some(id) => IconMenuItem::with_id(id, &text, enabled, None, None),
                None => IconMenuItem::new(&text, enabled, None, None),
            });
            let item = Self::with_icon(item, icon, icon_as_template)?;
            Self::finish(item, accelerator)
        })
    }

    #[func]
    fn create_submenu(
        id: Variant,
        text: GString,
        enabled: bool,
        items: Vec<Gd<KirieTrayItem>>,
        icon: Variant,
        icon_as_template: bool,
    ) -> VarDictionary {
        reply(|| {
            let id = types::nullable_string(&id)?;
            let text = text.to_string();
            let native = ownership::prepare_objects(&items);
            let submenu = match id {
                Some(id) => Submenu::with_id(id, &text, enabled),
                None => Submenu::new(&text, enabled),
            };
            submenu.append_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>())?;
            let mut children = Children::default();
            children.reconcile(&submenu.items(), &items);
            let item = MenuItemKind::Submenu(submenu);
            let item = Self::with_icon(item, icon, icon_as_template)?;
            Ok(Self::wrap(item, children))
        })
    }

    #[func]
    fn separator() -> VarDictionary {
        reply(|| {
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::separator()),
                Children::default(),
            ))
        })
    }

    #[func]
    fn section_header(text: GString) -> VarDictionary {
        reply(|| {
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::section_header(&text.to_string())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn copy(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::copy)
    }

    #[func]
    fn cut(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::cut)
    }

    #[func]
    fn paste(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::paste)
    }

    #[func]
    fn paste_and_match_style(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::paste_and_match_style)
    }

    #[func]
    fn delete(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::delete)
    }

    #[func]
    fn select_all(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::select_all)
    }

    #[func]
    fn undo(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::undo)
    }

    #[func]
    fn redo(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::redo)
    }

    #[func]
    fn minimize(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::minimize)
    }

    #[func]
    fn maximize(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::maximize)
    }

    #[func]
    fn zoom(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::zoom)
    }

    #[func]
    fn actual_size(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::actual_size)
    }

    #[func]
    fn zoom_in(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::zoom_in)
    }

    #[func]
    fn zoom_out(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::zoom_out)
    }

    #[func]
    fn fullscreen(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::fullscreen)
    }

    #[func]
    fn hide(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::hide)
    }

    #[func]
    fn hide_others(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::hide_others)
    }

    #[func]
    fn show_all(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::show_all)
    }

    #[func]
    fn close_window(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::close_window)
    }

    #[func]
    fn quit(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::quit)
    }

    #[func]
    fn services(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::services)
    }

    #[func]
    fn bring_all_to_front(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::bring_all_to_front)
    }

    #[func]
    fn start_speaking(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::start_speaking)
    }

    #[func]
    fn stop_speaking(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::stop_speaking)
    }

    #[func]
    fn start_dictation(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::start_dictation)
    }

    #[func]
    fn emoji_and_symbols(text: Variant) -> VarDictionary {
        Self::predefined(text, PredefinedMenuItem::emoji_and_symbols)
    }

    #[func]
    fn about(text: Variant, metadata: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            let metadata = if metadata.is_nil() {
                None
            } else {
                Some(types::about_metadata(&metadata.try_to::<VarDictionary>()?)?)
            };
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::about(text.as_deref(), metadata)),
                Children::default(),
            ))
        })
    }

    #[func]
    fn id(&self) -> VarDictionary {
        reply(|| Ok(GString::from(self.native().id().0.as_str())))
    }

    #[func]
    fn kind(&self) -> VarDictionary {
        reply(|| {
            Ok(GString::from(match self.native() {
                MenuItemKind::MenuItem(_) => "item",
                MenuItemKind::Check(_) => "check",
                MenuItemKind::Icon(_) => "icon",
                MenuItemKind::Submenu(_) => "submenu",
                MenuItemKind::Predefined(_) => "predefined",
            }))
        })
    }

    #[func]
    fn text(&self) -> VarDictionary {
        reply(|| Ok(GString::from(item_text(self.native()).as_str())))
    }

    #[func]
    fn set_text(&self, text: GString) -> VarDictionary {
        reply(|| {
            set_text(self.native(), &text.to_string());
            Ok(())
        })
    }

    #[func]
    fn is_enabled(&self) -> VarDictionary {
        reply(|| is_enabled(self.native()))
    }

    #[func]
    fn set_enabled(&self, enabled: bool) -> VarDictionary {
        reply(|| set_enabled(self.native(), enabled))
    }

    #[func]
    fn is_checked(&self) -> VarDictionary {
        reply(|| {
            let MenuItemKind::Check(item) = self.native() else {
                return Err(Error::new("invalid-argument", "item must be a check item"));
            };
            Ok(item.is_checked())
        })
    }

    #[func]
    fn set_checked(&self, checked: bool) -> VarDictionary {
        reply(|| {
            let MenuItemKind::Check(item) = self.native() else {
                return Err(Error::new("invalid-argument", "item must be a check item"));
            };
            item.set_checked(checked);
            Ok(())
        })
    }

    #[func]
    fn set_accelerator(&self, accelerator: Variant) -> VarDictionary {
        reply(|| set_accelerator(self.native(), Accelerator::read(&accelerator)?))
    }

    #[func]
    fn set_icon(&self, icon: Variant, icon_as_template: bool) -> VarDictionary {
        reply(|| icons::set_item(self.native(), IconSource::read(&icon)?, icon_as_template))
    }

    #[func]
    fn icon_is_template(&self) -> VarDictionary {
        reply(|| {
            let item = self.native();
            if !matches!(item, MenuItemKind::Icon(_) | MenuItemKind::Submenu(_)) {
                return Err(Error::new(
                    "invalid-argument",
                    "item must be an icon item or submenu",
                ));
            }
            #[cfg(target_os = "macos")]
            {
                match item {
                    MenuItemKind::Icon(item) => Ok(item.icon_is_template()),
                    MenuItemKind::Submenu(item) => Ok(item.icon_is_template()),
                    _ => unreachable!(),
                }
            }
            #[cfg(not(target_os = "macos"))]
            {
                Err::<bool, _>(Error::unsupported("template icons require macOS"))
            }
        })
    }

    #[func]
    fn items(&self) -> VarDictionary {
        reply(|| self.children.items(&self.submenu()?.items()))
    }

    #[func]
    fn append(&mut self, items: Vec<Gd<KirieTrayItem>>) -> VarDictionary {
        reply(|| {
            let (objects, native) = ownership::prepare(items, self.base().instance_id())?;
            let submenu = self.submenu()?.clone();
            let result =
                submenu.append_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>());
            self.children.reconcile(&submenu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn prepend(&mut self, items: Vec<Gd<KirieTrayItem>>) -> VarDictionary {
        reply(|| {
            let (objects, native) = ownership::prepare(items, self.base().instance_id())?;
            let submenu = self.submenu()?.clone();
            let result =
                submenu.prepend_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>());
            self.children.reconcile(&submenu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn insert(&mut self, items: Vec<Gd<KirieTrayItem>>, position: u32) -> VarDictionary {
        reply(|| {
            let position = position as usize;
            let (objects, native) = ownership::prepare(items, self.base().instance_id())?;
            let submenu = self.submenu()?.clone();
            if position > submenu.items().len() {
                return Err(Error::new(
                    "invalid-argument",
                    "insert position exceeds the menu length",
                ));
            }
            let result = submenu.insert_items(
                &native.iter().map(ownership::as_item).collect::<Vec<_>>(),
                position,
            );
            self.children.reconcile(&submenu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn remove(&mut self, item: Gd<KirieTrayItem>) -> VarDictionary {
        reply(|| {
            if item.instance_id() == self.base().instance_id() {
                return Err(Error::new(
                    "invalid-argument",
                    "a submenu cannot contain itself",
                ));
            }
            let native = item.bind().native().clone();
            let submenu = self.submenu()?.clone();
            let result = submenu.remove(ownership::as_item(&native));
            self.children.reconcile(&submenu.items(), &[]);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn remove_at(&mut self, position: u32) -> VarDictionary {
        reply(|| {
            let position = position as usize;
            let submenu = self.submenu()?.clone();
            let removed = submenu.remove_at(position);
            let removed = removed
                .map(|item| self.children.get(&item.id().0))
                .transpose()?;
            self.children.reconcile(&submenu.items(), &[]);
            Ok(removed
                .map(|item| item.to_variant())
                .unwrap_or_else(Variant::nil))
        })
    }
}

fn item_text(item: &MenuItemKind) -> String {
    match item {
        MenuItemKind::MenuItem(item) => item.text(),
        MenuItemKind::Check(item) => item.text(),
        MenuItemKind::Icon(item) => item.text(),
        MenuItemKind::Submenu(item) => item.text(),
        MenuItemKind::Predefined(item) => item.text(),
    }
}
fn set_text(item: &MenuItemKind, text: &str) {
    match item {
        MenuItemKind::MenuItem(item) => item.set_text(text),
        MenuItemKind::Check(item) => item.set_text(text),
        MenuItemKind::Icon(item) => item.set_text(text),
        MenuItemKind::Submenu(item) => item.set_text(text),
        MenuItemKind::Predefined(item) => item.set_text(text),
    }
}
fn is_enabled(item: &MenuItemKind) -> Result<bool> {
    Ok(match item {
        MenuItemKind::MenuItem(item) => item.is_enabled(),
        MenuItemKind::Check(item) => item.is_enabled(),
        MenuItemKind::Icon(item) => item.is_enabled(),
        MenuItemKind::Submenu(item) => item.is_enabled(),
        MenuItemKind::Predefined(_) => {
            return Err(Error::unsupported(
                "predefined items have no upstream enabled getter",
            ));
        }
    })
}
fn set_enabled(item: &MenuItemKind, enabled: bool) -> Result<()> {
    match item {
        MenuItemKind::MenuItem(item) => item.set_enabled(enabled),
        MenuItemKind::Check(item) => item.set_enabled(enabled),
        MenuItemKind::Icon(item) => item.set_enabled(enabled),
        MenuItemKind::Submenu(item) => item.set_enabled(enabled),
        MenuItemKind::Predefined(_) => {
            return Err(Error::unsupported(
                "predefined items have no upstream enabled setter",
            ));
        }
    };
    Ok(())
}

fn set_accelerator(item: &MenuItemKind, accelerator: Option<Accelerator>) -> Result<()> {
    match accelerator {
        Some(Accelerator::Logical(accelerator)) => {
            match item {
                MenuItemKind::MenuItem(item) => item.set_key_accelerator(Some(accelerator)),
                MenuItemKind::Check(item) => item.set_key_accelerator(Some(accelerator)),
                MenuItemKind::Icon(item) => item.set_key_accelerator(Some(accelerator)),
                _ => {
                    return Err(Error::new(
                        "invalid-argument",
                        "item must be normal, check, or icon item",
                    ));
                }
            }?;
        }
        other => {
            let accelerator = match other {
                Some(Accelerator::Physical(accelerator)) => Some(accelerator),
                _ => None,
            };
            match item {
                MenuItemKind::MenuItem(item) => item.set_accelerator(accelerator),
                MenuItemKind::Check(item) => item.set_accelerator(accelerator),
                MenuItemKind::Icon(item) => item.set_accelerator(accelerator),
                _ => {
                    return Err(Error::new(
                        "invalid-argument",
                        "item must be normal, check, or icon item",
                    ));
                }
            }?;
        }
    }
    Ok(())
}
