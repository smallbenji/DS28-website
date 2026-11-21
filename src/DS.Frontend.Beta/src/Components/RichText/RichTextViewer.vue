<template>
    <div class="rich-text-viewer text-sm" v-html="sanitized" />
</template>

<script setup lang="ts">
import { computed } from "vue";
import DOMPurify from "dompurify";

const props = defineProps<{ html: string }>();

const sanitized = computed(() => DOMPurify.sanitize(props.html ?? ""));
</script>

<style scoped>
.rich-text-viewer :deep(h1) { font-size: 1.25rem; font-weight: 600; margin: 0.75rem 0 0.25rem; }
.rich-text-viewer :deep(h2) { font-size: 1.125rem; font-weight: 600; margin: 0.75rem 0 0.25rem; }
.rich-text-viewer :deep(h3) { font-weight: 600; margin: 0.5rem 0 0.25rem; }
.rich-text-viewer :deep(p) { margin: 0.25rem 0; }
.rich-text-viewer :deep(ul) { list-style: disc; padding-left: 1.25rem; }
.rich-text-viewer :deep(ol) { list-style: decimal; padding-left: 1.25rem; }
.rich-text-viewer :deep(blockquote) { border-left: 3px solid var(--ui-border-accented); padding-left: 0.75rem; color: var(--ui-text-muted); }
</style>
