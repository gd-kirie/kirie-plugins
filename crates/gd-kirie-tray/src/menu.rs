use crate::{
    KirieTrayItem,
    ownership::{self, Children},
    types::{self, Error, reply},
};
use godot::prelude::*;
use tray_icon::menu::Menu;

#[derive(GodotClass)]
#[class(no_init, base=RefCounted)]
pub struct KirieTrayMenu {
    menu: Menu,
    children: Children,
    godot: Base<RefCounted>,
}

impl KirieTrayMenu {
    pub fn native(&self) -> &Menu {
        &self.menu
    }
}

#[godot_api]
impl KirieTrayMenu {
    #[func]
    fn create(id: Variant, items: Vec<Gd<KirieTrayItem>>) -> VarDictionary {
        reply(|| {
            let id = types::nullable_string(&id)?;
            let native = ownership::prepare_objects(&items);
            let menu = match id {
                Some(id) => Menu::with_id(id),
                None => Menu::new(),
            };
            menu.append_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>())?;
            let mut children = Children::default();
            children.reconcile(&menu.items(), &items);
            Ok(Gd::from_init_fn(|godot| Self {
                menu,
                children,
                godot,
            }))
        })
    }

    #[func]
    fn id(&self) -> VarDictionary {
        reply(|| Ok(GString::from(self.native().id().0.as_str())))
    }

    #[func]
    fn items(&self) -> VarDictionary {
        reply(|| self.children.items(&self.native().items()))
    }

    #[func]
    fn append(&mut self, items: Vec<Gd<KirieTrayItem>>) -> VarDictionary {
        reply(|| {
            let (objects, native) = ownership::prepare(items, self.base().instance_id())?;
            let menu = self.native().clone();
            let result =
                menu.append_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>());
            self.children.reconcile(&menu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn prepend(&mut self, items: Vec<Gd<KirieTrayItem>>) -> VarDictionary {
        reply(|| {
            let (objects, native) = ownership::prepare(items, self.base().instance_id())?;
            let menu = self.native().clone();
            let result =
                menu.prepend_items(&native.iter().map(ownership::as_item).collect::<Vec<_>>());
            self.children.reconcile(&menu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn insert(&mut self, items: Vec<Gd<KirieTrayItem>>, position: u32) -> VarDictionary {
        reply(|| {
            let position = position as usize;
            let (objects, native) = ownership::prepare(items, self.base().instance_id())?;
            let menu = self.native().clone();
            if position > menu.items().len() {
                return Err(Error::new(
                    "invalid-argument",
                    "insert position exceeds the menu length",
                ));
            }
            let result = menu.insert_items(
                &native.iter().map(ownership::as_item).collect::<Vec<_>>(),
                position,
            );
            self.children.reconcile(&menu.items(), &objects);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn remove(&mut self, item: Gd<KirieTrayItem>) -> VarDictionary {
        reply(|| {
            let native = item.bind().native().clone();
            let menu = self.native().clone();
            let result = menu.remove(ownership::as_item(&native));
            self.children.reconcile(&menu.items(), &[]);
            result.map_err(Into::into)
        })
    }

    #[func]
    fn remove_at(&mut self, position: u32) -> VarDictionary {
        reply(|| {
            let position = position as usize;
            let menu = self.native().clone();
            let removed = menu.remove_at(position);
            let removed = removed
                .map(|item| self.children.get(&item.id().0))
                .transpose()?;
            self.children.reconcile(&menu.items(), &[]);
            Ok(removed
                .map(|item| item.to_variant())
                .unwrap_or_else(Variant::nil))
        })
    }
}
