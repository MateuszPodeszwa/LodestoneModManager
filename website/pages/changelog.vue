<script setup lang="ts">
const app = useAppConfig()
const { data: releases } = await useFetch('/api/releases')
const { data: latest } = await useFetch('/api/releases/latest')

useSeoMeta({
  title: 'Changelog',
  description: 'Every Lodestone release and fix in one place. Updates install automatically inside the app.',
})

const dotColors = ['#5ac26d', '#5a91c2', '#d2a96a', '#bb78d6', '#7d97b8']

// If the API returns nothing yet, show a single truthful entry from the fallback.
const entries = computed(() => {
  if (releases.value && releases.value.length) return releases.value
  if (latest.value) {
    return [
      {
        version: latest.value.version,
        tag: `v${latest.value.version}`,
        name: latest.value.name,
        date: latest.value.date,
        latest: true,
        prerelease: false,
        htmlUrl: latest.value.htmlUrl,
        notesHtml: '',
        notesMarkdown: '',
      },
    ]
  }
  return []
})
</script>

<template>
  <div>
    <header class="relative overflow-hidden px-6 pb-10 pt-16 text-center sm:px-10" style="background: radial-gradient(115% 130% at 50% 0%, #26352c 0%, #1a1c21 50%, #141519 100%)">
      <div class="grid-backdrop pointer-events-none absolute inset-0" style="-webkit-mask-image: radial-gradient(circle at 50% 0%, #000 0%, transparent 65%); mask-image: radial-gradient(circle at 50% 0%, #000 0%, transparent 65%)" />
      <div class="relative mx-auto max-w-[720px]">
        <div class="eyebrow">Changelog</div>
        <h1 class="mt-3 font-pixel font-bold leading-none tracking-[0.6px] text-[#f5f5f7]" style="font-size: clamp(34px, 5vw, 56px)">What's new in Lodestone</h1>
        <p class="mx-auto mt-4 max-w-[500px] text-[17px] leading-relaxed text-muted">Every release, every fix, in one place. Updates install automatically inside the app.</p>
      </div>
    </header>

    <main class="mx-auto max-w-[880px] px-6 pb-20 pt-12 sm:px-10">
      <Reveal v-for="(r, i) in entries" :key="r.tag" class="grid grid-cols-1 gap-3 sm:grid-cols-[150px_1fr] sm:gap-7">
        <!-- left meta -->
        <div class="relative pb-2 sm:pb-8">
          <div class="inline-flex items-center gap-2">
            <span class="font-pixel text-lg font-bold text-[#f3f3f5]">{{ r.tag }}</span>
            <span v-if="r.latest" class="rounded-full bg-brand px-1.5 py-0.5 text-[10px] font-bold tracking-[0.5px] text-[#10130f]">LATEST</span>
            <span v-else-if="r.prerelease" class="rounded-full bg-grape/20 px-1.5 py-0.5 text-[10px] font-bold tracking-[0.5px] text-grape">BETA</span>
          </div>
          <div class="mt-1.5 text-[12.5px] text-dim">{{ formatDate(r.date) }}</div>
        </div>
        <!-- right content -->
        <div class="relative border-l border-white/[0.08] pb-9 pl-7">
          <div class="absolute -left-1.5 top-1 h-[11px] w-[11px] rounded-[3px]" :style="{ background: dotColors[i % dotColors.length], boxShadow: '0 0 0 4px #141519' }" />
          <div class="text-xl font-bold tracking-[-0.2px] text-[#f3f3f5]">{{ r.name }}</div>
          <div v-if="r.notesHtml" class="changelog-notes mt-3 text-[14.5px] leading-relaxed text-soft" v-html="r.notesHtml" />
          <a v-else :href="r.htmlUrl" target="_blank" rel="noopener" class="mt-3 inline-block text-sm font-semibold text-brand no-underline hover:underline">Read the release notes on GitHub →</a>
        </div>
      </Reveal>

      <div class="mt-2.5 flex flex-wrap items-center justify-between gap-3.5 rounded-2xl border border-brand/20 bg-brand/[0.06] px-6 py-5">
        <div class="max-w-[520px] text-sm leading-relaxed text-[#b6c9ba]">Want a heads-up on every release? Follow the project on GitHub — full notes land there first.</div>
        <a :href="app.links.github" target="_blank" rel="noopener" class="inline-flex items-center gap-2 whitespace-nowrap rounded-[9px] border border-white/[0.14] bg-white/[0.06] px-4 py-2.5 text-sm font-semibold text-[#f0f0f2] no-underline transition hover:bg-white/10">Watch on GitHub →</a>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* Minimal "prose" styling for GitHub-rendered release notes (v-html). */
.changelog-notes :deep(h1),
.changelog-notes :deep(h2),
.changelog-notes :deep(h3) {
  font-weight: 700;
  color: #f0f0f2;
  margin: 1.1em 0 0.4em;
  font-size: 1.02em;
}
.changelog-notes :deep(p) {
  margin: 0.5em 0;
}
.changelog-notes :deep(ul) {
  margin: 0.5em 0;
  padding-left: 1.2em;
  list-style: disc;
}
.changelog-notes :deep(li) {
  margin: 0.3em 0;
}
.changelog-notes :deep(a) {
  color: #5ac26d;
  text-decoration: none;
}
.changelog-notes :deep(a:hover) {
  text-decoration: underline;
}
.changelog-notes :deep(code) {
  font-family: ui-monospace, Consolas, monospace;
  font-size: 0.88em;
  background: rgba(255, 255, 255, 0.06);
  padding: 0.1em 0.35em;
  border-radius: 4px;
}
.changelog-notes :deep(strong) {
  color: #ededf0;
}
.changelog-notes :deep(hr) {
  border: none;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  margin: 1em 0;
}
</style>
