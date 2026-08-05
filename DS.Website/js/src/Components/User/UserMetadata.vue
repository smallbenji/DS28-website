<template>
    <nav class="panel">
        <p class="panel-heading">
            Bruger metadata
        </p>
        <div class="panel-body group-body">
            <BField label="Fornavn">
                <BInput v-model="selectedUser.firstName" />
            </BField>
            <BField label="Efternavn">
                <BInput v-model="selectedUser.lastName" />
            </BField>
            <BField label="Email">
                <BInput v-model="selectedUser.email" />
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
import { storeToRefs } from 'pinia';
import { computed } from 'vue';
import type { UserDto } from '@/types';


const props = defineProps<{
    selectedUser: UserDto
}>();

const groupStore = useGroupsStore();
const { Groups: groups } = storeToRefs(groupStore);

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