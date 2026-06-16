// ╔══════════════════════════════════════════════════════════════════════╗
// ║  EDIT YOUR WEBSITE CONTENT HERE                                        ║
// ║                                                                        ║
// ║  This is the one file you edit to change text, links, pricing tiers,   ║
// ║  features and FAQ — no need to touch any other code. Save and the site ║
// ║  updates. (Version numbers, the changelog and download checksums are   ║
// ║  pulled automatically from GitHub Releases, so you don't edit those.)  ║
// ╚══════════════════════════════════════════════════════════════════════╝
export default defineAppConfig({
  // ── Links (used across nav, footer, buttons) ──────────────────────────
  links: {
    github: 'https://github.com/MateuszPodeszwa/LodestoneModManager',
    githubIssues: 'https://github.com/MateuszPodeszwa/LodestoneModManager/issues',
    patreon: 'https://www.patreon.com/c/mateuszpodeszwa',
    // Discord is "work in progress" — leave blank to keep it greyed out with a
    // hover tooltip. Paste an invite URL here to enable it everywhere at once.
    discord: '',
  },

  // ── Hero ──────────────────────────────────────────────────────────────
  hero: {
    badge: 'Free · Open source · No clutter',
    titleTop: 'Drag. Drop.',
    titleAccent: 'Done.',
    subtitle:
      'Lodestone is the simplest way to install and manage your Minecraft mods, resource packs and shaders. Drop a file on the window — it sorts out the rest. No folders, no config, ever.',
    // Quick facts shown under the hero buttons. Size auto-fills from the release;
    // {size} is replaced with the real installer size.
    facts: ['Windows 10/11', '{size}', 'also portable .zip'],
  },

  // ── About / feature grid ──────────────────────────────────────────────
  about: {
    eyebrow: 'What is Lodestone',
    title: 'One place for every mod,\npack and shader',
    subtitle:
      'No more digging through AppData, wrestling with loaders, or guessing which version fits. Lodestone handles the messy parts so you can just play.',
    features: [
      { icon: 'upload', tone: 'brand', title: 'Drag & drop install', body: 'Drop a .jar, .zip or shader anywhere on the window. Lodestone reads it and files it in the right folder automatically.' },
      { icon: 'compass', tone: 'sky', title: 'Modrinth, built in', body: 'Search Modrinth and install straight to your library with a single click. CurseForge support is on the way.' },
      { icon: 'refresh', tone: 'brand', title: 'One-click updates', body: "See what's outdated at a glance and bring everything to its latest compatible build in one tap." },
      { icon: 'layers', tone: 'grape', title: 'Version profiles', body: 'Keep separate sets of mods for 1.21, 1.20 and beyond. Switch profiles without touching a single file.' },
      { icon: 'toggle', tone: 'gold', title: "Toggle, don't delete", body: 'Flip mods on and off without losing them. Experiment freely and roll back the moment something breaks.' },
      { icon: 'check', tone: 'brand', title: 'Zero config', body: 'No editing JSON, no loader headaches, no broken installs. It detects what you have and just works.' },
    ],
  },

  // ── Tutorial (scroll-synced walkthrough) ──────────────────────────────
  tutorial: {
    eyebrow: 'How it works',
    title: 'From download to playing,\nin under a minute',
    subtitle: 'Scroll through a real walkthrough — the window on the right follows along with each step.',
    steps: [
      { kicker: '01 — Install', screen: 'home', title: 'Drop in any file', body: 'Grab a mod, resource pack or shader and drop it straight onto the window. Lodestone reads the file, works out exactly what it is, and installs it to the right folder — instantly.' },
      { kicker: '02 — Organize', screen: 'library', title: 'Manage your library', body: "Everything you've installed lives in one tidy list. Toggle mods on and off, organize them per Minecraft version, and uninstall in a single click — your setup stays exactly how you like it." },
      { kicker: '03 — Discover', screen: 'browse', title: 'Browse without leaving', body: 'Search Modrinth from inside the app. Filter by category, sort by downloads, and install with one click — no browser tabs, no zip files in your Downloads folder. CurseForge browsing is coming soon.' },
      { kicker: '04 — Support', screen: 'support', title: 'Support the project', body: 'Lodestone is free and open source, built by one person. Chip in through Patreon, drop a one-time tip, or star it on GitHub — every bit keeps the updates coming.' },
    ],
  },

  // ── Support / pricing ─────────────────────────────────────────────────
  support: {
    eyebrow: 'Support the project',
    title: 'Free — and always will be',
    subtitle:
      'No ads, no paywalls, no locked features. If Lodestone saves you time, a Patreon membership keeps development and hosting alive — and unlocks a few thank-you perks.',
    // Keep these in sync with your real Patreon tiers.
    tiers: [
      { name: 'Coffee', tagline: 'A small thanks', price: '$3', popular: false, perks: ['Supporter badge in the app', 'Our sincere gratitude'] },
      { name: 'Supporter', tagline: 'Keep the lights on', price: '$7', popular: true, perks: ['Everything in Coffee', 'Early access to beta builds', 'Your name in the credits'] },
      { name: 'Champion', tagline: 'Power the roadmap', price: '$15', popular: false, perks: ['Everything in Supporter', 'Vote on what we build next', 'Priority support'] },
    ],
    // Step-by-step shown under the tiers: how to turn a pledge into unlocked perks.
    claim: {
      title: 'Already supporting? Claim your code',
      subtitle: 'Turn your pledge into in-app perks in under a minute.',
      note: 'Each code is valid for one hour and is yours alone — we never store it, so keep it safe and please don’t share it.',
      steps: [
        { icon: 'patreon', title: 'Join on Patreon', body: 'Pick any tier above and become a patron — current or former patrons all count.' },
        { icon: 'login', title: 'Sign in with Patreon', body: 'Open the Supporter page and connect your account. We verify your pledge instantly — nothing else is touched.' },
        { icon: 'key', title: 'Generate your key', body: 'Create your one-hour membership code and copy it with a tap. Generate a fresh one whenever you need it.' },
        { icon: 'unlock', title: 'Activate in the app', body: 'Paste it into Lodestone → Settings → Supporter → Activate. Your badge and perks light up instantly.' },
      ],
    },
  },

  // ── Priority support / help page (/support) ───────────────────────────
  helpPage: {
    eyebrow: 'Help & support',
    title: 'Get help with Lodestone',
    subtitle:
      "Most answers are a click away. Supporters get a priority channel; everyone can open a GitHub issue and we'll take a look.",
    faq: [
      { q: 'How do I install a mod?', a: 'Just drag the .jar (mods) or .zip (resource packs / shaders) onto the Lodestone window. It detects the type and files it in the right folder automatically — no setup needed.' },
      { q: 'Is Lodestone really free?', a: 'Yes. Every mod-managing feature is free forever. Supporter perks are cosmetic/convenience only (a badge, accent themes, early beta access) and never gate core functionality.' },
      { q: 'How do I unlock my supporter perks?', a: 'Join on Patreon, then sign in on the Supporter page to generate a one-hour code. Paste it into the app under Settings → Supporter and your perks unlock permanently.' },
      { q: 'Does Lodestone work offline?', a: 'The app itself runs offline. Browsing Modrinth/CurseForge and checking for updates needs an internet connection, but your installed library is always available.' },
      { q: 'Where are my logs?', a: 'Open the app and go to Settings → Open logs. Attach anything relevant when you report a bug — it helps a lot.' },
    ],
  },

  // ── Footer ────────────────────────────────────────────────────────────
  footer: {
    tagline: 'The friendly, lightweight mod manager for Minecraft. Built by one developer, for the community.',
    legal:
      'Not an official Minecraft product. Not approved by or associated with Mojang or Microsoft.',
  },
})
