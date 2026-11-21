<template>
    <section aria-labelledby="board-card-pull-requests-heading">
        <div class="is-flex is-align-items-center is-justify-content-space-between">
            <h3 id="board-card-pull-requests-heading" class="board-card-heading">Pull requests</h3>
            <BButton
                v-if="canManage"
                size="is-small"
                class="is-ghost"
                icon-left="plus"
                :aria-expanded="formOpen"
                :disabled="saving"
                @click="toggleForm"
            >
                {{ formOpen ? 'Luk' : 'Tilføj' }}
            </BButton>
        </div>
        <ul v-if="card.pullRequests?.length" class="mb-3">
            <li v-for="link in card.pullRequests" :key="link" class="is-flex is-align-items-center is-flex-wrap-wrap mb-2">
                <a :href="link" target="_blank" rel="noopener noreferrer">{{ linkLabel(link) }}</a>
                <BButton
                    v-if="canManage"
                    class="ml-2"
                    size="is-small"
                    type="is-danger is-light"
                    :aria-label="`Fjern ${linkLabel(link)}`"
                    :disabled="saving"
                    @click="save(link, true)"
                >
                    Fjern
                </BButton>
            </li>
        </ul>
        <p v-else-if="!formOpen" class="has-text-grey is-size-7 mb-3">Ingen pull requests tilknyttet.</p>
        <div v-if="canManage && formOpen">
            <BField label="Link til GitHub-pull request" label-for="board-card-pull-request-url">
                <BInput
                    id="board-card-pull-request-url"
                    ref="linkInput"
                    v-model="url"
                    type="url"
                    maxlength="512"
                    :disabled="saving"
                    @keydown.enter.prevent="add"
                />
            </BField>
            <BButton type="is-primary" size="is-small" native-type="button" :disabled="!url.trim() || saving" @click="add">
                Tilføj PR-link
            </BButton>
        </div>
    </section>
</template>

<script setup lang="ts">
import { nextTick, ref } from 'vue';
import { BButton, BField, BInput, useToast } from 'buefy';
import { isAxiosError } from 'axios';
import BoardService from '@/Services/BoardService';
import { useBoardStore } from '@/Stores/BoardStore';
import type { WorkItemDto } from '@/types';

const props = defineProps<{ boardId: number; card: WorkItemDto; canManage: boolean }>();
const service = new BoardService();
const store = useBoardStore();
const toast = useToast();
const url = ref('');
const formOpen = ref(false);
const linkInput = ref<InstanceType<typeof BInput> | null>(null);
const saving = ref(false);

function linkLabel(link: string) {
    return link.replace('https://github.com/', '').replace('/pull/', '#');
}

async function toggleForm() {
    formOpen.value = !formOpen.value;
    if (formOpen.value) {
        await nextTick();
        linkInput.value?.focus();
    }
}

function add() {
    if (url.value.trim()) void save(url.value.trim(), false);
}

async function save(link: string, remove: boolean) {
    if (!props.canManage || saving.value) return;

    saving.value = true;
    try {
        if (remove) {
            await service.removePullRequest(props.boardId, props.card.id, link);
        } else {
            await service.addPullRequest(props.boardId, props.card.id, link);
            url.value = '';
            formOpen.value = false;
        }

        await store.GET_BOARD(props.boardId);
    } catch (error) {
        const status = isAxiosError(error) ? error.response?.status : undefined;
        const message = status === 409
            ? 'PR-linket er allerede tilknyttet.'
            : status === 400
                ? 'Angiv en HTTPS-adresse til en GitHub-pull request.'
                : 'PR-linket kunne ikke opdateres.';
        toast.open({ message, type: 'is-danger' });
    } finally {
        saving.value = false;
    }
}
</script>
