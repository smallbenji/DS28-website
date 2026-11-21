<template>
    <Can role="UsersResetPassword">
        <BButton type="is-info" :loading="isGenerating" @click="generateLink">
            Reset adgangskode
        </BButton>
    </Can>

    <BModal v-model="isModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Nulstil adgangskode</p>
            </header>
            <div class="modal-card-body">
                <template v-if="link">
                    <p class="mb-2">
                        Send nedenstående link til
                        <strong>{{ link.email }}</strong>. Linket er gyldigt ét brug, indtil det er brugt.
                    </p>
                    <div class="field has-addons">
                        <div class="control is-expanded">
                            <input class="input" :value="link.link" readonly />
                        </div>
                        <div class="control">
                            <BButton type="is-primary" icon-left="copy" @click="copyLink">
                                Kopiér
                            </BButton>
                        </div>
                    </div>
                </template>
                <p v-else>
                    Der skete en fejl under generering af linket. Prøv igen.
                </p>
            </div>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :loading="isGenerating" @click="generateLink">
                        Generér nyt link
                    </BButton>
                    <BButton @click="isModalOpen = false">Luk</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useUserStore } from '@/Stores/UserStore';
import { BButton, BModal, useToast } from 'buefy';
import { ref } from 'vue';
import Can from '@/Components/Can.vue';
import type { ResetPasswordLinkDto, UserDto } from '@/types';

const props = defineProps<{
    user: UserDto
}>();

const Toast = useToast();
const userStore = useUserStore();

const isModalOpen = ref(false);
const isGenerating = ref(false);
const link = ref<ResetPasswordLinkDto | null>(null);

const generateLink = async () => {
    isGenerating.value = true;
    link.value = null;

    try {
        link.value = await userStore.CREATE_RESET_PASSWORD_LINK(props.user);
        isModalOpen.value = true;

        if (!link.value) {
            Toast.open({
                message: 'Der skete en fejl under generering af linket',
                type: 'is-danger'
            });
        }
    } finally {
        isGenerating.value = false;
    }
};

const copyLink = async () => {
    if (!link.value) return;

    try {
        await navigator.clipboard.writeText(link.value.link);
        Toast.open({
            message: 'Linket er kopieret!',
            type: 'is-success'
        });
    } catch {
        Toast.open({
            message: 'Kunne ikke kopiere linket',
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
