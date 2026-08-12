<template>
    <nav class="panel team-members">
        <div class="panel-heading">
            <span>Medlemmer</span>
            <div class="flex"></div>
            <BButton
                v-if="selectedTeam"
                type="is-primary"
                size="is-small"
                icon-left="envelope"
                @click="openInvite"
            >
                Inviter bruger
            </BButton>
        </div>
        <div class="panel-body">
            <template v-if="SelectedTeamId">
                <BField label="Søg efter bruger">
                    <BInput
                        v-model="search"
                        icon="magnifying-glass"
                        placeholder="Navn eller email..."
                        :loading="searching"
                    />
                </BField>

                <div v-if="SearchResults.length > 0" class="member-search-results">
                    <div v-for="user in SearchResults" :key="user.id" class="member-result">
                        <div class="member-info">
                            <div class="member-name">
                                {{ user.firstName }} {{ user.lastName }}
                            </div>
                            <div class="member-email">
                                {{ user.email }}
                            </div>
                        </div>
                        <BButton type="is-primary" size="is-small" icon-left="plus" @click="addMember(user)">
                            Tilføj
                        </BButton>
                    </div>
                </div>
                <div v-else-if="search && !searching" class="empty-state">
                    <i class="fas fa-user-plus"></i>
                    <span>Ingen brugere fundet</span>
                </div>

                <div v-if="selectedTeam && selectedTeam.members.length > 0" class="member-list">
                    <div v-for="member in selectedTeam.members" :key="member.userId" class="member-result">
                        <div class="member-info">
                            <div class="member-name">
                                {{ member.name }}
                                <span v-if="member.isAdmin" class="tag is-dark is-small">Admin</span>
                            </div>
                            <div class="member-email">
                                {{ member.email }}
                            </div>
                        </div>
                        <BButton
                            v-if="canManageMembers && member.userId !== meStore.ME.id"
                            type="is-light"
                            size="is-small"
                            icon-left="trash"
                            @click="requestRemoveMember(member)"
                        >
                            Fjern
                        </BButton>
                    </div>
                </div>
                <div v-else-if="selectedTeam" class="empty-state">
                    <i class="fas fa-users"></i>
                    <span>Der er ingen medlemmer i teamet endnu</span>
                </div>
            </template>
        </div>
    </nav>
    <BModal v-model="inviteOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Inviter bruger til team</p>
            </header>
            <section class="modal-card-body">
                <template v-if="inviteLink">
                    <p class="mb-2">
                        Send nedenstående link til
                        <strong>{{ inviteLink.email }}</strong>. Linket er gyldigt ét brug, indtil det er brugt.
                    </p>
                    <div class="field has-addons">
                        <div class="control is-expanded">
                            <input class="input" :value="inviteLink.link" readonly />
                        </div>
                        <div class="control">
                            <BButton type="is-primary" icon-left="copy" @click="copyInviteLink">
                                Kopiér
                            </BButton>
                        </div>
                    </div>
                </template>
                <template v-else>
                    <BField label="Email">
                        <BInput v-model="inviteEmail" type="email" placeholder="eksempel@mail.dk" @keyup.enter="sendInvitation" />
                    </BField>
                    <BCheckbox v-model="inviteIsAdmin">
                        Admin
                    </BCheckbox>
                </template>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton v-if="inviteLink" type="is-primary" icon-left="copy" @click="copyInviteLink">
                        Kopiér
                    </BButton>
                    <BButton v-else type="is-primary" :loading="inviteLoading" @click="sendInvitation">
                        Opret invitationslink
                    </BButton>
                    <BButton @click="closeInvite">Luk</BButton>
                </div>
            </footer>
        </div>
    </BModal>
    <BModal v-model="removeConfirmOpen" has-modal-card>
        <div class="modal-card">
            <div class="modal-card-head">
                <p class="modal-card-title">Fjern medlem</p>
            </div>
            <div class="modal-card-body">
                <p>Er du sikker på, at du vil fjerne "{{ memberToRemove?.name }}" fra teamet?</p>
            </div>
            <div class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-danger" :loading="removing" @click="confirmRemoveMember">
                        Fjern medlem
                    </BButton>
                    <BButton type="is-primary" @click="removeConfirmOpen = false">
                        Annuller
                    </BButton>
                </div>
            </div>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { useMeStore } from '@/Stores/MeStore';
import { BButton, BCheckbox, BField, BInput, BModal, useToast } from 'buefy';
import { storeToRefs } from 'pinia';
import { computed, ref, watch } from 'vue';
import type { ActivityTeamDto, ActivityTeamInviteLinkDto, ActivityTeamMemberDto, UserDto } from '@/types';

const Toast = useToast();
const activityStore = useActivityStore();
const meStore = useMeStore();
const { Teams, SearchResults, SelectedTeamId } = storeToRefs(activityStore);

