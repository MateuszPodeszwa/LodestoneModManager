import { getLatestRelease, getAssetSha256 } from '~~/server/utils/github'

// SHA-256 checksums for the latest installer + portable zip (lazy, cached).
export default defineEventHandler(async () => {
  const latest = await getLatestRelease()
  if (!latest) return { available: false, setup: null, portable: null }

  const [setupSha, portableSha] = await Promise.all([
    latest.setup ? getAssetSha256(latest.setup) : Promise.resolve(null),
    latest.portable ? getAssetSha256(latest.portable) : Promise.resolve(null),
  ])

  return {
    available: true,
    version: latest.version,
    setup: latest.setup ? { name: latest.setup.name, sha256: setupSha } : null,
    portable: latest.portable ? { name: latest.portable.name, sha256: portableSha } : null,
  }
})
