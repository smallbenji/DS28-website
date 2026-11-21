<template>
    <UBadge
        :label="tag.name"
        size="sm"
        :color="tag.color ? undefined : 'neutral'"
        :variant="tag.color ? 'solid' : 'subtle'"
        :style="tag.color ? { backgroundColor: tag.color, color: foreground } : undefined"
    />
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { BoardTagDto } from "@/types";

const props = defineProps<{ tag: BoardTagDto }>();

const foreground = computed(() => {
    const hex = props.tag.color?.slice(1) ?? "ffffff";
    const channels = [0, 2, 4]
        .map((offset) => parseInt(hex.slice(offset, offset + 2), 16) / 255)
        .map((value) => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);

    return channels[0]! * 0.2126 + channels[1]! * 0.7152 + channels[2]! * 0.0722 > 0.179 ? "#000000" : "#ffffff";
});
</script>
