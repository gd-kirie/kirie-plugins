use crate::{
    icons,
    ownership::{self, Children},
    types::{self, Accelerator, Error, IconSource, ItemOptions, Result, reply},
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

    fn finish(item: MenuItemKind, options: ItemOptions) -> Result<Gd<Self>> {
        if options.accelerator.is_some() {
            set_accelerator(&item, options.accelerator)?;
        }
        if options.icon.is_some() || options.icon_as_template {
            icons::set_item(&item, options.icon, options.icon_as_template)?;
        }
        let mut children = Children::default();
        if let MenuItemKind::Submenu(submenu) = &item {
            children.reconcile(&submenu.items(), &options.items);
        }
        Ok(Self::wrap(item, children))
    }
}

#[godot_api]
impl KirieTrayItem {
    #[func]
    fn create_item(options: VarDictionary) -> VarDictionary {
        reply(|| {
            let options = ItemOptions::read(&options)?;
            let item = match options.id.as_deref() {
                Some(id) => MenuItem::with_id(id, &options.text, options.enabled, None),
                None => MenuItem::new(&options.text, options.enabled, None),
            };
            Self::finish(MenuItemKind::MenuItem(item), options)
        })
    }

    #[func]
    fn create_check(options: VarDictionary) -> VarDictionary {
        reply(|| {
            let options = ItemOptions::read(&options)?;
            let item = match options.id.as_deref() {
                Some(id) => CheckMenuItem::with_id(
                    id,
                    &options.text,
                    options.enabled,
                    options.checked,
                    None,
                ),
                None => CheckMenuItem::new(&options.text, options.enabled, options.checked, None),
            };
            Self::finish(MenuItemKind::Check(item), options)
        })
    }

    #[func]
    fn create_icon(options: VarDictionary) -> VarDictionary {
        reply(|| {
            let options = ItemOptions::read(&options)?;
            let item = match options.id.as_deref() {
                Some(id) => IconMenuItem::with_id(id, &options.text, options.enabled, None, None),
                None => IconMenuItem::new(&options.text, options.enabled, None, None),
            };
            Self::finish(MenuItemKind::Icon(item), options)
        })
    }

    #[func]
    fn create_submenu(options: VarDictionary) -> VarDictionary {
        reply(|| {
            let options = ItemOptions::read(&options)?;
            let native = ownership::prepare_objects(&options.items);
            let item = match options.id.as_deref() {
                Some(id) => Submenu::with_id(id, &options.text, options.enabled),
                None => Submenu::new(&options.text, options.enabled),
            };
            item.append_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>())?;
            Self::finish(MenuItemKind::Submenu(item), options)
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
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::copy(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn cut(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::cut(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn paste(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::paste(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn paste_and_match_style(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::paste_and_match_style(
                    text.as_deref(),
                )),
                Children::default(),
            ))
        })
    }

    #[func]
    fn delete(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::delete(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn select_all(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::select_all(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn undo(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::undo(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn redo(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::redo(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn minimize(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::minimize(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn maximize(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::maximize(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn zoom(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::zoom(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn actual_size(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::actual_size(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn zoom_in(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::zoom_in(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn zoom_out(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::zoom_out(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn fullscreen(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::fullscreen(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn hide(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::hide(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn hide_others(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::hide_others(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn show_all(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::show_all(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn close_window(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::close_window(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn quit(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::quit(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn services(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::services(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn bring_all_to_front(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::bring_all_to_front(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn start_speaking(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::start_speaking(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn stop_speaking(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::stop_speaking(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn start_dictation(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::start_dictation(text.as_deref())),
                Children::default(),
            ))
        })
    }

    #[func]
    fn emoji_and_symbols(text: Variant) -> VarDictionary {
        reply(|| {
            let text = types::nullable_string(&text)?;
            Ok(Self::wrap(
                MenuItemKind::Predefined(PredefinedMenuItem::emoji_and_symbols(text.as_deref())),
                Children::default(),
            ))
        })
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
    fn append(&mut self, items: VarArray) -> VarDictionary {
        reply(|| {
            let (objects, native) = ownership::prepare(&items, self.base().instance_id())?;
            let submenu = self.submenu()?.clone();
            let result =
                submenu.append_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>());
            self.children.reconcile(&submenu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn prepend(&mut self, items: VarArray) -> VarDictionary {
        reply(|| {
            let (objects, native) = ownership::prepare(&items, self.base().instance_id())?;
            let submenu = self.submenu()?.clone();
            let result =
                submenu.prepend_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>());
            self.children.reconcile(&submenu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn insert(&mut self, items: VarArray, position: i64) -> VarDictionary {
        reply(|| {
            let position = usize::try_from(position)?;
            let (objects, native) = ownership::prepare(&items, self.base().instance_id())?;
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
    fn remove_at(&mut self, position: i64) -> VarDictionary {
        reply(|| {
            let submenu = self.submenu()?.clone();
            let removed = submenu.remove_at(usize::try_from(position)?);
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
