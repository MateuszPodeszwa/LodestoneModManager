<script setup lang="ts">
import type { NuxtError } from '#app'
defineProps<{ error: NuxtError }>()
useHead({ title: 'Page not found' })
</script>

<template>
  <div class="flex min-h-screen flex-col items-center justify-center bg-ink px-6 text-center text-body" style="background: radial-gradient(120% 100% at 50% 0%, #26352c 0%, #15161b 55%)">
    <LogoMark :size="48" />
    <div class="mt-6 font-pixel text-[64px] font-bold leading-none text-[#f5f5f7]">{{ error?.statusCode || 404 }}</div>
    <h1 class="mt-2 font-pixel text-2xl font-semibold text-[#f3f3f5]">
      {{ error?.statusCode === 404 ? 'Lost in the mines' : 'Something broke' }}
    </h1>
    <p class="mt-3 max-w-[420px] text-[15px] leading-relaxed text-muted">
      {{ error?.statusCode === 404 ? "That page doesn't exist — it may have moved or never been mined." : 'An unexpected error occurred. Try again in a moment.' }}
    </p>
    <button class="btn-primary mc-clip mt-7 px-6 py-3.5 text-[15px]" @click="clearError({ redirect: '/' })">Back to home</button>
  </div>
</template>
