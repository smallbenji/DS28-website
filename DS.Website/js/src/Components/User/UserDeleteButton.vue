<template>
    <Can role="UsersDelete">
        <BButton type="is-danger" @click="openDeleteModal">
            Slet
        </BButton>
    </Can>

    <BModal v-model="isDeleteModalOpen" has-modal-card>
        <div class="modal-card">
            <div class="modal-card-head">
                <p class="modal-card-title">Slet bruger</p>
            </div>
            <div class="modal-card-body">
                <p>
                    Er du sikker på, at du vil slette brugeren "{{ user.firstName }} {{ user.lastName }}"? Dette kan ikke fortrydes.
                </p>
            </div>
            <div class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-danger" :loading="isDeleting" @click="deleteUser">
                        Slet bruger
                    </BButton>
                    <BButton type="is-primary" @click="isDeleteModalOpen = false">
                        Annuller
                    </BButton>
                </div>
            </div>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useUserStore } from '@/Stores/UserStore';
import { BButton, BModal, useToast } from 'buefy';
import { ref } from 'vue';
import Can from '@/Components/Can.vue';

const props = defineProps<{
    user: UserSummaryDTO
}>();

const emit = defineEmits<{
    (e: 'deleted', userId: string): void;
}>();

const Toast = useToast();
const userStore = useUserStore();

const isDeleteModalOpen = ref(false);
const isDeleting = ref(false);

const openDeleteModal = () => {
    isDeleteModalOpen.value = true;
};

const deleteUser = async () => {
    isDeleting.value = true;

    const success = await userStore.DELETE_USER(props.user);
    isDeleting.value = false;

    if (success) {
        Toast.open({
            message: 'Brugeren er slettet!',
            type: 'is-success'
        });
        isDeleteModalOpen.value = false;
        emit('deleted', props.user.id);
    } else {
        Toast.open({
            message: 'Der skete en fejl ved sletning af brugeren',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
.modal-card {
    .modal-card-head,
    .modal-card-title,
    .modal-card-body,
    .modal-card-foot {
        color: #363636;
    }
}
</style>
