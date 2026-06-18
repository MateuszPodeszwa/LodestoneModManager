# Per-feature risk analysis and mitigation

A design deliverable: for every feature, the things that can go wrong and how Lodestone guards
against them. Severity is High (data loss or crash), Med (broken feature) or Low (annoyance).

---

## 1. Onboarding and game auto-detection
| Risk | Sev | Mitigation |
|---|---|---|
| `.minecraft` missing or non-standard (MultiMC/Prism, custom launcher, OneDrive-redirected AppData) | Med | Probe a list of known paths; if none validate, onboarding shows a Choose folder fallback. A folder is accepted only after it passes `IGameLocator.Validate` (has `versions/` or `mods/`). |
| User skips onboarding with no valid directory | Low | Every file operation is guarded by `IGameLocator.IsValid`; a non-blocking banner prompts to set the directory, and nothing throws. |
| Detected path is read-only or needs elevation | Med | Write probe on selection; explain clearly and let the user pick another location. |

## 2. Drag-and-drop install (local files)
| Risk | Sev | Mitigation |
|---|---|---|
| Unknown, huge or corrupt file dropped | Med | Validate the extension and sniff the ZIP magic header; cap the size; corrupt archives fail with a friendly toast. |
| Type mis-detection (mod vs pack vs shader) | Low | Detect by extension first, then by archive contents (`fabric.mod.json`/`mods.toml` for a mod, `pack.mcmeta` for a resource pack, top-level `shaders/` for a shader). |
| Dropped while "All versions" is selected (no target) | Low | Fall back to the detected latest installed version and toast which version received the file. |
| Duplicate, or would overwrite an existing file | High | Detect by filename and sha512; never silently clobber. Keep-both or replace, and the replaced file goes to trash. |
| Multiple files dropped at once | Low | All routed through one bounded install queue with per-file progress. |

## 3. Browse and install (Modrinth, CurseForge pluggable)
| Risk | Sev | Mitigation |
|---|---|---|
| Network down or DNS failure | Med | Typed errors surface as the design's empty/error state, never a crash. Cached results shown when available. |
| Rate limiting (Modrinth asks for a descriptive `User-Agent`) | Med | Compliant UA string; retry with exponential backoff plus jitter; respect `Retry-After`. |
| API schema drift | Med | Responses parsed through tolerant DTOs plus an adapter; unknown fields ignored; a parse failure degrades to "no results", logged. |
| Slow or janky search UI | Low | Debounced input, per-keystroke `CancellationToken`, all calls async, virtualized lists. |
| Wrong build installed (game version/loader mismatch) | Med | The version resolver accepts only files matching the active game version and loader before download. |

## 4. Library management (toggle / uninstall / filter / search / profiles)
| Risk | Sev | Mitigation |
|---|---|---|
| File locked because Minecraft is running | Med | File ops return `Result`; on `IOException` we retry with backoff and, if still locked, explain "close Minecraft and retry". |
| Partial state between `library.json` and disk | High | A reconcile pass on every refresh re-syncs the index to what is actually on disk. |
| Uninstall removes a still-needed file | High | Soft delete to a trash folder first; confirmation dialog; toast. |
| Enable/disable corrupts a file | Med | Toggling only renames `…jar ⇄ …jar.disabled` (atomic, loader-ignored); content is never rewritten. |

## 5. Compatibility and dependency detection (headline extra)
| Risk | Sev | Mitigation |
|---|---|---|
| False positives from incomplete metadata | Med | Missing data is treated as unknown (no scary error). Only explicit declarations raise issues. |
| Dependency identity mismatch (local mod id vs Modrinth project id) | Med | A slug/id index maps both ways; unresolved ids are reported as "unknown dependency", not "missing". |
| Transitive dependencies | Low | Rules resolve one level; the modal shows the full declared tree from the source where available. |
| Performance on a large library | Low | Indexes built once per scan; each rule is O(n). |

