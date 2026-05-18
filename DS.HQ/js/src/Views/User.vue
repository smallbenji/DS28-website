<template>
    <Back icon="user" title="Brugeradministration" />
    <div class="management-wrapper">
        <aside class="sidebar-list">
            <div class="sidebar-header">
                <div class="search-container">
                    <BInput
                        v-model="searchQuery"
                        placeholder="Søg efter navn eller gruppenummer"
                        />
                </div>
            </div>
            <div class="sidebar-content compact-scroll">
                <div
                    v-for="user in filteredUsers"
                    class="sidebar-user"
                    :class="{ active: selectedUser?.user.id === user.user.id}"
                    @click="toggleUserSelection(user)"
                >
                    <div class="sidebar-user-name">
                        <span>{{ user.user.firstName }} {{ user.user.lastName }}</span>
                    </div>
                    <div class="sidebar-user-role-pills">
                        <span v-for="role in user.roles" :key="role.id" class="tag is-dark">
                            {{ role.name }}
                        </span>
                    </div>
                    <div class="sidebar-user-group-info" v-if="user.group?.name">
                        {{ user.group.name }}
                    </div>
                </div>
            </div>
            <div class="sidebar-footer">
                <UserCreateUser />
                <UserInviteUser />
            </div>
        </aside>
        <main class="workspace" :class="selectedUser ? 'filled' : 'dashed'">
            <div class="workspace-box" v-if="selectedUser">
                <section class="hero is-link">
                    <div class="hero-body">
                        <p class="title is-3">{{ selectedUser.user.firstName }} {{ selectedUser.user.lastName }}</p>
                        <p class="subtitle is-6">{{ selectedUser.user.id }}</p>
                    </div>
                </section>
                <div class="workspace-box-grid">
                    <div class="columns is-desktop">
                        <div class="column">
                            <UserMetadata :selected-user="selectedUser" />
                        </div>
                        <div class="column">
                            <UserRoles class="column is-half" :selected-user="selectedUser" />
                        </div>
                    </div>
                </div>
                <div class="flex"></div>
                <div class="workspace-box-footer">
                    <BButton type="is-success" @click="saveUser">Gem</BButton>
                </div>
            </div>
        </main>
    </div>

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
html body {
    background-color: #d4d4d4;
}

*, *::before, *::after {
    box-sizing: border-box;
}

.management-wrapper {
    display: flex;
    height: 100%;
    width: 100%;
    padding: 1rem;
    padding-top: 0;
    gap: 1rem;
    min-height: 0;
    scrollbar-width: thin;

    .sidebar {

        &-list {
            width: 380px;
            display: flex;
            flex-direction: column;
            background-color: #fff;
            height: 100%;
            flex-shrink: 0;
            border-radius: 10px;
            overflow: hidden;
        }

        &-header {
            padding: 1rem;
            border-bottom: 1px solid rgba(0, 0, 0, 0.1);
            box-shadow: 5px 0 5px 0 rgba(0, 0, 0, 0.1);
        }

        &-footer {
            padding: 1rem;
            display: flex;
            justify-content: space-between;
            border-top: 1px solid rgba(0, 0, 0, 0.1);
            box-shadow: -5px 0 5px 0 rgba(0, 0, 0, 0.1);
        }

        &-content {
            flex: 1;
            padding: 1rem;
            overflow-y: auto;
            min-height: 0;
        }

        &-user {
            border: 1px solid rgba(0, 0, 0, 0.1);
            padding: 1rem 1.2rem;
            border-radius: 10px;
            margin-bottom: 1rem;
            cursor: pointer;

            &-name {
                font-weight: 600;
                font-size: 1rem;
                margin-bottom: 0.4rem;
            }

            &-role-pills {
                display: flex;
                gap: 0.4rem;
                margin-bottom: 0.5rem;

                &-pill {
                    background-color: rgb(206, 206, 206);
                    color: #606060;
                    padding: 0.15rem 0.6rem;
                    border-radius: 20px;
                    font-size: 0.75rem;
                    font-weight: 500;
                }
            }

            &-group-info {
                font-size: 0.8rem;
            }

            &.active {
                background: #ececec;
                border: 1px solid #696969;
                box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);
            }
        }
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
}
</style>