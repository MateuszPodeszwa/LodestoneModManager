import { randomUUID } from 'node:crypto'
import { isPatreonConfigured, buildAuthorizeUrl } from '~~/server/utils/patreon'

// Kick off the Patreon OAuth flow. Stores an anti-CSRF `state` in a short-lived
// cookie that the callback re-checks.
export default defineEventHandler(async (event) => {
  if (!isPatreonConfigured()) {
    return sendRedirect(event, '/supporter?error=unconfigured', 302)
  }

  const state = randomUUID()
  setCookie(event, 'patreon_oauth_state', state, {
    httpOnly: true,
    secure: !import.meta.dev,
    sameSite: 'lax',
    path: '/',
    maxAge: 600, // 10 minutes
  })

  return sendRedirect(event, buildAuthorizeUrl(state), 302)
})
