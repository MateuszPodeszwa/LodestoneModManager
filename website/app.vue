<script setup lang="ts">
// Root component. Global SEO defaults live here; per-page overrides use useSeoMeta().
const config = useRuntimeConfig()
const site = useAppConfig()

useHead({
  titleTemplate: (title) => (title ? `${title} · Lodestone` : 'Lodestone — the friendly Minecraft mod manager'),
  htmlAttrs: { lang: 'en' },
})

useSeoMeta({
  ogSiteName: 'Lodestone',
  ogType: 'website',
  ogImage: `${config.public.siteUrl}/og.png`,
  twitterCard: 'summary_large_image',
  twitterImage: `${config.public.siteUrl}/og.png`,
  themeColor: '#141519',
})
</script>

<template>
  <div>
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
    <!-- Global Minecraft stone-sword cursor (desktop, fine-pointer only). -->
    <CursorSword />
    <!-- Global toast host. -->
    <ToastHost />
  </div>
</template>
