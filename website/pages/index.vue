<script setup lang="ts">
const app = useAppConfig()
const { data: rel } = await useFetch('/api/releases/latest')

const versionLabel = computed(() => (rel.value ? `v${rel.value.version}` : ''))
const facts = computed(() =>
  app.hero.facts.map((f) => f.replace('{size}', rel.value?.sizeText ?? '~8 MB')),
)

useSeoMeta({
  title: '',
  description:
    'Lodestone is the simplest way to install and manage your Minecraft mods, resource packs and shaders on Windows. Drag, drop, done — no folders, no config.',
  ogTitle: 'Lodestone — the friendly Minecraft mod manager',
  ogDescription: 'Drag. Drop. Done. Install and manage Minecraft mods, packs and shaders the easy way.',
})

// Rich result for search engines (free Windows software).
const siteUrl = useRuntimeConfig().public.siteUrl
useHead({
  link: [{ rel: 'canonical', href: siteUrl }],
  script: [
    {
      type: 'application/ld+json',
      innerHTML: JSON.stringify({
        '@context': 'https://schema.org',
        '@type': 'SoftwareApplication',
        name: 'Lodestone',
        applicationCategory: 'UtilitiesApplication',
        operatingSystem: 'Windows 10, Windows 11',
        description:
          'The friendly, lightweight mod manager for Minecraft — drag-and-drop install for mods, resource packs and shaders, with built-in Modrinth & CurseForge browsing and one-click updates.',
        url: siteUrl,
        downloadUrl: `${siteUrl}/download`,
        offers: { '@type': 'Offer', price: '0', priceCurrency: 'USD' },
        author: { '@type': 'Person', name: 'Mateusz Podeszwa' },
      }),
    },
  ],
})

// ── Tutorial scroll-sync ────────────────────────────────────────────────
const steps = app.tutorial.steps
const activeStep = ref(0)
let onScroll: (() => void) | null = null
let raf = 0

onMounted(() => {
  const { $gsap } = useNuxtApp()
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches

  // Hero entrance
  if (reduced) {
    $gsap.set('.hero-rise', { opacity: 1, y: 0 })
  } else {
    $gsap.to('.hero-rise', { opacity: 1, y: 0, duration: 0.7, stagger: 0.08, ease: 'power3.out' })
  }

  // Sync the pinned window to whichever step is nearest the viewport centre.
  onScroll = () => {
    if (raf) return
    raf = requestAnimationFrame(() => {
      raf = 0
      const els = document.querySelectorAll('[data-tut-step]')
      if (!els.length) return
      const mid = window.innerHeight / 2
      let best = activeStep.value
      let bestDist = Infinity
      els.forEach((el) => {
        const r = el.getBoundingClientRect()
        const c = r.top + r.height / 2
        const d = Math.abs(c - mid)
        if (d < bestDist) {
          bestDist = d
          best = Number(el.getAttribute('data-tut-step')) || 0
        }
      })
      if (best !== activeStep.value) activeStep.value = best
    })
  }
  window.addEventListener('scroll', onScroll, { passive: true })
  onScroll()
})

onBeforeUnmount(() => {
  if (onScroll) window.removeEventListener('scroll', onScroll)
  if (raf) cancelAnimationFrame(raf)
})
</script>

