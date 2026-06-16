// The website-side "regenerate" cooldown. Matches the app's 1-hour code validity:
// after generating a key you wait an hour before generating another.
export const REGEN_WINDOW_MS = 60 * 60 * 1000

export function genState(lastAt: Date | number | null | undefined) {
  const last = lastAt ? (lastAt instanceof Date ? lastAt.getTime() : lastAt) : null
  const now = Date.now()
  const nextAt = last ? last + REGEN_WINDOW_MS : null
  const locked = !!(nextAt && now < nextAt)
  return { lastAt: last, nextAt, locked }
}
