# NuGet publishing

This repository publishes `GdKirie.Tray`, `GdKirie.Notifications`, and `GdKirie.Qr` for .NET 10.
The packages include their Eventa integration and reference the published `GdKirie.EventaAdapter` package.
The Tray native extension ships in its npm package, not its NuGet package.
The QR npm plugin supplies its native dependencies.

## Local validation

Build and pack the managed plugins:

```sh
mise run build:dotnet
mise run pack:nuget
```

Packages are written to `artifacts/nuget`.
Runnable application examples live in [kirie-examples](https://github.com/gd-kirie/kirie-examples).

## GitHub Actions

Move the plugin packages' NuGet Trusted Publishing policy to this repository before release:

- Owner: `gd-kirie`
- Repository: `kirie-plugins`
- Workflow: `nuget-release.yml`
- Environment: empty

Set the repository Actions variable `NUGET_USER` to the NuGet profile username.
The workflow exchanges GitHub's OIDC token for a short-lived NuGet key.
It publishes the packages for `v*` tags that are reachable from main.
A manual workflow run also publishes the selected ref.

## References

- [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
- [NuGet OIDC login action](https://github.com/NuGet/login)
