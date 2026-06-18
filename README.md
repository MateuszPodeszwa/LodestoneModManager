<div align="center">

# Lodestone: Minecraft Mod Manager

Install and manage Minecraft mods, resource packs and shaders without the busywork.
No profiles to wrangle, no config files to hand-edit.

[![CI](https://github.com/MateuszPodeszwa/LodestoneModManager/actions/workflows/ci.yml/badge.svg)](https://github.com/MateuszPodeszwa/LodestoneModManager/actions/workflows/ci.yml)
&nbsp;·&nbsp; Windows · .NET 10 · WPF · MIT

</div>

---

Lodestone is a native Windows app for Minecraft (Java Edition). Drop a `.jar` on the window and it
goes straight into the right folder for whatever game version you have selected. You can search
thousands of mods on Modrinth, see at a glance when something is missing a dependency or clashes
with another mod, and keep the whole set current. None of it involves opening a config file.

It is free, and it stays that way. Every feature is there for everyone; supporters pick up a few
cosmetic extras and my thanks. More on that under [Supporters](#supporters).

## Download and install

**[Get the latest release →](https://github.com/MateuszPodeszwa/LodestoneModManager/releases/latest)**

Either build is self-contained, so there is nothing else to install (no .NET, no separate runtimes):

- **`Lodestone-win-Setup.exe`** is the one I would recommend. It updates itself from then on.
- **`Lodestone-win-Portable.zip`** runs without installing. To update, download the newer zip.

### Installing the Setup.exe

1. Grab `Lodestone-win-Setup.exe` from the [latest release](https://github.com/MateuszPodeszwa/LodestoneModManager/releases/latest).
2. Run it. Windows might show a blue "Windows protected your PC" screen. That happens because the app
   is not code-signed yet, and it is not a virus warning. Choose **More info**, then **Run anyway**.
   You only do this once.
3. Lodestone installs and opens by itself. There is no wizard to click through.

After that it keeps itself current: when a new version ships, it downloads and applies on the next
launch, so you will not reinstall.

> Rather not install anything? Take `Lodestone-win-Portable.zip`, unzip it wherever you like (a USB
> stick works fine), and run `Lodestone.exe`. The portable build does not auto-update, so grab a
> newer zip when you want one.

Maintainers: how releases and auto-update work is written up in **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**.

## What it does

- **Drag and drop to install.** Drop a `.jar`, `.zip`, `.litemod` or `.mcpack` anywhere on the
  window. Lodestone works out whether it is a mod, resource pack or shader and puts it in the
  correct folder for the game version you currently have selected.
- **Browse mods.** Search Modrinth (CurseForge is pluggable), filter by category, sort by downloads
  or followers, and install in a click.
- **My Content.** Per-version "profiles", category filters, enable or disable without deleting,
  uninstall, and search.
- **Compatibility and dependency checks.** Everything in the list gets scanned, and a symbol shows
  up next to anything that needs a missing library, conflicts with another mod, was built for a
  different game version or loader, or is duplicated. Hover to read the reason.
- **Updates when you ask for them.** Lodestone checks for mod updates on launch and when you hit
  refresh; there is no background daemon. Optional auto-update keeps enabled mods current.
- **Settings that actually do something.** Game directory, default loader, concurrent downloads,
  update and notification behaviour, CurseForge fallback, close-to-tray. Each one is wired to real
  logic.
- **App auto-update** via [Velopack](https://velopack.io); new releases install themselves.
- **Light footprint.** Nothing runs in the background, and closing the window ends the process
  (unless you opt into the tray). Your `.minecraft` only changes when you do something to it.

## Architecture in brief

Clean/Onion layering with MVVM at the edge. Dependencies always point inward, which keeps the core
logic unit-testable and means a future macOS port mostly comes down to swapping the UI layer.

```
Lodestone.Domain          pure entities, value objects, rules (no dependencies)
Lodestone.Application     ports (interfaces) + use-cases + the compatibility engine
Lodestone.Infrastructure  adapters: Modrinth API, archive readers, file system, settings, updater
Lodestone.App  (WPF)      views + viewmodels + DI composition root
Lodestone.Cli             headless surface (handy for scripting and integration tests)
```

Beyond Dependency Inversion, the code leans on a fair range of patterns: Strategy, Factory,
Chain-of-Responsibility, Specification, Decorator, Adapter, Repository, Result/Railway, Options,
Observer, Null-Object, Template-Method, Command and a light Mediator.
**[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** walks through them, and
**[docs/RISK-ANALYSIS.md](docs/RISK-ANALYSIS.md)** covers the per-feature failure modes.

## Getting started (developers)

```powershell
# Requires the .NET 10 SDK (see global.json)
dotnet restore
dotnet build
dotnet test                                   # runs the full unit-test suite
dotnet run --project src/Lodestone.App        # launches the app
```

## Releases and auto-update

Tag a commit `v*` and the release workflow builds a Velopack installer and publishes it to GitHub
Releases. Installed clients update from that feed. A pre-release tag (say `v1.3.0-beta.1`) ships a
patrons-first beta: it goes out as a GitHub pre-release that only the supporter early-access channel
sees, until you cut the stable `vX.Y.Z`. There is a plain-English walkthrough (cutting a release,
betas, how auto-update behaves, SmartScreen, troubleshooting) in **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**.

Maintainer setup (Patreon link, supporter keys, CurseForge key, signing, cutting a release) lives in
**[docs/HANDOFF.md](docs/HANDOFF.md)**.

## Supporters

Donations go through Patreon and are completely optional. Pledge on any paid tier and, while that
pledge is active, you get a redeemable code that switches on cosmetic extras: a supporter badge, a
few extra accent themes, and an opt-in beta update channel. No payment happens inside the app, and
nothing about how the app works is ever locked behind a donation. Details in
[docs/SUPPORTERS.md](docs/SUPPORTERS.md).

## License

[MIT](LICENSE). Not affiliated with Mojang, Microsoft, Modrinth or CurseForge.
