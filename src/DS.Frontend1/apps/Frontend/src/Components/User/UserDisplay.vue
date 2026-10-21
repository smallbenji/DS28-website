<template>
    <div class="user-display">
        <UserAvatar size="small" :user="user" />
        <div>
            {{ user.firstName }} {{ user.lastName }}
        </div>
    </div>
</template>

<script setup lang="ts">
import type { UserDto } from '@ds28/shared-core/types';
import UserAvatar from './UserAvatar.vue';

const props = defineProps<{
    user: Pick<UserDto, 'firstName' | 'lastName' | 'profilePicture'>
}>()

</script>
<style scoped>
.user-display {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 700;
    font-size: 0.875rem;
}
</style>
