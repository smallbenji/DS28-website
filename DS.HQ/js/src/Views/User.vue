<template>
    <Back icon="user" title="Brugeradministration" />
    <ManagementWrapper>
        <Sidebar>
            <SidebarHeader>
                <BInput
                    v-model="searchQuery"
                    placeholder="Søg efter navn eller gruppenummer"
                    />
            </SidebarHeader>
            <SidebarContent>
                <UserSidebarBox
                    v-for="user in filteredUsers"
                    :user="user"
                    :selected="selectedUser?.user.id === user.user.id"
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
                <div class="hero-body">
                    <p class="title is-3">{{ selectedUser?.user.firstName }} {{ selectedUser?.user.lastName }}</p>
                    <p class="subtitle is-6">{{ selectedUser?.user.id }}</p>
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
import { ref, computed } from 'vue';
import { useUserStore } from '@/Stores/UserStore';
import { storeToRefs } from 'pinia';
import { BButton, BInput, useToast } from 'buefy';
import UserRoles from '@/Components/User/UserRoles.vue';
import UserCreateUser from '@/Components/User/UserCreateUser.vue';
import UserInviteUser from '@/Components/User/UserInviteUser.vue';
import Back from '@/Components/Back.vue';
import UserMetadata from '@/Components/User/UserMetadata.vue';
import Sidebar from '@/Components/Sidebar/Sidebar.vue';
import SidebarHeader from '@/Components/Sidebar/SidebarHeader.vue';
import SidebarContent from '@/Components/Sidebar/SidebarContent.vue';
import SidebarFooter from '@/Components/Sidebar/SidebarFooter.vue';
import UserSidebarBox from '@/Components/User/UserSidebarBox.vue';
import ManagementWrapper from '@/Components/ManagementWrapper.vue';
import Workspace from '@/Components/Workspace/Workspace.vue';
import WorkspaceContent from '@/Components/Workspace/WorkspaceContent.vue';
import WorkspaceFooter from '@/Components/Workspace/WorkspaceFooter.vue';

const Toast = useToast();

const userStore = useUserStore();
const { Users: users } = storeToRefs(userStore);

const selectedUser = ref<DSUser | null>(null);

const searchQuery = ref('');

const filteredUsers = computed(() => {
  if (!searchQuery.value.trim()) return users.value;

  const query = searchQuery.value.toLowerCase();
  return users.value.filter(u => {
    const fullName = `${u.user.firstName || ''} ${u.user.lastName || ''}`.toLowerCase();
    const groupNum = u.groupNumber ? u.groupNumber.toLowerCase() : '';
    return fullName.includes(query) || groupNum.includes(query);
  });
});

const toggleUserSelection = (clickedUser: DSUser) => {
  if (selectedUser.value?.user.id === clickedUser.user.id) {
    selectedUser.value = null;
  } else {
    // Will maybe be used at a later time?
    // const rawUser = toRaw(clickedUser);
    // selectedUser.value = structuredClone(rawUser);
    selectedUser.value = clickedUser;
  }
};

const saveUser = async () => {
    const result = await userStore.UPDATE_USER(selectedUser.value as DSUser);
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
</script>
<style lang="scss">
*, *::before, *::after {
    box-sizing: border-box;
}

.workspace {
    height: 100%;
    flex: 1;
    border-radius: 10px;
    overflow: hidden;
    &-box {
        display: flex;
        flex-direction: column;
        height: 100%;
        overflow-x: auto;
        &-footer {
            display: flex;
            border-top: 1px solid rgba(0, 0, 0, 0.1);
            padding: 0.5rem 1rem;
            flex-direction: row-reverse;
        }
        &-grid {
            padding: 1rem;
        }
    }

    &.filled {
        // border: 1px solid rgba(0, 0, 0, 0.1);
        background-color: #fff;
    }

    &.dashed {
        border: 3px dashed rgba(0, 0, 0, 0.1);
    }

    .panel-heading {
        padding: 15px 25px;
        min-height: 60px;
        align-items: center;
        display: flex;
    }

    .group-body {
        padding: 1rem;
    }

    .role-line {
        display: flex;
    }
    .flex {
        flex: 1;
    }
}

</style>