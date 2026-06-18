# Lodestone website (`lodestonemc.net`)

The marketing and supporter site for Lodestone, the friendly Minecraft mod manager. It is a single
[Nuxt 3](https://nuxt.com) app (Vue 3, Tailwind, GSAP) with a small server layer that does three
things:

- pulls the version, changelog, downloads and checksums live from the app's GitHub Releases, so the
  site stays truthful and refreshes itself whenever you cut a release;
- verifies patrons through Patreon OAuth and mints real supporter codes the desktop app accepts
  (signed with your ECDSA key);
- keeps track of supporters (tier, pledge, key-generation counts, beta access) in Postgres.

> The app deep-links here. `DonateViewModel` opens `/supporter` (claim a code) and `/support`
> (priority support). Both pages live in this project.

---

## 1. Editing content (no coding)

Almost all text, links and pricing sit in **[`app.config.ts`](./app.config.ts)**. Open it, edit the
strings, save. That covers the hero, feature grid, tutorial steps, the home trust strip, support
tiers, FAQ, release codenames (`releases.names`), the footer, and the GitHub / Patreon / Discord
links.

Some things you do not edit by hand, because they come from GitHub automatically:

- the current version number (hero, download page, changelog);
- the changelog entries, which are generated from the commits in each release;
- download links and SHA-256 checksums.

The changelog is built from the commits between release tags (the GitHub Release bodies are left
empty). Conventional-commit subjects (`feat:`, `fix:`, `perf:` and so on) become the NEW / IMPROVED
/ FIXED lines, while housekeeping types (`docs`, `ci`, `chore`) and non-app scopes (`website`,
`design`) are filtered out. So cutting a `vX.Y.Z` release populates its own notes; you just need to
write clear commit messages. The one editorial touch is the optional per-release codename badge
(for example "Spawn Point"), set in `app.config.ts` under `releases.names`.

### Discord (work in progress)

Discord is greyed out with a "coming soon" tooltip everywhere until you paste an invite URL into
`links.discord` in `app.config.ts`. That single edit enables it site-wide.

---

## 2. Local development

```bash
cd website
cp .env.example .env       # then fill in values (see below)
npm install
npm run dev                # http://localhost:3000
```

You do not need a database or Patreon keys just to view the site. Those features degrade gracefully
when their env vars are missing.

Useful scripts:

| Script | What it does |
| --- | --- |
| `npm run dev` | Dev server with HMR |
| `npm run build` | Production build (`prisma generate` then `nuxt build`) |
| `npm run start` | Apply DB migrations (if `DATABASE_URL` is set) then serve `.output` |
| `npm run db:studio` | Browse the database (Prisma Studio) |
| `node scripts/gen-icons.mjs` | Regenerate favicons/OG from the app's source PNG (needs `npm i sharp --no-save` first) |

---

## 3. Environment variables

Set these in **Railway → your service → Variables** (and in `.env` for local dev). Full descriptions
are in [`.env.example`](./.env.example).

| Variable | Required | Purpose |
| --- | --- | --- |
| `NUXT_SESSION_PASSWORD` | Yes | 32+ random chars; encrypts the login cookie. `openssl rand -base64 32` |
| `NUXT_SUPPORTER_PRIVATE_KEY_B64` | For key gen | Contents of `keys/supporter.private.b64` (the app's signing key) |
| `NUXT_PATREON_CLIENT_ID` / `_SECRET` | For sign-in | Patreon OAuth client credentials |
| `NUXT_PATREON_REDIRECT_URI` | For sign-in | `https://lodestonemc.net/api/auth/patreon/callback` |
| `NUXT_PATREON_CAMPAIGN_ID` | Optional | Restrict eligibility to your campaign |
| `NUXT_GITHUB_REPO` | Optional | Defaults to `MateuszPodeszwa/LodestoneModManager` |
| `NUXT_GITHUB_TOKEN` | Optional | Raises the GitHub API rate limit |
| `DATABASE_URL` | Auto on Railway | Postgres connection (the plugin injects it) |
| `NUXT_PUBLIC_SITE_URL` | Recommended | Canonical URL, e.g. `https://lodestonemc.net` |

---

## 4. Supporter keys, how it works

The desktop app verifies offline, signed codes of the form `base64url(payload).base64url(signature)`,
where the payload is `{"v":1,"k":"supporter","h":<holder>,"iat":<unix-secs>}` signed with ECDSA P-256
/ SHA-256 (IEEE-P1363). This site signs exactly that shape with the private key matching the public
key embedded in the app (`SupporterKeys.DefaultPublicKey`).

- Codes go only to active, paying patrons. Former or declined patrons and free followers do not
  qualify (the `NUXT_PATREON_OWNER_*` allowlist is the one exception).
- The private key lives only in `NUXT_SUPPORTER_PRIVATE_KEY_B64`, a server secret. It is never sent
  to the browser.
- A code is valid to redeem for one hour, enforced by the app. The site also enforces a one-hour
  regenerate cooldown per patron, shown as a live countdown.
- We store generation counts and timestamps, never the code strings. Patrons are told they are
  responsible for their key and asked not to redistribute it.

You can sanity-check that a generated code is accepted by the real app:

```bash
# from the repo root
dotnet run --project src/Lodestone.Cli -- verify --pub "@keys/supporter.public.b64" --code <code>
# → VALID  holder=…  (redeemable for 1h after issue)
```

> If the private key ever leaks: run `dotnet run --project src/Lodestone.Cli -- keygen`, update
> `SupporterKeys.DefaultPublicKey`, ship an app update, and set the new private key here. Old codes
> simply stop verifying.

### Patreon OAuth setup

1. Create a client at <https://www.patreon.com/portal/registration/register-clients>.
2. Add the redirect URI `https://lodestonemc.net/api/auth/patreon/callback`.
3. Copy the Client ID and Secret into the env vars above.
4. Optionally set `NUXT_PATREON_CAMPAIGN_ID` to your campaign's numeric id to restrict eligibility.

---

## 5. Deploy to Railway

1. **New Project → Deploy from GitHub repo**, and pick this repository.
2. In the service **Settings → Build**, set **Root Directory** to `website`. Railway then uses this
   folder's `package.json` / `railway.json`.
3. In **Settings → Build**, set **Watch Paths** to `/website/**`. This site lives in a monorepo
   alongside the desktop app, and Watch Paths makes Railway redeploy only when website files change,
   so pure app commits do not trigger a website rebuild. (Root Directory controls what is built;
   Watch Paths controls what triggers a deploy. Set both.)
4. **Add a Postgres database** with *New → Database → PostgreSQL*. Railway injects `DATABASE_URL`
   into your service automatically.
5. Add the environment variables from the table above (at minimum `NUXT_SESSION_PASSWORD`; add the
   Patreon and signing-key vars to enable the supporter flow).
6. Deploy. Railway runs `npm run build`, then `npm run start`, which applies the Prisma migration and
   boots the server. The healthcheck hits `/`. Every later `git push` that touches `website/`
   auto-deploys.
7. Point your domain (`lodestonemc.net`) at the service under **Settings → Networking → Custom
   Domain**, and set `NUXT_PUBLIC_SITE_URL` to the final URL.

First deploy with no GitHub releases yet? The site still shows a truthful fallback
(`v0.1.0 "Spawn Point"`) and links to the GitHub releases page.

> You rarely need to redeploy. Version, changelog, downloads and checksums are fetched from the
> GitHub Releases API at runtime, so cutting a `vX.Y.Z` release updates the live site within a few
> minutes with no push and no redeploy. Only code or `app.config.ts` content changes need a push,
> which auto-deploys via Watch Paths.

---

## 6. Project structure

```
website/
├─ app.config.ts          ← editable site content (start here)
├─ nuxt.config.ts         ← modules, SEO defaults, runtime config
├─ pages/                 ← /, /download, /changelog, /supporter, /support, /report
├─ components/            ← Nav, Footer, AppWindow (in-app mock), CursorSword, …
├─ composables/           ← useToast, useFormat
├─ plugins/gsap.client.ts ← scroll-reveal animations
├─ server/
│  ├─ api/                ← releases, checksums, download, auth/patreon, me, key/generate
│  ├─ routes/             ← robots.txt, sitemap.xml
│  └─ utils/              ← github, patreon, supporterCode (signing), db, keylock
├─ prisma/schema.prisma   ← Supporter + KeyGeneration models
└─ public/                ← favicons/OG (from the app icon), stone-sword cursor
```

---

Not an official Minecraft product. Not approved by or associated with Mojang or Microsoft.
