<template>
    <UEditor
        v-slot="{ editor }"
        v-model="model"
        content-type="html"
        :starter-kit="{ heading: { levels: [1, 2, 3] } }"
        :image="false"
        :mention="false"
        :placeholder="placeholder"
        class="min-h-32 w-full rounded-md border border-default"
        :ui="{ content: 'px-3 py-2', base: 'min-h-28' }"
    >
        <UEditorToolbar :editor="editor" :items="toolbarItems" class="border-b border-default px-1 py-1" aria-label="Tekstformatering" />
    </UEditor>
</template>

<script setup lang="ts">
import type { EditorToolbarItem } from "@nuxt/ui";

withDefaults(defineProps<{ placeholder?: string }>(), { placeholder: "" });

const model = defineModel<string>({ required: true });

const toolbarItems: EditorToolbarItem[][] = [
    [
        { kind: "heading", level: 1, icon: "i-lucide-heading-1", "aria-label": "Overskrift 1" },
        { kind: "heading", level: 2, icon: "i-lucide-heading-2", "aria-label": "Overskrift 2" },
        { kind: "heading", level: 3, icon: "i-lucide-heading-3", "aria-label": "Overskrift 3" }
    ],
    [
        { kind: "mark", mark: "bold", icon: "i-lucide-bold", "aria-label": "Fed" },
        { kind: "mark", mark: "italic", icon: "i-lucide-italic", "aria-label": "Kursiv" }
    ],
    [
        { kind: "bulletList", icon: "i-lucide-list", "aria-label": "Punktliste" },
        { kind: "orderedList", icon: "i-lucide-list-ordered", "aria-label": "Nummereret liste" },
        { kind: "blockquote", icon: "i-lucide-text-quote", "aria-label": "Citat" }
    ],
    [
        { kind: "clearFormatting", icon: "i-lucide-remove-formatting", "aria-label": "Fjern formatering" }
    ]
];
</script>
