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
import { readableTextColor } from "@/lib/board/colorContrast";

const props = defineProps<{ tag: BoardTagDto }>();

const foreground = computed(() => readableTextColor(props.tag.color));
</script>
