<script setup lang="ts">
const app = useAppConfig()
const year = new Date().getFullYear()
</script>

<template>
  <footer class="border-t border-white/[0.07] bg-surface-foot px-5 pb-9 pt-14 sm:px-10">
    <div class="mx-auto grid max-w-content gap-10 md:grid-cols-[1.6fr_1fr_1fr_1fr]">
      <!-- brand -->
      <div>
        <div class="flex items-center gap-2.5">
          <LogoMark :size="30" />
          <span class="font-pixel text-[19px] font-semibold tracking-[0.5px] text-[#f2f2f4]">Lodestone</span>
        </div>
        <div class="mt-3.5 max-w-[300px] text-[13.5px] leading-relaxed text-dim">
          {{ app.footer.tagline }}
        </div>
        <div class="mt-4 flex gap-2.5">
          <a
            :href="app.links.github"
            target="_blank"
            rel="noopener"
            aria-label="GitHub"
            class="flex h-9 w-9 items-center justify-center rounded-[9px] border border-white/10 text-[#b9b9bf] no-underline transition hover:bg-white/[0.07] hover:text-white"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.48 2 2 6.58 2 12.25c0 4.53 2.87 8.37 6.84 9.73.5.1.68-.22.68-.49l-.01-1.9c-2.78.62-3.37-1.21-3.37-1.21-.46-1.18-1.11-1.5-1.11-1.5-.91-.64.07-.62.07-.62 1 .07 1.53 1.06 1.53 1.06.9 1.56 2.36 1.11 2.94.85.09-.67.35-1.11.63-1.37-2.22-.26-4.55-1.14-4.55-5.06 0-1.12.39-2.03 1.03-2.75-.1-.26-.45-1.3.1-2.72 0 0 .84-.27 2.75 1.05a9.4 9.4 0 0 1 5 0c1.91-1.32 2.75-1.05 2.75-1.05.55 1.42.2 2.46.1 2.72.64.72 1.03 1.63 1.03 2.75 0 3.93-2.34 4.79-4.57 5.05.36.32.68.94.68 1.9l-.01 2.82c0 .27.18.6.69.49A10.02 10.02 0 0 0 22 12.25C22 6.58 17.52 2 12 2Z" /></svg>
          </a>
          <DiscordLink variant="icon" />
          <a
            :href="app.links.patreon"
            target="_blank"
            rel="noopener"
            aria-label="Patreon"
            class="flex h-9 w-9 items-center justify-center rounded-[9px] border border-white/10 text-[#b9b9bf] no-underline transition hover:border-pink/40 hover:text-pink"
          >
            <svg width="17" height="17" viewBox="0 0 24 24" fill="currentColor"><circle cx="15" cy="9.2" r="6.2" /><rect x="2.5" y="2.6" width="3.6" height="18.8" rx="0.4" /></svg>
          </a>
        </div>

        <!-- Quick, prominent path to help so it's never buried. -->
        <NuxtLink
          to="/support"
          class="mt-5 inline-flex items-center gap-2 rounded-xl border border-brand/25 bg-brand/[0.07] px-3.5 py-2.5 text-[13px] font-semibold text-[#bfe0c6] no-underline transition hover:bg-brand/[0.12]"
        >
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M9.5 9a2.5 2.5 0 1 1 3.5 2.3c-.7.3-1 .8-1 1.7" /><path d="M12 17h.01" /></svg>
          Need a hand? Help &amp; support
        </NuxtLink>
      </div>

      <!-- product -->
      <div>
        <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">Product</div>
        <div class="mt-4 flex flex-col gap-2.5">
          <NuxtLink to="/download" class="text-sm text-[#b9b9bf] no-underline transition hover:text-brand">Download</NuxtLink>
          <NuxtLink to="/changelog" class="text-sm text-[#b9b9bf] no-underline transition hover:text-brand">Changelog</NuxtLink>
          <NuxtLink to="/#about" class="text-sm text-[#b9b9bf] no-underline transition hover:text-brand">Features</NuxtLink>
        </div>
      </div>

      <!-- help (its own column so support + bug reports are easy to find) -->
      <div>
        <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">Help</div>
        <div class="mt-4 flex flex-col gap-2.5">
          <NuxtLink to="/support" class="text-sm font-semibold text-[#d8d8dd] no-underline transition hover:text-brand">Help &amp; support</NuxtLink>
          <NuxtLink to="/report" class="text-sm font-semibold text-[#d8d8dd] no-underline transition hover:text-brand">Report a bug</NuxtLink>
          <a :href="app.links.github" target="_blank" rel="noopener" class="text-sm text-[#b9b9bf] no-underline transition hover:text-brand">GitHub</a>
          <DiscordLink variant="text" />
        </div>
      </div>

      <!-- support the project (donate - kept distinct from Help) -->
      <div>
        <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">Support the project</div>
        <div class="mt-4 flex flex-col gap-2.5">
          <a :href="app.links.patreon" target="_blank" rel="noopener" class="text-sm text-[#b9b9bf] no-underline transition hover:text-pink">Become a patron</a>
          <NuxtLink to="/supporter" class="text-sm text-[#b9b9bf] no-underline transition hover:text-brand">Claim your key</NuxtLink>
        </div>
      </div>
    </div>

    <div
      class="mx-auto mt-8 flex max-w-content flex-wrap items-center justify-between gap-3 border-t border-white/[0.06] pt-6"
    >
      <div class="text-[12.5px] text-[#6a6a72]">© {{ year }} Lodestone · Made for the Minecraft community.</div>
      <div class="max-w-[520px] text-right text-xs text-[#5c5c64]">{{ app.footer.legal }}</div>
    </div>
  </footer>
</template>
