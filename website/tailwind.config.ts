import type { Config } from 'tailwindcss'

// Design tokens lifted straight from the Claude-Design prototype so the site
// matches the app's look. Use these as Tailwind classes, e.g. `text-brand`,
// `bg-surface`, `font-pixel`.
export default <Partial<Config>>{
  content: [
    './components/**/*.{vue,js,ts}',
    './layouts/**/*.vue',
    './pages/**/*.vue',
    './composables/**/*.{js,ts}',
    './plugins/**/*.{js,ts}',
    './app.vue',
    './app.config.ts',
    './error.vue',
  ],
  theme: {
    extend: {
      colors: {
        // Page + surfaces
        ink: '#141519', // page background
        'surface-1': '#1c1c20',
        'surface-2': '#26262b',
        'surface-3': '#2a2a30',
        'surface-rail': '#171719',
        'surface-foot': '#121317',
        // Brand green
        brand: {
          DEFAULT: '#5ac26d',
          light: '#6fce80',
          dark: '#46a85a',
          ink: '#10221a', // text/icon color on a green button
        },
        // Accents used across cards/badges
        pink: '#e2719a',
        sky: '#5a91c2',
        grape: '#bb78d6',
        gold: '#d2a96a',
        // Text shades
        body: '#e6e6ea',
        soft: '#c2c2c8',
        muted: '#9a9aa2',
        faint: '#76767e',
        dim: '#8e8e96',
      },
      fontFamily: {
        pixel: ['"Pixelify Sans"', 'sans-serif'],
        sans: [
          '"Segoe UI Variable Display"',
          '"Segoe UI"',
          'system-ui',
          '-apple-system',
          'sans-serif',
        ],
        mono: ['ui-monospace', 'Consolas', 'monospace'],
      },
      maxWidth: {
        content: '1180px',
        wide: '1240px',
      },
      keyframes: {
        revUp: {
          from: { opacity: '0', transform: 'translateY(36px)' },
          to: { opacity: '1', transform: 'none' },
        },
        floatY: {
          '0%,100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-13px)' },
        },
        floatChip: {
          '0%,100%': { transform: 'translateY(0) rotate(-4deg)' },
          '50%': { transform: 'translateY(-9px) rotate(-4deg)' },
        },
        bobDown: {
          '0%,100%': { transform: 'translateY(0)', opacity: '0.7' },
          '50%': { transform: 'translateY(6px)', opacity: '1' },
        },
        spin: { to: { transform: 'rotate(360deg)' } },
        amFade: {
          from: { opacity: '0', transform: 'translateY(5px)' },
          to: { opacity: '1', transform: 'none' },
        },
      },
      animation: {
        revUp: 'revUp .7s ease both',
        floatY: 'floatY 7s ease-in-out infinite',
        floatChip: 'floatChip 5.5s ease-in-out infinite',
        bobDown: 'bobDown 1.8s ease-in-out infinite',
        spin: 'spin .8s linear infinite',
        amFade: 'amFade .28s ease',
      },
    },
  },
  plugins: [],
}
