<template>
    <div class="flex min-h-svh flex-col bg-muted">
        <header class="flex items-center justify-center gap-2 px-4 pt-10 pb-6">
            <UIcon name="i-lucide-tent-tree" class="size-6 text-primary" />
            <span class="text-lg font-semibold text-highlighted">DS28 HQ</span>
            <UBadge label="Beta" color="primary" variant="subtle" size="sm" />
        </header>
        <main id="main-content" class="mx-auto w-full flex-1 px-4 pb-10" :class="route.meta.wide ? 'max-w-5xl' : 'max-w-md'">
            <RouterView />
        </main>
    </div>
</template>

<script setup lang="ts">
import { useRoute } from "vue-router";

const route = useRoute();
</script>
