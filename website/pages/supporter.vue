<script setup lang="ts">
const app = useAppConfig()
const toast = useToast()
const route = useRoute()
// Shared auth state — clearing it here also updates the nav (otherwise the profile
// tab lingers until a manual refresh).
const { clear: clearSession } = useUserSession()

useSeoMeta({
  title: 'Supporter — claim your key',
  description: 'Sign in with Patreon to verify your membership and generate a Lodestone supporter key. Codes are valid for one hour.',
  robots: 'noindex', // account area — keep it out of search results
})

const { data: me, refresh, pending } = await useFetch('/api/me')

// Local key state (we never persist the code — the patron is responsible for it).
const code = ref<string | null>(null)
const expiresAt = ref<number | null>(null)
const nextAt = ref<number | null>(null)
const generating = ref(false)

watchEffect(() => {
  if (me.value?.gen?.nextAt && !nextAt.value) nextAt.value = me.value.gen.nextAt
})

// Live clock for the countdowns.
const now = ref(Date.now())
let timer: ReturnType<typeof setInterval> | null = null
onMounted(() => {
  timer = setInterval(() => (now.value = Date.now()), 1000)
  // Surface OAuth errors passed back via query string.
  const err = route.query.error as string | undefined
  if (err) {
    const map: Record<string, string> = {
      unconfigured: 'Patreon sign-in isn’t set up yet — please check back soon.',
      state: 'Your sign-in session expired. Please try again.',
      patreon: 'We couldn’t reach Patreon. Please try again.',
    }
    toast.error('Sign-in issue', map[err] ?? 'Something went wrong signing in.')
  }
  if (route.query.need === 'beta') {
    toast.info('Beta access', 'Beta builds are a Supporter-tier perk — generate a key to unlock them.')
  }
})
onBeforeUnmount(() => timer && clearInterval(timer))

const user = computed(() => me.value?.user ?? null)
const isPatron = computed(() => !!user.value?.isPatron)
const cooldownMs = computed(() => (nextAt.value ? nextAt.value - now.value : 0))
const locked = computed(() => cooldownMs.value > 0)
const expiresMs = computed(() => (expiresAt.value ? expiresAt.value - now.value : 0))

const tierAmount = computed(() => {
  const c = user.value?.currentlyEntitledCents ?? 0
  return c > 0 ? `$${(c / 100).toFixed(c % 100 === 0 ? 0 : 2)}/mo` : null
})

async function generate() {
  if (generating.value || locked.value) return
  generating.value = true
  try {
    const res = await $fetch('/api/key/generate', { method: 'POST' })
    code.value = res.code
    expiresAt.value = res.expiresAt
    nextAt.value = res.nextAt
    toast.success('Key ready', 'A fresh membership key was generated')
    await refresh()
  } catch (e: any) {
    const next = e?.data?.data?.nextAt
    if (next) nextAt.value = next
    toast.error("Couldn't generate", e?.statusMessage || e?.data?.statusMessage || 'Please try again in a moment')
  } finally {
    generating.value = false
  }
}

async function copyKey() {
  if (!code.value) return
  try {
    await navigator.clipboard.writeText(code.value)
    toast.success('Copied', 'Paste it into Lodestone → Settings → Supporter')
  } catch {
    toast.error('Copy failed', 'Select the code and copy it manually')
  }
}

async function signOut() {
  await $fetch('/api/auth/logout', { method: 'POST' })
  await clearSession() // reset the shared session so the nav drops the profile tab immediately
  code.value = null
  expiresAt.value = null
  nextAt.value = null
  await refresh()
  toast.info('Signed out', 'See you next time!')
}

const steps = [
  "We confirm you're an active patron — nothing else is touched.",
  'A unique membership key is reserved just for your account.',
  'Paste it into Lodestone to light up your supporter perks.',
]
const redeemSteps = [
  'Open Lodestone and go to <span class="text-[#f0f0f2] font-semibold">Settings → Supporter</span>.',
  'Paste your key and hit <span class="text-[#f0f0f2] font-semibold">Activate</span>.',
  'Your supporter badge and perks unlock instantly.',
]
const perks = ['Supporter badge in the app', 'Exclusive accent themes', 'Early access to beta builds', 'Your name in the credits']
</script>

