<template>
    <template v-if="isActivityAdmin">
        <div class="activity-sections">
            <div class="activity-admin-header">
                <div>
                    <h2 class="title is-4 mb-0">Aktivitetsoversigt</h2>
                    <p class="subtitle is-6">Alle aktiviteter på tværs af teams</p>
                </div>
                <BButton type="is-primary" icon-left="users-gear" @click="router.push('/activity/teams')">
                    Teamstyring
                </BButton>
            </div>
            <ActivityOverview />
        </div>
    </template>
    <template v-else>
        <ManagementWrapper :class="{ 'has-selection': selectedTeam != null }">
            <Sidebar>
                <SidebarHeader>
                    <BInput
                        ref="searchInput"
                        v-model="searchQuery"
                        icon="magnifying-glass"
                        placeholder="Søg efter team..."
                    />
                </SidebarHeader>
                <SidebarContent>
                    <TeamSidebarBox
                        v-for="team in filteredTeams"
                        :key="team.id"
                        :team="team"
                        :selected="selectedTeam?.id === team.id"
                        @click="toggleTeamSelection(team)"
                    />
                    <div v-if="filteredTeams.length === 0" class="empty-state">
                        <i class="fas fa-users"></i>
                        <span v-if="Teams.length === 0" class="empty-state-title">Du har ingen teams endnu</span>
                        <span v-else class="empty-state-title">Ingen teams matcher søgningen</span>
                    </div>
                </SidebarContent>
                <SidebarFooter>
                    <TeamCreateTeam />
                </SidebarFooter>
            </Sidebar>
            <Workspace :filled="workspaceFilled">
                <button class="mobile-back" @click="clearSelection">
                    <font-awesome-icon icon="arrow-left" />
                    <span>Tilbage</span>
                </button>
                <template v-if="selectedTeam">
                    <section class="hero is-link">
                        <div class="hero-body is-flex is-justify-content-space-between is-align-items-center">
                            <div>
                                <p class="title is-3">{{ selectedTeam.name }}</p>
                                <p class="subtitle is-6">
                                    {{ selectedTeam.members.length }} medlemmer · {{ selectedTeam.activities.length }} aktiviteter
                                </p>
                            </div>
                        </div>
                    </section>
                    <WorkspaceContent>
                        <TeamMembers />
                        <TeamActivities />
                    </WorkspaceContent>
                </template>
                <div v-else class="workspace-empty">
                    <i class="fas fa-users"></i>
                    <p class="workspace-empty-title">Du har ingen teams endnu</p>
                    <p class="workspace-empty-subtitle">Opret dit første team for at komme i gang med aktiviteter</p>
                    <TeamCreateTeam />
                </div>
            </Workspace>
        </ManagementWrapper>
    </template>
</template>
<script lang="ts" setup>
import { BButton, BInput } from 'buefy';
import ActivityOverview from '@/Components/Activity/ActivityOverview.vue';
import TeamSidebarBox from '@/Components/Activity/TeamSidebarBox.vue';
import TeamCreateTeam from '@/Components/Activity/TeamCreateTeam.vue';
import TeamMembers from '@/Components/Activity/TeamMembers.vue';
import TeamActivities from '@/Components/Activity/TeamActivities.vue';
import { useMeStore } from '@/Stores/MeStore';
import { useActivityStore } from '@/Stores/ActivityStore';
import ManagementWrapper from '@/Components/ManagementWrapper.vue';
import Sidebar from '@/Components/Sidebar/Sidebar.vue';
import SidebarHeader from '@/Components/Sidebar/SidebarHeader.vue';
import SidebarContent from '@/Components/Sidebar/SidebarContent.vue';
import SidebarFooter from '@/Components/Sidebar/SidebarFooter.vue';
import Workspace from '@/Components/Workspace/Workspace.vue';
import WorkspaceContent from '@/Components/Workspace/WorkspaceContent.vue';
import { storeToRefs } from 'pinia';
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import type { ActivityTeamDto } from '@/types';

const meStore = useMeStore();
const { ME } = storeToRefs(meStore);
const router = useRouter();

const activityStore = useActivityStore();
const { Teams, SelectedTeamId } = storeToRefs(activityStore);

const isActivityAdmin = computed(() => ME.value.appRoles.includes('ActivityAdmin'));

const selectedTeam = computed<ActivityTeamDto | null>(
    () => Teams.value.find((team) => team.id === SelectedTeamId.value) ?? null
);

const workspaceFilled = computed(() => selectedTeam.value != null || Teams.value.length === 0);

const searchQuery = ref('');
const filteredTeams = computed(() => {
    if (!searchQuery.value.trim()) return Teams.value;
    const query = searchQuery.value.toLowerCase();
    return Teams.value.filter((t) => t.name.toLowerCase().includes(query));
});

const clearSelection = () => {
    SelectedTeamId.value = null;
};

const toggleTeamSelection = (team: ActivityTeamDto) => {
    if (selectedTeam.value?.id === team.id) {
        clearSelection();
    } else {
        SelectedTeamId.value = team.id;
    }
};

const searchInput = ref<any>(null);
const handleGlobalKeyDown = (event: KeyboardEvent) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();

        if (searchInput.value) {
            if (typeof searchInput.value.focus === 'function') {
                searchInput.value.focus();
            } else if (searchInput.value.$el?.querySelector('input')) {
                searchInput.value.$el.querySelector('input').focus();
            }
        }
    }
};

onMounted(() => {
    window.addEventListener('keydown', handleGlobalKeyDown);
});

onUnmounted(() => {
    window.removeEventListener('keydown', handleGlobalKeyDown);
});
</script>
<style lang="scss">
.activity-sections {
    padding: 1rem 2rem;
}

.activity-admin-header {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}
</style>
