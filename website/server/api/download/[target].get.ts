import { getLatestRelease, getLatestBeta } from '~~/server/utils/github'
import { githubReleasesUrl } from '~~/server/utils/fallback'

// Stable download links that always resolve to the current release asset:
//   /api/download/setup     → latest Windows installer (.exe)
//   /api/download/portable  → latest portable .zip
//   /api/download/beta      → latest prerelease installer (supporters only)
export default defineEventHandler(async (event) => {
  const target = getRouterParam(event, 'target')

  if (target === 'beta') {
    const session = await getUserSession(event)
    if (!session.user?.betaAccess) {
      // Not entitled - send them to the supporter page to unlock beta access.
      return sendRedirect(event, '/supporter?need=beta', 302)
    }
    const beta = await getLatestBeta()
    if (beta?.setup) return sendRedirect(event, beta.setup.url, 302)
    return sendRedirect(event, githubReleasesUrl(), 302)
  }

  const latest = await getLatestRelease()
  if (!latest) return sendRedirect(event, githubReleasesUrl(), 302)

  const asset = target === 'portable' ? latest.portable : latest.setup
  if (asset) return sendRedirect(event, asset.url, 302)

  // Fall back to the release page if the expected asset isn't found.
  return sendRedirect(event, latest.htmlUrl || githubReleasesUrl(), 302)
})
