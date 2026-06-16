// GSAP + ScrollTrigger, registered once and exposed as `$gsap` and `$reveal`.
// Client-only (filename ends in `.client`) so it never runs during SSR.
import { gsap } from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'

export interface RevealOptions {
  y?: number
  x?: number
  delay?: number
  duration?: number
  start?: string
}

export default defineNuxtPlugin((nuxtApp) => {
  gsap.registerPlugin(ScrollTrigger)

  const prefersReduced =
    typeof window !== 'undefined' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches

  /** Animate an element (with class `.reveal`) into view on scroll. */
  const reveal = (el: Element | null | undefined, opts: RevealOptions = {}) => {
    if (!el) return
    const { y = 30, x = 0, delay = 0, duration = 0.7, start = 'top 85%' } = opts

    if (prefersReduced) {
      gsap.set(el, { opacity: 1, x: 0, y: 0 })
      return
    }

    gsap.fromTo(
      el,
      { opacity: 0, x, y },
      {
        opacity: 1,
        x: 0,
        y: 0,
        duration,
        delay,
        ease: 'power3.out',
        scrollTrigger: { trigger: el, start, once: true },
      },
    )
  }

  // Recompute triggers after route transitions settle.
  nuxtApp.hook('page:finish', () => {
    requestAnimationFrame(() => ScrollTrigger.refresh())
  })

  return {
    provide: { gsap, reveal },
  }
})
