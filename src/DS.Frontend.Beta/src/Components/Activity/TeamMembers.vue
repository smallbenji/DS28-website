<template>
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
        <template #header>
            <div class="flex items-center justify-between gap-2">
                <h3 class="font-semibold text-highlighted">Medlemmer <span class="font-normal text-muted">({{ team.members.length }})</span></h3>
                <UButton v-if="canManageMembers" size="sm" icon="i-lucide-mail" label="Inviter bruger" @click="openInvite" />
            </div>
        </template>

        <div class="border-b border-default p-4 sm:px-6">
            <UFormField label="Søg efter bruger">
                <UInput v-model="search" icon="i-lucide-search" placeholder="Navn eller email…" :loading="searching" class="w-full" />
            </UFormField>
            <ul v-if="SearchResults.length" class="mt-2 flex flex-col divide-y divide-default rounded-md border border-default">
                <li v-for="user in SearchResults" :key="user.id" class="flex items-center justify-between gap-2 px-3 py-2">
                    <span class="min-w-0 text-sm">
                        <span class="block truncate font-medium">{{ user.firstName }} {{ user.lastName }}</span>
                        <span class="block truncate text-xs text-muted">{{ user.email }}</span>
                    </span>
                    <UButton size="xs" icon="i-lucide-plus" label="Tilføj" :disabled="busy" @click="addMember(user)" />
                </li>
            </ul>
            <p v-else-if="search.trim() && !searching" class="mt-2 text-xs text-muted">Ingen brugere fundet.</p>
        </div>

        <p v-if="team.members.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Der er ingen medlemmer i teamet endnu.</p>
        <ul v-else class="divide-y divide-default">
            <li v-for="member in team.members" :key="member.userId" class="flex items-center gap-3 px-4 py-2 sm:px-6">
                <UAvatar :text="initials(member.name)" size="sm" />
                <span class="min-w-0 flex-1">
                    <span class="flex items-center gap-2 text-sm font-medium text-highlighted">
                        <span class="truncate">{{ member.name }}</span>
                        <UBadge v-if="member.isAdmin" label="Admin" color="neutral" variant="subtle" size="sm" />
                    </span>
                    <span class="block truncate text-xs text-muted">{{ member.email }}</span>
                </span>
                <UButton
                    v-if="canManageMembers && member.userId !== meStore.Me?.id"
                    color="error"
                    variant="ghost"
                    size="xs"
                    icon="i-lucide-trash-2"
                    :aria-label="`Fjern ${member.name} fra teamet`"
                    :disabled="busy"
                    @click="requestRemove(member)"
                />
            </li>
        </ul>

        <UModal v-model:open="inviteOpen" title="Inviter bruger til team">
            <template #body>
                <template v-if="inviteLink">
                    <p class="text-sm">Send linket til <strong>{{ inviteLink.email }}</strong>. Det kan kun bruges én gang.</p>
                    <UFieldGroup class="mt-3 w-full">
                        <UInput :model-value="inviteLink.link" readonly class="w-full" aria-label="Invitationslink" />
                        <UButton icon="i-lucide-copy" label="Kopiér" @click="copyInviteLink" />
                    </UFieldGroup>
                </template>
                <form v-else id="activity-team-invite-form" class="flex flex-col gap-4" @submit.prevent="sendInvitation">
                    <UFormField label="Email" required>
                        <UInput v-model="inviteEmail" type="email" placeholder="eksempel@mail.dk" class="w-full" autofocus />
                    </UFormField>
                    <UCheckbox v-model="inviteIsAdmin" label="Admin" description="Kan administrere teamets medlemmer og aktiviteter." />
                </form>
            </template>
            <template #footer>
                <div class="flex w-full justify-end gap-2">
                    <UButton color="neutral" variant="outline" label="Luk" @click="inviteOpen = false" />
                    <UButton v-if="!inviteLink" type="submit" form="activity-team-invite-form" label="Opret invitationslink" :loading="inviteLoading" :disabled="!inviteEmail.trim()" />
                </div>
            </template>
        </UModal>

        <ConfirmModal
            v-model:open="removeOpen"
            title="Fjern medlem"
            :description="`Fjern ${memberToRemove?.name} fra teamet?`"
            confirm-label="Fjern medlem"
            color="error"
            :loading="busy"
            @confirm="confirmRemove"
        />
    </UCard>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { storeToRefs } from "pinia";
