<template>
    <BTaginput
        ref="input"
        v-model="selected"
        :data="suggestions"
        field="name"
        autocomplete
        allow-new
        open-on-focus
        :disabled="disabled"
        :before-adding="beforeAdding"
        :create-tag="createTag"
        :confirm-keys="['Enter', 'Tab']"
        :on-paste-separators="[]"
        :has-counter="false"
        aria-label="Tags på denne tavle"
        aria-close-label="Fjern tag"
        placeholder="Søg eller skriv et nyt tag…"
        class="board-tag-input"
        @typing="search = String($event ?? '')"
    >
        <template #default="{ option }">
            <BoardTagBadge :tag="option" />
        </template>
        <template #tag="{ tag }">
            <BoardTagBadge :tag="tag" />
        </template>
        <template #empty>
            <span>{{ search.trim() ? 'Tryk Enter for at tilføje navnet.' : 'Skriv et navn for at oprette et tag.' }}</span>
        </template>
    </BTaginput>
    <p v-if="error" class="help is-danger" role="alert">{{ error }}</p>
    <p class="help">Nye tags gemmes med kortet og starter uden farve.</p>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { BTaginput } from 'buefy';
import type { BoardTagDto } from '@/types';
import BoardTagBadge from './BoardTagBadge.vue';

const props = defineProps<{ tags: BoardTagDto[]; disabled: boolean }>();
const selected = defineModel<BoardTagDto[]>({ required: true });
const input = ref<InstanceType<typeof BTaginput>>();
const search = ref('');
const error = ref('');
let nextDraftId = -1;

const normalizedName = (name: string) => name.trim().toLowerCase();
const suggestions = computed(() => props.tags.filter(tag =>
    normalizedName(tag.name).includes(normalizedName(search.value)) &&
    !selected.value.some(value => normalizedName(value.name) === normalizedName(tag.name))
));

function resolveTag(value: string | BoardTagDto) {
    const name = typeof value === 'string' ? value.trim() : value.name;
    return props.tags.find(tag => normalizedName(tag.name) === normalizedName(name)) ??
        { id: nextDraftId, name, color: null };
}

function beforeAdding(value: string | BoardTagDto) {
    error.value = '';
    const tag = resolveTag(value);
    if (!tag.name || tag.name.length > 64) {
        error.value = 'Tagget skal have et navn på højst 64 tegn.';
        return false;
    }
    return !selected.value.some(value => normalizedName(value.name) === normalizedName(tag.name));
}

function createTag(value: string | BoardTagDto) {
    const tag = resolveTag(value);
    if (tag.id < 0) nextDraftId--;
    return tag;
}

function commitPending() {
    const name = String(input.value?.newTag ?? '').trim();
    if (!name) return true;
    if (!selected.value.some(tag => normalizedName(tag.name) === normalizedName(name))) {
        if (!beforeAdding(name)) return false;
        selected.value = [...selected.value, createTag(name)];
    }
    search.value = '';
    if (input.value) input.value.newTag = '';
    return true;
}

defineExpose({ commitPending });
</script>

<style scoped>
.board-tag-input {
    min-width: 0;
    max-width: 100%;
}

.board-tag-input :deep(.taginput-container > .tag) {
    height: auto;
    max-width: 100%;
    white-space: normal;
}

.board-tag-input :deep(.taginput-container > .tag > span) {
    min-width: 0;
}

.board-tag-input :deep(.autocomplete) {
    flex: 1 1 8rem;
    max-width: 100%;
    min-width: 0;
}

.board-tag-input :deep(.autocomplete .input) {
    min-width: 0;
}
</style>
