<template>
    <div class="teams">
        <div class="teams-topbar">
            <RouterLink to="/" class="back" title="Tilbage til forsiden">
                <font-awesome-icon icon="arrow-left" />
            </RouterLink>
            <h1 class="title is-5">
                Holdstyring
            </h1>
            <div class="teams-topbar-actions">
                <BButton v-if="isActivityAdmin" type="is-success" icon-left="plus" @click="openCreateModal">
                    Opret hold
                </BButton>
            </div>
        </div>

        <div class="teams-body">
            <BTable :data="TEAMS" striped hoverable :loading="loading">
                <BTableColumn field="name" label="Navn" sortable>
                    <template v-slot:default="{ row }">
                        <div class="is-flex is-align-items-center">
                            <strong>{{ row.name }}</strong>
                            <BTag v-if="row.activities.length > 0" type="is-info" rounded size="is-small" class="ml-2">
                                {{ row.activities.length }} aktiviteter
                            </BTag>
                        </div>
                    </template>
                </BTableColumn>

                <BTableColumn field="members" label="Medlemmer" numeric>
                    <template v-slot:default="{ row }">
                        {{ row.members.length }}
                    </template>
                </BTableColumn>

                <BTableColumn field="role" label="Min rolle" centered>
                    <template v-slot:default="{ row }">
                        <BTag :type="roleTagType(row.role)" rounded size="is-small">
                            {{ translateRole(row.role) }}
                        </BTag>
                    </template>
                </BTableColumn>

                <BTableColumn label="Handlinger" centered>
                    <template v-slot:default="{ row }">
                        <div class="buttons are-small is-centered is-inline-flex">
                            <BButton v-if="canManage(row)" type="is-primary" @click="openManageModal(row)">
                                Administrer
                            </BButton>
                            <BButton v-if="isActivityAdmin" type="is-danger" icon-left="trash" @click="openDeleteModal(row)">
                                Slet
                            </BButton>
                        </div>
                    </template>
                </BTableColumn>
            </BTable>
        </div>
    </div>

    <BModal v-model="isCreateModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">
                    Opret hold
                </p>
            </header>
            <section class="modal-card-body">
                <BField label="Navn">
                    <BInput v-model="newTeamName" placeholder="Holdets navn" @keyup.enter="createTeam" />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <BButton type="is-success" @click="createTeam">
                    Opret hold
                </BButton>
                <BButton type="is-light" @click="isCreateModalOpen = false">
                    Annuller
                </BButton>
            </footer>
        </div>
    </BModal>

    <BModal v-model="isManageModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">
                    Medlemmer i {{ currentTeam?.name }}
                </p>
            </header>
            <section class="modal-card-body">
                <div v-if="currentTeam && currentTeam.members.length > 0">
                    <div v-for="member in currentTeam.members" :key="member.userID" class="field is-grouped is-align-items-center" style="margin-bottom: 0.5rem;">
                        <div class="control is-expanded">
                            <div class="is-flex is-align-items-center">
                                <span>{{ member.name }}</span>
                                <BTag v-if="member.isAdmin" type="is-warning" rounded size="is-small" class="ml-2">
                                    Admin
                                </BTag>
                            </div>
                            <div class="is-size-7 has-text-grey">
                                {{ member.userID }}
                            </div>
                        </div>
                        <div class="control">
                            <BButton :type="member.isAdmin ? 'is-warning is-small' : 'is-light is-small'" @click="toggleAdmin(member)">
                                {{ member.isAdmin ? 'Fjern admin' : 'Gør til admin' }}
                            </BButton>
                        </div>
                        <div class="control">
                            <BButton type="is-danger is-small" @click="removeMember(member)">
                                Fjern
                            </BButton>
                        </div>
                    </div>
                </div>
                <div v-else class="is-italic has-text-grey">
                    Ingen medlemmer
                </div>

                <div class="add-member">
                    <BField label="Tilføj medlem">
                        <BAutocomplete
                            v-model="searchText"
                            :data="filteredUsers"
                            field="name"
                            placeholder="Søg efter bruger..."
                            :loading="usersLoading"
                            open-on-focus
                            clear-on-select
                            @select="onUserSelect"
                        >
                            <template v-slot:default="{ option }">
                                <div>{{ option.name }}</div>
                                <div class="is-size-7 has-text-grey">
                                    {{ option.email || option.userName }}
                                </div>
                            </template>
                        </BAutocomplete>
                    </BField>
                    <BButton type="is-success" icon-left="user-plus" :disabled="!selectedUser" @click="addMember">
                        Tilføj medlem
                    </BButton>
                </div>
            </section>
            <footer class="modal-card-foot">
                <BButton type="is-light" @click="isManageModalOpen = false">
                    Luk
                </BButton>
            </footer>
        </div>
    </BModal>

    <BModal v-model="isDeleteModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">
                    Slet hold
                </p>
            </header>
            <section class="modal-card-body">
                Er du sikker på, at du vil slette holdet "{{ currentTeam?.name }}"? Alle medlemmer og aktiviteter fjernes.
            </section>
            <footer class="modal-card-foot">
                <BButton type="is-danger" @click="deleteTeam">
                    Slet hold
                </BButton>
                <BButton type="is-light" @click="isDeleteModalOpen = false">
                    Annuller
                </BButton>
            </footer>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { computed, onMounted, ref } from 'vue';
