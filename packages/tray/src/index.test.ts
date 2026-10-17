import type { KirieEventaContext } from "@gd-kirie/ipc-eventa";
import type { NativeEvent, Reply, Resource } from "./contracts";
import { createContext, defineEventa, matchBy } from "@moeru/eventa";
import { describe, expect, it } from "vitest";
import {
  CheckMenuItem,
  createTrayClient,
  IconMenuItem,
  Menu,
  MenuItem,
  PredefinedMenuItem,
  Submenu,
  TrayError,
  TrayIcon,
} from "./index";

interface InvokeBody<T> {
  content: T;
  invokeId: string;
}

interface Call {
  wire: string;
  args: Record<string, unknown>;
}

function host(respond: (call: Call) => Reply): {
  context: KirieEventaContext;
  calls: Call[];
  emit: (event: NativeEvent) => void;
} {
  const context = createContext() as KirieEventaContext;
  const calls: Call[] = [];
  context.on(matchBy("*"), (event) => {
    const name = event.id;
    if (!name.startsWith("kirie:tray:") || !name.endsWith("-send")) {
      return;
    }

    const body = event.body as InvokeBody<Record<string, unknown>> | undefined;
    if (!body) {
      throw new Error("Tray invocation has no body.");
    }

    // Mirror the JSON transport used by the real Eventa adapter.
    const call = {
      wire: name.slice(0, -"-send".length),
      args: JSON.parse(JSON.stringify(body.content)) as Record<string, unknown>,
    };
    calls.push(call);
    const reply = JSON.parse(JSON.stringify(respond(call))) as Reply;
    context.emit(defineEventa<InvokeBody<Reply>>(`${call.wire}-receive-${body.invokeId}`), {
      content: reply,
      invokeId: body.invokeId,
    });
  });
  return {
    context,
    calls,
    emit(event) {
      context.emit(defineEventa<NativeEvent>("kirie:tray:event"), event);
    },
  };
}

const check: Resource = { handle: "h-check", id: "enabled", kind: "check" };
const menu: Resource = { handle: "h-menu", id: "menu", kind: "menu" };
const tray: Resource = { handle: "h-tray", id: "tray", kind: "tray" };

