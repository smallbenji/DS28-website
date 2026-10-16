<template>
    <div class="rich-text-viewer" v-html="sanitized" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import DOMPurify from 'dompurify';

const props = defineProps<{
    html: string;
}>();

const sanitized = computed(() => DOMPurify.sanitize(props.html ?? ''));
</script>

<style scoped>
.rich-text-viewer :deep(p) {
    margin: 0 0 0.5rem;
}

.rich-text-viewer :deep(> *:last-child) {
    margin-bottom: 0;
}

.rich-text-viewer :deep(ul),
.rich-text-viewer :deep(ol) {
    margin: 0 0 0.5rem;
    padding-left: 1.4rem;
}

.rich-text-viewer :deep(li > p) {
    margin: 0;
}

.rich-text-viewer :deep(blockquote) {
    margin: 0 0 0.5rem;
    padding-left: 0.75rem;
    border-left: 3px solid rgba(10, 10, 10, 0.15);
    color: rgba(0, 0, 0, 0.7);
}

.rich-text-viewer :deep(h1),
.rich-text-viewer :deep(h2),
.rich-text-viewer :deep(h3) {
    margin: 0.85rem 0 0.4rem;
    line-height: 1.3;
    font-weight: 600;
}

.rich-text-viewer :deep(h1) {
    font-size: 1.35rem;
    font-weight: 700;
}

.rich-text-viewer :deep(h2) {
    font-size: 1.15rem;
}

.rich-text-viewer :deep(h3) {
    font-size: 1rem;
}

.rich-text-viewer :deep(h1:first-child),
.rich-text-viewer :deep(h2:first-child),
.rich-text-viewer :deep(h3:first-child) {
    margin-top: 0;
}

.rich-text-viewer :deep(pre) {
    margin: 0 0 0.5rem;
    padding: 0.6rem;
    border-radius: 4px;
    background: hsl(0, 0%, 96%);
    overflow-x: auto;
}

.rich-text-viewer :deep(code) {
    font-family: monospace;
    font-size: 0.9em;
}

.rich-text-viewer :deep(hr) {
    margin: 0.75rem 0;
    border: none;
    border-top: 1px solid rgba(10, 10, 10, 0.15);
}
</style>
