// Pulls releases/changelog/downloads from the GitHub Releases API so the site is
// always truthful and auto-updates the moment you cut a `vX.Y.Z` release.
// Results are cached in memory for a few minutes to stay well under rate limits.
import { createHash } from 'node:crypto'
import { marked } from 'marked'

export interface ReleaseAsset {
  name: string
  size: number
  url: string
  contentType: string
  downloadCount: number
  /** sha256 hex if GitHub exposes a digest for the asset (else resolved lazily). */
  sha256?: string
}

export interface NormalizedRelease {
  version: string // tag without leading "v"
  tag: string
  name: string
  date: string // ISO published_at
  prerelease: boolean
  draft: boolean
  notesMarkdown: string
  notesHtml: string
  htmlUrl: string
  assets: ReleaseAsset[]
  setup?: ReleaseAsset
  portable?: ReleaseAsset
}

interface CacheEntry<T> {
  at: number
  value: T
}
const TTL_MS = 5 * 60 * 1000
const cache = new Map<string, CacheEntry<unknown>>()
const shaCache = new Map<string, string>()

function getCached<T>(key: string): T | undefined {
  const hit = cache.get(key)
  if (hit && Date.now() - hit.at < TTL_MS) return hit.value as T
  return undefined
}
function setCached<T>(key: string, value: T) {
  cache.set(key, { at: Date.now(), value })
}

function ghHeaders() {
  const config = useRuntimeConfig()
  const headers: Record<string, string> = {
    Accept: 'application/vnd.github+json',
    'User-Agent': 'lodestone-website',
    'X-GitHub-Api-Version': '2022-11-28',
  }
  if (config.githubToken) headers.Authorization = `Bearer ${config.githubToken}`
  return headers
}

function pickAsset(assets: ReleaseAsset[], kind: 'setup' | 'portable'): ReleaseAsset | undefined {
  if (kind === 'setup') {
    return assets.find((a) => /setup/i.test(a.name) && /\.exe$/i.test(a.name)) ?? assets.find((a) => /\.exe$/i.test(a.name))
  }
  return (
    assets.find((a) => /portable/i.test(a.name) && /\.zip$/i.test(a.name)) ??
    assets.find((a) => /\.zip$/i.test(a.name) && !/\.nupkg$/i.test(a.name))
  )
}

function normalize(r: any): NormalizedRelease {
  const assets: ReleaseAsset[] = (r.assets ?? []).map((a: any) => ({
    name: a.name,
    size: a.size,
    url: a.browser_download_url,
    contentType: a.content_type,
    downloadCount: a.download_count ?? 0,
    sha256: typeof a.digest === 'string' && a.digest.startsWith('sha256:') ? a.digest.slice(7) : undefined,
  }))
  const markdown: string = r.body ?? ''
  return {
    version: String(r.tag_name ?? '').replace(/^v/, ''),
    tag: r.tag_name,
    name: r.name || r.tag_name,
    date: r.published_at || r.created_at,
    prerelease: !!r.prerelease,
    draft: !!r.draft,
    notesMarkdown: markdown,
    notesHtml: markdown ? (marked.parse(markdown, { async: false }) as string) : '',
    htmlUrl: r.html_url,
    assets,
    setup: pickAsset(assets, 'setup'),
    portable: pickAsset(assets, 'portable'),
  }
}

/** All published (non-draft) releases, newest first. */
export async function getReleases(): Promise<NormalizedRelease[]> {
  const cached = getCached<NormalizedRelease[]>('releases')
  if (cached) return cached
  const config = useRuntimeConfig()
  try {
    const raw = await $fetch<any[]>(`https://api.github.com/repos/${config.githubRepo}/releases?per_page=30`, {
      headers: ghHeaders(),
    })
    const list = raw.map(normalize).filter((r) => !r.draft)
    setCached('releases', list)
    return list
  } catch (e) {
    console.error('[github] failed to fetch releases:', e)
    return getCached<NormalizedRelease[]>('releases') ?? []
  }
}

/** Latest stable release (most recent non-prerelease), or newest of any kind. */
export async function getLatestRelease(): Promise<NormalizedRelease | null> {
  const all = await getReleases()
  if (!all.length) return null
  return all.find((r) => !r.prerelease) ?? all[0]
}

/** Latest prerelease (the "beta" channel), if any. */
export async function getLatestBeta(): Promise<NormalizedRelease | null> {
  const all = await getReleases()
  return all.find((r) => r.prerelease) ?? null
}