describe("tray objects over Eventa", () => {
  it("converts object arguments to handles and preserves optional and nullable fields", async () => {
    const resources: Record<string, unknown> = {
      "kirie:tray:check-create": check,
      "kirie:tray:menu-create": menu,
      "kirie:tray:tray-create": tray,
      "kirie:tray:menu-items": [check],
    };
    const native = host(({ wire }) => ({ ok: true, value: resources[wire] ?? null }));
    const client = createTrayClient(native.context);
    const item = await client.createCheckMenuItem({ text: "Enabled" });
    const container = await client.createMenu({ items: [item] });
    const icon = { kind: "rgba" as const, rgba: [0, 128, 255, 255], width: 1, height: 1 };
    const iconTray = await client.createTray({ icon, menu: container, tooltip: "" });
    await container.insert(2, item);
    await iconTray.setMenu(null);
    await iconTray.setIcon(null, false);
    expect((await container.items())[0]).toBeInstanceOf(CheckMenuItem);

    expect(native.calls).toContainEqual({ wire: "kirie:tray:check-create", args: { options: { text: "Enabled" } } });
    expect(native.calls).toContainEqual({ wire: "kirie:tray:menu-create", args: { options: { items: [check.handle] } } });
    expect(native.calls).toContainEqual({
      wire: "kirie:tray:tray-create",
      args: { options: { icon, menu: menu.handle, tooltip: "" } },
    });
    expect(native.calls).toContainEqual({ wire: "kirie:tray:menu-insert", args: { handle: menu.handle, items: [check.handle], position: 2 } });
    expect(native.calls).toContainEqual({ wire: "kirie:tray:tray-set-menu", args: { handle: tray.handle, menu: null } });
    expect(native.calls).toContainEqual({ wire: "kirie:tray:tray-set-icon", args: { handle: tray.handle, icon: null, iconAsTemplate: false } });
  });

  it("calls predefined constructors with upstream argument shapes and preserves text defaults", async () => {
    const resource: Resource = { handle: "h-predefined", id: "native-id", kind: "predefined" };
    const native = host(() => ({ ok: true, value: resource }));
    const client = createTrayClient(native.context);
    const item = await client.predefined.copy();
    await client.predefined.copy(null);
    await client.predefined.copy("");
    await client.predefined.pasteAndMatchStyle("Paste without formatting");
    await client.predefined.separator();
    await client.predefined.sectionHeader("Actions");
    await client.predefined.about();
    await client.predefined.about(null, { name: "My application", authors: ["Author"] });
    expect(item).toBeInstanceOf(PredefinedMenuItem);
    expect(item.id).toBe("native-id");
    expect(native.calls).toEqual([
      { wire: "kirie:tray:predefined-copy", args: {} },
      { wire: "kirie:tray:predefined-copy", args: { text: null } },
      { wire: "kirie:tray:predefined-copy", args: { text: "" } },
      { wire: "kirie:tray:predefined-paste-and-match-style", args: { text: "Paste without formatting" } },
      { wire: "kirie:tray:predefined-separator", args: {} },
      { wire: "kirie:tray:predefined-section-header", args: { text: "Actions" } },
      { wire: "kirie:tray:predefined-about", args: {} },
      { wire: "kirie:tray:predefined-about", args: { text: null, metadata: { name: "My application", authors: ["Author"] } } },
    ]);
  });

  it("wraps application descriptors locally as their concrete object kinds", () => {
    const resources: Resource[] = [
      tray,
      menu,
      check,
      { handle: "sub", id: "sub", kind: "submenu" },
      { handle: "item", id: "item", kind: "item" },
      { handle: "icon", id: "icon", kind: "icon" },
      { handle: "about", id: "about", kind: "predefined" },
    ];
    const native = host(() => ({ ok: true, value: null }));
    const client = createTrayClient(native.context);
    const objects = resources.map((resource) => client.fromResource(resource));
    const classes = [TrayIcon, Menu, CheckMenuItem, Submenu, MenuItem, IconMenuItem, PredefinedMenuItem];
    objects.forEach((object, index) => expect(object).toBeInstanceOf(classes[index]));
    expect(objects.map(({ id, handle, kind }) => ({ id, handle, kind }))).toEqual(resources);
    expect(native.calls).toHaveLength(0);
  });

  it("rejects foreign context references before invocation and permits the same borrowed context", async () => {
    const native = host(({ wire }) => ({ ok: true, value: wire === "kirie:tray:menu-create" ? menu : check }));
    const otherNative = host(() => ({ ok: true, value: menu }));
    const client = createTrayClient(native.context);
    const sameContextClient = createTrayClient(native.context);
    const foreignClient = createTrayClient(otherNative.context);
    const item = await client.createCheckMenuItem({ text: "Enabled" });
    const localMenu = await sameContextClient.createMenu({ items: [item] });
    const foreignMenu = await foreignClient.createMenu();
    await localMenu.append(item);
    const calls = otherNative.calls.length;
    await expect(foreignMenu.append(item)).rejects.toMatchObject({ code: "invalid-argument" });
    expect(otherNative.calls).toHaveLength(calls);
    await expect(foreignClient.createMenu({ items: [item] })).rejects.toBeInstanceOf(TrayError);
    await expect(client.createTray({ menu: foreignMenu })).rejects.toMatchObject({ code: "invalid-argument" });
  });

  it("preserves native error codes and messages", async () => {
    const native = host(({ wire }) => wire === "kirie:tray:tray-create" ?
        { ok: true, value: tray } :
        { ok: false, error: { code: "unsupported", message: "Native API is unavailable on this platform." } });
    const iconTray = await createTrayClient(native.context).createTray();
    await expect(iconTray.showMenu()).rejects.toMatchObject({
      name: "TrayError",
      code: "unsupported",
      message: "Native API is unavailable on this platform.",
    });
  });

  it("forwards global IDs without resource lookup and keeps subscriptions independent of handles", async () => {
    const native = host(() => ({ ok: true, value: check }));
    const client = createTrayClient(native.context);
    const received: NativeEvent[] = [];
    const off = client.onEvent((event) => received.push(event));
    const menuEvent: NativeEvent = { type: "menu", id: "unexposed-item" };
    const click: NativeEvent = {
      type: "Click",
      id: "unexposed-tray",
      position: { x: 4, y: 8 },
      rect: { position: { x: 1, y: 2 }, size: { width: 20, height: 20 } },
      button: "Left",
      buttonState: "Up",
    };
    native.emit(menuEvent);
    native.emit(click);
    expect(received).toEqual([menuEvent, click]);
    expect(native.calls).toHaveLength(0);

    const item = await client.createCheckMenuItem({ text: "Enabled" });
    await item.close();
    const afterClose: NativeEvent = { type: "menu", id: item.id };
    native.emit(afterClose);
    expect(received).toEqual([menuEvent, click, afterClose]);
    off();
    native.emit(click);
    expect(received).toEqual([menuEvent, click, afterClose]);
  });
});
