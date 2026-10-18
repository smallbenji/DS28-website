<template>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Medlemmer og invitation</p>
                <button type="button" class="delete" aria-label="Luk" @click="open = false" />
            </header>
            <section class="modal-card-body">
                <p class="mb-4">Alle medlemmer kan redigere tavlen og invitere andre. Systemadministratorer og lejrchefer har altid adgang.</p>
                <p v-if="error" class="notification is-danger is-light" role="alert">{{ error }}</p>
                <p v-if="loading" role="status">Indlæser medlemmer…</p>
                <ul v-else class="mb-5">
                    <li v-for="member in members" :key="member.id">{{ member.firstName }} {{ member.lastName }} · {{ member.email }}</li>
                </ul>
                <form @submit.prevent="searchUsers">
                    <BField label="Inviter en eksisterende bruger" message="Søg efter navn eller mailadresse. Invitationen giver adgang med det samme.">
                        <BInput v-model="search" placeholder="Navn eller mailadresse" expanded />
                        <BButton native-type="submit" :loading="searching" :disabled="!search.trim()">Søg</BButton>
                    </BField>
                </form>
                <div v-for="user in results" :key="user.id" class="member-result">
                    <div>{{ user.firstName }} {{ user.lastName }}<br /><small>{{ user.email }}</small></div>
                    <BButton size="is-small" type="is-primary" :disabled="adding || isMember(user.id)" @click="invite(user)">
                        {{ isMember(user.id) ? 'Medlem' : 'Inviter' }}
                    </BButton>
                </div>
                <p v-if="searched && !searching && results.length === 0" class="has-text-grey">Ingen brugere fundet.</p>
            </section>
            <footer class="modal-card-foot">
                <BButton @click="open = false">Luk</BButton>
            </footer>
        </div>
    </BModal>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { BButton, BField, BInput, BModal, useToast } from 'buefy';
import BoardService from '@/Services/BoardService';
import type { BoardMemberDto } from '@/types';

const props = defineProps<{ boardId: number }>();
const open = defineModel<boolean>({ default: false });
const service = new BoardService();
const Toast = useToast();
const members = ref<BoardMemberDto[]>([]);
const results = ref<BoardMemberDto[]>([]);
const search = ref('');
const loading = ref(false);
const searching = ref(false);
const searched = ref(false);
const adding = ref(false);
const error = ref('');

watch(open, async (value) => {
    if (!value) return;
    search.value = '';
    results.value = [];
    members.value = [];
    searched.value = false;
    error.value = '';
    loading.value = true;
    try {
        members.value = await service.getMembers(props.boardId);
    } catch {
        error.value = 'Medlemmerne kunne ikke indlæses. Luk og prøv igen.';
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
    error.value = '';
    try {
        await service.addMember(props.boardId, user.id);
        members.value.push(user);
        Toast.open({ message: 'Brugeren har nu adgang til tavlen.', type: 'is-success' });
    } catch {
        error.value = 'Brugeren kunne ikke inviteres. Prøv igen.';
    } finally {
        adding.value = false;
    }
}
</script>

<style scoped>
.member-result {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 0.75rem 0;
    border-bottom: 1px solid #eee;
    overflow-wrap: anywhere;
}
</style>
