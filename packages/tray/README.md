# `@gd-kirie/tray`

`@gd-kirie/tray` adds system tray icons and menus to desktop Kirie applications.
It uses the application's existing Eventa context.

## Install

Install the package in a Kirie project:

```sh
pnpm add @gd-kirie/tray
```

Add the plugin to `kirie.config.ts`:

```ts
import tray from "@gd-kirie/tray/plugin";
import { defineKirieConfig } from "kirie";
import core from "kirie/plugin/core";

export default defineKirieConfig({
  plugins: [core, tray],
});
```

Install the plugin dependencies:

```sh
pnpm kirie doctor --fix plugin:tray
```

This command installs the Godot addon and the `GdKirie.Tray` NuGet package.
The plugin supports macOS, Windows, and Linux on x64 and Arm64.
Linux requires a StatusNotifierItem host.

## Use

In Godot, [register and attach the adapter](../GdKirie.Tray/README.md#browser-access).
Create a client with the application's Eventa context:

```ts
import { createTrayClient } from "@gd-kirie/tray";

const client = createTrayClient(context);
const open = await client.createMenuItem({ text: "Open" });
const menu = await client.createMenu({ items: [open] });
const tray = await client.createTray({
  icon: { kind: "resource", path: "res://tray.svg" },
  menu,
  tooltip: "My application",
});

const stopEvents = client.onEvent((event) => {
  if (event.type === "menu" && event.id === open.id) {
    console.log("Open");
  }
});
```

Keep `res://tray.svg` in the Godot project.
Call `stopEvents()` when the page closes.
Release browser handles with `close()` when they are no longer needed.

See [GdKirie.Tray](../GdKirie.Tray/README.md) for C# usage and the
[tray example](https://github.com/gd-kirie/kirie-examples/blob/main/examples/tray/README.md) for a complete application.
