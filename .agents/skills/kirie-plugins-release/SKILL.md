---
name: kirie-plugins-release
description: Prepare and publish coordinated releases of all publishable plugins in kirie-plugins to npm and NuGet. Use when a user requests a plugin version release, release PR, or release tag in this repository.
---

# Kirie Plugins Release

Release `gd-kirie/kirie-plugins` through a pull request, then tag its merged commit.
Include every publishable plugin present in the target revision.
Keep the npm, NuGet, and Godot addon versions aligned across that release.
Core Kirie packages, templates, and examples have separate releases.

## Release rules

- Store the target in `KIRIE_PLUGINS_RELEASE_VERSION` without a `v` prefix.
- Read `README.md`, `bump.config.ts`, and the publishing documents under `docs/` before preparing the release.
- Read the current checks and publishing workflows under `.github/workflows/` before publishing.
- Use a clean checkout. If unrelated changes exist, preserve them and stop.
- Run language and package tools through mise.
- Keep the published core dependencies unchanged unless the user requests an upgrade.
- Keep generated addons, native libraries, and package artifacts outside Git.
- Do not replace an existing remote tag or published package version.
- Preserve the configured Git identity and signer in the default mode.

## Merge authorization

Choose the release mode from the user's authorization:

- In the default mode, open the release pull request and wait for a human to merge it.
- In auto-merge mode, an explicit request to merge and complete the release authorizes the normal release mutations.
  These include pushing the release branch, opening and merging the pull request, and pushing the release tag.
  Run these steps after the required checks pass without asking for confirmation again.
- If the user requests preparation only, stop after the requested preparation step.

Auto-merge authorization does not permit force pushes, replacing tags, bypassing branch protection, or merging failed checks.
Keep commit signing enabled unless the user explicitly authorizes unattended mode.
In unattended mode, first attempt the configured signer.
If the signer is unavailable or requires human interaction, use `--no-gpg-sign` for the current release commits.
Do not change Git's signing configuration.

## 1. Discover the release scope

Derive the package list from the revision that will be released.
Do not use a fixed list of plugin names or package counts.
Do not assume that every plugin has both npm and NuGet packages.

1. Read `pnpm-workspace.yaml` and the plugin manifests under `packages/` to determine workspace membership.
2. Record every public npm package name, version source, and registry from its manifest.
3. Read `KiriePlugins.slnx`, plugin project files, and imported properties that control package identity, version, or packability.
4. Record every packable NuGet package ID, version source, and registry used by the publishing workflow.
5. Read the included plugins' build scripts and addon metadata to identify tracked and generated addon versions.
6. Compare this inventory with workspace membership, solution membership, `bump.config.ts`, and the packages selected by the publishing workflows.
7. Record the build, test, packing, and native artifact requirements for every included plugin.

Exclude private npm packages and non-packable .NET projects from publication.
Include the root release version and other version sources selected by the release configuration in the version update.
If a plugin is missing from version updates or publication, report and resolve the configuration gap before bumping versions.
Do not silently omit it or update generated output by hand.
Use package metadata and build scripts to determine whether auxiliary crate or project versions participate in the release.

## 2. Prepare the release branch

1. Make sure that `origin` refers to `gd-kirie/kirie-plugins`.
2. Fetch `origin/main` and the remote tags.
3. Make sure that `v<VERSION>` does not exist locally or remotely.
4. Create branch `release/v<VERSION>` from `origin/main` and switch to it.
5. Refresh the release inventory from this branch before changing versions.
6. Install dependencies with `mise x -- pnpm install --frozen-lockfile`.
7. Run bumpp without creating a commit or tag:

   ```sh
   mise x -- pnpm exec bumpp --release "$KIRIE_PLUGINS_RELEASE_VERSION" --recursive --no-commit --no-tag --no-push --yes
   ```

The `--recursive` option updates workspace manifests.
The `--all` option controls Git staging, not recursive version updates.
The hook in `bump.config.ts` updates the configured NuGet and tracked addon versions.
It also builds the npm packages and runs the npm publish dry run.

Do not use bare `mise run release` for this pull request workflow.
Its defaults create a local commit and tag before the merge.
If bumpp fails after changing files, examine the partial state before another run.
The hook rejects unchanged version fields, so a blind retry can fail again.

## 3. Examine the release diff

Make sure that these discovered release fields contain `KIRIE_PLUGINS_RELEASE_VERSION`:

- The root release version and every public npm package version.
- The version sources for every packable NuGet package.
- The tracked addon versions and the source versions used to generate addon metadata.

