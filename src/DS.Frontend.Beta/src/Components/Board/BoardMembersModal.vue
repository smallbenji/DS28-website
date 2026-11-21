<template>
    <UModal v-model:open="open" title="Medlemmer og invitation" description="Alle medlemmer kan redigere tavlen og invitere andre. Systemadministratorer og lejrchefer har altid adgang.">
        <template #body>
            <UAlert v-if="error" class="mb-4" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <div v-if="loading" class="flex flex-col gap-2">
                <USkeleton v-for="index in 3" :key="index" class="h-8" />
            </div>
            <ul v-else class="mb-5 flex flex-col gap-2">
                <li v-for="member in members" :key="member.id" class="flex items-center gap-2 text-sm">
                    <UAvatar :text="`${member.firstName?.[0] ?? ''}${member.lastName?.[0] ?? ''}`" size="sm" />
                    <span class="min-w-0">
                        <span class="block font-medium text-highlighted">{{ member.firstName }} {{ member.lastName }}</span>
                        <span class="block truncate text-xs text-muted">{{ member.email }}</span>
                    </span>
                </li>
            </ul>

            <form @submit.prevent="searchUsers">
                <UFormField label="Inviter en eksisterende bruger" help="Søg efter navn eller mailadresse. Invitationen giver adgang med det samme.">
                    <UFieldGroup class="w-full">
                        <UInput v-model="search" placeholder="Navn eller mailadresse" icon="i-lucide-search" class="w-full" />
                        <UButton type="submit" label="Søg" :loading="searching" :disabled="!search.trim()" />
                    </UFieldGroup>
                </UFormField>
            </form>

            <ul v-if="results.length" class="mt-3 flex flex-col divide-y divide-default">
                <li v-for="user in results" :key="user.id" class="flex items-center justify-between gap-2 py-2">
                    <span class="min-w-0 text-sm">
                        <span class="block font-medium">{{ user.firstName }} {{ user.lastName }}</span>
                        <span class="block truncate text-xs text-muted">{{ user.email }}</span>
                    </span>
                    <UButton size="sm" :label="isMember(user.id) ? 'Medlem' : 'Inviter'" :disabled="adding || isMember(user.id)" @click="invite(user)" />
                </li>
            </ul>
            <p v-else-if="searched && !searching" class="mt-3 text-sm text-muted">Ingen brugere fundet.</p>
        </template>
        <template #footer>
            <div class="flex w-full justify-end">
                <UButton color="neutral" variant="outline" label="Luk" @click="open = false" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import BoardService from "@/Services/BoardService";
import { useNotify } from "@/lib/notify";
import type { BoardMemberDto } from "@/types";

const props = defineProps<{ boardId: number }>();
const open = defineModel<boolean>("open", { required: true });

const service = new BoardService();
const notify = useNotify();

const members = ref<BoardMemberDto[]>([]);
const results = ref<BoardMemberDto[]>([]);
const search = ref("");
const loading = ref(false);
const searching = ref(false);
const searched = ref(false);
const adding = ref(false);
const error = ref("");

watch(open, async (isOpen) => {
    if (!isOpen) return;

    search.value = "";
    results.value = [];
    members.value = [];
    searched.value = false;
    error.value = "";
    loading.value = true;
    try {
        members.value = await service.getMembers(props.boardId);
    } catch {
        error.value = "Medlemmerne kunne ikke indlæses. Luk og prøv igen.";
    } finally {
        loading.value = false;
    }
});

function isMember(userId: string) {
    return members.value.some((member) => member.id === userId);
}

async function searchUsers() {
    if (searching.value || !search.value.trim()) return;

    searching.value = true;
    try {
        results.value = await service.searchAssignees(search.value.trim(), props.boardId);
        searched.value = true;
    } finally {
        searching.value = false;
    }
}

async function invite(user: BoardMemberDto) {
    if (adding.value || isMember(user.id)) return;

    adding.value = true;
    error.value = "";
    try {
        await service.addMember(props.boardId, user.id);
        members.value.push(user);
        notify.success("Brugeren har nu adgang til tavlen.");
    } catch {
        error.value = "Brugeren kunne ikke inviteres. Prøv igen.";
    } finally {
        adding.value = false;
    }
}
</script>
