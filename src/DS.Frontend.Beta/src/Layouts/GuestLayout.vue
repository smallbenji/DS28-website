<template>
    <div class="guest-background flex min-h-svh flex-col" :style="{ backgroundImage: `url(${background})` }">
        <header class="flex items-center justify-center gap-2 px-4 pt-10 pb-6">
            <UIcon name="i-lucide-tent-tree" class="size-6 text-blue-600" />
            <span class="text-lg font-semibold text-gray-800">DS28 HQ</span>
            <UBadge label="Beta" color="primary" variant="subtle" size="sm" class="bg-blue-50 text-blue-600 ring-blue-200" />
        </header>
        <main id="main-content" class="mx-auto w-full flex-1 px-4 pb-10" :class="route.meta.wide ? 'max-w-5xl' : 'max-w-md'">
            <RouterView />
        </main>
    </div>
</template>

<script setup lang="ts">
import { useRoute } from "vue-router";
import background from "@/assets/login-background.svg";

const route = useRoute();
</script>

<style scoped>
.guest-background {
    background-color: #b5ddf6;
    background-position: center bottom;
    background-size: cover;
    background-repeat: no-repeat;
}
</style>
