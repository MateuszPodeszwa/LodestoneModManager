// Regenerate the site's favicon / PWA icons / OG image from the app's source PNG.
//
//   cd website && npm i sharp --no-save && node scripts/gen-icons.mjs
//
// Outputs are committed to public/; `sharp` is only needed when you re-run this
// (it is intentionally NOT a project dependency, so Railway builds stay light).
import sharp from 'sharp'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))
const SOURCE = resolve(here, '../../src/Lodestone.App/Assets/lodestone-source.png')
const PUB = resolve(here, '../public')

const sizes = [
  { name: 'icon.png', size: 256 },
  { name: 'icon-192.png', size: 192 },
  { name: 'icon-512.png', size: 512 },
  { name: 'apple-touch-icon.png', size: 180 },
]

for (const { name, size } of sizes) {
  await sharp(SOURCE).resize(size, size, { fit: 'cover' }).png().toFile(resolve(PUB, name))
  console.log('✓', name, `(${size}×${size})`)
}

// Open Graph / Twitter card: 1200×630, brand-dark background, icon on the left,
// wordmark on the right. Text is drawn as SVG (no external font dependency).
const iconBuf = await sharp(SOURCE).resize(320, 320, { fit: 'cover' }).png().toBuffer()
const bg = Buffer.from(`
<svg width="1200" height="630" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <radialGradient id="g" cx="20%" cy="10%" r="90%">
      <stop offset="0%" stop-color="#26352c"/>
      <stop offset="55%" stop-color="#1b1d23"/>
      <stop offset="100%" stop-color="#141519"/>
    </radialGradient>
  </defs>
  <rect width="1200" height="630" fill="url(#g)"/>
  <text x="600" y="300" font-family="Segoe UI, Arial, sans-serif" font-size="92" font-weight="700" fill="#f5f5f7">Lodestone</text>
  <text x="600" y="372" font-family="Segoe UI, Arial, sans-serif" font-size="34" fill="#9a9aa2">The friendly Minecraft mod manager</text>
  <text x="600" y="430" font-family="Segoe UI, Arial, sans-serif" font-size="26" fill="#5ac26d">Drag. Drop. Done.</text>
</svg>`)

await sharp(bg)
  .composite([{ input: iconBuf, left: 200, top: 155 }])
  .png()
  .toFile(resolve(PUB, 'og.png'))
console.log('✓ og.png (1200×630)')
