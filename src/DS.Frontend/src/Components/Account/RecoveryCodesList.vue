<template>
    <UAlert color="warning" variant="subtle" icon="i-lucide-triangle-alert" title="Gem disse recovery codes et sikkert sted" description="De vises kun én gang og kan bruges til at logge ind, hvis du mister adgang til din autentificeringsapp.">
    </UAlert>
    <ul class="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-3">
        <li v-for="code in codes" :key="code" class="rounded-md border border-default bg-elevated px-2 py-1.5 text-center font-mono text-sm">{{ code }}</li>
    </ul>
    <UButton class="mt-3" color="neutral" variant="outline" size="sm" icon="i-lucide-copy" label="Kopiér koderne" @click="copy" />
</template>

<script setup lang="ts">
import { useNotify } from "@/lib/notify";

const props = defineProps<{ codes: string[] }>();

const notify = useNotify();

async function copy() {
    try {
        await navigator.clipboard.writeText(props.codes.join("\n"));
        notify.success("Koderne er kopieret.");
    } catch {
        notify.error("Koderne kunne ikke kopieres. Skriv dem ned fra listen.");
    }
}
</script>
