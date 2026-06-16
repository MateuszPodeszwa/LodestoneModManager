<script setup lang="ts">
const app = useAppConfig()
const toast = useToast()
const { data: rel } = await useFetch('/api/releases/latest')
// Checksums can require hashing the asset, so fetch them lazily on the client.
const { data: sums } = await useFetch('/api/checksums', { lazy: true, server: false })

useSeoMeta({
  title: 'Download',
  description: 'Download Lodestone for Windows — free, ~8 MB, with built-in automatic updates. Installer and portable .zip, with SHA-256 checksums.',
})

const setupSha = computed(() => sums.value?.setup?.sha256 ?? null)

const included = [
  'Drag-and-drop installer for mods, packs & shaders',
  'Built-in Modrinth browser (CurseForge coming soon)',
  'Automatic updates & version profiles',
  'No bundled adware — ever',
]

function notifyDownload(label: string) {
  toast.success('Starting download…', label)
}
async function copyHash() {
  if (!setupSha.value) return
  try {
    await navigator.clipboard.writeText(setupSha.value)
    toast.success('Copied', 'SHA-256 checksum copied to clipboard')
  } catch {
    toast.error('Copy failed', 'Select and copy the checksum manually')
  }
}
</script>

<template>
  <div>
    <!-- HEADER -->
    <header class="relative overflow-hidden px-6 pb-11 pt-16 text-center sm:px-10" style="background: radial-gradient(115% 130% at 50% 0%, #26352c 0%, #1a1c21 50%, #141519 100%)">
      <div class="grid-backdrop pointer-events-none absolute inset-0" style="-webkit-mask-image: radial-gradient(circle at 50% 0%, #000 0%, transparent 65%); mask-image: radial-gradient(circle at 50% 0%, #000 0%, transparent 65%)" />
      <div class="relative mx-auto max-w-[760px]">
        <div class="eyebrow">Download</div>
        <h1 class="mt-3 font-pixel font-bold leading-none tracking-[0.6px] text-[#f5f5f7]" style="font-size: clamp(36px, 5.5vw, 60px)">Get Lodestone</h1>
        <p class="mx-auto mt-4 max-w-[520px] text-[17px] leading-relaxed text-muted">Free forever, around {{ rel?.sizeText ?? '8 MB' }}, and updates are built right in. Pick the build that fits your setup.</p>
        <div class="mt-5 inline-flex flex-wrap items-center justify-center gap-3 rounded-full border border-white/10 bg-white/[0.04] px-4 py-2.5">
          <span class="inline-flex items-center gap-1.5 text-[13px] font-semibold text-[#7ed98f]"><span class="h-[7px] w-[7px] rounded-full bg-brand" style="box-shadow: 0 0 7px #5ac26d" />Latest: v{{ rel?.version }}</span>
          <span class="h-3.5 w-px bg-white/[0.14]" />
          <span class="text-[13px] text-muted">Released {{ formatDate(rel?.date) }}</span>
          <span class="h-3.5 w-px bg-white/[0.14]" />
          <NuxtLink to="/changelog" class="text-[13px] font-semibold text-brand no-underline hover:underline">Changelog →</NuxtLink>
        </div>
      </div>
    </header>

    <main class="mx-auto max-w-[1080px] px-6 pb-20 pt-11 sm:px-10">
      <div class="grid gap-[18px] md:grid-cols-2">
        <!-- Windows installer -->
        <Reveal class="relative flex flex-col rounded-2xl p-7" style="background: rgba(90, 194, 109, 0.06); border: 1px solid rgba(90, 194, 109, 0.38); box-shadow: 0 30px 70px -34px rgba(90, 194, 109, 0.45)">
          <div class="absolute -top-3 left-7 rounded-full bg-brand px-3 py-1 font-pixel text-[11px] font-bold tracking-[0.6px] text-[#10130f]">RECOMMENDED</div>
          <div class="flex items-center gap-3.5">
            <div class="flex h-[52px] w-[52px] items-center justify-center rounded-xl bg-brand/[0.14]"><svg width="26" height="26" viewBox="0 0 24 24" fill="#5ac26d"><path d="M3 5.5 10.2 4.5v6.8H3V5.5Zm0 13L10.2 19.5v-6.7H3v5.7Zm8.3 1.2L21 21V12.8h-9.7v6.9ZM11.3 4.3 21 3v8.2h-9.7V4.3Z" /></svg></div>
            <div>
              <div class="text-[19px] font-bold text-[#f3f3f5]">Windows Installer</div>
              <div class="mt-0.5 text-[13px] text-muted">Windows 10 &amp; 11 · 64-bit · {{ rel?.setup?.sizeText ?? rel?.sizeText ?? '8 MB' }} · .exe</div>
            </div>
          </div>
          <div class="mt-4 flex-1 text-sm leading-relaxed text-[#a6a6ae]">One-click setup with automatic updates and a desktop shortcut. The easiest way to get started.</div>
          <a :href="rel?.setup?.url || '/api/download/setup'" class="btn-primary mc-clip mt-5 py-3.5 text-[15px]" @click="notifyDownload(rel?.setup?.name || 'Windows installer')">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#10221a" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 4v11" /><path d="m7 11 5 5 5-5" /><path d="M5 20h14" /></svg>
            Download installer
          </a>
        </Reveal>

        <!-- Portable zip -->
        <Reveal :delay="0.06" class="flex flex-col rounded-2xl border border-white/[0.08] bg-[rgba(28,28,32,0.8)] p-7">
          <div class="flex items-center gap-3.5">
            <div class="flex h-[52px] w-[52px] items-center justify-center rounded-xl bg-white/[0.06]"><svg width="25" height="25" viewBox="0 0 24 24" fill="none" stroke="#c2c2c8" stroke-width="1.7" stroke-linejoin="round" stroke-linecap="round"><path d="M21 8 12 3 3 8v8l9 5 9-5V8Z" /><path d="m3 8 9 5 9-5" /><path d="M12 13v8" /><path d="M12 3v4" /></svg></div>
            <div>
              <div class="text-[19px] font-bold text-[#f3f3f5]">Portable .zip</div>
              <div class="mt-0.5 text-[13px] text-muted">Windows · no install · {{ rel?.portable?.sizeText ?? '9 MB' }} · .zip</div>
            </div>
          </div>
          <div class="mt-4 flex-1 text-sm leading-relaxed text-[#a6a6ae]">Unzip and run — nothing written to your system. Perfect for USB drives or locked-down machines.</div>
          <a :href="rel?.portable?.url || '/api/download/portable'" class="btn-ghost mt-5 py-3.5 text-[15px]" @click="notifyDownload(rel?.portable?.name || 'Portable .zip')">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#ededf0" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 4v11" /><path d="m7 11 5 5 5-5" /><path d="M5 20h14" /></svg>
            Download .zip
          </a>
        </Reveal>

        <!-- macOS soon -->
        <Reveal class="flex flex-col rounded-2xl border border-white/5 bg-[rgba(22,22,26,0.6)] p-7 opacity-60">
          <div class="flex items-center gap-3.5">
            <div class="flex h-[52px] w-[52px] items-center justify-center rounded-xl bg-white/[0.04]"><svg width="25" height="25" viewBox="0 0 24 24" fill="#9a9aa2"><path d="M16.4 12.7c0-2 1.6-3 1.7-3a3.7 3.7 0 0 0-2.9-1.6c-1.2-.1-2.4.7-3 .7-.6 0-1.6-.7-2.6-.7a3.9 3.9 0 0 0-3.3 2c-1.4 2.5-.4 6.2 1 8.2.7 1 1.5 2.1 2.5 2 1-.04 1.4-.65 2.6-.65 1.2 0 1.6.65 2.6.63 1.1-.02 1.8-1 2.4-2a8.9 8.9 0 0 0 1.1-2.2c-.03-.01-2.1-.82-2.1-3.2ZM14.5 6.3c.5-.66.9-1.57.8-2.5-.8.03-1.7.53-2.3 1.2-.5.6-.9 1.5-.8 2.4.9.07 1.8-.45 2.3-1.1Z" /></svg></div>
            <div><div class="text-[19px] font-bold text-[#dcdce0]">macOS</div><div class="mt-0.5 text-[13px] text-dim">Apple Silicon &amp; Intel · .dmg</div></div>
          </div>
          <div class="mt-4 flex-1 text-sm leading-relaxed text-dim">Lodestone is a Windows app today — there's no macOS build yet.</div>
          <div class="mt-5 inline-flex items-center justify-center gap-2 rounded-[10px] border border-dashed border-white/[0.14] py-3.5 text-sm font-semibold text-dim">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#8e8e96" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>
            Not yet
          </div>
        </Reveal>

        <!-- Linux soon -->
        <Reveal :delay="0.06" class="flex flex-col rounded-2xl border border-white/5 bg-[rgba(22,22,26,0.6)] p-7 opacity-60">
          <div class="flex items-center gap-3.5">
            <div class="flex h-[52px] w-[52px] items-center justify-center rounded-xl bg-white/[0.04]"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#9a9aa2" stroke-width="1.7" stroke-linejoin="round" stroke-linecap="round"><rect x="3" y="4" width="18" height="13" rx="2" /><path d="m8 21 4-4 4 4" /><path d="M7 9l2 2-2 2" /><path d="M12 13h4" /></svg></div>
            <div><div class="text-[19px] font-bold text-[#dcdce0]">Linux</div><div class="mt-0.5 text-[13px] text-dim">.AppImage &amp; .deb</div></div>
          </div>
          <div class="mt-4 flex-1 text-sm leading-relaxed text-dim">No Linux build yet — Lodestone targets Windows for now.</div>
          <div class="mt-5 inline-flex items-center justify-center gap-2 rounded-[10px] border border-dashed border-white/[0.14] py-3.5 text-sm font-semibold text-dim">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#8e8e96" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>
            Not yet
          </div>
        </Reveal>
      </div>

      <!-- Beta early-access (supporters) -->
      <Reveal v-if="rel?.beta?.available" class="mt-[18px] flex flex-col items-start gap-4 rounded-2xl border border-grape/30 p-7 sm:flex-row sm:items-center" style="background: linear-gradient(120deg, rgba(187,120,214,0.08), rgba(90,145,194,0.06))">
        <div class="flex-1">
          <div class="flex items-center gap-2.5">
            <span class="font-pixel text-sm font-semibold uppercase tracking-[1.5px] text-grape">Beta · early access</span>
            <span class="rounded-full bg-grape/15 px-2 py-0.5 text-[11px] font-semibold text-grape">Supporters</span>
          </div>
          <div class="mt-2 text-lg font-bold text-[#f3f3f5]">Lodestone {{ rel.beta.version }} beta</div>
          <div class="mt-1 max-w-[560px] text-sm leading-relaxed text-muted">Try the next release early. Beta builds are a Supporter perk — sign in and unlock beta access, then grab it here or switch the update channel inside the app.</div>
        </div>
        <a href="/api/download/beta" class="btn-ghost border-grape/40 px-5 py-3 text-[15px] hover:bg-grape/10">Download beta</a>
      </Reveal>

      <!-- details row -->
      <div class="mt-[18px] grid gap-[18px] md:grid-cols-2">
        <Reveal class="rounded-2xl border border-white/[0.07] bg-[rgba(28,28,32,0.6)] p-6">
          <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">What's included</div>
          <div class="mt-4 flex flex-col gap-2.5">
            <div v-for="item in included" :key="item" class="flex items-start gap-2.5">
              <svg class="mt-px flex-none" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#5ac26d" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4 10-10" /></svg>
              <span class="text-sm text-soft">{{ item }}</span>
            </div>
          </div>
        </Reveal>
        <Reveal :delay="0.06" class="rounded-2xl border border-white/[0.07] bg-[rgba(28,28,32,0.6)] p-6">
          <div class="font-pixel text-[13px] font-semibold uppercase tracking-wide text-faint">Verify your download</div>
          <div class="mt-3.5 text-[13.5px] leading-snug text-muted">SHA-256 checksum for <span class="text-soft">{{ rel?.setup?.name || 'the latest installer' }}</span></div>
          <button :disabled="!setupSha" class="mt-3 flex w-full items-center gap-2.5 rounded-[9px] border border-white/10 bg-black/25 px-3.5 py-3 text-left transition hover:border-brand/40 disabled:opacity-60" @click="copyHash">
            <code class="min-w-0 flex-1 truncate font-mono text-xs text-[#9ad6a6]">{{ setupSha || 'Computing checksum…' }}</code>
            <svg class="flex-none" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#9a9aa2" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15V5a2 2 0 0 1 2-2h10" /></svg>
          </button>
          <a :href="app.links.github" target="_blank" rel="noopener" class="mt-3.5 inline-flex items-center gap-1.5 text-[13.5px] font-semibold text-brand no-underline hover:underline">Prefer to build from source? View on GitHub →</a>
        </Reveal>
      </div>
    </main>
  </div>
</template>
