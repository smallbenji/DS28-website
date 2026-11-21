<template>
    <UApp :locale="da">
        <RouterView />
    </UApp>
</template>

<script setup lang="ts">
import { da } from "@nuxt/ui/locale";
</script>
