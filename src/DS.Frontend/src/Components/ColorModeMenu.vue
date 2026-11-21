<template>
    <UDropdownMenu :items="items" :content="{ align: 'end', side: 'top' }">
        <UTooltip :text="`Tema: ${current.label}`">
            <UButton color="neutral" variant="ghost" size="sm" :icon="current.icon" :aria-label="`Tema: ${current.label}. Skift tema`" />
        </UTooltip>
    </UDropdownMenu>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useColorMode } from "@vueuse/core";
import type { DropdownMenuItem } from "@nuxt/ui";

type Mode = "auto" | "light" | "dark";

const { store } = useColorMode();

const modes: { value: Mode; label: string; icon: string }[] = [
    { value: "auto", label: "Automatisk", icon: "i-lucide-monitor" },
    { value: "light", label: "Lys", icon: "i-lucide-sun" },
    { value: "dark", label: "Mørk", icon: "i-lucide-moon" }
];

const current = computed(() => modes.find((mode) => mode.value === store.value) ?? modes[0]!);

const items = computed<DropdownMenuItem[]>(() => modes.map((mode) => ({
    type: "checkbox" as const,
    label: mode.label,
    icon: mode.icon,
    checked: store.value === mode.value,
    onSelect: () => {
        store.value = mode.value;
    }
})));
</script>
