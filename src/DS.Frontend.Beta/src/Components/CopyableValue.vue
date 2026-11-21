<template>
    <div class="flex min-w-0 items-center gap-2 text-sm">
        <span class="shrink-0 text-muted">{{ label }}</span>
        <code class="min-w-0 truncate font-mono text-xs text-highlighted" :title="value">{{ value }}</code>
        <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-copy" :aria-label="`Kopiér ${label.toLowerCase()}`" class="shrink-0" @click="copy" />
    </div>
</template>

<script setup lang="ts">
import { useNotify } from "@/lib/notify";

const props = defineProps<{ label: string; value: string }>();

const notify = useNotify();

async function copy() {
    try {
        await navigator.clipboard.writeText(props.value);
        notify.success(`${props.label} er kopieret.`);
    } catch {
        notify.error(`${props.label} kunne ikke kopieres.`);
    }
}
</script>
