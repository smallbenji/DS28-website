<template>
    <UPageCard
        :title="shortcut.title"
        :icon="shortcut.icon"
        :to="shortcut.to"
        :target="shortcut.opensNewWindow ? '_blank' : undefined"
        :aria-label="shortcut.opensNewWindow ? `${shortcut.title} (åbner i nyt vindue)` : shortcut.title"
        orientation="horizontal"
        variant="outline"
        spotlight
        :ui="{ container: 'lg:items-center', leadingIcon: 'size-6', title: 'flex items-center gap-1.5' }"
    >
        <template #title>
            {{ shortcut.title }}
            <UIcon v-if="shortcut.opensNewWindow" name="i-lucide-external-link" class="size-3.5 text-muted" />
        </template>
    </UPageCard>
</template>

<script setup lang="ts">
import type { HqShortcut } from "@/lib/hqShortcuts";

defineProps<{ shortcut: HqShortcut }>();
</script>
