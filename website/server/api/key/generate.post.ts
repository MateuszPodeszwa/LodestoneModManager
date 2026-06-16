import { issueSupporterCode } from '~~/server/utils/supporterCode'
import { getDb } from '~~/server/utils/db'
import { genState, REGEN_WINDOW_MS } from '~~/server/utils/keylock'

// Mint a fresh supporter code for the signed-in patron, enforcing the 1-hour
// regen cooldown. We store counts/timestamps — never the code itself.
export default defineEventHandler(async (event) => {
  const session = await requireUserSession(event)
  const user = session.user

  if (!user.isPatron) {
    throw createError({ statusCode: 403, statusMessage: 'Your Patreon membership could not be confirmed.' })
  }

  const privateKey = useRuntimeConfig().supporterPrivateKeyB64
  if (!privateKey) {
    throw createError({ statusCode: 503, statusMessage: 'Key signing is not configured on this server yet.' })
  }

  const db = getDb()

  // Resolve current cooldown from the DB (preferred) or the session.
  let lastAt: number | null = session.lastKeyGenAt ?? null
  let row = null
  if (db && user.supporterId) {
    row = await db.supporter.findUnique({ where: { id: user.supporterId } })
    lastAt = row?.lastKeyGenAt ? row.lastKeyGenAt.getTime() : null
  }

  const state = genState(lastAt)
  if (state.locked) {
    throw createError({
      statusCode: 429,
      statusMessage: 'A key was generated recently. Please wait before generating another.',
      data: { nextAt: state.nextAt },
    })
  }

  // Holder identifies the patron inside the signed code (shown in-app on redeem).
  const holder = user.email || user.name || `patreon:${user.patreonUserId}`
  const issued = issueSupporterCode(privateKey, holder)
  const now = Date.now()

  // Record the generation (best-effort).
  if (db && user.supporterId) {
    try {
      await db.supporter.update({
        where: { id: user.supporterId },
        data: { keyGenCount: { increment: 1 }, lastKeyGenAt: new Date(now) },
      })
      await db.keyGeneration.create({ data: { supporterId: user.supporterId, holder } })
    } catch (e) {
      console.error('[key] failed to record generation:', e)
    }
  }

  // Keep the cooldown working even without a DB (per-browser via the session).
  await setUserSession(event, { ...session, lastKeyGenAt: now })

  return {
    code: issued.code,
    holder,
    issuedAt: issued.iat * 1000,
    expiresAt: issued.expiresAt.getTime(),
    nextAt: now + REGEN_WINDOW_MS,
  }
})
