<template>
    <UModal v-model:open="open" title="Medlemmer og invitation" :description="loading ? 'Indlæser medlemmer…' : memberCountLabel">
        <template #body>
            <UAlert v-if="error" class="mb-4" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <UFormField label="Invitér en bruger" help="Brugeren får adgang med det samme. Kun brugere med en konto i HQ kan findes.">
                <UInput
                    v-model="search"
                    icon="i-lucide-user-plus"
                    placeholder="Søg på navn eller mailadresse"
                    class="w-full"
                    autofocus
                    :loading="searching"
                    role="combobox"
                    :aria-expanded="showResults"
                    aria-controls="board-member-results"
                    @keydown.down.prevent="moveActive(1)"
                    @keydown.up.prevent="moveActive(-1)"
                    @keydown.enter.prevent="inviteActive"
                />
            </UFormField>

            <ul v-if="showResults" id="board-member-results" class="mt-2 divide-y divide-default overflow-hidden rounded-md border border-default" role="listbox">
                <li
                    v-for="(user, index) in results"
                    :key="user.id"
                    class="flex items-center gap-3 px-3 py-2"
                    :class="index === activeResult ? 'bg-primary/10' : ''"
                    role="option"
                    :aria-selected="index === activeResult"
                >
                    <UAvatar :text="initials(user)" size="sm" />
                    <span class="min-w-0 flex-1 text-sm">
                        <span class="block font-medium text-highlighted">{{ fullName(user) }}</span>
                        <span class="block truncate text-xs text-muted">{{ user.email }}</span>
                    </span>
                    <span v-if="isMember(user.id)" class="flex items-center gap-1 text-xs text-muted">
                        <UIcon name="i-lucide-check" class="size-3.5" />
                        Medlem
                    </span>
                    <UButton v-else label="Tilføj" icon="i-lucide-plus" size="xs" variant="soft" :loading="addingId === user.id" :disabled="addingId !== null" @click="invite(user)" />
                </li>
                <li v-if="!searching && results.length === 0" class="px-3 py-2 text-sm text-muted">Ingen brugere matcher »{{ search.trim() }}«.</li>
            </ul>

            <div class="mt-5 mb-2 flex items-center justify-between gap-2">
                <h3 class="text-sm font-semibold text-highlighted">Medlemmer</h3>
                <UInput v-if="members.length > 5" v-model="filter" size="sm" icon="i-lucide-filter" placeholder="Filtrér …" class="w-36" aria-label="Filtrér medlemmer" />
            </div>

            <div v-if="loading" class="flex flex-col gap-2">
                <USkeleton v-for="index in 3" :key="index" class="h-12" />
            </div>
            <ul v-else class="divide-y divide-default rounded-md border border-default">
                <li v-for="member in filteredMembers" :key="member.id">
                    <UAlert
                        v-if="confirmRemoveId === member.id"
                        color="error"
                        variant="subtle"
                        class="rounded-none"
                        orientation="horizontal"
                        :description="removeQuestion(member)"
                        :actions="[
                            { label: 'Annuller', color: 'neutral', variant: 'outline', size: 'xs', onClick: () => { confirmRemoveId = null; } },
                            { label: isMe(member) ? 'Forlad' : 'Fjern', color: 'error', size: 'xs', loading: removing, onClick: () => { void remove(member); } }
                        ]"
                    />
                    <div v-else class="flex items-center gap-3 px-3 py-2">
                        <UAvatar :text="initials(member)" size="sm" />
                        <span class="min-w-0 flex-1 text-sm">
                            <span class="flex items-center gap-1.5 font-medium text-highlighted">
                                <span class="truncate">{{ fullName(member) }}</span>
                                <UBadge v-if="isMe(member)" label="Dig" color="neutral" variant="subtle" size="sm" />
                                <UBadge v-if="newMemberIds.includes(member.id)" label="Ny" color="success" variant="subtle" size="sm" />
                            </span>
                            <span class="block truncate text-xs text-muted">{{ member.email }}</span>
                        </span>
                        <span class="hidden shrink-0 text-xs text-muted sm:inline">{{ cardCountLabel(member) }}</span>
                        <UTooltip v-if="!isMe(member) && members.length > 1" text="Fjern fra tavlen">
                            <UButton icon="i-lucide-user-minus" color="neutral" variant="ghost" size="xs" :aria-label="`Fjern ${fullName(member)} fra tavlen`" @click="confirmRemoveId = member.id" />
                        </UTooltip>
                    </div>
                </li>
                <li v-if="filteredMembers.length === 0" class="px-3 py-2 text-sm text-muted">{{ members.length ? 'Ingen medlemmer matcher filteret.' : 'Tavlen har ingen medlemmer.' }}</li>
            </ul>

            <UAlert class="mt-4" color="info" variant="subtle" icon="i-lucide-info" description="Alle medlemmer kan redigere tavlen og invitere andre. Systemadministratorer og lejrchefer har altid adgang, også uden at være medlemmer." />
        </template>
        <template #footer>
            <div class="flex w-full items-center justify-between gap-2">
                <UTooltip v-if="me" :text="canLeave ? 'Forlad tavlen' : 'Tavlen skal have mindst ét medlem.'">
                    <UButton label="Forlad tavle" icon="i-lucide-log-out" color="error" variant="soft" :disabled="!canLeave" @click="askLeave" />
                </UTooltip>
                <span v-else />
                <UButton label="Luk" color="neutral" variant="outline" @click="open = false" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { useDebounceFn } from "@vueuse/core";
