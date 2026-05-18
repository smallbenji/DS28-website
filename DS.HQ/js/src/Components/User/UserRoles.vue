<template>
    <nav class="panel">
        <div class="panel-heading">
            Roller
            <div class="flex"></div>
            <BButton type="is-small is-warning" @click="open = true">
                Tildel rolle
            </BButton>
        </div>
        <div class="panel-block role-line" v-for="group in selectedUser.roles">
            <span class="panel-icon">
                <font-awesome-icon icon="dice-d6" />
            </span>
            {{ group.name }}
            <div class="flex"></div>
            <BButton type="is-small is-danger" @click="Remove(group.id)">
                Fjern
            </BButton>
        </div>
    </nav>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <div class="modal-card-body">
                <BField label="Rolle">
                    <BSelect expanded v-model="selectedGroup">
                        <option v-for="group in filteredGroups" :value="group.id">
                            {{ group.name }}
                        </option>
                    </BSelect>
                </BField>
                <BButton @click="Assign">
                    Tildel rolle
                </BButton>
            </div>
        </div>
    </BModal>
    <BModal v-model="confirmationOpen" has-modal-card>
        <div class="modal-card">
            <div class="modal-card-head">
                Er du sikker?
            </div>
            <div class="modal-card-foot">
                <BButton type="is-danger" @click="ApprovedRemove">
                    Fjern Rolle
                </BButton>
                <BButton type="is-primary" @click="confirmationOpen = false">
                    Annuller
                </BButton>
            </div>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useUserStore } from '@/Stores/UserStore';
import { BButton, BField, BModal, BSelect } from 'buefy';
import { storeToRefs } from 'pinia';
import { computed, ref } from 'vue';

const props = defineProps<{
    selectedUser: DSUser
}>();

const userStore = useUserStore();
const {GROUPS: groups} = storeToRefs(userStore);

const selectedGroup = ref<string>();
const open = ref<boolean>(false);
const confirmationOpen = ref<boolean>(false);

const selectedGroupToRemove = ref<string>("");

const filteredGroups = computed(() => {
    return groups.value.filter(x => !props.selectedUser.roles.some(y => y.id === x.id)) as KcGroup[];
})

const Assign = async () => {
    await userStore.ADD_USER_TO_ROLE(props.selectedUser, selectedGroup.value ?? "");

    open.value = false;
}

const Remove = async (groupId: string) => {
    selectedGroupToRemove.value = groupId;
    confirmationOpen.value = true;
}

const ApprovedRemove = async () => {
    confirmationOpen.value = false;

    await userStore.REMOVE_USER_FROM_ROLE(props.selectedUser, selectedGroupToRemove.value);
}

</script>