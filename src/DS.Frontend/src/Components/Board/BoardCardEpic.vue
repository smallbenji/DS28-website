<template>
    <section aria-labelledby="card-epic-heading">
        <div class="mb-2 flex items-center justify-between gap-2">
            <h3 id="card-epic-heading" class="text-sm font-semibold text-highlighted">Epic</h3>
            <USwitch
                v-if="canManage && !card.epicCardId"
                :model-value="card.isEpic"
                label="Kortet er en epic"
                size="sm"
                :disabled="saving"
                @update:model-value="onToggle"
            />
            <UBadge v-else-if="card.isEpic" label="Epic" icon="i-lucide-layers" color="primary" variant="subtle" size="sm" />
        </div>

        <template v-if="card.isEpic">
            <div v-if="progress.total > 0" class="mb-3">
                <p class="mb-1 flex items-center gap-1.5 text-xs" :class="isComplete ? 'font-medium text-success' : 'text-muted'">
                    <UIcon v-if="isComplete" name="i-lucide-circle-check" class="size-3.5" />
                    {{ isComplete ? `Alle ${progress.total} kort er færdige` : `${progress.done} af ${progress.total} færdige` }}
                </p>
                <UProgress :model-value="progress.done" :max="progress.total" size="xs" :color="isComplete ? 'success' : 'primary'" />
            </div>

            <ul v-if="children.length" class="flex flex-col gap-1">
                <li v-for="child in children" :key="child.card.id">
                    <button
                        type="button"
                        class="flex w-full items-center gap-2 rounded-md bg-elevated px-2 py-1.5 text-start text-sm hover:bg-accented focus-visible:outline-2 focus-visible:outline-primary"
                        :aria-label="`Åbn DS-${child.card.id}: ${child.card.title}`"
                        @click="emit('open', child.card)"
                    >
                        <span class="font-mono text-xs text-muted">DS-{{ child.card.id }}</span>
                        <span class="min-w-0 flex-1 truncate" :class="{ 'text-muted line-through': child.isDone }">{{ child.card.title }}</span>
                        <UBadge :label="child.columnTitle" :icon="child.isDone ? 'i-lucide-check' : undefined" :color="child.isDone ? 'success' : 'neutral'" variant="subtle" size="sm" class="shrink-0" />
                    </button>
                </li>
            </ul>
            <p v-else class="text-xs text-muted">Ingen kort hører til denne epic endnu. Vælg epic'en i et andet korts dialog.</p>
        </template>

        <template v-else>
            <div v-if="epic" class="mb-2 flex items-center gap-2">
                <button
                    type="button"
                    class="flex min-w-0 flex-1 items-center gap-2 rounded-md bg-elevated px-2 py-1.5 text-start text-sm hover:bg-accented focus-visible:outline-2 focus-visible:outline-primary"
                    :aria-label="`Åbn epic DS-${epic.id}: ${epic.title}`"
                    @click="emit('open', epic)"
                >
                    <UIcon name="i-lucide-layers" class="size-4 shrink-0 text-primary" />
                    <span class="font-mono text-xs text-muted">DS-{{ epic.id }}</span>
                    <span class="min-w-0 flex-1 truncate">{{ epic.title }}</span>
                </button>
                <UButton v-if="canManage" color="neutral" variant="ghost" size="xs" icon="i-lucide-x" :aria-label="`Fjern kortet fra epic'en ${epic.title}`" :disabled="saving" @click="removeEpic" />
            </div>
            <p v-else-if="card.epicCardId" class="mb-2 text-xs text-muted">Hører til DS-{{ card.epicCardId }}.</p>

            <USelectMenu
                v-if="canManage && epicItems.length"
                :model-value="card.epicCardId ? String(card.epicCardId) : undefined"
                :items="epicItems"
                value-key="value"
                :placeholder="card.epicCardId ? 'Flyt til en anden epic…' : 'Vælg epic…'"
                :search-input="{ placeholder: 'Søg efter epic…' }"
                size="sm"
                class="w-full"
                :disabled="saving"
                aria-label="Vælg epic"
                @update:model-value="onSelectEpic"
            />
            <p v-else-if="!card.epicCardId" class="text-xs text-muted">
                {{ canManage ? 'Der er ingen andre epics på tavlen. Markér et kort som epic for at samle kort under det.' : 'Kortet hører ikke til en epic.' }}
            </p>
            <p v-if="canManage && card.epicCardId" class="mt-2 text-xs text-muted">Et kort, der hører til en epic, kan ikke selv være epic.</p>
        </template>

        <ConfirmModal
            v-model:open="unmarkOpen"
            title="Fjern epic-markering"
            :description="`${children.length} kort hører til ${card.title}. De mister deres epic, men bevares. Fortsæt?`"
            confirm-label="Fjern markering"
            color="error"
            :loading="saving"
            @confirm="setMarking(false)"
        />
    </section>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import BoardService from "@/Services/BoardService";
