<script setup lang="ts">
const app = useAppConfig()
</script>

<template>
  <section id="support" class="relative flex min-h-screen flex-col justify-center overflow-hidden px-6 py-28 sm:px-10">
    <!-- animated gradient blobs -->
    <div class="pointer-events-none absolute -left-[6%] top-[8%] h-[560px] w-[560px] animate-floatY rounded-full" style="background: radial-gradient(circle, rgba(226, 113, 154, 0.16), transparent 64%)" />
    <div class="pointer-events-none absolute -right-[10%] bottom-[4%] h-[520px] w-[520px] rounded-full" style="background: radial-gradient(circle, rgba(90, 194, 109, 0.12), transparent 64%)" />

    <div class="relative mx-auto w-full max-w-[1080px]">
      <Reveal class="text-center">
        <div class="inline-flex items-center gap-2.5">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e2719a" stroke-width="1.9" stroke-linejoin="round" stroke-linecap="round"><path d="M12 20s-7-4.7-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.3-7 10-7 10Z" /></svg>
          <span class="font-pixel text-[13px] font-semibold uppercase tracking-[2.4px] text-pink">{{ app.support.eyebrow }}</span>
        </div>
        <h2 class="mt-3.5 font-pixel font-bold leading-[1.05] tracking-[0.5px]" style="font-size: clamp(30px, 4.2vw, 48px)">
          <span class="bg-gradient-to-r from-[#f3f3f5] via-[#f3f3f5] to-[#f7c6da] bg-clip-text text-transparent">{{ app.support.title }}</span>
        </h2>
        <p class="mx-auto mt-4 max-w-[560px] text-[17px] leading-relaxed text-muted">{{ app.support.subtitle }}</p>
      </Reveal>

      <!-- Patreon widget (default - shown when no tiers are configured). Full-width to
           match the "Claim your code" widget below it. -->
      <Reveal v-if="!app.support.tiers.length" class="mt-12">
        <div
          class="group relative overflow-hidden rounded-3xl border border-pink/30 p-8 text-center transition-transform duration-300 hover:-translate-y-1 sm:p-10"
          style="background: linear-gradient(135deg, rgba(226,113,154,0.12), rgba(90,194,109,0.06))"
        >
          <div class="pointer-events-none absolute -top-24 left-1/2 h-60 w-60 -translate-x-1/2 rounded-full opacity-70 transition-opacity duration-300 group-hover:opacity-100" style="background: radial-gradient(circle, rgba(226,113,154,0.28), transparent 70%)" />
          <div class="relative">
            <div class="mx-auto flex h-16 w-16 animate-floatY items-center justify-center rounded-2xl shadow-[0_12px_28px_-8px_rgba(236,95,91,0.6)]" style="background: linear-gradient(140deg, #ec5f5b, #e2719a)">
              <svg width="30" height="30" viewBox="0 0 24 24" fill="#fff"><circle cx="15" cy="9.2" r="6.2" /><rect x="2.5" y="2.6" width="3.6" height="18.8" rx="0.4" /></svg>
            </div>
            <h3 class="mt-5 font-pixel text-2xl font-bold text-white">{{ app.support.patreon.heading }}</h3>
            <p class="mx-auto mt-3 max-w-[460px] text-[15px] leading-relaxed text-muted">{{ app.support.patreon.blurb }}</p>

            <div class="mt-6 flex flex-wrap justify-center gap-2.5">
              <span v-for="perk in app.support.patreon.perks" :key="perk" class="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.04] px-3.5 py-1.5 text-[13px] text-soft">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="#5ac26d" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4 10-10" /></svg>
                {{ perk }}
              </span>
            </div>

            <a
              :href="app.links.patreon"
              target="_blank"
              rel="noopener"
              class="mt-7 inline-flex items-center gap-2.5 rounded-xl px-7 py-3.5 text-[15px] font-bold text-white no-underline transition duration-200 will-change-transform hover:-translate-y-0.5 hover:brightness-110"
              style="background: linear-gradient(140deg, #ec5f5b, #d85691); box-shadow: 0 16px 34px -12px rgba(236,95,91,0.6)"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="#fff"><circle cx="15" cy="9.2" r="6.2" /><rect x="2.5" y="2.6" width="3.6" height="18.8" rx="0.4" /></svg>
              {{ app.support.patreon.cta }} →
            </a>
            <div class="mt-3 text-[12.5px] text-faint">Choose the tier that suits you on Patreon — every bit helps.</div>
          </div>
        </div>
      </Reveal>

      <!-- Optional pricing cards (only if you fill in `support.tiers`) -->
      <div v-else class="mt-12 grid items-start gap-[18px] md:grid-cols-3">
        <Reveal
          v-for="(t, i) in app.support.tiers"
          :key="t.name"
          :delay="i * 0.07"
          tag="div"
          class="group relative flex flex-col rounded-2xl p-7 transition-transform duration-300 hover:-translate-y-1"
          :style="
            t.popular
              ? 'background:rgba(90,194,109,0.06);border:1px solid rgba(90,194,109,0.4);box-shadow:0 30px 70px -30px rgba(90,194,109,0.45)'
              : 'background:rgba(28,28,32,0.8);border:1px solid rgba(255,255,255,0.07)'
          "
        >
          <div v-if="t.popular" class="absolute -top-3 left-1/2 -translate-x-1/2 whitespace-nowrap rounded-full bg-brand px-3 py-1 font-pixel text-[11px] font-bold tracking-[0.6px] text-[#10130f]">MOST POPULAR</div>
          <div class="font-pixel text-lg font-semibold text-[#eaeaec]">{{ t.name }}</div>
          <div class="mt-0.5 text-[12.5px] text-dim">{{ t.tagline }}</div>
          <div class="my-4 text-[38px] font-bold tracking-[-1px] text-[#f3f3f5]">{{ t.price }}<span class="text-base font-medium text-dim">/mo</span></div>
          <div class="flex flex-1 flex-col gap-2.5">
            <div v-for="perk in t.perks" :key="perk" class="flex items-start gap-2.5">
              <svg class="mt-0.5 flex-none" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#5ac26d" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4 10-10" /></svg>
              <span class="text-[13.5px] leading-snug text-soft">{{ perk }}</span>
            </div>
          </div>
          <a
            :href="app.links.patreon"
            target="_blank"
            rel="noopener"
            class="mt-6 rounded-[10px] py-3 text-center text-sm font-bold no-underline transition"
            :style="t.popular ? 'background:linear-gradient(140deg,#6fce80,#46a85a);color:#10221a' : 'background:rgba(255,255,255,0.06);color:#e8e8ea;border:1px solid rgba(255,255,255,0.12)'"
            :class="t.popular ? 'hover:brightness-110' : 'hover:bg-white/10'"
          >
            {{ t.popular ? 'Become a ' + t.name : 'Choose ' + t.name }}
          </a>
        </Reveal>
      </div>

      <!-- How to claim your code -->
      <Reveal class="mt-7 overflow-hidden rounded-2xl border border-white/[0.08] p-7 sm:p-9" style="background: linear-gradient(135deg, rgba(90,194,109,0.08), rgba(226,113,154,0.06))">
        <div class="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <h3 class="font-pixel text-2xl font-bold text-[#f3f3f5]">{{ app.support.claim.title }}</h3>
            <p class="mt-1.5 text-[15px] text-muted">{{ app.support.claim.subtitle }}</p>
          </div>
          <NuxtLink to="/supporter" class="btn-primary mc-clip whitespace-nowrap px-5 py-3 text-[15px]">Claim your key →</NuxtLink>
        </div>

        <div class="mt-7 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Reveal
            v-for="(s, i) in app.support.claim.steps"
            :key="i"
            :delay="i * 0.07"
            class="relative rounded-xl border border-white/[0.07] bg-[rgba(20,21,25,0.55)] p-5"
          >
            <!-- connector arrow between steps (desktop) -->
            <div v-if="i < app.support.claim.steps.length - 1" class="absolute -right-2.5 top-9 z-10 hidden text-faint lg:block">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14" /><path d="m13 6 6 6-6 6" /></svg>
            </div>
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 flex-none items-center justify-center rounded-xl" style="background: linear-gradient(140deg, #6fce80, #46a85a)">
                <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="#10221a" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <template v-if="s.icon === 'patreon'"><circle cx="15" cy="9.2" r="6.2" fill="#10221a" stroke="none" /><rect x="2.5" y="2.6" width="3.6" height="18.8" rx="0.4" fill="#10221a" stroke="none" /></template>
                  <template v-else-if="s.icon === 'login'"><path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" /><path d="m10 17 5-5-5-5" /><path d="M15 12H3" /></template>
                  <template v-else-if="s.icon === 'key'"><circle cx="8" cy="15" r="4" /><path d="m10.8 12.2 8.2-8.2" /><path d="m16 6 3 3" /><path d="m18 4 3 3" /></template>
                  <template v-else><rect x="3" y="11" width="18" height="11" rx="2" /><path d="M7 11V7a5 5 0 0 1 9.9-1" /></template>
                </svg>
              </div>
              <span class="font-pixel text-[13px] font-bold tracking-[1px] text-faint">STEP {{ i + 1 }}</span>
            </div>
            <div class="mt-3 text-[15px] font-semibold text-[#f0f0f2]">{{ s.title }}</div>
            <div class="mt-1.5 text-[13px] leading-relaxed text-muted">{{ s.body }}</div>
          </Reveal>
        </div>

        <div class="mt-6 flex items-start gap-2.5 rounded-xl border border-white/[0.06] bg-black/20 p-3.5">
          <svg class="mt-0.5 flex-none" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#d2a96a" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 9v4" /><path d="M12 17h.01" /><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /></svg>
          <div class="text-[12.5px] leading-relaxed text-muted">{{ app.support.claim.note }}</div>
        </div>
      </Reveal>
    </div>
  </section>
</template>
