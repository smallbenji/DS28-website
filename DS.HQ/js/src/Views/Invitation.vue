<template>
    <div class="card frame center" v-if="canBeUsed">
        <div class="card-header">
            <p class="card-header-title">
                Brugeroprettelse
            </p>
        </div>
        <div class="card-content">
            <BField label="Email">
                <BInput type="email" v-model="email" disabled />
            </BField>
            <BField label="Fornavn">
                <BInput v-model="firstName" />
            </BField>
            <BField label="Efternavn">
                <BInput v-model="lastName" />
            </BField>
            <BField label="Indtast adgangskode">
                <BInput type="password" v-model="password" />
            </BField>
            <BField label="Gentag adgangskode">
                <BInput type="password" v-model="repeatPassword" />
            </BField>
        </div>
        <div class="card-footer">
            <p class="card-footer-item">
                <BButton type="is-success" :disabled="!canComplete" @click="useInvitation">
                    Opret konto
                </BButton>
            </p>
        </div>
    </div>
    <div class="center" v-else-if="isUsed">
        Invitation er brugt
    </div>
    <div class="center" v-else>
        Ingen invitation fundet
    </div>
</template>

<script lang="ts" setup>
import InvitationService from '@/Services/InvitationService';
import { BButton, BField, BInput } from 'buefy';
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';

const router = useRoute();
const invitationService = new InvitationService();

const invitationData = ref<Invitation | null>(null);
const firstName = ref("");
const lastName = ref("");
const email = ref("");
const password = ref("");
const repeatPassword = ref("");

const canBeUsed = computed(() => { return invitationData && !invitationData.value?.used});
const isUsed = computed(() => { return invitationData && invitationData.value?.used});

const invitationId = computed(() => router.params.id);

onMounted(async () => {
    try {
        if (!invitationId.value) return;

        const data = await invitationService.getInvitation(invitationId.value.toString());

        if (data) {
            invitationData.value = data;
            email.value = data.email;
        }
    } catch (error) {
        console.error("Failed to fetch invitation:", error);
    }
});

const canComplete = computed(() => {
    if (password.value.length < 4) return false;
    if (password.value !== repeatPassword.value) return false;
    if (firstName.value.trim() === "") return false;
    if (lastName.value.trim() === "") return false;

    return true;
});

const useInvitation = async () => {
    const data: UserInvitationCreationDTO = {
        FirstName: firstName.value,
        LastName: lastName.value,
        Password: password.value
    }
    const response = await invitationService.useInvitation(invitationId.value.toString(), data);

    if (response) {
        window.location.href = "/";
    }
}
</script>

<style>
.center {
    margin: auto;
}
.frame {
    min-width: 350px;
}
</style>