import BoardService from "@/Services/BoardService";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import type { BoardMemberDto } from "@/types";

const props = defineProps<{ boardId: number }>();
const open = defineModel<boolean>("open", { required: true });

const service = new BoardService();
const notify = useNotify();
const meStore = useMeStore();
const router = useRouter();

const members = ref<BoardMemberDto[]>([]);
const newMemberIds = ref<string[]>([]);
const loading = ref(false);
const error = ref("");
const filter = ref("");

const search = ref("");
const results = ref<BoardMemberDto[]>([]);
const searching = ref(false);
const activeResult = ref(0);
const addingId = ref<string | null>(null);
let searchRequest = 0;

const confirmRemoveId = ref<string | null>(null);
const removing = ref(false);

const me = computed(() => members.value.find((member) => member.id === meStore.Me?.id) ?? null);
const canLeave = computed(() => me.value !== null && members.value.length > 1);
const showResults = computed(() => search.value.trim().length >= 2);
const memberCountLabel = computed(() => members.value.length === 1 ? "1 medlem" : `${members.value.length} medlemmer`);

const sortedMembers = computed(() => [...members.value].sort((a, b) =>
    Number(isMe(b)) - Number(isMe(a))
    || Number(newMemberIds.value.includes(b.id)) - Number(newMemberIds.value.includes(a.id))
    || fullName(a).localeCompare(fullName(b), "da")));

const filteredMembers = computed(() => {
    const query = filter.value.trim().toLowerCase();
    if (!query) return sortedMembers.value;

    return sortedMembers.value.filter((member) => fullName(member).toLowerCase().includes(query) || member.email.toLowerCase().includes(query));
});

watch(open, async (isOpen) => {
    if (!isOpen) return;

    search.value = "";
    results.value = [];
    filter.value = "";
    newMemberIds.value = [];
    confirmRemoveId.value = null;
    error.value = "";
    await loadMembers();
});

watch(search, () => {
    activeResult.value = 0;
    if (!showResults.value) {
        results.value = [];
        return;
    }

    searching.value = true;
    void runSearch();
});

const runSearch = useDebounceFn(async () => {
    const query = search.value.trim();
    if (query.length < 2) return;

    const request = ++searchRequest;
    try {
        const found = await service.searchAssignees(query, props.boardId);
        if (request === searchRequest) results.value = found;
    } catch {
        if (request === searchRequest) results.value = [];
    } finally {
        if (request === searchRequest) searching.value = false;
    }
}, 250);

async function loadMembers() {
    loading.value = true;
    try {
        members.value = await service.getMembers(props.boardId);
    } catch {
        error.value = "Medlemmerne kunne ikke indlæses. Luk og prøv igen.";
    } finally {
        loading.value = false;
    }
}

function fullName(user: BoardMemberDto) {
    return `${user.firstName} ${user.lastName}`.trim();
}

function initials(user: BoardMemberDto) {
    return `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toLocaleUpperCase("da-DK") || "?";
}

function isMe(member: BoardMemberDto) {
    return member.id === meStore.Me?.id;
}

function isMember(userId: string) {
    return members.value.some((member) => member.id === userId);
}

function cardCountLabel(member: BoardMemberDto) {
    const count = Number(member.assignedCardCount ?? 0);
    if (count === 0) return "Ingen kort";

    return `Ansvarlig for ${count} kort`;
}

function removeQuestion(member: BoardMemberDto) {
    if (isMe(member)) return "Forlad tavlen? Du mister adgangen, indtil nogen inviterer dig igen.";

    const count = Number(member.assignedCardCount ?? 0);
    const cards = count ? ` Personen er stadig ansvarlig for ${count} kort.` : "";

    return `Fjern ${fullName(member)} fra tavlen?${cards}`;
}

function moveActive(step: number) {
    if (results.value.length === 0) return;

    activeResult.value = Math.max(0, Math.min(results.value.length - 1, activeResult.value + step));
}

function inviteActive() {
    const user = results.value[activeResult.value];
    if (user && !isMember(user.id)) void invite(user);
}

async function invite(user: BoardMemberDto) {
    if (addingId.value || isMember(user.id)) return;

    addingId.value = user.id;
    error.value = "";
    try {
        await service.addMember(props.boardId, user.id);
        members.value = [...members.value, { ...user, assignedCardCount: 0 }];
        newMemberIds.value = [...newMemberIds.value, user.id];
        search.value = "";
        notify.success(`${fullName(user)} har nu adgang til tavlen.`);
    } catch {
        error.value = "Brugeren kunne ikke inviteres. Prøv igen.";
    } finally {
        addingId.value = null;
    }
}

function askLeave() {
    if (!me.value) return;

    filter.value = "";
    confirmRemoveId.value = me.value.id;
}

async function remove(member: BoardMemberDto) {
    if (removing.value) return;

    removing.value = true;
    error.value = "";
    try {
        await service.removeMember(props.boardId, member.id);
        confirmRemoveId.value = null;

        if (isMe(member)) {
            open.value = false;
            notify.success("Du har forladt tavlen.");
            await router.push("/board");
            return;
        }

        members.value = members.value.filter((candidate) => candidate.id !== member.id);
        notify.success(`${fullName(member)} er fjernet fra tavlen.`);
    } catch {
        error.value = isMe(member) ? "Du kunne ikke forlade tavlen. Prøv igen." : "Medlemmet kunne ikke fjernes. Prøv igen.";
    } finally {
        removing.value = false;
    }
}
</script>
