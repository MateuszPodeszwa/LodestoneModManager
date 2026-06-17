import { getReleases, getReleaseNotes } from '~~/server/utils/github'

// Full release history for the changelog page. Per-release notes are derived from
// the commits between tags (the GitHub release bodies are empty).
export default defineEventHandler(async () => {
  const releases = await getReleases()
  return await Promise.all(
    releases.map(async (r, i) => ({
      version: r.version,
      tag: r.tag,
      name: r.name,
      date: r.date,
      prerelease: r.prerelease,
      latest: i === 0,
      htmlUrl: r.htmlUrl,
      notesHtml: r.notesHtml,
      notesMarkdown: r.notesMarkdown,
      // releases are newest-first, so the previous (older) tag is the next item.
      notes: await getReleaseNotes(r.tag, releases[i + 1]?.tag ?? null),
    })),
  )
})
