<template>
    <Can role="UsersLock">
        <BButton type="is-warning" @click="openLockModal">
            {{ isLocked ? 'Lås op' : 'Lås' }}
        </BButton>
    </Can>

    <BModal v-model="isLockModalOpen" has-modal-card>
        <div class="modal-card">
            <div class="modal-card-head">
                <p class="modal-card-title">{{ isLocked ? 'Lås op bruger' : 'Lås bruger' }}</p>
            </div>
            <div class="modal-card-body">
                <p>
                    Er du sikker på, at du vil {{ isLocked ? 'låse op for brugeren' : 'låse brugeren' }} "{{ user.firstName }} {{ user.lastName }}"?
                </p>
            </div>
            <div class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-warning" :loading="isLocking" @click="toggleLock">
                        {{ isLocked ? 'Lås op' : 'Lås bruger' }}
                    </BButton>
                    <BButton type="is-primary" @click="isLockModalOpen = false">
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
import { computed, ref } from 'vue';
import Can from '@/Components/Can.vue';
import type { UserDto } from '@/types';

const props = defineProps<{
    user: UserDto
}>();

const emit = defineEmits<{
    (e: 'changed', userId: string): void;
}>();

const Toast = useToast();
const userStore = useUserStore();

const isLockModalOpen = ref(false);
const isLocking = ref(false);

const isLocked = computed(() => {
    if (!props.user.lockoutEnd) return false;
    return new Date(props.user.lockoutEnd).getTime() > Date.now();
});

const openLockModal = () => {
    isLockModalOpen.value = true;
};

const toggleLock = async () => {
    isLocking.value = true;

    const success = isLocked.value
        ? await userStore.UNLOCK_USER(props.user)
        : await userStore.LOCK_USER(props.user);
    isLocking.value = false;

    if (success) {
        Toast.open({
            message: isLocked.value ? 'Brugeren er låst op!' : 'Brugeren er låst!',
            type: 'is-success'
        });
        isLockModalOpen.value = false;
        emit('changed', props.user.id);
    } else {
        Toast.open({
            message: isLocked.value ? 'Der skete en fejl ved oplåsning af brugeren' : 'Der skete en fejl ved låsning af brugeren',
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
