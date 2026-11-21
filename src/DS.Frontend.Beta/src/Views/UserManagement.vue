<template>
    <UDashboardPanel id="users-list" :default-size="22" :min-size="18" :max-size="32" resizable>
        <template #header>
            <UDashboardNavbar title="Brugerstyring">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/admin" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til admin" />
                </template>
                <template #trailing>
                    <UBadge :label="String(Users.length)" color="neutral" variant="subtle" />
                </template>
            </UDashboardNavbar>
            <UDashboardToolbar>
                <UInput v-model="search" icon="i-lucide-search" placeholder="Søg efter navn eller gruppe" class="w-full" variant="soft" />
            </UDashboardToolbar>
        </template>

        <template #body>
            <div v-if="loading" class="flex flex-col gap-2">
                <USkeleton v-for="index in 8" :key="index" class="h-16" />
            </div>

            <UAlert v-else-if="loadFailed" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Brugerne kunne ikke hentes" description="Prøv at genindlæse siden." />

            <p v-else-if="filteredUsers.length === 0" class="py-6 text-center text-sm text-muted">Ingen brugere matcher søgningen.</p>

            <ul v-else class="-mx-2 flex flex-col gap-1">
                <li v-for="user in filteredUsers" :key="user.id">
                    <button
                        type="button"
                        class="flex w-full items-center gap-3 rounded-md px-2 py-2 text-left transition-colors hover:bg-elevated/60 focus-visible:outline-2 focus-visible:outline-primary"
                        :class="{ 'bg-elevated': selectedUser?.id === user.id }"
                        :aria-current="selectedUser?.id === user.id ? 'true' : undefined"
                        @click="select(user)"
                    >
                        <UserAvatar :user="user" />
                        <span class="min-w-0 flex-1">
                            <span class="flex items-center gap-2">
                                <span class="truncate text-sm font-medium text-highlighted">{{ user.firstName }} {{ user.lastName }}</span>
                                <UBadge v-if="isLocked(user)" label="Låst" color="warning" variant="subtle" size="sm" />
                            </span>
                            <span class="block truncate text-xs text-muted">{{ user.group?.name || 'Ingen gruppe' }}</span>
                            <span v-if="user.roles.length" class="mt-1 flex flex-wrap gap-1">
                                <UBadge v-for="role in user.roles" :key="role" :label="role" color="neutral" variant="outline" size="sm" />
                            </span>
                        </span>
                    </button>
                </li>
            </ul>
        </template>

        <template #footer>
            <div class="flex gap-2 border-t border-default p-4">
                <UButton icon="i-lucide-plus" label="Ny bruger" class="flex-1 justify-center" @click="createOpen = true" />
                <UButton icon="i-lucide-mail" label="Inviter" color="neutral" variant="outline" class="flex-1 justify-center" @click="inviteOpen = true" />
            </div>
        </template>
    </UDashboardPanel>

    <UDashboardPanel v-if="!isMobile" id="user-detail">
        <template #header>
            <UDashboardNavbar :title="selectedUser ? `${selectedUser.firstName} ${selectedUser.lastName}` : 'Bruger'">
                <template v-if="selectedUser" #right>
                    <UButton color="neutral" variant="ghost" icon="i-lucide-x" aria-label="Luk bruger" @click="clearSelection" />
                </template>
            </UDashboardNavbar>
        </template>
        <template #body>
            <UserDetail v-if="selectedUser" :user="selectedUser" @deleted="clearSelection" />
            <UEmpty v-else icon="i-lucide-user-round" title="Vælg en bruger" description="Vælg en bruger på listen for at se og redigere den." class="h-full" />
        </template>
    </UDashboardPanel>

    <USlideover v-else v-model:open="detailOpen" :title="selectedUser ? `${selectedUser.firstName} ${selectedUser.lastName}` : 'Bruger'">
        <template #body>
            <UserDetail v-if="selectedUser" :user="selectedUser" @deleted="clearSelection" />
        </template>
    </USlideover>

    <UserCreateModal v-model:open="createOpen" />
    <UserInviteModal v-model:open="inviteOpen" />
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { storeToRefs } from "pinia";
import { useUserStore } from "@/Stores/UserStore";
import { useGroupsStore } from "@/Stores/GroupsStore";
import { useIsMobile } from "@/lib/useIsMobile";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import UserDetail from "@/Components/User/UserDetail.vue";
import UserCreateModal from "@/Components/User/UserCreateModal.vue";
import UserInviteModal from "@/Components/User/UserInviteModal.vue";
import type { UserDto } from "@/types";

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();
const groupsStore = useGroupsStore();
const { Users } = storeToRefs(userStore);
const isMobile = useIsMobile();

const loading = ref(true);
const loadFailed = ref(false);
const search = ref("");
const createOpen = ref(false);
const inviteOpen = ref(false);

onMounted(async () => {
    try {
        await Promise.all([
            userStore.GET_USERS(),
            userStore.GET_GROUPS(),
            userStore.GET_ASSIGNABLE_GROUPS(),
            groupsStore.GET_GROUPS()
        ]);
    } catch {
        loadFailed.value = true;
    } finally {
        loading.value = false;
    }
});

const selectedUser = computed<UserDto | null>(() => {
    const id = route.params.id;
    if (!id) return null;

    const idValue = Array.isArray(id) ? id[0] : id;

    return Users.value.find((user) => user.id === idValue) ?? null;
});

const detailOpen = computed({
    get: () => selectedUser.value != null,
    set: (open: boolean) => {
        if (!open) clearSelection();
    }
});

const filteredUsers = computed(() => {
    const query = search.value.trim().toLocaleLowerCase("da-DK");
    if (!query) return Users.value;

    return Users.value.filter((user) => `${user.firstName} ${user.lastName} ${user.group?.name ?? ""}`
        .toLocaleLowerCase("da-DK")
        .includes(query));
});

function isLocked(user: UserDto): boolean {
    return !!user.lockoutEnd && new Date(user.lockoutEnd).getTime() > Date.now();
}

function select(user: UserDto) {
    if (selectedUser.value?.id === user.id) {
        clearSelection();
        return;
    }

    router.replace(`/admin/user/${user.id}`);
}

function clearSelection() {
    router.replace("/admin/user");
}

</script>