import { useBoardStore } from "@/Stores/BoardStore";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { WorkItemDto } from "@/types";

const props = defineProps<{ boardId: number; card: WorkItemDto; canManage: boolean }>();

const emit = defineEmits<{ open: [card: WorkItemDto] }>();

const store = useBoardStore();
const boardService = new BoardService();
const notify = useNotify();

const saving = ref(false);
const unmarkOpen = ref(false);

const boardCards = computed(() => store.SelectedBoard?.cards ?? []);
const columns = computed(() => store.SelectedBoard?.columns ?? []);

const epic = computed(() => props.card.epicCardId
    ? boardCards.value.find((card) => String(card.id) === String(props.card.epicCardId))
    : undefined);

const epicItems = computed(() => boardCards.value
    .filter((card) => card.isEpic && String(card.id) !== String(props.card.id))
    .map((card) => ({ label: `DS-${card.id} ${card.title}`, value: String(card.id) })));

const children = computed(() => boardCards.value
    .filter((card) => String(card.epicCardId) === String(props.card.id))
    .map((card) => {
        const column = columns.value.find((item) => String(item.id) === String(card.columnId));

        return { card, columnTitle: column?.title ?? "Ukendt kolonne", isDone: column?.isDoneColumn ?? false };
    }));

const progress = computed(() => props.card.epicProgress ?? { done: 0, total: 0 });
const isComplete = computed(() => progress.value.total > 0 && progress.value.done === progress.value.total);

function onToggle(value: boolean) {
    if (!value && children.value.length > 0) {
        unmarkOpen.value = true;
        return;
    }

    void setMarking(value);
}

async function setMarking(isEpic: boolean) {
    await run(
        () => boardService.setEpicMarking(props.boardId, Number(props.card.id), isEpic),
        isEpic ? "Kortet er markeret som epic." : "Kortet er ikke længere en epic.",
        "Epic-markeringen kunne ikke ændres."
    );
    unmarkOpen.value = false;
}

function onSelectEpic(value: string | undefined) {
    if (!value || value === String(props.card.epicCardId)) return;

    void run(
        () => boardService.setCardEpic(props.boardId, Number(props.card.id), Number(value)),
        `Kortet hører nu til DS-${value}.`,
        "Kortet kunne ikke lægges under epic'en."
    );
}

function removeEpic() {
    void run(
        () => boardService.removeCardEpic(props.boardId, Number(props.card.id)),
        "Kortet er fjernet fra epic'en.",
        "Kortet kunne ikke fjernes fra epic'en."
    );
}

async function run(action: () => Promise<void>, success: string, failure: string) {
    if (!props.canManage || saving.value) return;

    saving.value = true;
    try {
        await action();
        await store.GET_BOARD(props.boardId);
        notify.success(success);
    } catch (error) {
        notify.error(getErrorMessage(error, failure));
    } finally {
        saving.value = false;
    }
}
</script>
