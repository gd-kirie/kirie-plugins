# Kirie plugins

This repository contains optional plugins for [Kirie](https://github.com/gd-kirie/gd-kirie).
Browser adapters use the application's existing Eventa context.
Runnable projects live in [kirie-examples](https://github.com/gd-kirie/kirie-examples).

## Packages

| Capability | Browser package | Godot host |
| --- | --- | --- |
| System tray | [@gd-kirie/tray](packages/tray/README.md) | [GdKirie.Tray](packages/GdKirie.Tray/README.md) |
| Notifications | [@gd-kirie/notifications](packages/notifications/README.md) | [GdKirie.Notifications](packages/GdKirie.Notifications/README.md) |
| QR codes | [@gd-kirie/qr](packages/qr/README.md) | [GdKirie.Qr](packages/GdKirie.Qr/README.md) |

## Development

Install tools and dependencies from this directory:

```sh
mise trust
mise install
mise x -- pnpm install --frozen-lockfile
mise x -- apm install --frozen
```

On Linux, mise installs Clang and sets the [Cargo linker](https://doc.rust-lang.org/cargo/reference/config.html#targettriplelinker) for x64 and Arm64.
The [mise Clang backend](https://mise.jdx.dev/dev-tools/backends/conda.html) installs its compiler dependencies without a separate Conda installation.
On macOS, install Xcode Command Line Tools.
On Windows, install Visual Studio Build Tools with the C++ workload and Windows SDK.

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

Release instructions are in [npm publishing](docs/npm-publishing.md) and
[NuGet publishing](docs/nuget-publishing.md).
Design rationale is in [architecture decisions](docs/decisions/README.md).
