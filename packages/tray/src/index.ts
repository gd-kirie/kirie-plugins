import type { KirieEventaContext } from "@gd-kirie/ipc-eventa";
import type {
  AboutMetadata,
  Accelerator,
  Arguments,
  CheckMenuItemOptions,
  IconMenuItemOptions,
  IconSource,
  MenuCreateOptions,
  MenuItemOptions,
  MethodName,
  NativeEvent,
  Rect,
  Reply,
  Resource,
  ResourceKind,
  Result,
  SubmenuCreateOptions,
  TrayCreateOptions,
} from "./contracts";
import { defineInboundEventa, defineInvoke, defineInvokeEventa } from "@moeru/eventa";

export type {
  AboutMetadata,
  Accelerator,
  CheckMenuItemOptions,
  IconMenuItemOptions,
  IconSource,
  MenuEvent,
  MenuItemOptions,
  NativeEvent,
  Position,
  Rect,
  Resource,
  ResourceKind,
  TrayEvent,
} from "./contracts";

export interface TrayOptions extends Omit<TrayCreateOptions, "menu"> {
  menu?: Menu | Submenu | null;
}

export interface MenuOptions extends Omit<MenuCreateOptions, "items"> {
  items?: readonly MenuEntry[];
}

export interface SubmenuOptions extends Omit<SubmenuCreateOptions, "items"> {
  items?: readonly MenuEntry[];
}

export type MenuEntry = MenuItem | CheckMenuItem | IconMenuItem | PredefinedMenuItem | Submenu;
export type TrayResource = TrayIcon | Menu | MenuEntry;

const nativeEvent = defineInboundEventa<NativeEvent>("kirie:tray:event");

export class TrayError extends Error {
  readonly code: string;

  constructor(code: string, message: string) {
    super(message);
    this.name = "TrayError";
    this.code = code;
  }
}

class Connection {
  readonly context: KirieEventaContext;

  constructor(context: KirieEventaContext) {
    this.context = context;
  }

  async request<M extends MethodName>(name: M, args: Arguments<M>): Promise<Result<M>> {
    const event = defineInvokeEventa<Reply, Arguments<M>>(`kirie:tray:${name.replace(".", "-")}`);
    const reply = await defineInvoke(this.context, event)(args);
    if (!reply.ok) {
      throw new TrayError(reply.error.code, reply.error.message);
    }

    return reply.value as Result<M>;
  }

  handle(resource: TrayObject): string {
    if (!resource.belongsTo(this.context)) {
      throw new TrayError("invalid-argument", "Tray resources belong to different Eventa contexts.");
    }

    return resource.handle;
  }

  onEvent(handler: (event: NativeEvent) => void): () => void {
    return this.context.on(nativeEvent, ({ body }) => {
      handler(body!);
    });
  }

  object(resource: Resource): TrayResource {
    const constructors = {
      tray: TrayIcon,
      menu: Menu,
      submenu: Submenu,
      item: MenuItem,
      check: CheckMenuItem,
      icon: IconMenuItem,
      predefined: PredefinedMenuItem,
    };
    const Constructor = constructors[resource.kind];
    return new Constructor(this, resource);
  }
}

abstract class TrayObject {
  readonly handle: string;
  readonly id: string;
  abstract readonly kind: ResourceKind;
  protected readonly connection: Connection;

  /** @internal */
  constructor(connection: Connection, resource: Resource) {
    this.connection = connection;
    this.handle = resource.handle;
    this.id = resource.id;
  }

  /** @internal */
  belongsTo(context: KirieEventaContext): boolean {
    return this.connection.context === context;
  }

  /** Closes this shared browser handle without removing the object from its native parents. */
  async close(): Promise<void> {
    await this.connection.request("resource.close", { handle: this.handle });
  }
}

export class TrayIcon extends TrayObject {
  readonly kind = "tray" as const;

  async setIcon(icon: IconSource | null, iconAsTemplate?: boolean): Promise<void> {
    await this.connection.request("tray.set-icon", { handle: this.handle, icon, iconAsTemplate });
  }

  async setMenu(menu: Menu | Submenu | null): Promise<void> {
    await this.connection.request("tray.set-menu", {
      handle: this.handle,
      menu: menu === null ? null : this.connection.handle(menu),
    });
  }

  async setTooltip(tooltip: string | null): Promise<void> {
    await this.connection.request("tray.set-tooltip", { handle: this.handle, tooltip });
  }