Regenerate addon metadata through its owning build task.
Do not edit or commit generated addons.
Compare the resulting versions with the inventory, not only the paths already listed in `bump.config.ts`.

1. Run `git diff --check`.
2. Examine `git diff --stat` and the complete `git diff`.
3. Make sure that core dependency versions remain unchanged unless the user requested their upgrade.
4. Make sure that no generated artifacts or unexpected lockfile changes exist.
5. Make sure that bumpp created no commit and no tag.
6. Refresh the frozen dependency state with `mise x -- pnpm install --frozen-lockfile`.

## 4. Validate the packages

Run the checks required by `mise.toml` and `.github/workflows/check.yml` for the complete release inventory.
Include each plugin's native build and test tasks where the repository defines them.
Build native addons for the current host before the final package builds and publish dry run.
Use the repository tasks for shared checks and packaging:

```sh
mise run lint
mise run typecheck
mise run test:unit
mise run build:packages
mise run build:dotnet
mise run pack:nuget
mise x -- pnpm publish -r --access public --no-git-checks --dry-run
```

Make sure that the dry run includes every npm package in the inventory at the target version.
If it reports no new packages, stop and examine the workspace versions and registry state.
Inspect each package's included addons, generated code, and required native artifacts.
Make sure that the NuGet output contains every package ID in the inventory at the target version.
Use only the target version's artifacts if the directory contains older packages.
Make sure that generated files remain outside Git after these commands.

For each plugin with native artifacts, derive the required platforms and architectures from its publishing workflow and build configuration.
Make sure that CI assembles every required artifact before publication.
A local native build validates only the current host.

Read [npm publishing](../../../docs/npm-publishing.md) and [NuGet publishing](../../../docs/nuget-publishing.md) for authentication requirements.
Make sure that trusted publishers refer to `gd-kirie/kirie-plugins` and the corresponding workflow, with no environment.
Make sure that the repository Actions variable `NUGET_USER` is set.
If required publishing configuration is missing, report the exact missing setting before publication.
Do not introduce tokens, local patches, or dependency overrides as workarounds.

## 5. Create and merge the pull request

1. Commit the examined release files with `chore(release): v<VERSION>`.
2. Push branch `release/v<VERSION>` to `origin`.
3. Open a pull request into `main` with title `chore(release): v<VERSION>`.
4. Leave the pull request body empty unless the user requests text.
5. Report the pull request URL and the actual local validation results.

In the default mode, stop after opening the pull request.
Resume tagging only after GitHub reports its state as `MERGED`.
In auto-merge mode, monitor required checks and review feedback.
Fix release failures within the authorized scope, then wait for all required checks to pass.
Merge with squash or rebase without creating a merge commit.
Record the pull request's full merged commit SHA as `MERGE_SHA`.
Do not use the branch commit SHA because squash and rebase can change it.

## 6. Create and push the release tag

1. Fetch `origin/main` and the remote tags.
2. Make sure that `MERGE_SHA` is reachable from `origin/main`.
3. Refresh the release inventory at `MERGE_SHA` and make sure that every included version matches the target.
4. Make sure that tag `v<VERSION>` does not exist locally or remotely.
5. Create lightweight tag `v<VERSION>` at `MERGE_SHA`.
6. Push only `refs/tags/v<VERSION>` to `origin`.
7. Read the remote tag and make sure that it points to `MERGE_SHA`.

If a tag exists at another commit, stop and report both SHAs.
If the remote tag already points to `MERGE_SHA`, continue by inspecting its workflows without pushing it again.
Both publishing workflows reject tag commits that are not reachable from `main`.

## 7. Monitor publication

Monitor every workflow that publishes packages in the release inventory for the pushed tag.
Report success only after those workflows pass and every expected package version appears in its registry.
If a workflow fails, report its URL, failed job, and any packages that already published.
Inspect the failure and registry state before retrying publication.
Do not move the tag to repair a partial release.

Manual runs have different effects:

- `npm-publish.yml` builds and checks packages without publishing them.
- `nuget-release.yml` publishes the selected ref and skips duplicate package versions.
- Both workflows publish automatically on a `v*` tag push.

Do not use manual npm dispatch to retry a failed publication.
Use the failed tag run's supported rerun action after addressing its cause within the authorized scope.

See the upstream [bumpp documentation](https://github.com/antfu-collective/bumpp#readme) for recursive version updates.
See [npm trusted publishing](https://docs.npmjs.com/trusted-publishers/) for publisher configuration.
