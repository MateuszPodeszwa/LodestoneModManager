<script setup lang="ts">
// Faithful reproduction of the in-app UI mock (design: AppMock.dc.html).
// `screen` switches the visible view: home | browse | library | updates | support.
const props = withDefaults(defineProps<{ screen?: string }>(), { screen: 'home' })

const nav = computed(() =>
  props.screen === 'browse'
    ? 'browse'
    : props.screen === 'library'
      ? 'library'
      : props.screen === 'support'
        ? 'support'
        : 'home',
)

const railBase =
  'relative flex items-center gap-2.5 rounded-[7px] px-2.5 py-[7px] text-xs font-medium transition'
function rail(active: boolean) {
  return [railBase, active ? 'bg-white/[0.06] text-[#f0f0f2]' : 'text-[#cfcfd4]']
}
function railPink(active: boolean) {
  return [railBase, active ? 'bg-[rgba(226,113,154,0.12)] text-[#f4cdda]' : 'text-[#cfcfd4]']
}

const browseMods = [
  { name: 'Sodium', author: 'CaffeineMC', letter: 'S', color: '#5ac26d', fg: '#10130f', installed: false },
  { name: 'Iris Shaders', author: 'coderbot', letter: 'I', color: '#5a91c2', fg: '#fff', installed: false },
  { name: 'Fabric API', author: 'modmuss50', letter: 'F', color: '#9a6cc9', fg: '#fff', installed: true },
  { name: 'Just Enough Items', author: 'mezz', letter: 'J', color: '#c2a65a', fg: '#10130f', installed: false },
]
const libraryMods = [
  { name: 'Sodium', meta: 'v0.5.8 · Fabric · 1.2 MB', letter: 'S', color: '#5ac26d', fg: '#10130f', on: true, update: false, dim: false },
  { name: 'Iris Shaders', meta: 'v1.8.0 · Fabric · 3.1 MB', letter: 'I', color: '#5a91c2', fg: '#fff', on: true, update: true, dim: false },
  { name: 'Lithium', meta: 'v0.13.0 · Fabric · 0.8 MB', letter: 'L', color: '#5ac2b4', fg: '#10130f', on: false, update: false, dim: true },
  { name: 'Faithful 32x', meta: 'Resource Pack · 42 MB', letter: 'F', color: '#c27a5a', fg: '#fff', on: true, update: false, dim: false },
]
</script>

