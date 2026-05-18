<template>
    <nav class="panel">
        <p class="panel-heading">
            Bruger metadata
        </p>
        <div class="panel-body group-body">
            <BField label="Fornavn">
                <BInput v-model="selectedUser.user.firstName" />
            </BField>
            <BField label="Efternavn">
                <BInput v-model="selectedUser.user.lastName" />
            </BField>
            <BField label="Email">
                <BInput v-model="selectedUser.user.email" />
            </BField>
            <BField label="Gruppe">
                <BSelect v-model="selectedUser.groupNumber" expanded>
                    <option value=""></option>
                    <option v-for="group in groups" :value="group.id">
                        {{ group.name }}
                    </option>
                </BSelect>
            </BField>
        </div>
    </nav>
</template>
<script lang="ts" setup>
import { useGroupStore } from '@/Stores/GroupStore';
import { storeToRefs } from 'pinia';


const props = defineProps<{
    selectedUser: DSUser
}>();

const groupStore = useGroupStore();
const { Groups: groups } = storeToRefs(groupStore);
</script>
<style lang="scss">
.group-body {
    padding: 1rem;
}

</style>