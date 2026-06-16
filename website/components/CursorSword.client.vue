<script setup lang="ts">
// A custom Minecraft stone-sword cursor that follows the pointer with a little
// easing, swings on left-click and "jabs" on right-click. Desktop/fine-pointer
// only; disabled for touch and reduced-motion users (falls back to the OS cursor).
const wrap = ref<HTMLElement | null>(null)
const blade = ref<HTMLElement | null>(null)
const visible = ref(false)

let cleanup: (() => void) | null = null

onMounted(() => {
  const finePointer = window.matchMedia('(pointer: fine)').matches
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  if (!finePointer) return // touch / coarse pointer → keep native cursor

  const { $gsap } = useNuxtApp()
  const gsap = $gsap

  const root = document.documentElement
  root.classList.add('mc-cursor-on')

  // Smoothly chase the pointer (quickTo gives a natural trailing feel).
  const xTo = gsap.quickTo(wrap.value, 'x', { duration: 0.14, ease: 'power3.out' })
  const yTo = gsap.quickTo(wrap.value, 'y', { duration: 0.14, ease: 'power3.out' })

  const onMove = (e: PointerEvent) => {
    if (!visible.value) visible.value = true
    xTo(e.clientX)
    yTo(e.clientY)
  }

  const swing = () => {
    if (reduced || !blade.value) return
    gsap.fromTo(
      blade.value,
      { rotation: 0 },
      { rotation: -42, duration: 0.09, ease: 'power2.out', yoyo: true, repeat: 1 },
    )
  }
  const jab = () => {
    if (reduced || !blade.value) return
    gsap.fromTo(
      blade.value,
      { scale: 1, rotation: 0 },
      { scale: 0.72, rotation: 12, duration: 0.08, ease: 'power2.out', yoyo: true, repeat: 1 },
    )
  }

  const onDown = (e: PointerEvent) => (e.button === 2 ? jab() : swing())
  const onLeave = () => (visible.value = false)
  const onEnter = () => (visible.value = true)
  // Don't suppress the browser context menu — just animate alongside it.
  const onContext = () => jab()

  window.addEventListener('pointermove', onMove, { passive: true })
  window.addEventListener('pointerdown', onDown, { passive: true })
  window.addEventListener('contextmenu', onContext)
  document.addEventListener('mouseleave', onLeave)
  document.addEventListener('mouseenter', onEnter)

  cleanup = () => {
    root.classList.remove('mc-cursor-on')
    window.removeEventListener('pointermove', onMove)
    window.removeEventListener('pointerdown', onDown)
    window.removeEventListener('contextmenu', onContext)
    document.removeEventListener('mouseleave', onLeave)
    document.removeEventListener('mouseenter', onEnter)
  }
})

onBeforeUnmount(() => cleanup?.())
</script>

<template>
  <div
    ref="wrap"
    class="mc-cursor"
    :style="{ opacity: visible ? 1 : 0 }"
    aria-hidden="true"
  >
    <img ref="blade" src="/cursor/stone-sword.png" alt="" draggable="false" />
  </div>
</template>

<style scoped>
.mc-cursor {
  position: fixed;
  top: 0;
  left: 0;
  width: 34px;
  height: 34px;
  pointer-events: none;
  z-index: 2147483647;
  transition: opacity 0.18s ease;
  /* wrap is positioned via GSAP x/y (translate); offset so the blade tip sits
     right at the pointer hot-spot. */
  margin-left: -3px;
  margin-top: -2px;
}
.mc-cursor img {
  width: 100%;
  height: 100%;
  display: block;
  /* Keep the pixel art crisp and flip it so the blade tip points up-left. */
  image-rendering: pixelated;
  transform: scaleX(-1);
  transform-origin: 6px 4px; /* pivot near the tip when swinging */
  filter: drop-shadow(0 2px 3px rgba(0, 0, 0, 0.45));
}
</style>
