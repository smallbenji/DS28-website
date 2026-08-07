<template>
    <Back icon="user-pen" title="Brugeradministration" />
    <ManagementWrapper>
        <Sidebar>
            <SidebarHeader>
                <BInput
                    ref="searchInput"
                    v-model="searchQuery"
                    icon="magnifying-glass"
                    placeholder="Søg efter navn eller gruppe"
                    />
            </SidebarHeader>
            <SidebarContent>
                <UserSidebarBox
                    v-for="user in filteredUsers"
                    :user="user"
                    :selected="selectedUser?.id === user.id"
                    @click="toggleUserSelection(user)"
                />
            </SidebarContent>
            <SidebarFooter>
                <UserCreateUser />
                <UserInviteUser />
            </SidebarFooter>
        </Sidebar>
        <Workspace :filled="selectedUser != null">
            <section class="hero is-link">
                <div class="hero-body is-flex is-justify-content-space-between is-align-items-center">
                    <div>
                        <p class="title is-3">{{ selectedUser?.firstName }} {{ selectedUser?.lastName }}</p>
                        <p class="subtitle is-6">{{ selectedUser?.id }}</p>
                    </div>
                    <div>
                        <div class="buttons">
                            <UserLockButton v-if="selectedUser" :user="selectedUser" @changed="handleUserChanged" />
                            <UserResetPasswordButton v-if="selectedUser" :user="selectedUser" />
                            <UserDeleteButton v-if="selectedUser" :user="selectedUser" @deleted="handleUserDeleted" />
                        </div>
                    </div>
                </div>
            </section>
            <WorkspaceContent>
                <div class="columns is-desktop">
                    <div class="column">
                        <UserMetadata v-if="selectedUser" :selected-user="selectedUser" />
                    </div>
                    <div class="column">
                        <UserRoles class="column is-half" v-if="selectedUser" :selected-user="selectedUser" />
                    </div>
                </div>
            </WorkspaceContent>
            <WorkspaceFooter>
                <BButton type="is-success" @click="saveUser">Gem</BButton>
            </WorkspaceFooter>
        </Workspace>
    </ManagementWrapper>
</template>
<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useUserStore } from '@/Stores/UserStore';
import { storeToRefs } from 'pinia';
import { BButton, BInput, useToast } from 'buefy';
import UserRoles from '@/Components/User/UserRoles.vue';
import UserCreateUser from '@/Components/User/UserCreateUser.vue';
import UserInviteUser from '@/Components/User/UserInviteUser.vue';
import Back from '@/Components/Back.vue';
import UserMetadata from '@/Components/User/UserMetadata.vue';
import UserResetPasswordButton from '@/Components/User/UserResetPasswordButton.vue';
import Sidebar from '@/Components/Sidebar/Sidebar.vue';
import SidebarHeader from '@/Components/Sidebar/SidebarHeader.vue';
import SidebarContent from '@/Components/Sidebar/SidebarContent.vue';
import SidebarFooter from '@/Components/Sidebar/SidebarFooter.vue';
import UserSidebarBox from '@/Components/User/UserSidebarBox.vue';
import UserDeleteButton from '@/Components/User/UserDeleteButton.vue';
import UserLockButton from '@/Components/User/UserLockButton.vue';
import ManagementWrapper from '@/Components/ManagementWrapper.vue';
import Workspace from '@/Components/Workspace/Workspace.vue';
import WorkspaceContent from '@/Components/Workspace/WorkspaceContent.vue';
import WorkspaceFooter from '@/Components/Workspace/WorkspaceFooter.vue';
import type { UserDto } from '@/types';

const Toast = useToast();

const userStore = useUserStore();
const { Users: users } = storeToRefs(userStore);

const selectedUser = ref<UserDto | null>(null);

const searchQuery = ref('');

const filteredUsers = computed(() => {
  if (!searchQuery.value.trim()) return users.value;

  const query = searchQuery.value.toLowerCase();
  return users.value.filter(u => {
    const fullName = `${u.firstName || ''} ${u.lastName || ''}`.toLowerCase();
    const groupName = u.group?.name ? u.group.name.toLowerCase() : '';
    return fullName.includes(query) || groupName.includes(query);
  });
});

const toggleUserSelection = (clickedUser: UserDto) => {
        if (selectedUser.value?.id === clickedUser.id) {
        selectedUser.value = null;
    } else {
        // Will maybe be used at a later time?
        // const rawUser = toRaw(clickedUser);
        // selectedUser.value = structuredClone(rawUser);
        selectedUser.value = clickedUser;
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
}

onMounted(() => {
    window.addEventListener('keydown', handleGlobalKeyDown);
});

onUnmounted(() => {
    window.removeEventListener('keydown', handleGlobalKeyDown)
});

const saveUser = async () => {
    const result = await userStore.UPDATE_USER(selectedUser.value as UserDto);
    if (result) {
        Toast.open({
            message: "Brugere er blevet opdateret!",
            type: "is-success"
        });
    } else {
        Toast.open({
            message: "Der skete en fejl",
            type: "is-danger"
        });
    }
}

const handleUserDeleted = () => {
    selectedUser.value = null;
}

const handleUserChanged = (userId: string) => {
    selectedUser.value = users.value.find(user => user.id === userId) ?? null;
}
</script>