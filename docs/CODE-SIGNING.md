# Code signing (free, via SignPath Foundation)

Goal: make the one-time Windows "Windows protected your PC" / SmartScreen prompt go away, for free, by
signing released builds with a real, CA-trusted certificate.

There is no free shortcut with a self-signed certificate (Windows treats it as "unknown publisher",
identical to unsigned). The only genuinely free path is a CA-issued certificate, and the one program
that gives those to open-source projects for free is [SignPath Foundation](https://signpath.org/).
Lodestone is public and MIT-licensed, so it qualifies.

> Status: not yet signed. Releases are currently unsigned (SmartScreen shows a one-time prompt; click
> More info, then Run anyway). This document is the plan and policy to switch signing on.

---

## Part 1: Code Signing Policy (required by SignPath, public)

SignPath Foundation requires each project to publish a code-signing policy. This is ours.

- Project: Lodestone Mod Manager, <https://github.com/MateuszPodeszwa/LodestoneModManager>
- License: MIT (OSI-approved, no commercial dual-licensing).
- What is signed: the Windows installer (`Lodestone-win-Setup.exe`) and the application executable
  (`Lodestone.exe`) shipped in each GitHub Release.
- Who may sign (roles): Lodestone is currently maintained by a single maintainer, Mateusz Podeszwa,
  who acts as author, reviewer and approver. Any change to this is reflected here.
- How builds are produced and signed: signing happens only inside the project's GitHub Actions
  `Release` workflow, triggered by a maintainer-pushed `vX.Y.Z` git tag, building from the public
  source at the tagged commit. No local or manual signing of release artifacts.
- Account security: the SignPath organization enforces multi-factor authentication; the CI signing
  token is stored as an encrypted GitHub Actions secret and is not exposed to forks or pull requests.
- Scope: we sign only this project's own artifacts.
- Distribution channels: GitHub Releases of the repository above (and links from the project README and
  `lodestonemc.net` once live). Builds from any other source are not signed or endorsed by us.

---

## Part 2: One-time setup (maintainer actions, only you can do these)

1. Create a SignPath account. Go to <https://signpath.io/>, Sign in with GitHub (free open-source /
   community plan). Enable MFA when prompted.
2. Apply to SignPath Foundation for a free certificate: <https://signpath.org/apply>. Use the drafted
   answers in Part 3 below. Approval typically takes a few days to a few weeks; they review manually.
3. After approval, in the SignPath web portal note or create:
   - Organization ID (a GUID, on the org settings page).
   - Project, linked to this GitHub repo; suggested slug: `LodestoneModManager`.
   - Signing policy, suggested slug: `release-signing`.
   - API token, a CI user token (this is the secret CI uses).
4. Add credentials to GitHub (repo → Settings → Secrets and variables → Actions):
   - Secret `SIGNPATH_API_TOKEN` = the CI token from step 3.
   - Variable `SIGNPATH_ORGANIZATION_ID` = the Organization ID from step 3.
5. Tell me you are approved and give me the slugs, and I'll switch on the workflow block in Part 4. We
   verify it on a throwaway tag (e.g. `v0.0.1-signtest`) before your next real release.

---

## Part 3: Application answers (ready to paste into the SignPath application)

- Project name: Lodestone Mod Manager
- Repository: https://github.com/MateuszPodeszwa/LodestoneModManager
- License: MIT
- Description: A free, native Windows (WPF / .NET 10) mod manager for Minecraft: Java Edition.
  Drag-and-drop install of mods, resource packs and shaders, Modrinth browsing, dependency and
  compatibility checks, and self-updating via Velopack. No bundled proprietary components (system
  libraries only).
- What will be signed: the Velopack-built `Lodestone-win-Setup.exe` installer and `Lodestone.exe`.
- Build system: GitHub Actions (public workflow at `.github/workflows/release.yml`), built from tagged
  commits on the public repository.
- Code signing policy: this document, `docs/CODE-SIGNING.md` (linked from the project README).
- Maintainer: Mateusz Podeszwa (sole author, reviewer and approver).

---

## Part 4: Release workflow integration (apply after approval)

This is the change to `.github/workflows/release.yml`. It signs in two places: the app `.exe` before
Velopack packs it, and the final `Setup.exe` after, using the official
[`signpath/github-action-submit-signing-request@v2`](https://github.com/SignPath/github-action-submit-signing-request)
action. Don't add this yet; it requires the secrets and slugs from Part 2, or every release will fail.

```yaml
# (1) After "Publish (self-contained win-x64)" and BEFORE "Package with Velopack":

      - name: Upload unsigned app
        id: upload-unsigned-app
        uses: actions/upload-artifact@v4
        with:
          name: app-unsigned
          if-no-files-found: error
          path: publish/Lodestone.exe

      - name: Sign app exe (SignPath)
        uses: signpath/github-action-submit-signing-request@v2
        with:
          api-token: '${{ secrets.SIGNPATH_API_TOKEN }}'
          organization-id: '${{ vars.SIGNPATH_ORGANIZATION_ID }}'
          project-slug: 'LodestoneModManager'
          signing-policy-slug: 'release-signing'
          github-artifact-id: '${{ steps.upload-unsigned-app.outputs.artifact-id }}'
          wait-for-completion: true
          output-artifact-directory: 'publish'   # overwrites publish/Lodestone.exe with the signed one

# (2) After "Package with Velopack" and BEFORE "Publish release to GitHub":

      - name: Upload unsigned installer
        id: upload-unsigned-setup
        uses: actions/upload-artifact@v4
        with:
          name: setup-unsigned
          if-no-files-found: error
          path: Releases/Lodestone-win-Setup.exe

      - name: Sign installer (SignPath)
        uses: signpath/github-action-submit-signing-request@v2
        with:
          api-token: '${{ secrets.SIGNPATH_API_TOKEN }}'
          organization-id: '${{ vars.SIGNPATH_ORGANIZATION_ID }}'
          project-slug: 'LodestoneModManager'
          signing-policy-slug: 'release-signing'
          github-artifact-id: '${{ steps.upload-unsigned-setup.outputs.artifact-id }}'
          wait-for-completion: true
          output-artifact-directory: 'Releases'  # overwrites the Setup.exe with the signed one
```

Notes:
- The exact set of files SignPath will sign is also constrained by the artifact configuration you
  define in the SignPath project (a one-time XML/UI step in their portal). We will align the two when
  we test.
- SmartScreen reputation: an OV certificate gives you a verified publisher immediately; the scary
  warning typically clears as a small amount of download reputation accrues under that publisher. EV
  certificates (paid) are the only ones that are instant from zero, and not worth it here.

---

## Summary

| Step | Who | Status |
|------|-----|--------|
| Publish the code-signing policy | this file | done |
| Create SignPath account and apply | you | to do |
| Get Foundation approval | SignPath | to do |
| Add `SIGNPATH_API_TOKEN` and org id to GitHub | you | to do |
| Wire and test the workflow signing block | me, once approved | to do |

Until this is switched on, unsigned releases work fine. See [DEPLOYMENT.md](DEPLOYMENT.md) section 5.
