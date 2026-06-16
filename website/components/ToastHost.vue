<script setup lang="ts">
const { toasts, remove } = useToast()
</script>

<template>
  <div
    class="fixed bottom-5 right-5 z-[80] flex flex-col gap-2.5"
    aria-live="polite"
    aria-atomic="false"
  >
    <TransitionGroup name="toast">
      <div
        v-for="t in toasts"
        :key="t.id"
        class="flex min-w-[250px] max-w-[330px] items-start gap-3 rounded-xl border border-white/10 bg-[#2b2b31] p-3.5 shadow-[0_16px_36px_-12px_rgba(0,0,0,0.6)]"
        role="status"
        @click="remove(t.id)"
      >
        <div
          class="mt-px flex h-6 w-6 flex-none items-center justify-center rounded-full"
          :class="t.kind === 'error' ? 'bg-[rgba(226,80,63,0.18)]' : 'bg-[rgba(90,194,109,0.16)]'"
        >
          <svg
            v-if="t.kind !== 'error'"
            width="13"
            height="13"
            viewBox="0 0 24 24"
            fill="none"
            stroke="#5ac26d"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path d="m5 12 4 4 10-10" />
          </svg>
          <span v-else class="text-[13px] font-bold text-[#e2503f]">!</span>
        </div>
        <div class="min-w-0">
          <div class="text-[13.5px] font-semibold text-[#f1f1f3]">{{ t.title }}</div>
          <div v-if="t.msg" class="mt-0.5 text-[12.5px] text-[#9d9da5]">{{ t.msg }}</div>
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: opacity 0.22s ease, transform 0.22s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateY(14px);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(20px);
}
</style>