// ── Per-release changelog notes, derived from the commits in each release ──────
// GitHub release bodies are empty (Velopack doesn't write them), so the changelog
// lists the actual commits between tags instead - categorized like the design.
export interface ChangeNote {
  type: 'new' | 'improved' | 'fixed'
  text: string
}

// Conventional-commit type → changelog category.
const NOTE_CATEGORY: Record<string, ChangeNote['type']> = {
  feat: 'new',
  feature: 'new',
  add: 'new',
  fix: 'fixed',
  bugfix: 'fixed',
  hotfix: 'fixed',
  revert: 'fixed',
  perf: 'improved',
  refactor: 'improved',
  improve: 'improved',
  improvement: 'improved',
  change: 'improved',
  update: 'improved',
}
// Housekeeping commit types that don't belong in a user-facing changelog.
const NOTE_SKIP = new Set(['chore', 'ci', 'build', 'docs', 'doc', 'test', 'tests', 'style', 'release', 'deps', 'dep', 'wip'])
// Scopes outside the app itself (this is a monorepo) - kept out of the app changelog.
const NOTE_SKIP_SCOPE = new Set(['website', 'site', 'web', 'design', 'repo', 'meta', 'deploy', 'ci'])

function tidyNoteText(text: string): string {
  let t = text.trim().replace(/\s*\(#\d+\)\s*$/g, '').replace(/\s+/g, ' ')
  if (t) t = t.charAt(0).toUpperCase() + t.slice(1)
  return t.replace(/[.\s]+$/, '')
}

function commitToNote(subject: string): ChangeNote | null {
  const line = subject.trim()
  if (!line || /^merge\b/i.test(line)) return null
  // Only structured (conventional) commits make the changelog - keeps noise out.
  const m = line.match(/^(\w+)(?:\(([^)]*)\))?(!)?:\s*(.+)$/)
  if (!m) return null
  const type = m[1].toLowerCase()
  const scope = (m[2] ?? '').toLowerCase()
  if (NOTE_SKIP.has(type) || NOTE_SKIP_SCOPE.has(scope)) return null
  const text = tidyNoteText(m[4])
  return text ? { type: NOTE_CATEGORY[type] ?? 'improved', text } : null
}

/** Turn commit subjects into de-duplicated, capped changelog notes. */
export function commitsToNotes(subjects: string[], max = 24): ChangeNote[] {
  const out: ChangeNote[] = []
  const seen = new Set<string>()
  for (const s of subjects) {
    const note = commitToNote(s)
    if (!note) continue
    const key = note.text.toLowerCase()
    if (seen.has(key)) continue
    seen.add(key)
    out.push(note)
    if (out.length >= max) break
  }
  return out
}

// Published compares never change, so cache them indefinitely (no TTL).
const compareCache = new Map<string, string[]>()

/** Commit subjects added between two tags (newest first). Empty when base is null or on error. */
export async function getCommitSubjects(base: string | null, head: string): Promise<string[]> {
  if (!base) return []
  const key = `${base}...${head}`
  const cached = compareCache.get(key)
  if (cached) return cached
  const config = useRuntimeConfig()
  try {
    const res = await $fetch<any>(
      `https://api.github.com/repos/${config.githubRepo}/compare/${base}...${head}?per_page=100`,
      { headers: ghHeaders() },
    )
    const subjects: string[] = (res.commits ?? [])
      .map((c: any) => String(c.commit?.message ?? '').split('\n')[0].trim())
      .filter(Boolean)
    subjects.reverse() // GitHub returns oldest-first - show newest first
    compareCache.set(key, subjects)
    return subjects
  } catch (e) {
    console.error('[github] compare failed', key, e)
    return []
  }
}

/** Changelog notes for a release, derived from the commits since the previous tag. */
export async function getReleaseNotes(tag: string, previousTag: string | null): Promise<ChangeNote[]> {
  return commitsToNotes(await getCommitSubjects(previousTag, tag))
}

/**
 * SHA-256 (hex) for an asset. Uses GitHub's digest when present; otherwise streams
 * the asset once and caches the hash (keyed by URL).
 */
export async function getAssetSha256(asset: ReleaseAsset): Promise<string | null> {
  if (asset.sha256) return asset.sha256
  const cached = shaCache.get(asset.url)
  if (cached) return cached
  try {
    const buf = Buffer.from(await $fetch<ArrayBuffer>(asset.url, { responseType: 'arrayBuffer' }))
    const hex = createHash('sha256').update(buf).digest('hex')
    shaCache.set(asset.url, hex)
    return hex
  } catch (e) {
    console.error('[github] failed to hash asset', asset.name, e)
    return null
  }
}
