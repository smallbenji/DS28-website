<template>
    <figure class="user-avatar" :class="`is-${size}`">
        <img v-if="url" :src="url" :alt="`${user.firstName} ${user.lastName}`" />
        <span v-else class="user-avatar-initials">{{ initials }}</span>
    </figure>
</template>
<script lang="ts" setup>
import { computed } from 'vue';
import type { UserDto } from '@/types';

const props = withDefaults(defineProps<{
    user: UserDto;
    size?: 'small' | 'medium' | 'large';
}>(), {
    size: 'medium'
});

const url = computed(() => props.user.profilePicture?.url ?? null);
const initials = computed(() => {
    const value = `${props.user.firstName?.[0] ?? ''}${props.user.lastName?.[0] ?? ''}`.toUpperCase();
    return value || '?';
});
</script>
<style lang="scss">
.user-avatar {
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    border-radius: 50%;
    overflow: hidden;
    background-color: hsl(0, 0%, 86%);
    color: hsl(0, 0%, 29%);
    font-weight: 600;
    text-transform: uppercase;

    &.is-small {
        width: 2rem;
        height: 2rem;
        font-size: 0.75rem;
    }

    &.is-medium {
        width: 3rem;
        height: 3rem;
        font-size: 1rem;
    }

    &.is-large {
        width: 5rem;
        height: 5rem;
        font-size: 1.5rem;
    }

    img {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }
}
</style>
