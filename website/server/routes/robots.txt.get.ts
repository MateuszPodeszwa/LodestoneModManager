export default defineEventHandler((event) => {
  const site = useRuntimeConfig().public.siteUrl
  setHeader(event, 'Content-Type', 'text/plain; charset=utf-8')
  return [
    'User-agent: *',
    'Allow: /',
    'Disallow: /api/',
    'Disallow: /supporter', // account area
    '',
    `Sitemap: ${site}/sitemap.xml`,
    '',
  ].join('\n')
})
