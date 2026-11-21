<template>
    <div class="rich-text-editor">
        <div v-if="editor" class="rich-text-toolbar" role="toolbar" aria-label="Tekstformatering">
            <button
                v-for="level in headingLevels"
                :key="level"
                type="button"
                class="rich-text-button rich-text-button-label"
                :class="{ 'is-active': editor.isActive('heading', { level }) }"
                :aria-label="`Overskrift ${level}`"
                @click="editor.chain().focus().toggleHeading({ level }).run()"
            >
                H{{ level }}
            </button>
            <button
                type="button"
                class="rich-text-button"
                :class="{ 'is-active': editor.isActive('bold') }"
                aria-label="Fed"
                @click="editor.chain().focus().toggleBold().run()"
            >
                <font-awesome-icon icon="bold" />
            </button>
            <button
                type="button"
                class="rich-text-button"
                :class="{ 'is-active': editor.isActive('italic') }"
                aria-label="Kursiv"
                @click="editor.chain().focus().toggleItalic().run()"
            >
                <font-awesome-icon icon="italic" />
            </button>
            <button
                type="button"
                class="rich-text-button"
                :class="{ 'is-active': editor.isActive('bulletList') }"
                aria-label="Punktliste"
                @click="editor.chain().focus().toggleBulletList().run()"
            >
                <font-awesome-icon icon="list-ul" />
            </button>
            <button
                type="button"
                class="rich-text-button"
                :class="{ 'is-active': editor.isActive('orderedList') }"
                aria-label="Nummereret liste"
                @click="editor.chain().focus().toggleOrderedList().run()"
            >
                <font-awesome-icon icon="list-ol" />
            </button>
            <button
                type="button"
                class="rich-text-button"
                :class="{ 'is-active': editor.isActive('blockquote') }"
                aria-label="Citat"
                @click="editor.chain().focus().toggleBlockquote().run()"
            >
                <font-awesome-icon icon="quote-left" />
            </button>
            <button
                type="button"
                class="rich-text-button"
                aria-label="Fjern formatering"
                @click="editor.chain().focus().unsetAllMarks().clearNodes().run()"
            >
                <font-awesome-icon icon="text-slash" />
            </button>
        </div>

        <EditorContent :editor="editor" class="rich-text-content" />
    </div>
</template>

<script setup lang="ts">
import { EditorContent, useEditor } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import { watch } from 'vue';

const props = defineProps<{
    modelValue: string;
}>();

const emit = defineEmits<{
    'update:modelValue': [value: string];
}>();

const editor = useEditor({
    content: props.modelValue,
    extensions: [StarterKit.configure({ heading: { levels: [1, 2, 3] } })],
    onUpdate: ({ editor: instance }) => {
        emit('update:modelValue', instance.getHTML());
    }
});

const headingLevels = [1, 2, 3] as const;

watch(() => props.modelValue, (value) => {
    if (!editor.value || value === editor.value.getHTML()) return;

    editor.value.commands.setContent(value, { emitUpdate: false });
});
</script>

<style scoped>
.rich-text-editor {
    border: 1px solid rgba(10, 10, 10, 0.2);
    border-radius: 4px;
    background: #fff;
}

.rich-text-toolbar {
    display: flex;
    flex-wrap: wrap;
    gap: 0.15rem;
    padding: 0.25rem;
    border-bottom: 1px solid rgba(10, 10, 10, 0.12);
    background: hsl(0, 0%, 98%);
    border-radius: 4px 4px 0 0;
}

.rich-text-button {
    border: none;
    background: transparent;
    color: rgba(0, 0, 0, 0.6);
    cursor: pointer;
    padding: 0.3rem 0.45rem;
    border-radius: 4px;
    line-height: 1;
}

.rich-text-button:hover {
    background: rgba(0, 0, 0, 0.06);
    color: rgba(0, 0, 0, 0.85);
}

.rich-text-button.is-active {
    background: rgba(0, 0, 0, 0.14);
    color: rgba(0, 0, 0, 0.85);
}

.rich-text-button-label {
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.02em;
    min-width: 1.9rem;
}

.rich-text-content :deep(.tiptap) {
    min-height: 9rem;
    padding: 0.75rem;
    outline: none;
}

.rich-text-content :deep(.tiptap p) {
    margin: 0 0 0.5rem;
}

.rich-text-content :deep(.tiptap > *:last-child) {
    margin-bottom: 0;
}

.rich-text-content :deep(.tiptap ul),
.rich-text-content :deep(.tiptap ol) {
    margin: 0 0 0.5rem;
    padding-left: 1.4rem;
}

.rich-text-content :deep(.tiptap li > p) {
    margin: 0;
}

.rich-text-content :deep(.tiptap blockquote) {
    margin: 0 0 0.5rem;
    padding-left: 0.75rem;
    border-left: 3px solid rgba(10, 10, 10, 0.15);
    color: rgba(0, 0, 0, 0.7);
}

.rich-text-content :deep(.tiptap h1),
.rich-text-content :deep(.tiptap h2),
.rich-text-content :deep(.tiptap h3) {
    margin: 0.85rem 0 0.4rem;
    line-height: 1.3;
    font-weight: 600;
}

.rich-text-content :deep(.tiptap h1) {
    font-size: 1.35rem;
    font-weight: 700;
}

.rich-text-content :deep(.tiptap h2) {
    font-size: 1.15rem;
}

.rich-text-content :deep(.tiptap h3) {
    font-size: 1rem;
}

.rich-text-content :deep(.tiptap h1:first-child),
.rich-text-content :deep(.tiptap h2:first-child),
.rich-text-content :deep(.tiptap h3:first-child) {
    margin-top: 0;
}

.rich-text-content :deep(.tiptap pre) {
    margin: 0 0 0.5rem;
    padding: 0.6rem;
    border-radius: 4px;
    background: hsl(0, 0%, 96%);
    overflow-x: auto;
}

.rich-text-content :deep(.tiptap code) {
    font-family: monospace;
    font-size: 0.9em;
}

.rich-text-content :deep(.tiptap hr) {
    margin: 0.75rem 0;
    border: none;
    border-top: 1px solid rgba(10, 10, 10, 0.15);
}
</style>
