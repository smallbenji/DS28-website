<template>
    <div class="back">
        <a href="/">
            <font-awesome-icon icon="arrow-left" />
        </a>
        <h1 class="title is-5">
            <font-awesome-icon icon="user" /> Brugeradministration
        </h1>
    </div>
    <div class="management-wrapper">
        <aside class="sidebar-list">
            <div class="sidebar-header">
                <div class="search-container">
                    <BInput v-model="searchQuery" />
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
                        <!-- <span v-for="role in user.roles" :key="role" class="sidebar-user-role-pills-pill">
                            {{ role.name }}
                        </span> -->
                        <span v-for="role in user.roles" :key="role" class="tag is-dark">
                            {{ role.name }}
                        </span>
                    </div>
                    <div class="sidebar-user-group-info" v-if="user.groupNumber">
                        {{ user.groupNumber }}
                    </div>
                </div>
            </div>
            <div class="sidebar-footer">
                <BButton type="is-primary" @click="createNewUser">
                    + Tilføj ny bruger
                </BButton>
                <BButton type="is-secondary">
                    Inviter bruger
                </BButton>
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
            </div>
        </main>
    </div>
</template>
<script lang="ts" setup>
import { ref, computed } from 'vue';
import { useUserStore } from '@/Stores/UserStore';
import { storeToRefs } from 'pinia';
import { BButton, BInput } from 'buefy';


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
    selectedUser.value = clickedUser;
  }
};

const createNewUser = () => {
  // lige pt. ingen funktionalitet
};
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

        &.filled {
            border: 1px solid rgba(0, 0, 0, 0.1);
            background-color: #fff;
        }

        &.dashed {
            border: 3px dashed rgba(0, 0, 0, 0.1);
        }
    }
}

.back {
    background-color: #fff;
    border-radius: 10px;
    margin: 1rem;
    display: flex;
    gap: 0.25rem;
    padding: 1rem;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
}
</style>