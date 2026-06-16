<script setup lang="ts">
// The little green "block" mark used in the nav/footer. Defaults to 30px.
withDefaults(defineProps<{ size?: number }>(), { size: 30 })
</script>

<template>
  <div
    class="flex flex-none items-center justify-center rounded-[7px]"
    :style="{
      width: size + 'px',
      height: size + 'px',
      background: 'linear-gradient(140deg,#6fce80,#46a85a)',
      boxShadow: '0 3px 9px rgba(70,168,90,.45)',
    }"
  >
    <svg
      :width="size * 0.6"
      :height="size * 0.6"
      viewBox="0 0 24 24"
      fill="none"
      stroke="#0e2b16"
      stroke-width="2.2"
      stroke-linejoin="round"
      stroke-linecap="round"
    >
      <path d="M12 3 4 7v10l8 4 8-4V7z" />
      <path d="M4 7l8 4 8-4" />
      <path d="M12 11v10" />
    </svg>
  </div>
</template>
