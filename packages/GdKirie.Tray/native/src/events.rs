use crate::types::{Error, Result};
use godot::{classes::Object, obj::InstanceId, prelude::*};
use std::{
    collections::HashMap,
    sync::{LazyLock, Mutex},
};
use tray_icon::{MouseButton, MouseButtonState, Rect, TrayIconEvent, menu::MenuEvent};

enum NativeEvent {
    Menu(MenuEvent),
    Tray(TrayIconEvent),
}

#[derive(Default)]
struct Events {
    routes: HashMap<(bool, String), InstanceId>,
    pending: Vec<(InstanceId, NativeEvent)>,
}

impl Events {
    fn receive(&mut self, event: NativeEvent) {
        let key = match &event {
            NativeEvent::Menu(event) => (false, event.id.0.clone()),
            NativeEvent::Tray(event) => (true, event.id().0.clone()),
        };
        if let Some(owner) = self.routes.get(&key) {
            self.pending.push((*owner, event));
        }
    }
}

// KSNI can deliver events on worker threads. Only opaque identities and upstream
// values cross this mutex; Godot objects are accessed by the main-loop callback.
static EVENTS: LazyLock<Mutex<Events>> = LazyLock::new(Mutex::default);

pub fn install_handlers() {
    MenuEvent::set_event_handler(Some(|event| {
        EVENTS
            .lock()
            .expect("tray event state lock poisoned")
            .receive(NativeEvent::Menu(event));
    }));
    TrayIconEvent::set_event_handler(Some(|event| {
        EVENTS
            .lock()
            .expect("tray event state lock poisoned")
            .receive(NativeEvent::Tray(event));
    }));
}

pub fn check_id(tray: bool, id: &str) -> Result<()> {
    let owner = EVENTS
        .lock()
        .expect("tray event state lock poisoned")
        .routes
        .get(&(tray, id.to_owned()))
        .copied();
    if owner.is_some_and(InstanceId::lookup_validity) {
        return Err(Error::new(
            "invalid-argument",
            format!("native ID is already in use: {id}"),
        ));
    }
    Ok(())
}

pub fn register(tray: bool, id: &str, owner: InstanceId) -> Result<()> {
    check_id(tray, id)?;
    EVENTS
        .lock()
        .expect("tray event state lock poisoned")
        .routes
        .insert((tray, id.to_owned()), owner);
    Ok(())
}

pub fn unregister(tray: bool, id: &str, owner: InstanceId) {
    let mut events = EVENTS.lock().expect("tray event state lock poisoned");
    let key = (tray, id.to_owned());
    if events.routes.get(&key) == Some(&owner) {
        events.routes.remove(&key);
    }
}

pub fn pump() {
    let pending = {
        let mut events = EVENTS.lock().expect("tray event state lock poisoned");
        std::mem::take(&mut events.pending)
    };
    for (owner, event) in pending {
        match event {
            NativeEvent::Menu(_) => emit(owner, "clicked", &[]),
            NativeEvent::Tray(event) => {
                if let Some(value) = event_dictionary(&event) {
                    emit(owner, "tray_event", &[value.to_variant()]);
                }
            }
        }
    }
}

// No lock or wrapper binding survives into signal emission. A queued event keeps
// its original identity, even when that object's upstream ID has since been reused.
fn emit(owner: InstanceId, signal: &str, args: &[Variant]) {
    let Ok(mut object) = Gd::<Object>::try_from_instance_id(owner) else {
        return;
    };
    let error = object.emit_signal(signal, args);
    if error != godot::global::Error::OK {
        godot_error!("native tray signal failed: {error:?}");
    }
}

fn event_dictionary(event: &TrayIconEvent) -> Option<VarDictionary> {
    match event {
        TrayIconEvent::Click {
            position,
            rect,
            button,
            button_state,
            ..
        } => Some(vdict! {
            "type" => "click",
            "position" => &point(*position),
            "rect" => &rectangle(*rect),
            "button" => button_name(*button),
            "buttonState" => match button_state {
                MouseButtonState::Up => "up",
                MouseButtonState::Down => "down",
            },
        }),
        TrayIconEvent::DoubleClick {
            position,
            rect,
            button,
            ..
        } => Some(vdict! {
            "type" => "double-click",
            "position" => &point(*position),
            "rect" => &rectangle(*rect),
            "button" => button_name(*button),
        }),
        TrayIconEvent::Enter { position, rect, .. } => {
            Some(pointer_event("enter", *position, *rect))
        }
        TrayIconEvent::Move { position, rect, .. } => Some(pointer_event("move", *position, *rect)),
        TrayIconEvent::Leave { position, rect, .. } => {
            Some(pointer_event("leave", *position, *rect))
        }
        _ => None,
    }
}

fn pointer_event(
    kind: &str,
    position: tray_icon::dpi::PhysicalPosition<f64>,
    rect: Rect,
) -> VarDictionary {
    vdict! {
        "type" => kind,
        "position" => &point(position),
        "rect" => &rectangle(rect),
    }
}

fn point(position: tray_icon::dpi::PhysicalPosition<f64>) -> VarDictionary {
    vdict! { "x" => position.x, "y" => position.y }
}

fn button_name(button: MouseButton) -> &'static str {
    match button {
        MouseButton::Left => "left",
        MouseButton::Right => "right",
        MouseButton::Middle => "middle",
    }
}

pub fn rectangle(rect: Rect) -> VarDictionary {
    vdict! {
        "position" => &vdict! { "x" => rect.position.x, "y" => rect.position.y },
        "size" => &vdict! { "width" => rect.size.width, "height" => rect.size.height },
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn queued_event_keeps_original_identity_after_id_reuse() {
        let original = InstanceId::from_i64(1);
        let replacement = InstanceId::from_i64(2);
        let mut events = Events::default();
        let key = (false, "reused".to_owned());
        events.routes.insert(key.clone(), original);
        events.receive(NativeEvent::Menu(MenuEvent {
            id: "reused".into(),
        }));
        events.routes.insert(key, replacement);
        events.receive(NativeEvent::Menu(MenuEvent {
            id: "reused".into(),
        }));

        let owners: Vec<_> = events.pending.iter().map(|(owner, _)| *owner).collect();
        assert_eq!(owners, [original, replacement]);
    }
}
