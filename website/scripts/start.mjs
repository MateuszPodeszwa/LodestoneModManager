// Production entrypoint (used by `npm run start`, which Railway runs).
// 1. If a database is configured, apply pending Prisma migrations.
// 2. Boot the built Nuxt server (.output/server/index.mjs).
//
// Migrations are best-effort: if they fail, we still start the server so the
// public site stays up (supporter/Patreon features degrade gracefully).
import { spawnSync } from 'node:child_process'

if (process.env.DATABASE_URL) {
  console.log('[start] DATABASE_URL detected — running `prisma migrate deploy`…')
  const res = spawnSync('npx', ['prisma', 'migrate', 'deploy'], {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  })
  if (res.status !== 0) {
    console.warn('[start] prisma migrate deploy failed (status ' + res.status + ') — continuing anyway.')
  }
} else {
  console.warn('[start] No DATABASE_URL set — running without a database (supporter records disabled).')
}

console.log('[start] Booting Nuxt server…')
await import('../.output/server/index.mjs')