## 6. Updates (per refresh / per start) plus auto-update
| Risk | Sev | Mitigation |
|---|---|---|
| "Latest" is incompatible and breaks the game | High | Only versions matching the active game version and loader are offered; auto-update is opt-in and still filtered; the prior file is kept in trash for rollback. |
| User expects silent background updates | Low | By design there is no daemon; updates run on start/refresh, and a manual control is always present and documented. |
| Update interrupted mid-download | Med | Download to a temp file, verify sha512, then atomically swap; a failed download leaves the old file intact. |

## 7. Settings (each option implemented)
Game directory, default loader, auto-update, notify, concurrent downloads (1–6), CurseForge fallback,
close-to-tray, app version / check-for-updates.
| Risk | Sev | Mitigation |
|---|---|---|
| Corrupt, missing or older-schema settings file | Med | Options pattern with defaults plus validation; atomic temp-then-rename writes; a corrupt file is backed up and reset to defaults with a toast. |
| "Concurrent downloads" set absurdly | Low | Clamped to 1–6 (matches the design's stepper) and enforced by the download semaphore. |
| Close-to-tray conflicts with "process must end on close" | Low | Documented decision: the feature stays but ships OFF by default; even when ON the tray app does zero background polling and only keeps the window resident. With it OFF, closing fully terminates the process. |

## 8. App auto-update and deployment
| Risk | Sev | Mitigation |
|---|---|---|
| A bad update bricks the install | High | Velopack stages updates atomically and rolls back on failure; updates apply on next launch, never mid-session. |
| SmartScreen or AV flags an unsigned exe | Low | Code-signing is a documented opt-in via CI secrets; reproducible builds reduce false positives. |
| Update feed unreachable | Low | Check-for-updates fails gracefully with a clear message; the app keeps working on the current version. |

## 9. Patreon donate and supporter unlock
| Risk | Sev | Mitigation |
|---|---|---|
| No in-app payment processor, yet must "unlock" | Med | Donate buttons open Patreon in the browser; unlocking uses an offline signed code (ECDSA P-256, public key embedded, private key stays with the maintainer). |
| Design says functionality must never be gated | Med | Unlocks are cosmetic and convenience only (badge, accent themes, beta channel). Core features stay free. |
| Codes get shared or cracked | Low | Accepted: perks are cosmetic, so signature verification is enough; no secret ships in the client beyond a public key; codes can carry an expiry/nonce. Codes are issued only to active, paying patrons. |
| Beta builds are a supporter perk, but the repo is public | Low | Soft-gated: the in-app Beta channel and the website's beta download are supporter-only, but a GitHub pre-release is still directly downloadable. Accepted (a perk, not paid content); serve betas from an authenticated endpoint if hard-gating is ever needed. |

## 10. Window and UX (custom chrome, DPI, responsiveness, a11y)
| Risk | Sev | Mitigation |
|---|---|---|
| Custom title bar breaks min/max/snap/drag | Med | `WindowChrome` with correct caption and resize-border hit-testing; system commands wired explicitly. |
| Blurry UI on high-DPI or multi-monitor | Low | Per-Monitor-V2 DPI awareness; vector icons; layout in DIPs. |
| Small window clips content | Low | Minimum window size plus fluid grids that reflow (the design already wraps panels). |
| Keyboard or screen-reader users | Low | Focus order, access keys, and `AutomationProperties` on interactive elements. |

## 11. Cross-cutting (persistence, networking, concurrency, security)
| Risk | Sev | Mitigation |
|---|---|---|
| JSON store corruption on crash or power-loss | High | All stores use atomic temp-then-rename writes; a corrupt file is quarantined and defaults restored. |
| Tampered or MITM download | High | HTTPS only; every download verified against the sha512 supplied by the source. |
| Zip-slip path traversal from a malicious archive | High | Archives are only read in memory for metadata; entry names are sanitized; nothing is extracted to disk. |
| Unbounded concurrency exhausts sockets/CPU | Low | Download concurrency is semaphore-bounded from settings; `HttpClient` via `IHttpClientFactory`. |
| Silent crash | Med | Global exception handlers log to `%AppData%/Lodestone/logs` and show a toast; the app never dies without a trace. |
