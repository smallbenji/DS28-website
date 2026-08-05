<template>
    <Back icon="users" title="Gruppeadministration" />
    <ManagementWrapper>
        <Sidebar>
            <SidebarHeader>
                <BInput
                    ref="searchInput"
                    v-model="searchQuery"
                    icon="magnifying-glass"
                    placeholder="Søg på navn eller ID"
                    />
            </SidebarHeader>
            <SidebarContent>
                <GroupSidebarBox
                    v-for="group in filteredGroups"
                    :key="group.id"
                    :group="group"
                    :selected="selectedGroup?.id === group.id"
                    @click="toggleGroupSelection(group)"
                />
            </SidebarContent>
            <SidebarFooter>
                <GroupCreateGroup />
            </SidebarFooter>
        </Sidebar>
        <Workspace :filled="selectedGroup != null">
            <section class="hero is-link">
                <div class="hero-body">
                    <p class="title is-3">{{ selectedGroup?.name }}</p>
                    <p class="subtitle is-6">Gruppe-ID: {{ selectedGroup?.id }} · Distrikt: {{ selectedGroup?.district }}</p>
                </div>
            </section>
            <WorkspaceContent>
                <div class="columns is-desktop">
                    <div class="column">
                        <GroupMetadata v-if="selectedGroup" :selected-group="selectedGroup" />
                        <GroupScouts
                            v-if="selectedGroup"
                            :selected-group="selectedGroup"
                            @scout-created="handleScoutCreated"
                            @scout-deleted="handleScoutDeleted"
                            @patrol-assigned="handlePatrolAssigned"
                            @patrol-leader-toggled="handlePatrolLeaderToggled"
                        />
                    </div>
                    <div class="column">
                        <GroupUsers v-if="selectedGroup" :selected-group="selectedGroup" />
                        <GroupPatrols 
                            v-if="selectedGroup" 
                            :selected-group="selectedGroup" 
                            @patrol-created="handlePatrolCreated" 
                            @patrol-deleted="handlePatrolDeleted"
                        />
                    </div>
                </div>
            </WorkspaceContent>
            <WorkspaceFooter>
                <BButton type="is-success" @click="saveGroup">Gem</BButton>
            </WorkspaceFooter>
        </Workspace>
    </ManagementWrapper>
</template>
<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useGroupsStore } from '@/Stores/GroupsStore';
import { storeToRefs } from 'pinia';
import { BButton, BInput, useToast } from 'buefy';
import Back from '@/Components/Back.vue';
import ManagementWrapper from '@/Components/ManagementWrapper.vue';
import Sidebar from '@/Components/Sidebar/Sidebar.vue';
import SidebarHeader from '@/Components/Sidebar/SidebarHeader.vue';
import SidebarContent from '@/Components/Sidebar/SidebarContent.vue';
import SidebarFooter from '@/Components/Sidebar/SidebarFooter.vue';
import Workspace from '@/Components/Workspace/Workspace.vue';
import WorkspaceContent from '@/Components/Workspace/WorkspaceContent.vue';
import WorkspaceFooter from '@/Components/Workspace/WorkspaceFooter.vue';
import GroupSidebarBox from '@/Components/Groups/GroupsSidebarBox.vue';
import GroupMetadata from '@/Components/Groups/GroupsMetadata.vue';
import GroupCreateGroup from '@/Components/Groups/GroupsCreateGroup.vue';
import GroupUsers from '@/Components/Groups/GroupsUsers.vue';
import GroupPatrols from '@/Components/Groups/GroupsPatrols.vue';
import GroupScouts from '@/Components/Groups/GroupsScouts.vue';
import type { GroupDto, PatrolDto, ScoutDto } from '@/types';

const Toast = useToast();

const groupStore = useGroupsStore();
const { Groups: groups } = storeToRefs(groupStore);

const selectedGroup = ref<GroupDto | null>(null);
const searchQuery = ref('');

const filteredGroups = computed(() => {
    const allGroups = groups.value?.groups ?? [];

    if (!searchQuery.value.trim()) return allGroups;

    const query = searchQuery.value.toLowerCase();
    return allGroups.filter(g =>
        g.name.toLowerCase().includes(query) ||
        g.id.toString().toLowerCase().includes(query)
    );
});

const toggleGroupSelection = (clickedGroup: GroupDto) => {
    if (selectedGroup.value?.id === clickedGroup.id) {
        selectedGroup.value = null;
    } else {
        selectedGroup.value = JSON.parse(JSON.stringify(clickedGroup));
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

const saveGroup = async () => {
    if (!selectedGroup.value) return;

    const result = await groupStore.UPDATE_GROUP(selectedGroup.value);
    if (result) {
        Toast.open({
            message: 'Gruppen er blevet opdateret',
            type: 'is-success'
        });
    } else {
        Toast.open({
            message: 'Der skete en fejl',
            type: 'is-danger'
        });
    }
};

const handlePatrolCreated = (patrol: PatrolDto) => {
    if (selectedGroup.value) {
        if (!selectedGroup.value.patrols) {
            selectedGroup.value.patrols = [];
        }
        selectedGroup.value.patrols.push(patrol);
        selectedGroup.value = { ...selectedGroup.value };
    }
};

const handlePatrolDeleted = (patrolId: number) => {
    if (selectedGroup.value) {
        selectedGroup.value.patrols = (selectedGroup.value.patrols ?? []).filter(p => p.id !== patrolId);
        if (selectedGroup.value.scouts) {
            for (const scout of selectedGroup.value.scouts) {
                if (scout.memberships) {
                    scout.memberships = scout.memberships.filter(m => m.patrolId !== patrolId);
                }
            }
        }
        selectedGroup.value = { ...selectedGroup.value };
    }
};

const handleScoutCreated = (scout: ScoutDto) => {
    if (selectedGroup.value) {
        if (!selectedGroup.value.scouts) {
            selectedGroup.value.scouts = [];
        }
        selectedGroup.value.scouts.push(scout);
        selectedGroup.value = { ...selectedGroup.value };
    }
};

const handleScoutDeleted = (scoutId: number) => {
    if (selectedGroup.value) {
        selectedGroup.value.scouts = (selectedGroup.value.scouts ?? []).filter(s => s.id !== scoutId);
        selectedGroup.value = { ...selectedGroup.value };
    }
};

const handlePatrolAssigned = (scoutId: number, patrolId: number, action: 'add' | 'remove') => {
    if (selectedGroup.value && selectedGroup.value.scouts) {
        const scout = selectedGroup.value.scouts.find(s => s.id === scoutId);
        if (scout) {
            if (!scout.memberships) {
                scout.memberships = [];
            }
            if (action === 'add') {
                if (!scout.memberships.some(m => m.patrolId === patrolId)) {
                    scout.memberships.push({
                        id: 0,
                        scoutId,
                        patrolId,
                        joinedDate: new Date().toISOString(),
                        isPatrolLeader: false
                    });
                }
            } else if (action === 'remove') {
                scout.memberships = scout.memberships.filter(m => m.patrolId !== patrolId);
            }
            selectedGroup.value = { ...selectedGroup.value };
        }
    }
};

const handlePatrolLeaderToggled = (scoutId: number, patrolId: number) => {
    if (selectedGroup.value && selectedGroup.value.scouts) {
        const scout = selectedGroup.value.scouts.find(s => s.id === scoutId);
        if (scout && scout.memberships) {
            const membership = scout.memberships.find(m => m.patrolId === patrolId);
            if (membership) {
                membership.isPatrolLeader = !membership.isPatrolLeader;
                selectedGroup.value = { ...selectedGroup.value };
            }
        }
    }
};
</script>
