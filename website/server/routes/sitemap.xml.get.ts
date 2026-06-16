export default defineEventHandler((event) => {
  const site = useRuntimeConfig().public.siteUrl.replace(/\/+$/, '')
  // Public, indexable pages (the /supporter account area is intentionally excluded).
  const paths = ['/', '/download', '/changelog', '/support', '/report']
  const now = new Date().toISOString()

  const urls = paths
    .map(
      (p) =>
        `  <url><loc>${site}${p}</loc><lastmod>${now}</lastmod><changefreq>${p === '/changelog' ? 'weekly' : 'monthly'}</changefreq><priority>${p === '/' ? '1.0' : '0.7'}</priority></url>`,
    )
    .join('\n')

  setHeader(event, 'Content-Type', 'application/xml; charset=utf-8')
  return `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`
})
