# Kirie plugins

This repository contains optional plugins for [Kirie](https://github.com/gd-kirie/gd-kirie).
The plugins use the application's existing Eventa context.
Runnable projects live in [kirie-examples](https://github.com/gd-kirie/kirie-examples).

## Packages

| Capability | Browser package | Godot host |
| --- | --- | --- |
| System tray | [@gd-kirie/tray](packages/tray/README.md) | [GdKirie.Tray](packages/GdKirie.Tray/README.md) |
| Notifications | [@gd-kirie/notifications](packages/notifications/README.md) | [GdKirie.Notifications](packages/GdKirie.Notifications/README.md) |

## Development

Install tools and dependencies from this directory:

```sh
mise trust
mise install
mise x -- pnpm install --frozen-lockfile
mise x -- apm install --frozen
```

Build the native tray library for your host before packaging the npm plugins:

```sh
mise run build:tray-native
mise run build:packages
mise run build:dotnet
mise run typecheck
mise run test:unit
mise run test:tray-native
mise run lint
```

Use `mise run format` to format source files.
Edit notification addon code in `packages/notifications/src-godot`.
The package build generates GDScript under its addon directory.

Core npm and NuGet dependencies use published version 0.13.0.
The .NET plugin libraries exclude the Eventa adapter's `contentFiles` bridge,
which belongs in applications that compile the addon-provided `KirieClient`.
See [NuGet asset controls](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#controlling-dependency-assets).

Release instructions are in [npm publishing](docs/npm-publishing.md) and
[NuGet publishing](docs/nuget-publishing.md).
