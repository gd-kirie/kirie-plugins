# npm publishing

This repository publishes `@gd-kirie/tray` and `@gd-kirie/notifications`.
Core npm packages are published from [gd-kirie](https://github.com/gd-kirie/gd-kirie).
The plugin manifests use published core dependencies and point to this repository.

## Setup

Move each plugin package's npm trusted publisher to this repository before release:

- Owner: `gd-kirie`
- Repository: `kirie-plugins`
- Workflow: `npm-publish.yml`
- Environment: empty

## Release

Run `mise run release` to update npm, NuGet, and notification addon versions.
The release task builds packages, checks npm packing with a dry run, and creates a local commit and tag.
Merge the release commit into main before pushing its `v*` tag.
The workflow rejects tags that are not reachable from main.
A manual workflow run builds and checks packages without publishing them.

The npm workflow builds the Tray GDExtension for macOS, Windows, and Linux on x64 and Arm64.
It checks all six native libraries before publishing the plugin packages.
For a local host build, run:

```sh
mise run build:tray-native
mise run build:packages
```

Native output is staged in `packages/GdKirie.Tray/native/artifacts/<rid>`.
The Tray package build copies it to `packages/tray/addon/kirie_tray/bin/<rid>`.
These files are generated and stay outside Git.

## References

- [npm trusted publishing](https://docs.npmjs.com/trusted-publishers/)
- [pnpm workspace dependencies](https://pnpm.io/workspaces#workspace-protocol-workspace)
- [bumpp](https://github.com/antfu-collective/bumpp)
