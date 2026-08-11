<template>
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
                    <span v-if="Teams.length === 0" class="empty-state-title">Der er endnu ingen teams</span>
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
                </WorkspaceContent>
            </template>
            <div v-else class="workspace-empty">
                <i class="fas fa-users"></i>
                <p class="workspace-empty-title">Der er endnu ingen teams</p>
                <p class="workspace-empty-subtitle">Opret dit første team for at komme i gang</p>
                <TeamCreateTeam />
            </div>
        </Workspace>
    </ManagementWrapper>
</template>
<script lang="ts" setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useActivityStore } from '@/Stores/ActivityStore';
import { storeToRefs } from 'pinia';
import { BInput } from 'buefy';
import ManagementWrapper from '@/Components/ManagementWrapper.vue';
import Sidebar from '@/Components/Sidebar/Sidebar.vue';
import SidebarHeader from '@/Components/Sidebar/SidebarHeader.vue';
import SidebarContent from '@/Components/Sidebar/SidebarContent.vue';
import SidebarFooter from '@/Components/Sidebar/SidebarFooter.vue';
import TeamSidebarBox from '@/Components/Activity/TeamSidebarBox.vue';
import TeamCreateTeam from '@/Components/Activity/TeamCreateTeam.vue';
import TeamMembers from '@/Components/Activity/TeamMembers.vue';
import Workspace from '@/Components/Workspace/Workspace.vue';
import WorkspaceContent from '@/Components/Workspace/WorkspaceContent.vue';
import type { ActivityTeamDto } from '@/types';

const route = useRoute();
const router = useRouter();

const activityStore = useActivityStore();
const { Teams, SelectedTeamId } = storeToRefs(activityStore);

const selectedTeam = computed<ActivityTeamDto | null>(() => {
    const id = route.params.id;
    if (!id) return null;
    const idStr = Array.isArray(id) ? id[0] : id;
    return Teams.value.find((t) => String(t.id) === idStr) ?? null;
});

const workspaceFilled = computed(() => selectedTeam.value != null || Teams.value.length === 0);

watch(selectedTeam, (team) => {
    SelectedTeamId.value = team ? team.id : null;
}, { immediate: true });

const searchQuery = ref('');

const filteredTeams = computed(() => {
    if (!searchQuery.value.trim()) return Teams.value;
    const query = searchQuery.value.toLowerCase();
    return Teams.value.filter((t) => t.name.toLowerCase().includes(query));
});

const clearSelection = () => {
    router.replace('/activity/teams');
};

const toggleTeamSelection = (team: ActivityTeamDto) => {
    if (selectedTeam.value?.id === team.id) {
        clearSelection();
    } else {
        router.replace(`/activity/teams/${team.id}`);
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
