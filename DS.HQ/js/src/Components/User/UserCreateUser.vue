<template>
    <BButton type="is-primary" @click="createNewUser" icon-left="plus">
        Tilføj ny bruger
    </BButton>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <section class="modal-card-body" v-if="newUser">
                <BField label="Brugernavn">
                    <BInput v-model="newUser.user.userName" disabled />
                </BField>
                <BField label="Fornavn">
                    <BInput v-model="newUser.user.firstName" />
                </BField>
                <BField label="Efternavn">
                    <BInput v-model="newUser.user.lastName" />
                </BField>
                <BField label="Email">
                    <BInput v-model="newUser.user.email" type="email" />
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
    user: {
        firstName: "",
        id: "",
        lastName: "",
        email: "",
        userName: ""
    },
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

watch(() => [newUser.value?.user.firstName, newUser.value?.user.lastName], ([newFirst, newLast]) => {
    const first = (newFirst || '').toLowerCase();
    const last = (newLast || '').toLowerCase();

    if (newUser.value?.user) {
        newUser.value.user.userName = first.toLowerCase() + last.toLowerCase();
    }
});
</script>