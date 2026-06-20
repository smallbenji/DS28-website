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
</template>
<script lang="ts" setup>
import { BField, BInput } from 'buefy';

const props = defineProps<{
    selectedGroup: DSGroup
}>();

</script>
<style lang="scss">
.group-body {
    padding: 1rem;
}
</style>