<template>
  <div
    class="flex h-full w-full flex-col overflow-hidden rounded-[11px] border border-white/[0.09] bg-[#1c1c20] font-sans"
  >
    <!-- title bar -->
    <div
      class="flex h-9 flex-none items-center gap-2.5 border-b border-white/5 px-3.5"
      style="background: rgba(255, 255, 255, 0.014)"
    >
      <LogoMark :size="20" />
      <div class="text-[11.5px] font-semibold tracking-[0.2px] text-[#f2f2f4]">Lodestone</div>
      <div class="text-[11px] text-[#75757d]">Mod Manager</div>
      <div class="ml-auto flex gap-[7px]">
        <div class="h-[9px] w-[9px] rounded-sm bg-white/[0.13]" />
        <div class="h-[9px] w-[9px] rounded-sm bg-white/[0.13]" />
        <div class="h-[9px] w-[9px] rounded-sm" style="background: rgba(226, 80, 63, 0.55)" />
      </div>
    </div>

    <div class="flex min-h-0 flex-1">
      <!-- rail -->
      <div
        class="flex w-[148px] flex-none flex-col gap-[3px] border-r border-white/5 bg-surface-rail px-[9px] py-[11px]"
      >
        <div class="px-[9px] pb-1.5 pt-1 text-[9px] font-bold tracking-[1.1px] text-[#6a6a72]">LIBRARY</div>
        <div :class="rail(nav === 'home')">
          <div v-if="nav === 'home'" class="absolute left-0 top-1/2 h-[15px] w-[3px] -translate-y-1/2 rounded bg-brand" />
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" stroke-linecap="round"><path d="M4 11 12 4l8 7" /><path d="M6 10v9h12v-9" /></svg>
          <span>Home</span>
        </div>
        <div :class="rail(nav === 'library')">
          <div v-if="nav === 'library'" class="absolute left-0 top-1/2 h-[15px] w-[3px] -translate-y-1/2 rounded bg-brand" />
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"><rect x="4" y="4" width="16" height="5" rx="1.5" /><rect x="4" y="10.5" width="16" height="5" rx="1.5" /><rect x="4" y="17" width="16" height="3" rx="1.5" /></svg>
          <span>My Content</span>
        </div>
        <div class="px-[9px] pb-1.5 pt-[11px] text-[9px] font-bold tracking-[1.1px] text-[#6a6a72]">DISCOVER</div>
        <div :class="rail(nav === 'browse')">
          <div v-if="nav === 'browse'" class="absolute left-0 top-1/2 h-[15px] w-[3px] -translate-y-1/2 rounded bg-brand" />
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" stroke-linecap="round"><circle cx="12" cy="12" r="9" /><path d="m15 9-2.4 5.4L7 17l2.4-5.4L15 9Z" /></svg>
          <span>Browse mods</span>
        </div>
        <div class="mt-auto" />
        <div :class="railPink(nav === 'support')">
          <div v-if="nav === 'support'" class="absolute left-0 top-1/2 h-[15px] w-[3px] -translate-y-1/2 rounded bg-pink" />
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#e2719a" stroke-width="1.8" stroke-linejoin="round" stroke-linecap="round"><path d="M12 20s-7-4.7-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.3-7 10-7 10Z" /></svg>
          <span>Support us</span>
        </div>
      </div>

      <!-- content -->
      <div class="relative min-w-0 flex-1 overflow-hidden bg-[#1e1e22]">
        <Transition name="amfade" mode="out-in">
          <!-- HOME -->
          <div v-if="screen === 'home'" key="home" class="absolute inset-0 px-[22px] py-5">
            <div class="text-lg font-bold tracking-[-0.3px] text-[#f3f3f5]">Welcome back</div>
            <div class="mt-[3px] text-[11.5px] text-muted">Drop a file below to install it instantly — no config, no fuss.</div>
            <div class="mt-[15px] grid grid-cols-3 gap-[9px]">
              <div class="rounded-[9px] border border-white/[0.06] bg-surface-2 px-3 py-[11px]"><div class="text-xl font-bold text-[#f3f3f5]">24</div><div class="text-[10px] text-muted">Mods</div></div>
              <div class="rounded-[9px] border border-white/[0.06] bg-surface-2 px-3 py-[11px]"><div class="text-xl font-bold text-[#f3f3f5]">3</div><div class="text-[10px] text-muted">Packs</div></div>
              <div class="rounded-[9px] border border-white/[0.06] bg-surface-2 px-3 py-[11px]"><div class="text-xl font-bold text-brand">1.21.4</div><div class="text-[10px] text-muted">Version</div></div>
            </div>
            <div class="mt-[15px] flex flex-col items-center rounded-[13px] border-2 border-dashed border-brand/40 px-[18px] py-[26px] text-center" style="background: rgba(90, 194, 109, 0.07)">
              <div class="flex h-[46px] w-[46px] items-center justify-center rounded-xl" style="background: rgba(90, 194, 109, 0.16)">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#5ac26d" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 16V4" /><path d="m7 9 5-5 5 5" /><path d="M5 20h14" /></svg>
              </div>
              <div class="mt-[11px] text-sm font-semibold text-[#f0f0f2]">Drag &amp; drop to install</div>
              <div class="mt-1 text-[11px] text-muted">Mods (.jar) · Packs &amp; shaders (.zip)</div>
            </div>
          </div>

          <!-- BROWSE -->
          <div v-else-if="screen === 'browse'" key="browse" class="absolute inset-0 px-[22px] py-5">
            <div class="text-lg font-bold tracking-[-0.3px] text-[#f3f3f5]">Browse mods</div>
            <div class="mt-3 flex items-center gap-2.5 rounded-lg border border-white/10 bg-surface-3 px-[11px] py-2">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#8e8e96" stroke-width="1.8" stroke-linecap="round"><circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" /></svg>
              <span class="text-[11.5px] text-dim">Search mods, packs and shaders</span>
            </div>
            <div class="mt-[13px] grid grid-cols-2 gap-[9px]">
              <div v-for="m in browseMods" :key="m.name" class="rounded-[10px] border border-white/[0.06] bg-surface-2 p-3">
                <div class="flex items-center gap-2.5">
                  <div class="flex h-8 w-8 items-center justify-center rounded-lg text-[15px] font-bold" :style="{ background: m.color, color: m.fg }">{{ m.letter }}</div>
                  <div class="min-w-0"><div class="text-[12.5px] font-semibold text-[#f0f0f2]">{{ m.name }}</div><div class="text-[10px] text-dim">{{ m.author }}</div></div>
                </div>
                <div v-if="m.installed" class="mt-[11px] flex items-center justify-center gap-1.5 p-1.5 text-[11px] font-semibold text-brand">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="#5ac26d" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4 10-10" /></svg>Installed
                </div>
                <button v-else class="mt-[11px] w-full rounded-[7px] bg-brand p-1.5 text-[11px] font-semibold text-brand-ink">Install</button>
              </div>
            </div>
          </div>

          <!-- LIBRARY -->
          <div v-else-if="screen === 'library'" key="library" class="absolute inset-0 px-[22px] py-5">
            <div class="text-lg font-bold tracking-[-0.3px] text-[#f3f3f5]">My Content</div>
            <div class="mt-[3px] text-[11.5px] text-muted">9 mods · 1.21.4</div>
            <div class="mt-3.5 flex flex-col gap-2">
              <div v-for="m in libraryMods" :key="m.name" class="flex items-center gap-2.5 rounded-[10px] border border-white/[0.06] bg-surface-2 px-[13px] py-2.5" :style="m.dim ? 'opacity:.55' : ''">
                <div class="flex h-[34px] w-[34px] items-center justify-center rounded-lg text-[15px] font-bold" :style="{ background: m.color, color: m.fg }">{{ m.letter }}</div>
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-1.5">
                    <span class="text-[12.5px] font-semibold text-[#f0f0f2]">{{ m.name }}</span>
                    <span v-if="m.update" class="rounded-full px-1.5 py-px text-[9.5px] font-semibold" style="background: rgba(90, 194, 109, 0.15); color: #5ac26d">Update</span>
                  </div>
                  <div class="text-[10px] text-dim">{{ m.meta }}</div>
                </div>
                <div class="relative h-[19px] w-[34px] rounded-full" :style="m.on ? 'background:#5ac26d' : 'background:rgba(255,255,255,0.14)'">
                  <div class="absolute top-[3px] h-[13px] w-[13px] rounded-full bg-white" :style="m.on ? 'left:18px' : 'left:3px'" />
                </div>
              </div>
            </div>
          </div>

          <!-- UPDATES -->
          <div v-else-if="screen === 'updates'" key="updates" class="absolute inset-0 px-[22px] py-5">
            <div class="text-lg font-bold tracking-[-0.3px] text-[#f3f3f5]">Updates</div>
            <div class="mt-[3px] text-[11.5px] text-muted">2 updates available</div>
            <div class="mt-3.5 rounded-[11px] border border-white/[0.06] bg-surface-2 px-4 py-[15px]">
              <div class="flex items-center gap-2.5 py-[7px]"><div class="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-sky text-[13px] font-bold text-white">I</div><div><div class="text-xs font-semibold text-[#ededf0]">Iris Shaders</div><div class="text-[10px] text-dim">v1.8.0 → 1.8.1</div></div></div>
              <div class="flex items-center gap-2.5 py-[7px]"><div class="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-gold text-[13px] font-bold text-brand-ink">J</div><div><div class="text-xs font-semibold text-[#ededf0]">Just Enough Items</div><div class="text-[10px] text-dim">v19.4.0 → 19.5.0</div></div></div>
              <button class="mt-[13px] w-full rounded-lg bg-brand p-[9px] text-[12.5px] font-bold text-brand-ink">Update all</button>
            </div>
          </div>

          <!-- SUPPORT -->
          <div v-else key="support" class="absolute inset-0 px-[22px] py-5">
            <div class="text-lg font-bold tracking-[-0.3px] text-[#f3f3f5]">Support Lodestone</div>
            <div class="mt-[3px] text-[11.5px] text-muted">Free and open source. Built by one person — your support keeps it going.</div>
            <div class="mt-[15px] flex flex-col gap-[9px]">
              <div class="flex items-center gap-3 rounded-[11px] border bg-surface-2 px-[15px] py-[13px]" style="border-color: rgba(226, 113, 154, 0.22)">
                <div class="flex h-9 w-9 items-center justify-center rounded-[9px]" style="background: rgba(226, 113, 154, 0.16)"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#e2719a" stroke-width="1.9" stroke-linejoin="round" stroke-linecap="round"><path d="M12 20s-7-4.7-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.3-7 10-7 10Z" /></svg></div>
                <div class="min-w-0 flex-1"><div class="text-[12.5px] font-semibold text-[#f0f0f2]">Become a patron</div><div class="text-[10px] text-dim">Monthly support from $3</div></div>
                <button class="rounded-[7px] px-3.5 py-[7px] text-[11px] font-bold text-[#2a0f1b]" style="background: #e2719a">Patreon</button>
              </div>
              <div class="flex items-center gap-3 rounded-[11px] border border-white/[0.06] bg-surface-2 px-[15px] py-[13px]">
                <div class="flex h-9 w-9 items-center justify-center rounded-[9px] bg-white/[0.07]"><svg width="18" height="18" viewBox="0 0 24 24" fill="#cdcdd3"><path d="M12 2 9.2 8.6 2 9.2l5.5 4.7L5.8 21 12 17l6.2 4-1.7-7.1L22 9.2l-7.2-.6z" /></svg></div>
                <div class="min-w-0 flex-1"><div class="text-[12.5px] font-semibold text-[#f0f0f2]">Star on GitHub</div><div class="text-[10px] text-dim">Free — helps others find it</div></div>
                <div class="flex items-center gap-1.5 rounded-[7px] bg-white/[0.06] px-3 py-[7px] text-[11px] font-semibold text-[#cfcfd4]">★ Star</div>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </div>
  </div>
</template>

<style scoped>
.amfade-enter-active,
.amfade-leave-active {
  transition: opacity 0.28s ease, transform 0.28s ease;
}
.amfade-enter-from {
  opacity: 0;
  transform: translateY(5px);
}
.amfade-leave-to {
  opacity: 0;
}
</style>
