<script setup lang="ts">
// Wrap any content to fade/slide it in on scroll (via GSAP ScrollTrigger).
// SSR-safe: content renders normally; the entrance only applies once JS mounts.
import type { RevealOptions } from '~/plugins/gsap.client'

const props = withDefaults(
  defineProps<{
    tag?: string
    y?: number
    x?: number
    delay?: number
    duration?: number
    start?: string
  }>(),
  { tag: 'div', y: 30, x: 0, delay: 0, duration: 0.7, start: 'top 85%' },
)

const el = ref<HTMLElement | null>(null)

onMounted(() => {
  const { $reveal } = useNuxtApp()
  const opts: RevealOptions = {
    y: props.y,
    x: props.x,
    delay: props.delay,
    duration: props.duration,
    start: props.start,
  }
  $reveal?.(el.value, opts)
})
</script>

<template>
  <component :is="tag" ref="el" class="reveal">
    <slot />
  </component>
</template>
