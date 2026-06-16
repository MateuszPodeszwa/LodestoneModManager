// Optional Prisma client. Returns null when DATABASE_URL is unset so the site
// still runs (supporter records / regen-lock simply degrade) in local dev or a
// misconfigured deploy. In production on Railway, DATABASE_URL is injected.
import { PrismaClient } from '@prisma/client'

declare global {
  // eslint-disable-next-line no-var
  var __lodestonePrisma: PrismaClient | null | undefined
}

export function getDb(): PrismaClient | null {
  if (globalThis.__lodestonePrisma !== undefined) return globalThis.__lodestonePrisma
  if (!process.env.DATABASE_URL) {
    console.warn('[db] DATABASE_URL not set — running without a database.')
    globalThis.__lodestonePrisma = null
    return null
  }
  globalThis.__lodestonePrisma = new PrismaClient()
  return globalThis.__lodestonePrisma
}