<template>
  <div>
    <!-- ============ HERO ============ -->
    <section
      id="top"
      class="relative flex min-h-[calc(100vh-64px)] items-center overflow-hidden"
      style="background: radial-gradient(125% 115% at 14% 2%, #26352c 0%, #1b1d23 47%, #141519 100%)"
    >
      <div
        class="grid-backdrop pointer-events-none absolute inset-0"
        style="-webkit-mask-image: radial-gradient(circle at 28% 18%, #000 0%, transparent 72%); mask-image: radial-gradient(circle at 28% 18%, #000 0%, transparent 72%)"
      />
      <div
        class="pointer-events-none absolute -right-[6%] -top-[12%] h-[640px] w-[640px] rounded-full"
        style="background: radial-gradient(circle, rgba(90, 194, 109, 0.16), transparent 64%)"
      />

      <div class="relative mx-auto grid w-full max-w-wide items-center gap-14 px-6 py-16 sm:px-10 lg:grid-cols-[1.02fr_1fr]">
        <div>
          <div class="hero-rise inline-flex items-center gap-2.5 rounded-full border border-brand/30 bg-brand/[0.08] px-3 py-1.5">
            <span class="h-[7px] w-[7px] rounded-sm bg-brand" style="box-shadow: 0 0 8px #5ac26d" />
            <span class="font-pixel text-xs font-semibold uppercase tracking-[1.6px] text-[#7ed98f]">{{ app.hero.badge }}</span>
          </div>
          <h1 class="hero-rise mt-5 font-pixel font-bold leading-[0.96] tracking-[1px] text-[#f5f5f7]" style="font-size: clamp(50px, 7.4vw, 90px)">
            {{ app.hero.titleTop }}<br /><span class="text-brand" style="text-shadow: 0 6px 30px rgba(90, 194, 109, 0.35)">{{ app.hero.titleAccent }}</span>
          </h1>
          <p class="hero-rise mt-5 max-w-[480px] text-[17.5px] leading-relaxed text-[#a6a6ae]">{{ app.hero.subtitle }}</p>
          <div class="hero-rise mt-7 flex flex-wrap gap-3">
            <NuxtLink to="/download" class="btn-primary mc-clip px-6 py-[15px] text-[15.5px]">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#10221a" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 4v11" /><path d="m7 11 5 5 5-5" /><path d="M5 20h14" /></svg>
              Download for Windows
            </NuxtLink>
            <a :href="app.links.github" target="_blank" rel="noopener" class="btn-ghost px-6 py-[15px] text-[15.5px]">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.48 2 2 6.58 2 12.25c0 4.53 2.87 8.37 6.84 9.73.5.1.68-.22.68-.49l-.01-1.9c-2.78.62-3.37-1.21-3.37-1.21-.46-1.18-1.11-1.5-1.11-1.5-.91-.64.07-.62.07-.62 1 .07 1.53 1.06 1.53 1.06.9 1.56 2.36 1.11 2.94.85.09-.67.35-1.11.63-1.37-2.22-.26-4.55-1.14-4.55-5.06 0-1.12.39-2.03 1.03-2.75-.1-.26-.45-1.3.1-2.72 0 0 .84-.27 2.75 1.05a9.4 9.4 0 0 1 5 0c1.91-1.32 2.75-1.05 2.75-1.05.55 1.42.2 2.46.1 2.72.64.72 1.03 1.63 1.03 2.75 0 3.93-2.34 4.79-4.57 5.05.36.32.68.94.68 1.9l-.01 2.82c0 .27.18.6.69.49A10.02 10.02 0 0 0 22 12.25C22 6.58 17.52 2 12 2Z" /></svg>
              View on GitHub
            </a>
          </div>
          <div class="hero-rise mt-5 flex flex-wrap items-center gap-2.5 text-[13px] text-faint">
            <span v-if="versionLabel">{{ versionLabel }}</span>
            <template v-for="(f, i) in facts" :key="i">
              <span class="opacity-50">·</span><span>{{ f }}</span>
            </template>
          </div>
        </div>

        <div class="relative hidden lg:block">
          <div class="relative ml-auto aspect-[62/42] w-full max-w-[600px] animate-floatY">
            <div class="pointer-events-none absolute -inset-0.5 rounded-2xl" style="box-shadow: 0 50px 90px -28px rgba(0, 0, 0, 0.7), 0 0 90px -8px rgba(90, 194, 109, 0.22)" />
            <AppWindow screen="home" class="relative block h-full w-full" />
          </div>
          <div class="absolute -left-1.5 bottom-[6%] flex animate-floatChip items-center gap-2.5 rounded-xl border border-white/10 bg-[#26262b] px-3.5 py-2.5 shadow-[0_18px_38px_-14px_rgba(0,0,0,0.6)]">
            <div class="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-brand text-sm font-bold text-[#10130f]">S</div>
            <div><div class="text-[12.5px] font-semibold text-[#f0f0f2]">Sodium.jar</div><div class="text-[10.5px] text-brand">Drop to install →</div></div>
          </div>
        </div>
      </div>

      <a href="#about" class="absolute bottom-6 left-1/2 flex -translate-x-1/2 flex-col items-center gap-1.5 text-faint no-underline">
        <span class="font-pixel text-[11px] tracking-[1.5px]">SCROLL</span>
        <svg class="animate-bobDown" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m6 9 6 6 6-6" /></svg>
      </a>
    </section>

    <!-- ============ ABOUT ============ -->
    <section id="about" class="flex min-h-screen flex-col justify-center px-6 py-28 sm:px-10">
      <div class="mx-auto w-full max-w-content">
        <Reveal class="text-center">
          <div class="eyebrow">{{ app.about.eyebrow }}</div>
          <h2 class="mt-3.5 whitespace-pre-line font-pixel font-bold leading-[1.05] tracking-[0.5px] text-[#f3f3f5]" style="font-size: clamp(30px, 4.2vw, 48px)">{{ app.about.title }}</h2>
          <p class="mx-auto mt-4 max-w-[580px] text-[17px] leading-relaxed text-muted">{{ app.about.subtitle }}</p>
        </Reveal>

        <div class="mt-14 grid gap-[18px] sm:grid-cols-2 lg:grid-cols-3">
          <Reveal v-for="(f, i) in app.about.features" :key="f.title" :delay="(i % 3) * 0.06" class="card p-[26px]">
            <FeatureIcon :name="f.icon" :tone="f.tone as any" />
            <div class="mt-4 text-lg font-semibold text-[#f0f0f2]">{{ f.title }}</div>
            <div class="mt-2 text-sm leading-relaxed text-muted">{{ f.body }}</div>
          </Reveal>
        </div>
      </div>
    </section>

    <!-- ============ TUTORIAL ============ -->
    <section id="tutorial" class="px-6 py-24 sm:px-10" style="background: linear-gradient(180deg, #141519, #16181d 50%, #141519)">
      <div class="mx-auto max-w-content">
        <Reveal class="text-center">
          <div class="eyebrow">{{ app.tutorial.eyebrow }}</div>
          <h2 class="mt-3.5 whitespace-pre-line font-pixel font-bold leading-[1.05] tracking-[0.5px] text-[#f3f3f5]" style="font-size: clamp(30px, 4.2vw, 48px)">{{ app.tutorial.title }}</h2>
          <p class="mx-auto mt-4 max-w-[560px] text-[17px] leading-relaxed text-muted">{{ app.tutorial.subtitle }}</p>
        </Reveal>

        <div class="mt-8 grid gap-14 lg:grid-cols-[1fr_1.08fr]">
          <!-- steps -->
          <div class="relative">
            <div class="pointer-events-none absolute left-[21px] top-[32vh] bottom-[32vh] w-0.5" style="background: linear-gradient(180deg, rgba(255, 255, 255, 0.13), rgba(255, 255, 255, 0.05))" />
            <div
              v-for="(s, i) in steps"
              :key="i"
              :data-tut-step="i"
              class="flex min-h-[64vh] items-center"
            >
              <div class="flex items-start gap-6">
                <div
                  class="relative z-[1] flex h-11 w-11 flex-none items-center justify-center rounded-full font-pixel text-[19px] font-bold transition-all duration-300"
                  :style="
                    activeStep === i
                      ? 'background:#5ac26d;color:#10130f;border:1px solid rgba(90,194,109,0.9);box-shadow:0 0 0 5px #15161b, 0 0 32px -2px rgba(90,194,109,0.65)'
                      : 'background:#1b1d22;color:#74747c;border:1px solid rgba(255,255,255,0.13);box-shadow:0 0 0 5px #15161b'
                  "
                >
                  {{ i + 1 }}
                </div>
                <div class="pt-0.5 transition-all duration-300" :style="activeStep === i ? 'opacity:1' : 'opacity:.34'">
                  <div class="font-pixel text-xs font-semibold uppercase tracking-[2px] text-brand">{{ s.kicker }}</div>
                  <div class="mt-2 text-[23px] font-bold tracking-[-0.3px] text-[#f3f3f5]">{{ s.title }}</div>
                  <div class="mt-3 max-w-[430px] text-[15.5px] leading-relaxed text-[#a6a6ae]">{{ s.body }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- pinned window -->
          <div>
            <div class="sticky top-[90px] hidden h-[calc(100vh-150px)] items-center justify-center lg:flex">
              <div class="relative aspect-[62/42] max-h-full w-full max-w-[640px]" style="filter: drop-shadow(0 44px 80px rgba(0, 0, 0, 0.6))">
                <AppWindow :screen="steps[activeStep].screen" class="relative block h-full w-full" />
                <div class="absolute right-4 top-3.5 rounded-full border border-brand/30 px-2.5 py-[5px] font-pixel text-[13px] font-semibold tracking-[1px] text-brand" style="background: rgba(13, 14, 17, 0.7)">
                  {{ '0' + (activeStep + 1) }} / 0{{ steps.length }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ============ DOWNLOAD CTA ============ -->
    <section id="get" class="relative flex min-h-screen flex-col justify-center overflow-hidden px-6 py-28 sm:px-10" style="background: radial-gradient(120% 100% at 80% 100%, #20302a 0%, #15161b 55%)">
      <div class="grid-backdrop pointer-events-none absolute inset-0" style="-webkit-mask-image: radial-gradient(circle at 75% 90%, #000 0%, transparent 70%); mask-image: radial-gradient(circle at 75% 90%, #000 0%, transparent 70%)" />
      <Reveal class="relative mx-auto w-full max-w-[880px] text-center">
        <div class="eyebrow">Get Lodestone</div>
        <h2 class="mt-3.5 font-pixel font-bold leading-[1.05] tracking-[0.5px] text-[#f3f3f5]" style="font-size: clamp(30px, 4.2vw, 48px)">Ready when you are</h2>
        <p class="mx-auto mt-4 max-w-[520px] text-[17px] leading-relaxed text-muted">Free forever, around {{ rel?.sizeText ?? '8 MB' }}, and no account needed just to download. Updates are built right into the app.</p>

        <div class="mt-9 rounded-[18px] border border-white/[0.08] bg-[rgba(28,28,32,0.8)] p-8 shadow-[0_40px_80px_-30px_rgba(0,0,0,0.6)]">
          <div class="flex items-center justify-center gap-3">
            <svg width="30" height="30" viewBox="0 0 24 24" fill="#5ac26d"><path d="M3 5.5 10.2 4.5v6.8H3V5.5Zm0 13L10.2 19.5v-6.7H3v5.7Zm8.3 1.2L21 21V12.8h-9.7v6.9ZM11.3 4.3 21 3v8.2h-9.7V4.3Z" /></svg>
            <div class="text-left">
              <div class="text-xl font-bold text-[#f3f3f5]">Lodestone for Windows</div>
              <div class="text-[13px] text-muted">{{ versionLabel }} · Windows 10 &amp; 11 · 64-bit · {{ rel?.sizeText ?? '8 MB' }}</div>
            </div>
          </div>
          <NuxtLink to="/download" class="btn-primary mc-clip mt-6 px-[30px] py-[15px] text-base">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#10221a" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 4v11" /><path d="m7 11 5 5 5-5" /><path d="M5 20h14" /></svg>
            Download the installer
          </NuxtLink>
          <div class="mt-6 flex flex-wrap justify-center gap-2.5">
            <span class="inline-flex items-center gap-1.5 rounded-full border border-brand/30 bg-brand/10 px-3.5 py-2 text-[12.5px] font-semibold text-[#7ed98f]"><span class="h-1.5 w-1.5 rounded-full bg-brand" />Windows installer</span>
            <span class="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/5 px-3.5 py-2 text-[12.5px] font-semibold text-soft">Portable .zip</span>
            <span class="inline-flex items-center gap-1.5 rounded-full border border-white/[0.06] bg-white/[0.02] px-3.5 py-2 text-[12.5px] font-semibold text-[#6a6a72]">Auto-updates built in</span>
          </div>
          <NuxtLink to="/download" class="mt-5 inline-block text-sm font-semibold text-brand no-underline hover:underline">All download options &amp; checksums →</NuxtLink>
        </div>
      </Reveal>
    </section>

    <!-- ============ SUPPORT ============ -->
    <LandingSupport />
  </div>
</template>
