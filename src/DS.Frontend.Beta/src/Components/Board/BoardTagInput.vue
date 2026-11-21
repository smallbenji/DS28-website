<template>
    <UFormField :error="error || undefined" help="Nye tags gemmes med kortet og starter uden farve.">
        <UInputMenu
            v-model="selected"
            v-model:search-term="searchTerm"
            :items="items"
            label-key="name"
            by="id"
            multiple
            create-item
            icon="i-lucide-tags"
            placeholder="Søg eller skriv et nyt tag…"
            :disabled="disabled"
            aria-label="Tags på denne tavle"
            class="w-full"
            @create="create"
        />
    </UFormField>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import type { BoardTagDto } from "@/types";

const props = defineProps<{ tags: BoardTagDto[]; disabled: boolean }>();
const selected = defineModel<BoardTagDto[]>({ required: true });

const searchTerm = ref("");
const error = ref("");
let nextDraftId = -1;

const normalized = (name: string) => name.trim().toLowerCase();

const items = computed(() => [...props.tags, ...selected.value.filter((tag) => tag.id < 0)]);

function create(name: string): boolean {
    error.value = "";
    const trimmed = name.trim();
    if (!trimmed || trimmed.length > 64) {
        error.value = "Tagget skal have et navn på højst 64 tegn.";
        return false;
    }

    if (!selected.value.some((tag) => normalized(tag.name) === normalized(trimmed))) {
        const existing = props.tags.find((tag) => normalized(tag.name) === normalized(trimmed));
        selected.value = [...selected.value, existing ?? { id: nextDraftId--, name: trimmed, color: null }];
    }

    searchTerm.value = "";
    return true;
}

function commitPending(): boolean {
    if (!searchTerm.value.trim()) return true;

    return create(searchTerm.value);
}

defineExpose({ commitPending });
</script>
