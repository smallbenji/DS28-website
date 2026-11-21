<template>
    <div class="guest-background relative isolate flex min-h-svh flex-col">
        <PrairieBackground class="absolute inset-0 -z-10" />
        <header class="flex items-center justify-center gap-2 px-4 pt-10 pb-6">
            <UIcon name="i-lucide-tent-tree" class="size-6 text-blue-600" />
            <span class="text-lg font-semibold text-gray-800">DS28 HQ</span>
        </header>
        <main id="main-content" class="mx-auto w-full flex-1 px-4 pb-10" :class="route.meta.wide ? 'max-w-5xl' : 'max-w-md'">
            <RouterView />
        </main>
    </div>
</template>

<script setup lang="ts">
import { useRoute } from "vue-router";
import PrairieBackground from "@/Components/Auth/PrairieBackground.vue";

const route = useRoute();
</script>

<style scoped>
.guest-background {
    background-color: #b5ddf6;
}
</style>
