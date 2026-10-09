<template>
    <nav class="panel">
        <p class="panel-heading">
            Bruger metadata
        </p>
        <div class="panel-body group-body">
            <div class="is-flex is-align-items-center mb-4">
                <UserAvatar :user="selectedUser" size="large" class="mr-4" />
                <Can role="UsersEditProfilePicture">
                    <div>
                        <div class="buttons mb-2">
                            <BUpload v-model="selectedPicture" accept="image/*" :disabled="isUpdatingPicture">
                                <BButton tag="a" type="is-primary" :loading="isUpdatingPicture">
                                    {{ selectedUser.profilePicture ? 'Vælg nyt billede' : 'Vælg billede' }}
                                </BButton>
                            </BUpload>
                            <BButton v-if="selectedUser.profilePicture" type="is-danger is-light"
                                :disabled="isUpdatingPicture" @click="removePicture">
                                Fjern
                            </BButton>
                        </div>
                        <p class="help">Profilbilledet ændres med det samme.</p>
                    </div>
                </Can>
            </div>
            <BField label="Fornavn">
                <BInput v-model="selectedUser.firstName" />
            </BField>
            <BField label="Efternavn">
                <BInput v-model="selectedUser.lastName" />
            </BField>
            <BField label="Email">
                <BInput v-model="selectedUser.email" />
            </BField>
            <BField label="Telefon">
                <BInput v-model="selectedUser.phone" />
            </BField>
            <BField label="Gruppe">
                <BSelect v-model="selectedGroupId" expanded>
                    <option value=""></option>
                    <option v-for="group in groups.groups" :key="group.id" :value="group.id">
                        {{ group.name }}
                    </option>
                </BSelect>
            </BField>
        </div>
    </nav>
</template>
<script lang="ts" setup>
import { useGroupsStore } from '@/Stores/GroupsStore';
import { useUserStore } from '@/Stores/UserStore';
import { storeToRefs } from 'pinia';
import { computed, ref, watch } from 'vue';
import { BButton, BUpload, useToast } from 'buefy';
import Can from '@/Components/Can.vue';
import UserAvatar from '@/Components/User/UserAvatar.vue';
import FileService from '@/Services/FileService';
import type { UserDto } from '@/types';


const props = defineProps<{
    selectedUser: UserDto
}>();

const Toast = useToast();
const groupStore = useGroupsStore();
const userStore = useUserStore();
const fileService = new FileService();
const { Groups: groups } = storeToRefs(groupStore);

const selectedPicture = ref<File | null>(null);
const isUpdatingPicture = ref(false);

const updatePicture = async (file: File) => {
    selectedPicture.value = null;
    isUpdatingPicture.value = true;

    try {
        const uploaded = await fileService.uploadProfilePicture(file);
        const ready = await fileService.waitUntilReady(uploaded.publicId);

        if (!ready) {
            Toast.open({
                message: 'Billedet kunne ikke behandles. Prøv igen.',
                type: 'is-danger'
            });
            return;
        }

        const ok = await userStore.UPDATE_USER_PROFILE_PICTURE(props.selectedUser.id, uploaded.publicId);
        Toast.open({
            message: ok
                ? 'Profilbilledet er blevet opdateret!'
                : 'Der skete en fejl under opdatering af profilbilledet',
            type: ok ? 'is-success' : 'is-danger'
        });
    } catch {
        Toast.open({
            message: 'Der skete en fejl under upload af billedet',
            type: 'is-danger'
        });
    } finally {
        isUpdatingPicture.value = false;
    }
};

const removePicture = async () => {
    isUpdatingPicture.value = true;

    try {
        const ok = await userStore.UPDATE_USER_PROFILE_PICTURE(props.selectedUser.id, null);
        Toast.open({
            message: ok
                ? 'Profilbilledet er blevet fjernet!'
                : 'Der skete en fejl under fjernelse af profilbilledet',
            type: ok ? 'is-success' : 'is-danger'
        });
    } finally {
        isUpdatingPicture.value = false;
    }
};

watch(selectedPicture, (file) => {
    if (file) updatePicture(file);
});

const selectedGroupId = computed({
    get: () => props.selectedUser.group?.id != null ? String(props.selectedUser.group.id) : "",
    set: (id: string) => {
        props.selectedUser.group = groups.value.groups.find(g => String(g.id) === id) ?? null;
    }
});
</script>
<style lang="scss">
.group-body {
    padding: 1rem;
}

</style>
