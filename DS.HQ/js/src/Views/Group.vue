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
                    <p class="subtitle is-6">Gruppe-ID: {{ selectedGroup?.id }}</p>
                </div>
            </section>
            <WorkspaceContent>
                <div class="columns is-desktop">
                    <div class="column">
                        <GroupMetadata v-if="selectedGroup" :selected-group="selectedGroup" />
                    </div>
                    <div class="column"></div>
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
import { useGroupStore } from '@/Stores/GroupStore';
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
import GroupSidebarBox from '@/Components/Group/GroupSidebarBox.vue';
import GroupMetadata from '@/Components/Group/GroupMetadata.vue';
import GroupCreateGroup from '@/Components/Group/GroupCreateGroup.vue';

const Toast = useToast();

const groupStore = useGroupStore();
const { Groups: groups } = storeToRefs(groupStore);

const selectedGroup = ref<DSGroup | null>(null);
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

const toggleGroupSelection = (clickedGroup: DSGroup) => {
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
</script>
