import { getDb } from '~~/server/utils/db'
import { isPatreonConfigured } from '~~/server/utils/patreon'
import { genState, REGEN_WINDOW_MS } from '~~/server/utils/keylock'

// Current auth + key-generation status, used by the supporter page.
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const signingConfigured = !!config.supporterPrivateKeyB64
  const patreonConfigured = isPatreonConfigured()

  const session = await getUserSession(event)
  if (!session.user) {
    return { loggedIn: false, patreonConfigured, signingConfigured }
  }

  const user = session.user
  let lastAt: number | null = session.lastKeyGenAt ?? null
  let count = 0

  const db = getDb()
  if (db && user.supporterId) {
    const row = await db.supporter.findUnique({ where: { id: user.supporterId } })
    if (row) {
      lastAt = row.lastKeyGenAt ? row.lastKeyGenAt.getTime() : null
      count = row.keyGenCount
    }
  }

  const state = genState(lastAt)
  return {
    loggedIn: true,
    user,
    patreonConfigured,
    signingConfigured,
    gen: {
      count,
      lastAt: state.lastAt,
      nextAt: state.nextAt,
      locked: state.locked,
      windowMs: REGEN_WINDOW_MS,
      canGenerate: user.isPatron && signingConfigured && !state.locked,
    },
  }
})
