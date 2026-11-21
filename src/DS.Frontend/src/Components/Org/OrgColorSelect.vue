<template>
    <USelect v-model="selected" :items="items" class="w-full">
        <template #leading>
            <span class="size-3 rounded-full border border-default" :style="model ? { backgroundColor: model } : undefined" />
        </template>
    </USelect>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { ORG_COLORS } from "@/lib/org/orgPalette";

const model = defineModel<string | null>({ required: true });

const noColor = "none";
const items = ORG_COLORS.map((color) => ({ label: color.label, value: color.value ?? noColor }));

const selected = computed({
    get: () => model.value ?? noColor,
    set: (value: string) => {
        model.value = value === noColor ? null : value;
    }
});
</script>
