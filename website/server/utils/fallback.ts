// Used only when the GitHub Releases API returns nothing (e.g. before the very
// first release has propagated). Keeps the site truthful rather than blank.
export const FALLBACK_RELEASE = {
  version: '0.1.0',
  codename: 'Spawn Point',
  date: '2026-06-16T00:00:00Z',
}

export function githubReleasesUrl(): string {
  const repo = useRuntimeConfig().githubRepo
  return `https://github.com/${repo}/releases`
}

export function formatBytes(bytes?: number): string {
  if (!bytes || bytes <= 0) return '—'
  const mb = bytes / (1024 * 1024)
  if (mb >= 1) return `${mb.toFixed(mb >= 10 ? 0 : 1)} MB`
  return `${Math.max(1, Math.round(bytes / 1024))} KB`
}
