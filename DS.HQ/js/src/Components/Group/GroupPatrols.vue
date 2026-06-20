<template>
    <nav class="panel">
        <p class="panel-heading">
            Gruppe patruljer
        </p>
        <div class="panel-body">
            <div v-if="selectedGroup.patrols.length > 0" v-for="patrol in selectedGroup.patrols" class="panel-block">
                {{ patrol.name }}
            </div>
            <div v-else class="panel-block">
                Ingen patruljer
            </div>
            <div class="panel-block">
                <BButton type="is-success">Opret patrulje</BButton>
            </div>
        </div>
    </nav>
</template>
<script lang="ts" setup>
import { BButton } from 'buefy';

const props = defineProps<{
    selectedGroup: DSGroup
}>();

</script>
<style lang="scss">
</style>