import { useActivityStore } from "@/Stores/ActivityStore";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { ActivityTeamDto, ActivityTeamInviteLinkDto, ActivityTeamMemberDto, UserDto } from "@/types";

const props = defineProps<{ team: ActivityTeamDto }>();

const activityStore = useActivityStore();
const meStore = useMeStore();
const { SearchResults } = storeToRefs(activityStore);
const notify = useNotify();

const search = ref("");
const searching = ref(false);
const busy = ref(false);
const inviteOpen = ref(false);
const inviteEmail = ref("");
const inviteIsAdmin = ref(false);
const inviteLoading = ref(false);
const inviteLink = ref<ActivityTeamInviteLinkDto | null>(null);
const removeOpen = ref(false);
const memberToRemove = ref<ActivityTeamMemberDto | null>(null);
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const canManageMembers = computed(() => {
    if (meStore.Me?.appRoles.includes("ActivityAdmin")) return true;

    return props.team.members.some((member) => member.userId === meStore.Me?.id && member.isAdmin);
});

function initials(name: string) {
    return name.split(" ").filter(Boolean).slice(0, 2).map((part) => part[0]).join("").toLocaleUpperCase("da-DK") || "?";
}

watch(() => props.team.id, () => {
    search.value = "";
    SearchResults.value = [];
}, { immediate: true });

watch(search, (value) => {
    if (searchTimeout) clearTimeout(searchTimeout);

    if (!value.trim()) {
        SearchResults.value = [];
        searching.value = false;
        return;
    }

    searching.value = true;
    searchTimeout = setTimeout(async () => {
        await activityStore.SEARCH_USERS(value.trim(), props.team.id);
        searching.value = false;
    }, 300);
});

onBeforeUnmount(() => {
    if (searchTimeout) clearTimeout(searchTimeout);
});

async function addMember(user: UserDto) {
    busy.value = true;
    try {
        const ok = await activityStore.ADD_MEMBER(props.team.id, user.id);
        notify.result(ok, `${user.firstName} ${user.lastName} er tilføjet til teamet.`, "Brugeren kunne ikke tilføjes.");
        if (ok) {
            search.value = "";
            SearchResults.value = [];
        }
    } finally {
        busy.value = false;
    }
}

function requestRemove(member: ActivityTeamMemberDto) {
    memberToRemove.value = member;
    removeOpen.value = true;
}

async function confirmRemove() {
    if (!memberToRemove.value) return;

    busy.value = true;
    try {
        const ok = await activityStore.REMOVE_MEMBER(props.team.id, memberToRemove.value.userId);
        notify.result(ok, "Brugeren er fjernet fra teamet.", "Brugeren kunne ikke fjernes.");
        if (ok) {
            removeOpen.value = false;
            memberToRemove.value = null;
        }
    } finally {
        busy.value = false;
    }
}

function openInvite() {
    inviteEmail.value = "";
    inviteIsAdmin.value = false;
    inviteLink.value = null;
    inviteOpen.value = true;
}

async function sendInvitation() {
    if (!inviteEmail.value.trim()) return;

    inviteLoading.value = true;
    try {
        const data = await activityStore.INVITE_USER(props.team.id, inviteEmail.value.trim(), inviteIsAdmin.value);
        if (data) inviteLink.value = data;
        else notify.error("Invitationslinket kunne ikke oprettes.");
    } finally {
        inviteLoading.value = false;
    }
}

async function copyInviteLink() {
    if (!inviteLink.value) return;

    try {
        await navigator.clipboard.writeText(inviteLink.value.link);
        notify.success("Linket er kopieret.");
    } catch {
        notify.error("Linket kunne ikke kopieres.");
    }
}
</script>
