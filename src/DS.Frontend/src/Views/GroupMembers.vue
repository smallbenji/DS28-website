<template>
    <UDashboardPanel id="group-members">
        <template #header>
            <UDashboardNavbar title="Gruppens brugere">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/group" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til gruppen" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto flex w-full max-w-5xl flex-col gap-4">
                <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" :actions="group ? undefined : [{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

                <div v-if="loading" class="grid gap-6 lg:grid-cols-2">
                    <USkeleton class="h-64" />
                    <USkeleton class="h-64" />
                </div>

                <template v-else-if="group">
                    <h1 class="text-xl font-semibold text-highlighted">{{ group.name }}</h1>

                    <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
                        <UCard class="self-start" :ui="{ body: 'p-0 sm:p-0' }">
                            <template #header>
                                <h2 class="font-semibold text-highlighted">Medlemmer <span class="font-normal text-muted">({{ group.users.length }})</span></h2>
                                <p class="text-sm text-muted">Alle medlemmer kan invitere og fjerne andre medlemmer.</p>
                            </template>
                            <ul class="divide-y divide-default">
                                <li v-for="user in group.users" :key="user.id" class="flex items-center gap-3 px-4 py-3 sm:px-6">
                                    <UserAvatar :user="user" />
                                    <div class="min-w-0 flex-1">
                                        <p class="truncate text-sm font-medium text-highlighted">{{ user.firstName }} {{ user.lastName }}</p>
                                        <p class="truncate text-xs text-muted">{{ user.email }}</p>
                                    </div>
                                    <UBadge v-if="user.id === meStore.Me?.id" label="Dig" color="neutral" variant="subtle" />
                                    <UButton v-else color="error" variant="ghost" size="sm" label="Fjern" :aria-label="`Fjern ${user.firstName} ${user.lastName} fra gruppen`" :disabled="busy" @click="confirmRemove(user)" />
                                </li>
                            </ul>
                        </UCard>

                        <div class="flex flex-col gap-6">
                            <UCard>
                                <template #header>
                                    <h2 class="font-semibold text-highlighted">Inviter til gruppen</h2>
                                    <p class="text-sm text-muted">Den inviterede får automatisk en email med et personligt invitationslink.</p>
                                </template>
                                <form class="flex flex-col gap-3" @submit.prevent="invite">
                                    <UFormField label="Email" required>
                                        <UInput v-model="email" type="email" placeholder="eksempel@mail.dk" :disabled="busy" class="w-full" />
                                    </UFormField>
                                    <UButton type="submit" icon="i-lucide-mail" label="Send invitation" :loading="busy" :disabled="busy || !email.trim()" class="self-start" />
                                </form>
                                <UFormField v-if="link" label="Invitationslink" help="Linket kan også deles manuelt." class="mt-4">
                                    <UFieldGroup class="w-full">
                                        <UInput :model-value="link" readonly class="w-full" aria-label="Invitationslink" />
                                        <UButton icon="i-lucide-copy" aria-label="Kopiér link" @click="copyLink" />
                                    </UFieldGroup>
                                </UFormField>
                            </UCard>

                            <UCard :ui="{ body: 'p-0 sm:p-0' }">
                                <template #header>
                                    <h2 class="font-semibold text-highlighted">Afventende invitationer <span class="font-normal text-muted">({{ invitations.length }})</span></h2>
                                </template>
                                <p v-if="invitations.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen afventende invitationer.</p>
                                <ul v-else class="divide-y divide-default">
                                    <li v-for="invitation in invitations" :key="invitation.invitationId" class="flex items-center gap-2 px-4 py-2 sm:px-6">
                                        <span class="min-w-0 flex-1 truncate text-sm">{{ invitation.email }}</span>
                                        <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-link" :aria-label="`Vis link til ${invitation.email}`" :disabled="busy" @click="showLink(invitation.invitationId)" />
                                        <UButton color="error" variant="ghost" size="xs" icon="i-lucide-x" :aria-label="`Annuller invitationen til ${invitation.email}`" :disabled="busy" @click="revoke(invitation.invitationId)" />
                                    </li>
                                </ul>
                            </UCard>
                        </div>
                    </div>
                </template>
            </div>

            <ConfirmModal
                v-model:open="removeOpen"
                title="Fjern fra gruppen"
                :description="`Fjern ${removing?.firstName} ${removing?.lastName} fra gruppen? Brugerens konto bevares, men adgangen til gruppen ophører.`"
                confirm-label="Fjern medlem"
                color="error"
                :loading="busy"
                @confirm="remove"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import GroupMembersService, { type GroupMemberInvitationDto } from "@/Services/GroupMembersService";
import { useMeStore } from "@/Stores/MeStore";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import type { GroupDto, UserDto } from "@/types";

const service = new GroupMembersService();
const meStore = useMeStore();
const notify = useNotify();

const group = ref<GroupDto | null>(null);
const invitations = ref<GroupMemberInvitationDto[]>([]);
const email = ref("");
const link = ref("");
const error = ref("");
const busy = ref(false);
const loading = ref(true);
const removeOpen = ref(false);
const removing = ref<UserDto | null>(null);

function fail(cause: unknown) {
    error.value = getErrorMessage(cause, "Handlingen kunne ikke gennemføres. Kontrollér din adgang til gruppen, og prøv igen.");
}

async function refresh() {
    const [members, pending] = await Promise.all([service.getGroup(), service.getInvitations()]);
    group.value = members;
    invitations.value = pending;
}

async function load() {
    loading.value = true;
    error.value = "";
    try {
        await refresh();
    } catch (cause) {
        fail(cause);
    } finally {
        loading.value = false;
    }
}

function showLink(invitationId: string) {
    link.value = `${window.location.origin}/group-invitation/${invitationId}`;
}

async function copyLink() {
    try {
        await navigator.clipboard.writeText(link.value);
        notify.success("Linket er kopieret.");
    } catch {
        notify.error("Linket kunne ikke kopieres.");
    }
}

async function invite() {
    if (busy.value || !email.value.trim()) return;

    busy.value = true;
    error.value = "";
    link.value = "";
    try {
        const sentTo = email.value.trim();
        const data = await service.invite(sentTo);
        link.value = new URL(data.path, window.location.origin).href;
        email.value = "";
        await refresh();
        notify.success(`Invitationen er sendt til ${sentTo}.`);
    } catch (cause) {
        fail(cause);
    } finally {
        busy.value = false;
    }
}

function confirmRemove(user: UserDto) {
    removing.value = user;
    removeOpen.value = true;
}

async function remove() {
    if (!removing.value || busy.value) return;

    busy.value = true;
    error.value = "";
    try {
        await service.removeMember(removing.value.id);
        removeOpen.value = false;
        await refresh();
        notify.success("Medlemmet er fjernet fra gruppen.");
    } catch (cause) {
        fail(cause);
        notify.error(error.value);
    } finally {
        busy.value = false;
    }
}

async function revoke(invitationId: string) {
    if (busy.value) return;

    busy.value = true;
    error.value = "";
    try {
        await service.revokeInvitation(invitationId);
        link.value = "";
        await refresh();
        notify.success("Invitationen er annulleret.");
    } catch (cause) {
        fail(cause);
    } finally {
        busy.value = false;
    }
}

onMounted(load);
</script>
