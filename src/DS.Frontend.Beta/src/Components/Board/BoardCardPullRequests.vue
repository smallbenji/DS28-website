<template>
    <section aria-labelledby="card-pull-requests-heading">
        <div class="mb-2 flex items-center justify-between gap-2">
            <h3 id="card-pull-requests-heading" class="text-sm font-semibold text-highlighted">Pull requests</h3>
            <UButton v-if="canManage" color="neutral" variant="ghost" size="xs" :icon="formOpen ? 'i-lucide-x' : 'i-lucide-plus'" :label="formOpen ? 'Luk' : 'Tilføj'" :aria-expanded="formOpen" :disabled="saving" @click="formOpen = !formOpen" />
        </div>

        <ul v-if="card.pullRequests?.length" class="mb-3 flex flex-col gap-1">
            <li v-for="link in card.pullRequests" :key="link" class="flex items-center gap-2 text-sm">
                <UIcon name="i-lucide-git-pull-request" class="size-4 text-muted" />
                <ULink :to="link" target="_blank" rel="noopener noreferrer" class="min-w-0 truncate">{{ linkLabel(link) }}</ULink>
                <UButton v-if="canManage" color="error" variant="ghost" size="xs" icon="i-lucide-x" :aria-label="`Fjern ${linkLabel(link)}`" :disabled="saving" @click="save(link, true)" />
            </li>
        </ul>
        <p v-else-if="!formOpen" class="mb-3 text-xs text-muted">Ingen pull requests tilknyttet.</p>

        <form v-if="canManage && formOpen" @submit.prevent="add">
            <UFormField label="Link til GitHub-pull request">
                <UFieldGroup class="w-full">
                    <UInput v-model="url" type="url" maxlength="512" placeholder="https://github.com/…/pull/1" :disabled="saving" class="w-full" autofocus />
                    <UButton type="submit" label="Tilføj" :disabled="!url.trim() || saving" :loading="saving" />
                </UFieldGroup>
            </UFormField>
        </form>
    </section>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { isAxiosError } from "axios";
import BoardService from "@/Services/BoardService";
import { useBoardStore } from "@/Stores/BoardStore";
import { useNotify } from "@/lib/notify";
import type { WorkItemDto } from "@/types";

const props = defineProps<{ boardId: number; card: WorkItemDto; canManage: boolean }>();

const service = new BoardService();
const store = useBoardStore();
const notify = useNotify();

const url = ref("");
const formOpen = ref(false);
const saving = ref(false);

function linkLabel(link: string) {
    return link.replace("https://github.com/", "").replace("/pull/", "#");
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
            url.value = "";
            formOpen.value = false;
        }

        await store.GET_BOARD(props.boardId);
    } catch (error) {
        const status = isAxiosError(error) ? error.response?.status : undefined;
        notify.error(status === 409
            ? "PR-linket er allerede tilknyttet."
            : status === 400
                ? "Angiv en HTTPS-adresse til en GitHub-pull request."
                : "PR-linket kunne ikke opdateres.");
    } finally {
        saving.value = false;
    }
}
</script>