import { BAutocomplete, BButton, BField, BInput, BModal, BTable, BTableColumn, BTag, useToast } from 'buefy';
import { storeToRefs } from 'pinia';
import { useMeStore } from '@/Stores/MeStore';
import { useTeamStore } from '@/Stores/TeamStore';
import UserService from '@/Services/UserService';

const Toast = useToast();
const meStore = useMeStore();
const teamStore = useTeamStore();
const userService = new UserService();
const { ME } = storeToRefs(meStore);
const { TEAMS } = storeToRefs(teamStore);

const loading = ref(false);

const isCreateModalOpen = ref(false);
const newTeamName = ref('');

const isManageModalOpen = ref(false);
const selectedTeam = ref<DSTeam | null>(null);

const users = ref<DSUser[]>([]);
const usersLoading = ref(false);
const searchText = ref('');
const selectedUser = ref<DSUser | null>(null);

const isDeleteModalOpen = ref(false);

const isActivityAdmin = computed(() => ME.value?.isActivityAdmin ?? false);

const currentTeam = computed(() =>
    TEAMS.value.find(t => t.id === selectedTeam.value?.id) ?? selectedTeam.value
);

const filteredUsers = computed(() => {
    const memberIds = new Set((currentTeam.value?.members ?? []).map(m => m.userID));
    const query = searchText.value.trim().toLowerCase();

    return users.value
        .filter(u => !memberIds.has(u.id))
        .filter(u =>
            !query ||
            u.name?.toLowerCase().includes(query) ||
            u.userName?.toLowerCase().includes(query) ||
            u.email?.toLowerCase().includes(query)
        )
        .slice(0, 100);
});

const canManage = (team: DSTeam) => team.role === 'Admin';

onMounted(async () => {
    loading.value = true;
    await meStore.GET_ME();
    await teamStore.GET_TEAMS();
    loading.value = false;
});

const loadUsers = async () => {
    usersLoading.value = true;
    users.value = await userService.getUsers();
    usersLoading.value = false;
};

const onUserSelect = (user: DSUser) => {
    selectedUser.value = user;
};

const openCreateModal = () => {
    newTeamName.value = '';
    isCreateModalOpen.value = true;
};

