import { exchangeCode, fetchEligibility } from '~~/server/utils/patreon'
import { getDb } from '~~/server/utils/db'

// Patreon redirects here with ?code&state. We verify state, swap the code for a
// token, read the membership, persist a Supporter row, and open a session.
export default defineEventHandler(async (event) => {
  const { code, state, error } = getQuery(event)

  if (error) return sendRedirect(event, `/supporter?error=${encodeURIComponent(String(error))}`, 302)

  const expected = getCookie(event, 'patreon_oauth_state')
  deleteCookie(event, 'patreon_oauth_state', { path: '/' })
  if (!code || !state || !expected || state !== expected) {
    return sendRedirect(event, '/supporter?error=state', 302)
  }

  let eligibility
  try {
    const token = await exchangeCode(String(code))
    eligibility = await fetchEligibility(token.access_token)
  } catch (e) {
    console.error('[patreon] callback failed:', e)
    return sendRedirect(event, '/supporter?error=patreon', 302)
  }

  // Persist / refresh the supporter record (if a DB is configured).
  let supporterId: string | null = null
  let effectiveBeta = eligibility.betaAccess
  const db = getDb()
  if (db) {
    try {
      const row = await db.supporter.upsert({
        where: { patreonUserId: eligibility.patreonUserId },
        create: {
          patreonUserId: eligibility.patreonUserId,
          fullName: eligibility.fullName,
          email: eligibility.email,
          imageUrl: eligibility.imageUrl,
          patronStatus: eligibility.patronStatus,
          tierTitle: eligibility.tierTitle,
          currentlyEntitledCents: eligibility.currentlyEntitledCents,
          lifetimeSupportCents: eligibility.lifetimeSupportCents,
        },
        update: {
          fullName: eligibility.fullName,
          email: eligibility.email,
          imageUrl: eligibility.imageUrl,
          patronStatus: eligibility.patronStatus,
          tierTitle: eligibility.tierTitle,
          currentlyEntitledCents: eligibility.currentlyEntitledCents,
          lifetimeSupportCents: eligibility.lifetimeSupportCents,
          lastLoginAt: new Date(),
        },
      })
      supporterId = row.id
      // Manual override (betaOverride) wins over the tier-based default.
      if (row.betaOverride !== null && row.betaOverride !== undefined) effectiveBeta = row.betaOverride
    } catch (e) {
      console.error('[patreon] supporter upsert failed:', e)
    }
  }

  await setUserSession(event, {
    user: {
      patreonUserId: eligibility.patreonUserId,
      name: eligibility.fullName,
      email: eligibility.email,
      imageUrl: eligibility.imageUrl,
      isPatron: eligibility.isPatron,
      betaAccess: effectiveBeta,
      tierTitle: eligibility.tierTitle,
      patronStatus: eligibility.patronStatus,
      currentlyEntitledCents: eligibility.currentlyEntitledCents,
      supporterId,
    },
    loggedInAt: Date.now(),
  })

  return sendRedirect(event, '/supporter', 302)
})
