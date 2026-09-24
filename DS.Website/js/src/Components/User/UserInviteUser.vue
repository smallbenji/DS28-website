<template>
    <div>
        <BButton type="is-secondary" icon-left="envelope" icon-pack="far" @click="openModal">
            Inviter bruger
        </BButton>

        <BModal v-model="isModalActive" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Inviter ny frivillig</p>
                    <button type="button" class="delete" @click="isModalActive = false" />
                </header>
                <section class="modal-card-body">
                    <BField label="Email">
                        <BInput
                            v-model="email"
                            type="email"
                            placeholder="eksempel@mail.dk"
                            required
                        />
                    </BField>

                    <BField label="Roller">
                        <BTaginput
                            v-model="selectedRoles"
                            autocomplete
                            append-to-body
                            :data="filteredRoleNames"
                            :allow-new="false"
                            icon="tags"
                            placeholder="Vælg roller..."
                            @typing="onTyping"
                        />
                    </BField>
                </section>
                <footer class="modal-card-foot">
                    <div class="buttons">
                        <BButton
                            type="is-primary"
                            :loading="isLoading"
                            @click="sendInvitation"
                            >
                            Send invitation
                        </BButton>
                        <BButton @click="isModalActive = false">Annuller</BButton>
                    </div>
                </footer>
            </div>
        </BModal>
    </div>
</template>
<script setup lang="ts">
import { ref, computed } from 'vue';
import { BButton, BModal, BField, BInput, BTaginput, useToast } from 'buefy';
import { useUserStore } from '@/Stores/UserStore';
import { storeToRefs } from 'pinia';

const userStore = useUserStore();
const { GROUPS: groups, ASSIGNABLE_GROUPS: assignableGroups } = storeToRefs(userStore);
const Toast = useToast();

const isModalActive = ref(false);
const isLoading = ref(false);
const email = ref('');
const selectedRoles = ref<string[]>([]);

const filteredGroups = computed(() => {
    return groups.value.filter(x => assignableGroups.value.includes(x.name));
});

const assignableGroupNames = computed(() => {
    return filteredGroups.value.map(x => x.name);
});

const tagQuery = ref('');
const filteredRoleNames = computed(() => {
    if (!tagQuery.value) {
        return assignableGroupNames.value;
    }

    const query = tagQuery.value.toLowerCase();
    return assignableGroupNames.value.filter(name => name.toLowerCase().includes(query));
});

const onTyping = (text: string | number | undefined) => {
    tagQuery.value = text == null ? '' : String(text);
};

const openModal = () => {
    email.value = '';
    selectedRoles.value = [];
    isModalActive.value = true;
};

const sendInvitation = async () => {
    if (!email.value) {
        Toast.open({
            message: 'Indtast venligst en email',
            type: 'is-warning'
        });
        return;
    }

    isLoading.value = true;
    try {
        const success = await userStore.INVITE_USER(email.value, selectedRoles.value);
        if (success) {
            Toast.open({
                message: 'Invitation sendt til ' + email.value,
                type: 'is-success'
            });
            isModalActive.value = false;
        } else {
            Toast.open({
                message: 'Der skete en fejl under afsendelse af invitationen',
                type: 'is-danger'
            });
        }
    } finally {
        isLoading.value = false;
    }
};
</script>