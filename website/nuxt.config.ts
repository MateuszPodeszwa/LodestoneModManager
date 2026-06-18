// Nuxt configuration for the Lodestone website (lodestonemc.net).
// Most *content* you'll want to edit lives in `app.config.ts`, not here.
// This file wires modules, SEO defaults and the server runtime config (secrets).
export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',

  // SSR is on: pages are rendered on the server so search engines and link
  // previews see real HTML (good SEO + social cards).
  ssr: true,

  modules: [
    '@nuxtjs/tailwindcss',
    'nuxt-auth-utils',
  ],

  css: ['~/assets/css/main.css'],

  // Runtime config. Anything under `public` is exposed to the browser; everything
  // else is server-only (never shipped to the client). Values are populated from
  // environment variables in production - see `.env.example`.
  runtimeConfig: {
    // ── Server-only secrets ──────────────────────────────────────────────
    // PKCS#8 ECDSA P-256 private key (base64, single line) used to SIGN supporter
    // codes. Must match the public key embedded in the app (SupporterKeys.DefaultPublicKey).
    supporterPrivateKeyB64: '',
    // Optional: paste the matching public key to self-check signatures on boot.
    supporterPublicKeyB64:
      'MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEIuvwsBO94NHMwXbse+TRTibyDfT5Z9XSRl+8ChQAbZnoom2TRJn8s2elR3Jb5jx7EMdquQgiwT5jtxxAi/JYvg==',

    // Patreon OAuth (create an app at https://www.patreon.com/portal/registration/register-clients)
    patreonClientId: '',
    patreonClientSecret: '',
    patreonRedirectUri: '', // e.g. https://lodestonemc.net/api/auth/patreon/callback
    patreonCampaignId: '', // optional - restrict to your campaign; blank = accept any returned membership
    // Owner/teammate allowlist (comma-separated). A campaign owner is NOT a patron of their own campaign,
    // so list your Patreon user id and/or login email here to be recognised as a supporter (+ beta).
    patreonOwnerIds: '', // e.g. 12345678  (PATREON_OWNER_IDS)
    patreonOwnerEmails: '', // e.g. podinatubie@gmail.com  (PATREON_OWNER_EMAILS)

    // GitHub source for releases/changelog/downloads. A token is optional and only
    // raises the anonymous API rate limit; the repo is public so none is required.
    githubRepo: 'MateuszPodeszwa/LodestoneModManager',
    githubToken: '',

    // ── Public (safe to expose to the browser) ───────────────────────────
    public: {
      siteUrl: 'https://lodestonemc.net',
    },
  },

  app: {
    // Smooth cross-page fade - see assets/css/main.css (.page-* classes).
    pageTransition: { name: 'page', mode: 'out-in' },
    head: {
      htmlAttrs: { lang: 'en' },
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'theme-color', content: '#141519' },
      ],
      // Set `has-js` before paint so GSAP reveal styles never flash for no-JS users.
      script: [
        {
          innerHTML: "document.documentElement.classList.add('has-js')",
          tagPosition: 'head',
          tagPriority: 'critical',
        },
      ],
      link: [
        { rel: 'icon', type: 'image/png', href: '/icon.png' },
        { rel: 'apple-touch-icon', href: '/apple-touch-icon.png' },
        { rel: 'manifest', href: '/site.webmanifest' },
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        {
          rel: 'stylesheet',
          href: 'https://fonts.googleapis.com/css2?family=Pixelify+Sans:wght@400;500;600;700&display=swap',
        },
      ],
    },
  },

  // Railway runs a Node server. The default 'node-server' Nitro preset is correct;
  // we set it explicitly so `node .output/server/index.mjs` always works.
  nitro: {
    preset: 'node-server',
  },

  typescript: {
    strict: true,
  },

  devtools: { enabled: true },
})
