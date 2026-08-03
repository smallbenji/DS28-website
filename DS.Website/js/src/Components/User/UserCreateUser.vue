<template>
    <BButton type="is-primary" @click="createNewUser" icon-left="plus">
        Tilføj ny bruger
    </BButton>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <section class="modal-card-body" v-if="newUser">
                <BField label="Brugernavn">
                    <BInput v-model="newUser.userName" disabled />
                </BField>
                <BField label="Fornavn">
                    <BInput v-model="newUser.firstName" />
                </BField>
                <BField label="Efternavn">
                    <BInput v-model="newUser.lastName" />
                </BField>
                <BField label="Email">
                    <BInput v-model="newUser.email" type="email" />
                </BField>
                <BButton type="is-primary" @click="createUser">
                    Opret bruger
                </BButton>
            </section>
        </div>
    </BModal>
</template>
<script setup lang="ts">
import { useUserStore } from '@/Stores/UserStore';
import { BModal, BField, BInput, BButton } from 'buefy';
import { ref, toRaw, watch } from 'vue';

const newUser = ref<DSUser | null>();
const open = ref<boolean>(false);

const userStore = useUserStore();

const createNewUser = () => {
  open.value = true;
  newUser.value = {
        id: "",
        userName: "",
        firstName: "",
        lastName: "",
        email: "",
    groupNumber: "",
    roles: [],
    group: null
  } as DSUser;
};

const createUser = async () => {
    if (newUser.value) {
        await userStore.CREATE_USER(toRaw(newUser.value));

        // Clean up
        open.value = false;
        newUser.value = null;
    }
};

watch(() => [newUser.value?.firstName, newUser.value?.lastName], ([newFirst, newLast]) => {
    const first = (newFirst || '').toLowerCase();
    const last = (newLast || '').toLowerCase();

    if (newUser.value) {
        newUser.value.userName = first.toLowerCase() + last.toLowerCase();
    }
});
</script>