  async setTitle(title: string | null): Promise<void> {
    await this.connection.request("tray.set-title", { handle: this.handle, title });
  }

  async setVisible(visible: boolean): Promise<void> {
    await this.connection.request("tray.set-visible", { handle: this.handle, visible });
  }

  async setMenuOnLeftClick(enabled: boolean): Promise<void> {
    await this.connection.request("tray.set-menu-on-left-click", { handle: this.handle, enabled });
  }

  async setMenuOnRightClick(enabled: boolean): Promise<void> {
    await this.connection.request("tray.set-menu-on-right-click", { handle: this.handle, enabled });
  }

  async showMenu(): Promise<void> {
    await this.connection.request("tray.show-menu", { handle: this.handle });
  }

  rect(): Promise<Rect | null> {
    return this.connection.request("tray.rect", { handle: this.handle });
  }

  iconIsTemplate(): Promise<boolean> {
    return this.connection.request("tray.icon-is-template", { handle: this.handle });
  }

  async setTempDirPath(path: string | null): Promise<void> {
    await this.connection.request("tray.set-temp-dir-path", { handle: this.handle, path });
  }
}

abstract class MenuContainer extends TrayObject {
  async items(): Promise<MenuEntry[]> {
    const items = await this.connection.request("menu.items", { handle: this.handle });
    return items.map((item) => this.connection.object(item) as MenuEntry);
  }

  async append(...items: MenuEntry[]): Promise<void> {
    await this.connection.request("menu.append", {
      handle: this.handle,
      items: items.map((item) => this.connection.handle(item)),
    });
  }

  async prepend(...items: MenuEntry[]): Promise<void> {
    await this.connection.request("menu.prepend", {
      handle: this.handle,
      items: items.map((item) => this.connection.handle(item)),
    });
  }

  async insert(position: number, ...items: MenuEntry[]): Promise<void> {
    await this.connection.request("menu.insert", {
      handle: this.handle,
      items: items.map((item) => this.connection.handle(item)),
      position,
    });
  }

  async remove(item: MenuEntry): Promise<void> {
    await this.connection.request("menu.remove", { handle: this.handle, item: this.connection.handle(item) });
  }

  async removeAt(position: number): Promise<MenuEntry | null> {
    const item = await this.connection.request("menu.remove-at", { handle: this.handle, position });
    return item === null ? null : this.connection.object(item) as MenuEntry;
  }
}

export class Menu extends MenuContainer {
  readonly kind = "menu" as const;
}

export class Submenu extends MenuContainer {
  readonly kind = "submenu" as const;

  text(): Promise<string> {
    return this.connection.request("item.text", { handle: this.handle });
  }

  async setText(text: string): Promise<void> {
    await this.connection.request("item.set-text", { handle: this.handle, text });
  }

  isEnabled(): Promise<boolean> {
    return this.connection.request("item.is-enabled", { handle: this.handle });
  }

  async setEnabled(enabled: boolean): Promise<void> {
    await this.connection.request("item.set-enabled", { handle: this.handle, enabled });
  }

  async setIcon(icon: IconSource | null, iconAsTemplate?: boolean): Promise<void> {
    await this.connection.request("item.set-icon", { handle: this.handle, icon, iconAsTemplate });
  }

  iconIsTemplate(): Promise<boolean> {
    return this.connection.request("item.icon-is-template", { handle: this.handle });
  }
}

abstract class TextMenuItem extends TrayObject {
  text(): Promise<string> {
    return this.connection.request("item.text", { handle: this.handle });
  }

  async setText(text: string): Promise<void> {
    await this.connection.request("item.set-text", { handle: this.handle, text });
  }
}

abstract class EditableMenuItem extends TextMenuItem {
  isEnabled(): Promise<boolean> {
    return this.connection.request("item.is-enabled", { handle: this.handle });
  }

  async setEnabled(enabled: boolean): Promise<void> {
    await this.connection.request("item.set-enabled", { handle: this.handle, enabled });
  }

  async setAccelerator(accelerator: Accelerator | null): Promise<void> {
    await this.connection.request("item.set-accelerator", { handle: this.handle, accelerator });
  }
}

export class MenuItem extends EditableMenuItem {
  readonly kind = "item" as const;
}

export class CheckMenuItem extends EditableMenuItem {
  readonly kind = "check" as const;

