<template>
    <nav class="panel">
        <p class="panel-heading">
            Gruppe metadata
        </p>
        <div class="panel-body group-body">
            <BField label="ID">
                <BInput :model-value="String(selectedGroup.id)" disabled />
            </BField>
            <BField label="Navn">
                <BInput v-model="selectedGroup.name" />
            </BField>
        </div>
    </nav>

    <nav class="panel">
        <p class="panel-heading">
            Gruppe brugere
        </p>
        <div class="panel-body">
            <div v-for="user in users">
                <a class="panel-block">
                    <span class="panel-icon">
                        <i class="fas fa-user" aria-hidden="true"></i>
                    </span>
                    {{ user.user.firstName + " " + user.user.lastName}}
                </a>
            </div>
            <div v-if="users.length <= 0" class="panel-block">
                Ingen brugere
            </div>
        </div>

    </nav>
</template>
<script lang="ts" setup>
import { useGroupStore } from '@/Stores/GroupStore';
import { BField, BInput } from 'buefy';
import { storeToRefs } from 'pinia';
import { computed } from 'vue';

const props = defineProps<{
    selectedGroup: DSGroup
}>();

const groupStore = useGroupStore();
const { Groups: groups } = storeToRefs(groupStore);

const users = computed(() => {
    return groups.value.users[props.selectedGroup.id] ?? [];
})

</script>
<style lang="scss">
.group-body {
    padding: 1rem;
}
</style>
