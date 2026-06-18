<script setup lang="ts">
// Custom Minecraft stone-sword cursor: follows the pointer, swings on left-click
// and jabs on right-click. Self-contained (no GSAP dependency) and fail-safe -
// the native cursor is only hidden once the sword image has actually loaded, so
// a missing image or touch device just falls back to the normal cursor.
const wrap = ref<HTMLElement | null>(null)
const ready = ref(false)
const visible = ref(false)

function finePointer() {
  return (
    typeof window !== 'undefined' &&
    window.matchMedia('(pointer: fine)').matches &&
    window.matchMedia('(hover: hover)').matches
  )
}

// Fires when the sword PNG has loaded. Only now do we take over the cursor.
function onImgLoad() {
  if (!finePointer()) return
  document.documentElement.classList.add('mc-cursor-on')
  ready.value = true
}

let cleanup: (() => void) | null = null

onMounted(() => {
  if (!finePointer()) return

  let px = 0
  let py = 0
  let raf = 0

  const blade = () => (wrap.value?.firstElementChild as HTMLElement | null) ?? null

  const onMove = (e: PointerEvent) => {
    px = e.clientX
    py = e.clientY
    if (!visible.value && ready.value) visible.value = true
    if (raf) return
    raf = requestAnimationFrame(() => {
      raf = 0
      if (wrap.value) wrap.value.style.transform = `translate3d(${px}px, ${py}px, 0)`
    })
  }
  const swing = (cls: 'mc-swing' | 'mc-jab') => {
    const b = blade()
    if (!b) return
    b.classList.remove('mc-swing', 'mc-jab')
    void b.offsetWidth // restart the CSS animation
    b.classList.add(cls)
  }
  const onDown = (e: PointerEvent) => swing(e.button === 2 ? 'mc-jab' : 'mc-swing')
  const onEnter = () => {
    if (ready.value) visible.value = true
  }
  const onLeave = () => (visible.value = false)

  window.addEventListener('pointermove', onMove, { passive: true })
  window.addEventListener('pointerdown', onDown, { passive: true })
  document.addEventListener('mouseenter', onEnter)
  document.addEventListener('mouseleave', onLeave)

  // In case the image was already cached and fired @load before mount.
  const img = blade() as HTMLImageElement | null
  if (img?.complete && img.naturalWidth > 0) onImgLoad()

  cleanup = () => {
    document.documentElement.classList.remove('mc-cursor-on')
    window.removeEventListener('pointermove', onMove)
    window.removeEventListener('pointerdown', onDown)
    document.removeEventListener('mouseenter', onEnter)
    document.removeEventListener('mouseleave', onLeave)
    if (raf) cancelAnimationFrame(raf)
  }
})

onBeforeUnmount(() => cleanup?.())
</script>

<template>
  <div ref="wrap" class="mc-cursor" :style="{ opacity: visible ? 1 : 0 }" aria-hidden="true">
    <img src="/cursor/stone-sword.png" alt="" draggable="false" @load="onImgLoad" />
  </div>
</template>

<style scoped>
.mc-cursor {
  position: fixed;
  top: 0;
  left: 0;
  width: 40px;
  height: 40px;
  margin-left: -3px; /* nudge the blade tip onto the actual pointer */
  margin-top: -2px;
  pointer-events: none;
  z-index: 2147483647;
  opacity: 0;
  transition: opacity 0.18s ease;
  will-change: transform;
}
.mc-cursor img {
  display: block;
  width: 100%;
  height: 100%;
  image-rendering: pixelated;
  transform: scaleX(-1); /* flip so the blade tip points up-left */
  transform-origin: 30% 20%;
  filter: drop-shadow(0 2px 3px rgba(0, 0, 0, 0.5));
}
/* Click animations compose with the flip so it isn't lost mid-swing. */
.mc-cursor img.mc-swing {
  animation: mcSwing 0.22s ease;
}
.mc-cursor img.mc-jab {
  animation: mcJab 0.18s ease;
}
@keyframes mcSwing {
  0% { transform: scaleX(-1) rotate(0); }
  45% { transform: scaleX(-1) rotate(-40deg); }
  100% { transform: scaleX(-1) rotate(0); }
}
@keyframes mcJab {
  0% { transform: scaleX(-1) scale(1) rotate(0); }
  45% { transform: scaleX(-1) scale(0.72) rotate(12deg); }
  100% { transform: scaleX(-1) scale(1) rotate(0); }
}
@media (prefers-reduced-motion: reduce) {
  .mc-cursor img.mc-swing,
  .mc-cursor img.mc-jab {
    animation: none;
  }
}
</style>