const search = ref('');
const inviteOpen = ref(false);
const inviteEmail = ref('');
const inviteIsAdmin = ref(false);
const inviteLoading = ref(false);
const inviteLink = ref<ActivityTeamInviteLinkDto | null>(null);
const searching = ref(false);
const removeConfirmOpen = ref(false);
const removing = ref(false);
const memberToRemove = ref<ActivityTeamMemberDto | null>(null);
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const selectedTeam = computed<ActivityTeamDto | null>(
    () => Teams.value.find((team) => team.id === SelectedTeamId.value) ?? null
);

const canManageMembers = computed(() => {
    if (meStore.ME.appRoles.includes('ActivityAdmin')) return true;
    return selectedTeam.value?.members.some(
        (member) => member.userId === meStore.ME.id && member.isAdmin
    ) ?? false;
});

watch(SelectedTeamId, () => {
    search.value = '';
    SearchResults.value = [];
});

watch(search, (value) => {
    if (searchTimeout) {
        clearTimeout(searchTimeout);
    }

    if (!value.trim() || !SelectedTeamId.value) {
        return;
    }

    searching.value = true;
    searchTimeout = setTimeout(async () => {
        await activityStore.SEARCH_USERS(value.trim(), SelectedTeamId.value!);
        searching.value = false;
    }, 300);
});

const addMember = async (user: UserDto) => {
    if (!SelectedTeamId.value) return;

    const success = await activityStore.ADD_MEMBER(SelectedTeamId.value, user.id);
    if (success) {
        Toast.open({
            message: `${user.firstName} ${user.lastName} er tilføjet til teamet`,
            type: 'is-success'
        });
        search.value = '';
        SearchResults.value = [];
    } else {
        Toast.open({
            message: 'Der skete en fejl ved tilføjelse af brugeren',
            type: 'is-danger'
        });
    }
};

const requestRemoveMember = (member: ActivityTeamMemberDto) => {
    memberToRemove.value = member;
    removeConfirmOpen.value = true;
};

const confirmRemoveMember = async () => {
    if (!memberToRemove.value || !SelectedTeamId.value) return;

    const userId = memberToRemove.value.userId;
    removing.value = true;
    const success = await activityStore.REMOVE_MEMBER(SelectedTeamId.value, userId);
    removing.value = false;

    if (success) {
        Toast.open({
            message: 'Brugeren er fjernet fra teamet',
            type: 'is-success'
        });
        removeConfirmOpen.value = false;
        memberToRemove.value = null;
    } else {
        Toast.open({
            message: 'Der skete en fejl ved fjernelse af brugeren',
            type: 'is-danger'
        });
    }
};

const openInvite = () => {
    inviteEmail.value = '';
    inviteIsAdmin.value = false;
    inviteLink.value = null;
    inviteOpen.value = true;
};

const closeInvite = () => {
    inviteOpen.value = false;
    inviteLink.value = null;
};

const sendInvitation = async () => {
    if (!SelectedTeamId.value) return;

    if (!inviteEmail.value) {
        Toast.open({
            message: 'Indtast venligst en email',
            type: 'is-warning'
        });
        return;
    }

    inviteLoading.value = true;
    try {
        const data = await activityStore.INVITE_USER(SelectedTeamId.value, inviteEmail.value, inviteIsAdmin.value);
        if (data) {
            inviteLink.value = data;
        } else {
            Toast.open({
                message: 'Der skete en fejl ved oprettelse af invitationslinket',
                type: 'is-danger'
            });
        }
    } finally {
        inviteLoading.value = false;
    }
};

const copyInviteLink = async () => {
    if (!inviteLink.value) return;

    try {
        await navigator.clipboard.writeText(inviteLink.value.link);
        Toast.open({
            message: 'Linket er kopieret!',
            type: 'is-success'
        });
    } catch {
        Toast.open({
            message: 'Kunne ikke kopiere linket',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
.team-members {
    margin-bottom: 1rem;

    .panel-heading {
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }

    .panel-body {
        padding: 1rem;
    }

    .member-search-results {
        border-top: 1px solid rgba(0, 0, 0, 0.08);
        margin-top: 1rem;
        padding-top: 0.5rem;
    }

    .member-list {
        border-top: 1px solid rgba(0, 0, 0, 0.08);
        margin-top: 1rem;
        padding-top: 0.5rem;
    }

    .member-result {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 0.6rem 0.5rem;
        border-bottom: 1px solid rgba(0, 0, 0, 0.05);
        border-radius: 8px;
        transition: background-color 0.15s ease;

        &:hover {
            background-color: #f7f8fa;
        }

        &:last-child {
            border-bottom: none;
        }
    }

    .member-info {
        min-width: 0;
    }

    .member-name {
        font-weight: 600;
        font-size: 0.9rem;
        display: flex;
        align-items: center;
        gap: 0.4rem;
    }

    .member-email {
        font-size: 0.8rem;
        color: #6b7280;
        word-break: break-word;
    }
}
</style>
