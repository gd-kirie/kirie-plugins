export type ResourceKind = "tray" | "menu" | "submenu" | "item" | "check" | "icon" | "predefined";

export interface Resource {
  handle: string;
  kind: ResourceKind;
  id: string;
}

export type IconSource =
  | { kind: "resource"; path: string } |
  { kind: "rgba"; rgba: readonly number[]; width: number; height: number } |
  { kind: "native" | "native-name"; name: string } |
  { kind: "windows-stock"; id: number } |
  { kind: "windows-path"; path: string; width?: number; height?: number } |
  { kind: "windows-resource"; resource: number | string; width?: number; height?: number };

export interface Accelerator {
  kind: "physical" | "logical";
  value: string;
}

export interface AboutMetadata {
  name?: string | null;
  version?: string | null;
  shortVersion?: string | null;
  authors?: string[] | null;
  comments?: string | null;
  copyright?: string | null;
  license?: string | null;
  website?: string | null;
  websiteLabel?: string | null;
  credits?: string | null;
  icon?: IconSource | null;
}

export type PredefinedKind =
  | "separator" | "section_header" | "copy" | "cut" | "paste" | "paste_and_match_style" |
  "delete" | "select_all" | "undo" | "redo" | "minimize" | "maximize" | "zoom" |
  "actual_size" | "zoom_in" | "zoom_out" | "fullscreen" | "hide" | "hide_others" |
  "show_all" | "close_window" | "quit" | "about" | "services" | "bring_all_to_front" |
  "start_speaking" | "stop_speaking" | "start_dictation" | "emoji_and_symbols";

export interface TrayCreateOptions {
  id?: string;
  icon?: IconSource | null;
  iconAsTemplate?: boolean;
  menu?: string | null;
  tooltip?: string | null;
  title?: string | null;
  menuOnLeftClick?: boolean;
  menuOnRightClick?: boolean;
  guid?: string;
  autosaveName?: string;
  tempDirPath?: string;
}

export interface MenuCreateOptions {
  id?: string;
  items?: string[];
}

export interface SubmenuCreateOptions extends MenuCreateOptions {
  text: string;
  enabled?: boolean;
  icon?: IconSource | null;
  iconAsTemplate?: boolean;
}

export interface MenuItemOptions {
  id?: string;
  text: string;
  enabled?: boolean;
  accelerator?: Accelerator | null;
}

export interface CheckMenuItemOptions extends MenuItemOptions {
  checked?: boolean;
}

export interface IconMenuItemOptions extends MenuItemOptions {
  icon?: IconSource | null;
  iconAsTemplate?: boolean;
}

export interface PredefinedMenuItemOptions {
  kind: PredefinedKind;
  text?: string | null;
  metadata?: AboutMetadata;
}

export interface Position {
  x: number;
  y: number;
}

export interface Rect {
  position: Position;
  size: { width: number; height: number };
}

export interface MenuEvent {
  type: "menu";
  item: Resource;
}

export interface TrayEvent {
  type: "click" | "double-click" | "enter" | "move" | "leave";
  tray: Resource;
  position: Position;
  rect: Rect;
  button?: "left" | "right" | "middle";
  buttonState?: "up" | "down";
}

export type NativeEvent = MenuEvent | TrayEvent;

export interface NativeError {
  code: string;
  message: string;
}

export type Reply = { ok: true; value: unknown } | { ok: false; error: NativeError };

// Each entry defines the wire fields and its native response together.
interface Methods {
  "resource.close": { args: { handle: string }; result: null };
  "tray.create": { args: { options: TrayCreateOptions }; result: Resource };
  "tray.set-icon": { args: { handle: string; icon: IconSource | null; iconAsTemplate?: boolean }; result: null };
  "tray.set-menu": { args: { handle: string; menu: string | null }; result: null };
  "tray.set-tooltip": { args: { handle: string; tooltip: string | null }; result: null };
  "tray.set-title": { args: { handle: string; title: string | null }; result: null };
  "tray.set-visible": { args: { handle: string; visible: boolean }; result: null };
  "tray.set-menu-on-left-click": { args: { handle: string; enabled: boolean }; result: null };
  "tray.set-menu-on-right-click": { args: { handle: string; enabled: boolean }; result: null };
  "tray.show-menu": { args: { handle: string }; result: null };
  "tray.rect": { args: { handle: string }; result: Rect | null };
  "tray.icon-is-template": { args: { handle: string }; result: boolean };
  "tray.set-temp-dir-path": { args: { handle: string; path: string | null }; result: null };
  "menu.create": { args: { options: MenuCreateOptions }; result: Resource };
  "submenu.create": { args: { options: SubmenuCreateOptions }; result: Resource };
  "item.create": { args: { options: MenuItemOptions }; result: Resource };
  "check.create": { args: { options: CheckMenuItemOptions }; result: Resource };
  "icon.create": { args: { options: IconMenuItemOptions }; result: Resource };
  "predefined.create": { args: { options: PredefinedMenuItemOptions }; result: Resource };
  "menu.items": { args: { handle: string }; result: Resource[] };
  "menu.append": { args: { handle: string; items: string[] }; result: null };
  "menu.prepend": { args: { handle: string; items: string[] }; result: null };
  "menu.insert": { args: { handle: string; items: string[]; position: number }; result: null };
  "menu.remove": { args: { handle: string; item: string }; result: null };
  "menu.remove-at": { args: { handle: string; position: number }; result: Resource | null };
  "item.text": { args: { handle: string }; result: string };
  "item.set-text": { args: { handle: string; text: string }; result: null };
  "item.is-enabled": { args: { handle: string }; result: boolean };
  "item.set-enabled": { args: { handle: string; enabled: boolean }; result: null };
  "item.set-accelerator": { args: { handle: string; accelerator: Accelerator | null }; result: null };
  "check.is-checked": { args: { handle: string }; result: boolean };
  "check.set-checked": { args: { handle: string; checked: boolean }; result: null };
  "item.set-icon": { args: { handle: string; icon: IconSource | null; iconAsTemplate?: boolean }; result: null };
  "item.icon-is-template": { args: { handle: string }; result: boolean };
}

export type MethodName = keyof Methods;
export type Arguments<M extends MethodName> = Methods[M]["args"];
export type Result<M extends MethodName> = Methods[M]["result"];
