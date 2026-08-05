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
            <BField label="Distrikt">
                <BSelect v-model="selectedGroup.district" expanded placeholder="Vælg distrikt...">
                    <option value="DANEHOF">Danehof</option>
                    <option value="FIONIA">Fionia</option>
                </BSelect>
            </BField>
        </div>
    </nav>
</template>
<script lang="ts" setup>
import { BField, BInput, BSelect } from 'buefy';
import type { GroupDto } from '@/types';

const props = defineProps<{
    selectedGroup: GroupDto
}>();
</script>
<style lang="scss">
.group-body {
    padding: 1rem;
}
</style>