<template>
  <main class="relative flex min-h-[calc(100vh-64px)] items-start justify-center overflow-hidden px-6 pb-20 pt-14" style="background: radial-gradient(110% 90% at 50% 0%, #221f2a 0%, #17171c 48%, #141519 100%)">
    <div class="pointer-events-none absolute -top-[10%] left-1/2 h-[520px] w-[680px] -translate-x-1/2 rounded-full" style="background: radial-gradient(circle, rgba(226, 113, 154, 0.12), transparent 64%)" />

    <!-- ===== LOADING ===== -->
    <div v-if="pending" class="relative w-full max-w-[420px] pt-16 text-center">
      <div class="mx-auto h-[54px] w-[54px] animate-spin rounded-full border-[3px] border-white/[0.12] border-t-pink" />
      <div class="mt-5 text-lg font-semibold text-[#f0f0f2]">Just a moment…</div>
    </div>

    <!-- ===== LOGGED OUT ===== -->
    <div v-else-if="!me?.loggedIn" class="relative w-full max-w-[480px]">
      <div class="text-center">
        <div class="font-pixel text-[13px] font-semibold uppercase tracking-[2.4px] text-pink">Claim Portal</div>
        <h1 class="mt-3 font-pixel text-[34px] font-bold leading-[1.05] tracking-[0.5px] text-[#f5f5f7]">Unlock your supporter perks</h1>
        <p class="mt-3.5 text-[15.5px] leading-relaxed text-muted">Sign in with Patreon and we'll check your membership, then generate a key to paste into the app.</p>
      </div>
      <div class="mt-6 rounded-2xl border border-white/[0.08] bg-[rgba(28,28,32,0.85)] p-7 shadow-[0_36px_80px_-34px_rgba(0,0,0,0.6)]">
        <a
          v-if="me?.patreonConfigured"
          href="/api/auth/patreon/login"
          class="flex w-full items-center justify-center gap-2.5 rounded-[11px] bg-[#ec5f5b] px-4 py-3.5 text-[15.5px] font-bold text-white no-underline transition hover:brightness-105"
        >
          <svg width="19" height="19" viewBox="0 0 24 24" fill="#fff"><circle cx="15" cy="9" r="6" /><rect x="2" y="3" width="3.5" height="18" /></svg>
          Continue with Patreon
        </a>
        <div v-else class="rounded-xl border border-white/10 bg-white/[0.03] p-4 text-center text-sm text-muted">
          Patreon sign-in is being set up. For now, <a :href="app.links.patreon" target="_blank" rel="noopener" class="font-semibold text-pink no-underline hover:underline">become a patron</a> and reach out — your perks will be sorted.
        </div>
        <div class="mt-[22px] flex flex-col gap-3">
          <div v-for="(s, i) in steps" :key="i" class="flex items-start gap-3">
            <div class="flex h-[26px] w-[26px] flex-none items-center justify-center rounded-[7px] bg-brand/[0.14] font-pixel text-[13px] font-bold text-brand">{{ i + 1 }}</div>
            <div class="text-[13.5px] leading-relaxed text-[#a6a6ae]">{{ s }}</div>
          </div>
        </div>
      </div>
      <div class="mt-[18px] text-center text-[13px] text-faint">Not a patron yet? <a :href="app.links.patreon" target="_blank" rel="noopener" class="font-semibold text-pink no-underline hover:underline">Become one on Patreon →</a></div>
    </div>

    <!-- ===== LOGGED IN ===== -->
    <div v-else class="relative w-full max-w-[560px]">
      <!-- profile -->
      <div class="flex items-center gap-4 rounded-2xl border border-white/[0.08] bg-[rgba(28,28,32,0.85)] p-6 shadow-[0_30px_70px_-36px_rgba(0,0,0,0.6)]">
        <img v-if="user?.imageUrl" :src="user.imageUrl" alt="" class="h-[58px] w-[58px] flex-none rounded-[13px] object-cover" />
        <div v-else class="flex h-[58px] w-[58px] flex-none items-center justify-center rounded-[13px] font-pixel text-[26px] font-bold text-[#10130f]" style="background: linear-gradient(140deg, #6fce80, #46a85a)">{{ (user?.name || 'S').charAt(0) }}</div>
        <div class="min-w-0 flex-1">
          <div class="flex flex-wrap items-center gap-2.5">
            <span class="text-[19px] font-bold text-[#f3f3f5]">{{ user?.name || 'Supporter' }}</span>
            <span v-if="isPatron" class="inline-flex items-center gap-1.5 rounded-full bg-brand/[0.16] px-2.5 py-1 text-[11px] font-bold tracking-[0.4px] text-brand"><span class="h-1.5 w-1.5 rounded-full bg-brand" />{{ user?.patronStatus === 'active_patron' ? 'ACTIVE PATRON' : 'PATRON' }}</span>
            <span v-else class="rounded-full bg-[rgba(226,80,63,0.15)] px-2.5 py-1 text-[11px] font-bold text-[#e2503f]">NOT A PATRON</span>
          </div>
          <div class="mt-1 text-[13px] text-muted">
            <template v-if="user?.tierTitle">{{ user.tierTitle }} tier<span v-if="tierAmount"> · {{ tierAmount }}</span></template>
            <template v-else-if="tierAmount">{{ tierAmount }}</template>
            <template v-else>Patreon member</template>
          </div>
        </div>
        <button class="flex-none rounded-[9px] border border-white/[0.12] bg-transparent px-3.5 py-2.5 text-[13px] font-medium text-soft transition hover:bg-white/[0.06]" @click="signOut">Sign out</button>
      </div>

      <!-- not a patron notice -->
      <div v-if="!isPatron" class="mt-3.5 rounded-2xl border border-[rgba(226,80,63,0.25)] bg-[rgba(226,80,63,0.07)] p-5">
        <div class="text-[15px] font-semibold text-[#f0d6d2]">We couldn't confirm an active pledge</div>
        <div class="mt-1.5 text-[13.5px] leading-relaxed text-muted">Your Patreon account is connected, but we didn't find a membership to this project. If you just joined, it can take a minute — try signing out and back in, or <a :href="app.links.patreon" target="_blank" rel="noopener" class="font-semibold text-pink no-underline hover:underline">join on Patreon</a>.</div>
      </div>

      <!-- key section -->
      <div v-if="isPatron" class="mt-3.5 rounded-2xl border border-white/[0.08] bg-[rgba(28,28,32,0.85)] p-6">
        <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">Your membership key</div>

        <!-- signing not configured -->
        <div v-if="!me?.signingConfigured" class="mt-3.5 rounded-xl border border-white/10 bg-white/[0.03] p-4 text-sm text-muted">
          Key generation is being configured on the server. Please check back shortly.
        </div>

        <template v-else>
          <!-- no code yet -->
          <div v-if="!code">
            <div class="mt-3.5 text-sm leading-relaxed text-[#a6a6ae]">Generate a unique key tied to your Patreon account. Each key is valid for <strong class="text-soft">one hour</strong> — generate a fresh one whenever you need it.</div>
            <button
              class="btn-primary mc-clip mt-[18px] w-full py-3.5 text-[15px] disabled:cursor-not-allowed disabled:opacity-60"
              :disabled="generating || locked"
              @click="generate"
            >
              <span v-if="generating" class="h-4 w-4 animate-spin rounded-full border-2 border-[rgba(16,34,26,0.35)] border-t-[#10221a]" />
              <svg v-else-if="!locked" width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="#10221a" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round"><circle cx="8" cy="15" r="4" /><path d="m10.8 12.2 8.2-8.2" /><path d="m16 6 3 3" /><path d="m18 4 3 3" /></svg>
              <template v-if="generating">Generating…</template>
              <template v-else-if="locked">Available in {{ formatCountdown(cooldownMs) }}</template>
              <template v-else>Generate my key</template>
            </button>
            <div v-if="locked" class="mt-2.5 text-center text-[12.5px] text-faint">You generated a key recently. You can create another once the timer runs out.</div>
          </div>

          <!-- code shown -->
          <div v-else>
            <div class="mt-3.5 flex items-center gap-3 rounded-xl border border-dashed border-brand/45 bg-brand/[0.07] px-[18px] py-4">
              <code class="min-w-0 flex-1 truncate font-mono text-[15px] font-semibold tracking-wide text-[#9ad6a6]">{{ code }}</code>
              <button class="flex flex-none items-center gap-1.5 rounded-[9px] bg-brand px-3.5 py-2.5 text-[13px] font-bold text-[#10130f] transition hover:bg-brand-light" @click="copyKey">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#10130f" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15V5a2 2 0 0 1 2-2h10" /></svg>
                Copy
              </button>
            </div>
            <div class="mt-2.5 flex flex-wrap items-center justify-between gap-2 text-[12.5px]">
              <span class="text-brand" :class="{ '!text-[#e2719a]': expiresMs <= 0 }">
                <template v-if="expiresMs > 0">Valid for {{ formatCountdown(expiresMs) }} — redeem it in the app now.</template>
                <template v-else>This code has expired — generate a fresh one when the timer allows.</template>
              </span>
            </div>

            <!-- redeem steps -->
            <div class="mt-[18px] flex flex-col gap-3">
              <div v-for="(s, i) in redeemSteps" :key="i" class="flex items-start gap-3">
                <div class="flex h-6 w-6 flex-none items-center justify-center rounded-[7px] bg-white/[0.06] font-pixel text-[12px] font-bold text-soft">{{ i + 1 }}</div>
                <div class="text-[13.5px] leading-relaxed text-soft" v-html="s" />
              </div>
            </div>

            <button
              class="mt-[18px] inline-flex items-center gap-1.5 rounded-[9px] border border-white/[0.12] bg-transparent px-3.5 py-2.5 text-[13px] font-medium text-soft transition hover:bg-white/[0.06] disabled:cursor-not-allowed disabled:opacity-60"
              :disabled="generating || locked"
              @click="generate"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 0 1 15-6.7L21 8" /><path d="M21 3v5h-5" /><path d="M21 12a9 9 0 0 1-15 6.7L3 16" /><path d="M3 21v-5h5" /></svg>
              <template v-if="locked">Regenerate in {{ formatCountdown(cooldownMs) }}</template>
              <template v-else>Regenerate key</template>
            </button>
          </div>

          <!-- responsibility notice -->
          <div class="mt-5 flex items-start gap-2.5 rounded-xl border border-white/[0.07] bg-white/[0.02] p-3.5">
            <svg class="mt-0.5 flex-none" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#d2a96a" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 9v4" /><path d="M12 17h.01" /><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /></svg>
            <div class="text-[12.5px] leading-relaxed text-muted">Your key is yours alone — we don't store it and can't recover it, so keep it safe. Please <strong class="text-soft">don't share or redistribute</strong> your codes; they're tied to your membership.</div>
          </div>
        </template>
      </div>

      <!-- perks + beta -->
      <div v-if="isPatron" class="mt-3.5 rounded-2xl border border-white/[0.06] bg-[rgba(28,28,32,0.6)] p-6">
        <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">Your perks</div>
        <div class="mt-3.5 flex flex-col gap-2.5">
          <div v-for="perk in perks" :key="perk" class="flex items-center gap-2.5">
            <svg class="flex-none" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#5ac26d" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4 10-10" /></svg>
            <span class="text-sm text-soft">{{ perk }}</span>
          </div>
        </div>
        <div v-if="user?.betaAccess" class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-grape/30 p-4" style="background: linear-gradient(120deg, rgba(187,120,214,0.08), rgba(90,145,194,0.06))">
          <div class="text-[13.5px] text-[#d9c7e6]"><strong>Beta access unlocked.</strong> Grab the latest early build, or switch the channel in-app.</div>
          <a href="/api/download/beta" class="btn-ghost border-grape/40 px-4 py-2.5 text-sm hover:bg-grape/10">Download beta</a>
        </div>
      </div>
    </div>
  </main>
</template>
