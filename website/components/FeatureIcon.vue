<script setup lang="ts">
// Small tinted icon tile used in the feature grid. `name` picks the glyph,
// `tone` picks the colour.
const props = withDefaults(
  defineProps<{ name: string; tone?: 'brand' | 'sky' | 'grape' | 'gold' }>(),
  { tone: 'brand' },
)

const tones: Record<string, { bg: string; stroke: string }> = {
  brand: { bg: 'rgba(90,194,109,0.14)', stroke: '#5ac26d' },
  sky: { bg: 'rgba(90,145,194,0.16)', stroke: '#5a91c2' },
  grape: { bg: 'rgba(187,120,214,0.16)', stroke: '#bb78d6' },
  gold: { bg: 'rgba(210,169,106,0.16)', stroke: '#d2a96a' },
}
const tone = computed(() => tones[props.tone] ?? tones.brand)
</script>

<template>
  <div class="flex h-[46px] w-[46px] items-center justify-center rounded-xl" :style="{ background: tone.bg }">
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" :stroke="tone.stroke" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round">
      <template v-if="name === 'upload'"><path d="M12 16V4" /><path d="m7 9 5-5 5 5" /><path d="M5 20h14" /></template>
      <template v-else-if="name === 'compass'"><circle cx="12" cy="12" r="9" /><path d="m15 9-2.4 5.4L7 17l2.4-5.4L15 9Z" /></template>
      <template v-else-if="name === 'refresh'"><path d="M3 12a9 9 0 0 1 15-6.7L21 8" /><path d="M21 3v5h-5" /><path d="M21 12a9 9 0 0 1-15 6.7L3 16" /><path d="M3 21v-5h5" /></template>
      <template v-else-if="name === 'layers'"><path d="m12 3 9 5-9 5-9-5 9-5Z" /><path d="m3 13 9 5 9-5" /></template>
      <template v-else-if="name === 'toggle'"><rect x="2" y="7" width="20" height="10" rx="5" /><circle cx="16" cy="12" r="3" :fill="tone.stroke" stroke="none" /></template>
      <template v-else><circle cx="12" cy="12" r="9" /><path d="m8.5 12 2.5 2.5 5-5" /></template>
    </svg>
  </div>
</template>
