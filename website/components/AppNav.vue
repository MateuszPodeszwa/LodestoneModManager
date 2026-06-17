<script setup lang="ts">
const app = useAppConfig()
const open = ref(false)
const route = useRoute()
watch(() => route.fullPath, () => (open.value = false))

// Auth state so the nav can swap "Sign in" for the account + "Sign out" when logged in.
const { loggedIn, user, clear } = useUserSession()

async function signOut() {
  await $fetch('/api/auth/logout', { method: 'POST' })
  await clear()
  open.value = false
  await navigateTo('/')
}

const navLinks = [
  { label: 'About', to: '/#about' },
  { label: 'How it works', to: '/#tutorial' },
  { label: 'Support', to: '/#support' },
  { label: 'Changelog', to: '/changelog' },
]
</script>

<template>
  <nav
    class="sticky top-0 z-[60] flex h-16 items-center gap-5 border-b border-white/[0.06] px-5 backdrop-blur-xl sm:px-6"
    style="background: rgba(18, 19, 23, 0.74)"
  >
    <NuxtLink to="/" class="flex items-center gap-2.5 no-underline">
      <LogoMark />
      <span class="font-pixel text-xl font-semibold tracking-[0.6px] text-[#f2f2f4]">Lodestone</span>
    </NuxtLink>

    <!-- desktop links -->
    <div class="ml-4 hidden items-center gap-0.5 md:flex">
      <NuxtLink
        v-for="l in navLinks"
        :key="l.to"
        :to="l.to"
        class="rounded-lg px-3 py-2 text-sm font-medium text-soft no-underline transition hover:bg-white/[0.06] hover:text-[#f0f0f2]"
        :class="{ 'bg-brand/10 !text-brand font-semibold': route.path === l.to }"
      >
        {{ l.label }}
      </NuxtLink>
    </div>

    <div class="ml-auto flex items-center gap-2.5">
      <template v-if="loggedIn">
        <NuxtLink
          to="/supporter"
          class="hidden items-center gap-2 rounded-lg border border-white/[0.12] px-3 py-1.5 text-sm font-semibold text-[#e8e8ea] no-underline transition hover:bg-white/[0.07] sm:inline-flex"
        >
          <img v-if="user?.imageUrl" :src="user.imageUrl" alt="" class="h-6 w-6 rounded-md object-cover" />
          <span class="max-w-[120px] truncate">{{ user?.name || 'Account' }}</span>
        </NuxtLink>
        <button
          class="hidden rounded-lg border border-white/[0.12] px-3.5 py-2 text-sm font-semibold text-soft transition hover:bg-white/[0.07] sm:inline-block"
          @click="signOut"
        >
          Sign out
        </button>
      </template>
      <NuxtLink
        v-else
        to="/supporter"
        class="hidden rounded-lg border border-white/[0.12] px-3.5 py-2 text-sm font-semibold text-[#e8e8ea] no-underline transition hover:bg-white/[0.07] sm:inline-block"
      >
        Sign in
      </NuxtLink>
      <NuxtLink
        to="/download"
        class="btn-primary mc-clip-sm px-4 py-2.5 text-sm"
        style="filter: drop-shadow(0 8px 16px rgba(90, 194, 109, 0.45))"
      >
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#10221a" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"><path d="M12 4v11" /><path d="m7 11 5 5 5-5" /><path d="M5 20h14" /></svg>
        Download
      </NuxtLink>
      <!-- mobile menu toggle -->
      <button
        class="flex h-9 w-9 items-center justify-center rounded-lg border border-white/[0.12] text-soft md:hidden"
        :aria-expanded="open"
        aria-label="Toggle menu"
        @click="open = !open"
      >
        <svg v-if="!open" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M4 7h16M4 12h16M4 17h16" /></svg>
        <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
      </button>
    </div>

    <!-- mobile dropdown -->
    <Transition name="page">
      <div
        v-if="open"
        class="absolute inset-x-0 top-16 flex flex-col gap-1 border-b border-white/[0.06] bg-[#16171b] p-4 md:hidden"
      >
        <NuxtLink
          v-for="l in navLinks"
          :key="l.to"
          :to="l.to"
          class="rounded-lg px-3 py-2.5 text-[15px] font-medium text-soft no-underline hover:bg-white/[0.06]"
        >
          {{ l.label }}
        </NuxtLink>
        <NuxtLink v-if="!loggedIn" to="/supporter" class="rounded-lg px-3 py-2.5 text-[15px] font-medium text-soft no-underline hover:bg-white/[0.06]">
          Sign in
        </NuxtLink>
        <template v-else>
          <NuxtLink to="/supporter" class="rounded-lg px-3 py-2.5 text-[15px] font-medium text-soft no-underline hover:bg-white/[0.06]">
            {{ user?.name || 'Account' }}
          </NuxtLink>
          <button class="rounded-lg px-3 py-2.5 text-left text-[15px] font-medium text-soft hover:bg-white/[0.06]" @click="signOut">
            Sign out
          </button>
        </template>
      </div>
    </Transition>
  </nav>
</template>