const createTeam = async () => {
    if (!newTeamName.value.trim()) {
        Toast.open({
            message: 'Udfyld venligst holdets navn',
            type: 'is-warning'
        });
        return;
    }

    const success = await teamStore.CREATE_TEAM(newTeamName.value.trim());
    if (success) {
        Toast.open({
            message: 'Holdet er oprettet',
            type: 'is-success'
        });
        isCreateModalOpen.value = false;
    } else {
        Toast.open({
            message: 'Der skete en fejl ved oprettelse af holdet',
            type: 'is-danger'
        });
    }
};

const openManageModal = (team: DSTeam) => {
    selectedTeam.value = team;
    searchText.value = '';
    selectedUser.value = null;
    isManageModalOpen.value = true;
    loadUsers();
};

const addMember = async () => {
    if (!selectedTeam.value || !selectedUser.value) return;

    const success = await teamStore.ADD_MEMBER(selectedTeam.value.id, selectedUser.value.id);
    if (success) {
        Toast.open({
            message: `${selectedUser.value.name} er tilføjet til holdet`,
            type: 'is-success'
        });
        searchText.value = '';
        selectedUser.value = null;
    } else {
        Toast.open({
            message: 'Der skete en fejl. Brugeren er måske allerede medlem.',
            type: 'is-danger'
        });
    }
};

const toggleAdmin = async (member: DSTeamMember) => {
    if (!selectedTeam.value) return;

    const success = await teamStore.SET_MEMBER_ADMIN(selectedTeam.value.id, member.userID, !member.isAdmin);
    if (success) {
        Toast.open({
            message: 'Admin-status opdateret',
            type: 'is-success'
        });
    } else {
        Toast.open({
            message: 'Der skete en fejl ved opdatering af admin-status',
            type: 'is-danger'
        });
    }
};

const removeMember = async (member: DSTeamMember) => {
    if (!selectedTeam.value) return;

    const success = await teamStore.REMOVE_MEMBER(selectedTeam.value.id, member.userID);
    if (success) {
        Toast.open({
            message: 'Medlemmet er fjernet',
            type: 'is-success'
        });
    } else {
        Toast.open({
            message: 'Der skete en fejl ved fjernelse af medlemmet',
            type: 'is-danger'
        });
    }
};

const openDeleteModal = (team: DSTeam) => {
    selectedTeam.value = team;
    isDeleteModalOpen.value = true;
};

const deleteTeam = async () => {
    if (!selectedTeam.value) return;

    const success = await teamStore.DELETE_TEAM(selectedTeam.value.id);
    if (success) {
        Toast.open({
            message: 'Holdet er slettet',
            type: 'is-success'
        });
        isDeleteModalOpen.value = false;
    } else {
        Toast.open({
            message: 'Der skete en fejl ved sletning af holdet',
            type: 'is-danger'
        });
    }
};

const translateRole = (role: TeamRole) => {
    switch (role) {
        case 'Admin': return 'Admin';
        case 'Member': return 'Medlem';
        default: return 'Ingen';
    }
};

const roleTagType = (role: TeamRole) => {
    switch (role) {
        case 'Admin': return 'is-warning';
        case 'Member': return 'is-info';
        default: return 'is-light';
    }
};
</script>
<style lang="scss">
.teams {
    height: 100%;
    border-radius: 10px;
    background-color: white;
    margin: 1rem;

    &-topbar {
        height: 3rem;
        border-bottom: 1px solid rgba(0, 0, 0, 0.1);
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;

        .back {
            height: 100%;
            display: flex;
            align-items: center;
            padding: 0 1rem;
            color: inherit;

            &:hover {
                background-color: rgba(0, 0, 0, 0.05);
            }
        }

        .title {
            margin: 0;
            flex: 1;
        }

        &-actions {
            padding: 0 1rem;
        }
    }

    &-body {
        padding: 1rem;
        overflow: auto;
    }
}

.add-member {
    margin-top: 1rem;
    border-top: 1px solid rgba(0, 0, 0, 0.1);
    padding-top: 1rem;
}
</style>
