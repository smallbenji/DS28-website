<template>
    <UFormField label="Ansvarlige">
        <template v-if="card && assignees.length" #hint>
            <span class="text-xs text-muted">{{ completedCount }}/{{ assignees.length }} klarmeldt</span>
        </template>

        <ul v-if="assignees.length" class="mb-2 flex flex-col gap-1">
            <li v-for="user in assignees" :key="user.id" class="flex items-center gap-2 text-sm">
                <UAvatar :src="user.profilePicture?.url" :text="initials(user)" size="2xs" />
                <span class="min-w-0 flex-1 truncate">{{ user.firstName }} {{ user.lastName }}</span>
                <UTooltip v-if="card" :text="user.completedAt ? `Klarmeldt ${formatDate(user.completedAt)}` : 'Ikke klarmeldt'">
                    <UIcon
                        :name="user.completedAt ? 'i-lucide-circle-check' : 'i-lucide-circle-dashed'"
                        class="size-4 shrink-0"
                        :class="user.completedAt ? 'text-success' : 'text-muted'"
                        :aria-label="user.completedAt ? 'Klarmeldt' : 'Ikke klarmeldt'"
                    />
                </UTooltip>
                <UButton v-if="canManage" color="neutral" variant="ghost" size="xs" icon="i-lucide-x" :aria-label="`Fjern ${user.firstName} ${user.lastName} som ansvarlig`" :disabled="saving" @click="remove(user.id)" />
            </li>
        </ul>
        <p v-else class="mb-2 text-sm text-muted">Ingen</p>

        <UButton
            v-if="card && myAssignment"
            class="mb-2"
            size="sm"
            block
            :color="myAssignment.completedAt ? 'neutral' : 'success'"
            :variant="myAssignment.completedAt ? 'outline' : 'soft'"
            :icon="myAssignment.completedAt ? 'i-lucide-undo-2' : 'i-lucide-check'"
            :label="myAssignment.completedAt ? 'Fortryd klarmelding' : 'Klarmeld min del'"
            :loading="saving"
            @click="toggleCompletion"
        />

        <template v-if="canManage">
            <UInput v-model="search" icon="i-lucide-search" placeholder="Tilføj: navn eller email…" :loading="searching" size="sm" class="w-full" aria-label="Søg efter ansvarlig" />
            <ul v-if="results.length" class="mt-1 flex flex-col rounded-md border border-default bg-default">
                <li v-for="user in results" :key="user.id">
                    <button
                        type="button"
                        class="flex w-full items-center justify-between gap-2 px-2 py-1.5 text-left text-sm hover:bg-elevated disabled:opacity-50"
                        :disabled="saving || isAssigned(user.id)"
                        @click="add(user)"
                    >
                        <span class="min-w-0">
                            <span class="block truncate">{{ user.firstName }} {{ user.lastName }}</span>
                            <span class="block truncate text-xs text-muted">{{ user.email }}</span>
                        </span>
                        <UIcon :name="isAssigned(user.id) ? 'i-lucide-check' : 'i-lucide-plus'" class="size-4 shrink-0 text-muted" />
                    </button>
                </li>
            </ul>
            <p v-else-if="search && !searching" class="mt-1 text-xs text-muted">Ingen brugere fundet.</p>
        </template>
    </UFormField>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import BoardService from "@/Services/BoardService";
import { useBoardStore } from "@/Stores/BoardStore";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import type { BoardMemberDto, UserSummaryDto, WorkItemAssigneeDto, WorkItemDto } from "@/types";

const props = defineProps<{ boardId: number; card: WorkItemDto | null; canManage: boolean }>();
const pending = defineModel<UserSummaryDto[]>("pending", { default: () => [] });

const service = new BoardService();
const store = useBoardStore();
const meStore = useMeStore();
const notify = useNotify();

const saving = ref(false);
const search = ref("");
const searching = ref(false);
const results = ref<BoardMemberDto[]>([]);
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const assignees = computed<WorkItemAssigneeDto[]>(() => props.card
    ? props.card.assignees
    : pending.value.map((user) => ({ ...user, completedAt: null })));
const completedCount = computed(() => assignees.value.filter((user) => user.completedAt).length);
const myAssignment = computed(() => assignees.value.find((user) => user.id === meStore.Me?.id));

watch(search, (value) => {
    if (searchTimeout) clearTimeout(searchTimeout);
    if (!value.trim()) {
        results.value = [];
        searching.value = false;
        return;
    }

    searching.value = true;
    searchTimeout = setTimeout(async () => {
        results.value = await service.searchAssignees(value.trim(), props.boardId);
        searching.value = false;
    }, 300);
});

onBeforeUnmount(() => {
    if (searchTimeout) clearTimeout(searchTimeout);
});

function initials(user: UserSummaryDto) {
    return `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toLocaleUpperCase("da-DK");
}

function formatDate(value: string) {
    return new Intl.DateTimeFormat("da-DK", { dateStyle: "medium", timeStyle: "short", timeZone: "Europe/Copenhagen" }).format(new Date(value));
}

function isAssigned(userId: string) {
    return assignees.value.some((user) => user.id === userId);
}

async function add(user: BoardMemberDto) {
    if (isAssigned(user.id)) return;

    search.value = "";
    results.value = [];
    if (!props.card) {
        pending.value = [...pending.value, { id: user.id, firstName: user.firstName, lastName: user.lastName, profilePicture: null }];
        return;
    }

    await run(() => service.addAssignee(props.boardId, props.card!.id, user.id), "Den ansvarlige kunne ikke tilføjes.");
}

async function remove(userId: string) {
    if (!props.card) {
        pending.value = pending.value.filter((user) => user.id !== userId);
        return;
    }

    await run(() => service.removeAssignee(props.boardId, props.card!.id, userId), "Den ansvarlige kunne ikke fjernes.");
}

async function toggleCompletion() {
    if (!props.card || !myAssignment.value) return;

    const completed = !myAssignment.value.completedAt;
    await run(() => service.setCompletion(props.boardId, props.card!.id, completed), "Klarmeldingen kunne ikke gemmes.");
}

async function run(action: () => Promise<void>, errorMessage: string) {
    if (saving.value) return;

    saving.value = true;
    try {
        await action();
        await store.GET_BOARD(props.boardId);
    } catch {
        notify.error(errorMessage);
    } finally {
        saving.value = false;
    }
}
</script>
