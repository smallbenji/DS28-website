<template>
    <div class="flex min-h-svh items-center justify-center p-4">
        <UCard class="w-full max-w-md text-center">
            <UIcon name="i-lucide-ban" class="size-10 text-muted" />
            <h1 class="mt-3 text-lg font-semibold text-highlighted">Adgang nægtet</h1>
            <p class="mt-1 text-sm text-muted">Du har ikke adgang til denne side.</p>
            <p class="mt-1 text-sm text-muted">Kontakt en administrator, hvis du mener, du skulle have adgang.</p>
            <code v-if="from" class="mt-3 block break-all rounded-md bg-elevated px-2 py-1 font-mono text-xs text-muted">{{ from }}</code>
            <div class="mt-4 flex flex-wrap justify-center gap-2">
                <UButton to="/" label="Til forsiden" />
                <UButton v-if="from" color="neutral" variant="outline" icon="i-lucide-arrow-left" label="Gå tilbage" @click="router.back()" />
            </div>
        </UCard>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";

const route = useRoute();
const router = useRouter();

const from = computed(() => {
    const value = route.query.from;

    return typeof value === "string" && value.startsWith("/") ? value : "";
});
</script>
