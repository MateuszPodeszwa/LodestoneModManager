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
