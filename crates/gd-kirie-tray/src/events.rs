use godot::{classes::Engine, prelude::*};
use tray_icon::{MouseButton, MouseButtonState, Rect, TrayIconEvent, menu::MenuEvent};

const SINGLETON: &str = "KirieTrayEvents";

#[derive(GodotClass)]
#[class(init, base=Object)]
pub struct KirieTrayEvents {
    godot: Base<Object>,
}

#[godot_api]
impl KirieTrayEvents {
    #[signal]
    fn menu_event(id: GString);

    #[signal]
    fn tray_event(event: VarDictionary);
}

pub fn initialize() {
    let events = KirieTrayEvents::new_alloc();
    Engine::singleton().register_singleton(SINGLETON, &events);
}

pub fn deinitialize() {
    let mut engine = Engine::singleton();
    let events = engine
        .get_singleton(SINGLETON)
        .expect("tray events singleton");
    engine.unregister_singleton(SINGLETON);
    events.free();
}

pub fn pump() {
    let events = Engine::singleton()
        .get_singleton(SINGLETON)
        .expect("tray events singleton")
        .cast::<KirieTrayEvents>();
    for event in MenuEvent::receiver().try_iter() {
        events.signals().menu_event().emit(&event.id.0);
    }
    for event in TrayIconEvent::receiver().try_iter() {
        if let Some(mut value) = event_dictionary(&event) {
            value.set("id", event.id().0.as_str());
            events.signals().tray_event().emit(&value);
        }
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
            "type" => "Click",
            "position" => &point(*position),
            "rect" => &rectangle(*rect),
            "button" => button_name(*button),
            "buttonState" => match button_state {
                MouseButtonState::Up => "Up",
                MouseButtonState::Down => "Down",
            },
        }),
        TrayIconEvent::DoubleClick {
            position,
            rect,
            button,
            ..
        } => Some(vdict! {
            "type" => "DoubleClick",
            "position" => &point(*position),
            "rect" => &rectangle(*rect),
            "button" => button_name(*button),
        }),
        TrayIconEvent::Enter { position, rect, .. } => {
            Some(pointer_event("Enter", *position, *rect))
        }
        TrayIconEvent::Move { position, rect, .. } => Some(pointer_event("Move", *position, *rect)),
        TrayIconEvent::Leave { position, rect, .. } => {
            Some(pointer_event("Leave", *position, *rect))
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
        MouseButton::Left => "Left",
        MouseButton::Right => "Right",
        MouseButton::Middle => "Middle",
    }
}

pub fn rectangle(rect: Rect) -> VarDictionary {
    vdict! {
        "position" => &vdict! { "x" => rect.position.x, "y" => rect.position.y },
        "size" => &vdict! { "width" => rect.size.width, "height" => rect.size.height },
    }
}
