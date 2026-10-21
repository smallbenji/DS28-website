<template>
    <nav class="panel">
        <div class="panel-heading">
            Stillinger
        </div>
        <div
            v-for="position in selectedUser.positions"
            :key="position.id"
            class="panel-block role-line"
        >
            <span class="panel-icon">
                <font-awesome-icon icon="sitemap" />
            </span>
            {{ position.title }}
        </div>
        <p v-if="!selectedUser.positions.length" class="panel-block has-text-grey">Ingen stillinger</p>
    </nav>
</template>
<script lang="ts" setup>
import type { UserDto } from '@ds28/shared-core/types';

defineProps<{
    selectedUser: UserDto
}>();
</script>
<style lang="scss">
.panel-heading {
    padding: 15px 25px;
    min-height: 60px;
    align-items: center;
    display: flex;
}

.role-line {
    display: flex;
}
</style>
