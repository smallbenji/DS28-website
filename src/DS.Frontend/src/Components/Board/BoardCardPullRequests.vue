<template>
    <section aria-labelledby="board-card-pull-requests-heading">
        <h3 id="board-card-pull-requests-heading" class="board-card-heading">Pull requests</h3>
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
        <p v-else class="has-text-grey is-size-7 mb-3">Ingen pull requests tilknyttet.</p>
        <div v-if="canManage">
            <BField label="Link til GitHub-pull request" label-for="board-card-pull-request-url">
                <BInput
                    id="board-card-pull-request-url"
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
import { ref } from 'vue';
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
const saving = ref(false);

function linkLabel(link: string) {
    return link.replace('https://github.com/', '').replace('/pull/', '#');
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
