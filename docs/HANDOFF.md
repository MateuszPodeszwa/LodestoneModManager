# Maintainer handoff

Lodestone runs today as-is. These optional steps connect it to your real accounts and assets and
harden releases. Each one is independent.

## 1. Patreon and website links
Configured in `DonateViewModel` (`src/Lodestone.App/ViewModels/DonateViewModel.cs`). `PatreonUrl`
points at the Patreon page (the Support us buttons open it); `WebsiteUrl` and `PrioritySupportUrl`
point at `lodestonemc.net/supporter` and `lodestonemc.net/support`. That domain is registered but not
yet deployed, so those two pages need to go live for the supporter sign-in and code flow to work.

## 2. Supporter codes (already wired)
A real key pair was generated; the public key is embedded in `SupporterKeys.DefaultPublicKey` and the
private key is under `keys/` (git-ignored). Issue codes with the CLI, see [SUPPORTERS.md](SUPPORTERS.md).
To rotate keys, run `lodestone keygen`, replace the public key, and ship an update. Keep `keys/` backed
up somewhere safe and out of version control. The website mints codes only for active, paying patrons
(former and free followers don't qualify); see [SUPPORTERS.md](SUPPORTERS.md).

## 3. CurseForge (optional second source)
Modrinth works with no key. To enable the CurseForge fallback, get an
[Eternal API key](https://console.curseforge.com/) and pass it through `LodestoneOptions.CurseForgeApiKey`
in `AddLodestone(...)` (App composition), then implement the calls in `CurseForgeModSource` (currently
a configured stub that reports "not configured" so it is skipped safely).

## 4. Code signing (optional, recommended for distribution)
Unsigned builds work but may trip SmartScreen on first run. Add a code-signing certificate as CI
secrets and the `--signParams` argument in `release.yml` (a placeholder comment marks the spot).

## 5. Cutting a release
Tag a commit `vX.Y.Z` and push the tag. `release.yml` tests, publishes a self-contained win-x64 build,
packages it with Velopack and publishes a GitHub Release, which is the feed that installed clients
update from. For example:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

Tag a pre-release version instead (e.g. `v1.3.0-beta.1`) to ship a patrons-first beta. `release.yml`
publishes it as a GitHub pre-release that only the supporter Beta channel and the website's beta
download resolve to. Promote it to everyone later with a normal `vX.Y.Z` tag.

A full, beginner-friendly walkthrough of releasing and auto-update lives in
[DEPLOYMENT.md](DEPLOYMENT.md).

## 6. App icon (cosmetic polish)
No custom `.ico` ships yet (the exe uses the default). Add one and set `<ApplicationIcon>` in
`Lodestone.App.csproj`; it will also become the title-bar and tray icon.