  isChecked(): Promise<boolean> {
    return this.connection.request("check.is-checked", { handle: this.handle });
  }

  async setChecked(checked: boolean): Promise<void> {
    await this.connection.request("check.set-checked", { handle: this.handle, checked });
  }
}

export class IconMenuItem extends EditableMenuItem {
  readonly kind = "icon" as const;

  async setIcon(icon: IconSource | null, iconAsTemplate?: boolean): Promise<void> {
    await this.connection.request("item.set-icon", { handle: this.handle, icon, iconAsTemplate });
  }

  iconIsTemplate(): Promise<boolean> {
    return this.connection.request("item.icon-is-template", { handle: this.handle });
  }
}

export class PredefinedMenuItem extends TextMenuItem {
  readonly kind = "predefined" as const;
}

export class TrayClient {
  private readonly connection: Connection;

  constructor(context: KirieEventaContext) {
    this.connection = new Connection(context);
  }

  async createTray(options: TrayOptions = {}): Promise<TrayIcon> {
    const resource = await this.connection.request("tray.create", {
      options: {
        ...options,
        menu: options.menu == null ? options.menu : this.connection.handle(options.menu),
      },
    });
    return new TrayIcon(this.connection, resource);
  }

  async createMenu(options: MenuOptions = {}): Promise<Menu> {
    const resource = await this.connection.request("menu.create", {
      options: { ...options, items: options.items?.map((item) => this.connection.handle(item)) },
    });
    return new Menu(this.connection, resource);
  }

  async createSubmenu(options: SubmenuOptions): Promise<Submenu> {
    const resource = await this.connection.request("submenu.create", {
      options: { ...options, items: options.items?.map((item) => this.connection.handle(item)) },
    });
    return new Submenu(this.connection, resource);
  }

  async createMenuItem(options: MenuItemOptions): Promise<MenuItem> {
    return new MenuItem(this.connection, await this.connection.request("item.create", { options }));
  }

  async createCheckMenuItem(options: CheckMenuItemOptions): Promise<CheckMenuItem> {
    return new CheckMenuItem(this.connection, await this.connection.request("check.create", { options }));
  }

  async createIconMenuItem(options: IconMenuItemOptions): Promise<IconMenuItem> {
    return new IconMenuItem(this.connection, await this.connection.request("icon.create", { options }));
  }

  /** Calls the public upstream PredefinedMenuItem constructors. */
  readonly predefined = {
    separator: async (): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.separator", {})),
    sectionHeader: async (text: string): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.section-header", { text })),
    copy: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.copy", { text })),
    cut: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.cut", { text })),
    paste: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.paste", { text })),
    pasteAndMatchStyle: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.paste-and-match-style", { text })),
    delete: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.delete", { text })),
    selectAll: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.select-all", { text })),
    undo: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.undo", { text })),
    redo: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.redo", { text })),
    minimize: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.minimize", { text })),
    maximize: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.maximize", { text })),
    zoom: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.zoom", { text })),
    actualSize: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.actual-size", { text })),
    zoomIn: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.zoom-in", { text })),
    zoomOut: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.zoom-out", { text })),
    fullscreen: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.fullscreen", { text })),
    hide: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.hide", { text })),
    hideOthers: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.hide-others", { text })),
    showAll: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.show-all", { text })),
    closeWindow: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.close-window", { text })),
    quit: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.quit", { text })),
    services: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.services", { text })),
    bringAllToFront: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.bring-all-to-front", { text })),
    startSpeaking: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.start-speaking", { text })),
    stopSpeaking: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.stop-speaking", { text })),
    startDictation: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.start-dictation", { text })),
    emojiAndSymbols: async (text?: string | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.emoji-and-symbols", { text })),
    about: async (text?: string | null, metadata?: AboutMetadata | null): Promise<PredefinedMenuItem> =>
      new PredefinedMenuItem(this.connection, await this.connection.request("predefined.about", { text, metadata })),
  };

  /** Wraps an application-provided descriptor without creating another native reference. */
  fromResource(resource: Resource): TrayResource {
    return this.connection.object(resource);
  }

  /** Receives global upstream events by native ID, independent of resource handles. */
  onEvent(handler: (event: NativeEvent) => void): () => void {
    return this.connection.onEvent(handler);
  }
}

export function createTrayClient(context: KirieEventaContext): TrayClient {
  return new TrayClient(context);
}
