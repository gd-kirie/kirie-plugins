use crate::{
    KirieTrayItem,
    types::{Error, Result},
};
use godot::{obj::InstanceId, prelude::*};
use std::collections::HashMap;
use tray_icon::menu::{IsMenuItem, MenuItemKind};

// References keep the Godot wrappers alive while the upstream container owns their items.
// Ordering and membership are always read back from muda.
#[derive(Default)]
pub struct Children {
    objects: HashMap<String, Gd<KirieTrayItem>>,
}

impl Children {
    pub fn reconcile(
        &mut self,
        actual: &[MenuItemKind],
        incoming: &[Gd<KirieTrayItem>],
    ) -> Result<()> {
        for item in incoming {
            let id = item.bind().native()?.id().0.clone();
            self.objects.insert(id, item.clone());
        }
        self.objects
            .retain(|id, _| actual.iter().any(|item| item.id().0 == *id));
        Ok(())
    }

    pub fn get(&self, id: &str) -> Result<Gd<KirieTrayItem>> {
        self.objects.get(id).cloned().ok_or_else(|| {
            Error::new(
                "native-error",
                "native menu item has no owning Godot object",
            )
        })
    }

    pub fn items(&self, actual: &[MenuItemKind]) -> Result<VarArray> {
        actual
            .iter()
            .map(|item| self.get(&item.id().0).map(|item| item.to_variant()))
            .collect()
    }
}

pub fn prepare(
    items: &VarArray,
    owner: InstanceId,
) -> Result<(Vec<Gd<KirieTrayItem>>, Vec<MenuItemKind>)> {
    let objects = items
        .iter_shared()
        .map(|item| {
            item.try_to::<Gd<KirieTrayItem>>()
                .map_err(|error| Error::new("invalid-argument", error))
        })
        .collect::<Result<Vec<_>>>()?;
    let native = prepare_objects(&objects, owner)?;
    Ok((objects, native))
}

pub fn prepare_objects(
    objects: &[Gd<KirieTrayItem>],
    owner: InstanceId,
) -> Result<Vec<MenuItemKind>> {
    let mut native = Vec::with_capacity(objects.len());
    for item in objects {
        // Check identity before borrowing: the owner may already be bound by its exported method.
        if item.instance_id() == owner {
            return Err(Error::new(
                "invalid-argument",
                "a menu item cannot contain itself",
            ));
        }
        native.push(item.bind().native()?.clone());
    }
    Ok(native)
}

pub fn as_item(item: &MenuItemKind) -> &dyn IsMenuItem {
    match item {
        MenuItemKind::MenuItem(item) => item,
        MenuItemKind::Check(item) => item,
        MenuItemKind::Icon(item) => item,
        MenuItemKind::Submenu(item) => item,
        MenuItemKind::Predefined(item) => item,
    }
}
