<script setup lang="ts">
// Discord is "work in progress": until app.config `links.discord` is set, this
// renders greyed-out with a hover tooltip. Set the invite URL once and every
// instance turns into a real link automatically.
withDefaults(defineProps<{ variant?: 'icon' | 'text' }>(), { variant: 'text' })
const app = useAppConfig()
const enabled = computed(() => !!app.links.discord)
const tip = 'Discord server is a work in progress — for now, reach me via Patreon.'
</script>

<template>
  <!-- Enabled: real link -->
  <a
    v-if="enabled"
    :href="app.links.discord"
    target="_blank"
    rel="noopener"
    :class="
      variant === 'icon'
        ? 'flex h-9 w-9 items-center justify-center rounded-[9px] border border-white/10 text-[#b9b9bf] no-underline transition hover:bg-white/[0.07] hover:text-white'
        : 'flex items-center gap-2.5 text-sm text-[#b9b9bf] no-underline transition hover:text-brand'
    "
  >
    <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M20 4.4A19 19 0 0 0 15.3 3l-.3.5a14 14 0 0 1 4 1.9 13 13 0 0 0-11.2 0 14 14 0 0 1 4-1.9L11.5 3A19 19 0 0 0 4 4.4 19.6 19.6 0 0 0 .5 17.3a19 19 0 0 0 5.7 2.8l.5-.7a8 8 0 0 1-2.2-1.1c1.9 1 4.5 1.7 7.5 1.7s5.6-.7 7.5-1.7a8 8 0 0 1-2.2 1.1l.5.7a19 19 0 0 0 5.7-2.8A19.6 19.6 0 0 0 20 4.4ZM8.5 14.4c-.9 0-1.6-.8-1.6-1.9s.7-1.9 1.6-1.9 1.6.9 1.6 1.9-.7 1.9-1.6 1.9Zm7 0c-.9 0-1.6-.8-1.6-1.9s.7-1.9 1.6-1.9 1.6.9 1.6 1.9-.7 1.9-1.6 1.9Z" /></svg>
    <span v-if="variant === 'text'">Discord</span>
  </a>

  <!-- Disabled: greyed with tooltip -->
  <span v-else class="group relative inline-flex">
    <span
      :class="
        variant === 'icon'
          ? 'flex h-9 w-9 cursor-help items-center justify-center rounded-[9px] border border-white/[0.07] text-faint opacity-50'
          : 'flex cursor-help items-center gap-2.5 text-sm text-faint opacity-60'
      "
      role="link"
      aria-disabled="true"
      tabindex="0"
    >
      <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M20 4.4A19 19 0 0 0 15.3 3l-.3.5a14 14 0 0 1 4 1.9 13 13 0 0 0-11.2 0 14 14 0 0 1 4-1.9L11.5 3A19 19 0 0 0 4 4.4 19.6 19.6 0 0 0 .5 17.3a19 19 0 0 0 5.7 2.8l.5-.7a8 8 0 0 1-2.2-1.1c1.9 1 4.5 1.7 7.5 1.7s5.6-.7 7.5-1.7a8 8 0 0 1-2.2 1.1l.5.7a19 19 0 0 0 5.7-2.8A19.6 19.6 0 0 0 20 4.4ZM8.5 14.4c-.9 0-1.6-.8-1.6-1.9s.7-1.9 1.6-1.9 1.6.9 1.6 1.9-.7 1.9-1.6 1.9Zm7 0c-.9 0-1.6-.8-1.6-1.9s.7-1.9 1.6-1.9 1.6.9 1.6 1.9-.7 1.9-1.6 1.9Z" /></svg>
      <span v-if="variant === 'text'">Discord</span>
      <span
        v-if="variant === 'text'"
        class="rounded-full bg-white/[0.07] px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wide text-faint"
        >Soon</span
      >
    </span>
    <!-- tooltip -->
    <span
      class="pointer-events-none absolute bottom-full left-1/2 z-50 mb-2 w-52 -translate-x-1/2 rounded-lg border border-white/10 bg-[#2b2b31] px-3 py-2 text-center text-xs leading-snug text-soft opacity-0 shadow-xl transition-opacity duration-150 group-hover:opacity-100 group-focus-within:opacity-100"
    >
      {{ tip }}
    </span>
  </span>
</template>
