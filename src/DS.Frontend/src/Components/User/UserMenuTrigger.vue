<template>
    <button type="button" class="user-menu-trigger" v-if="Me" aria-haspopup="true" :aria-expanded="active">
        <UserAvatar :user="Me" size="small" />
        <span class="user-menu-trigger-info">
            <span class="user-menu-trigger-name">{{ Me.name }}</span>
            <span class="user-menu-trigger-title">{{ Me.title }}</span>
        </span>
    </button>
</template>
<script lang="ts" setup>
import { useMeStore } from '@/Stores/MeStore';
import UserAvatar from '@/Components/User/UserAvatar.vue';
import { storeToRefs } from 'pinia';

defineProps<{ active: boolean }>();

const meStore = useMeStore();
const { Me } = storeToRefs(meStore);
</script>
<style lang="scss">
.user-menu-trigger {
    border: none;
    background: transparent;
    color: inherit;
    font: inherit;
    padding: 0;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;

    &:focus-visible {
        outline: 2px solid #3273dc;
        outline-offset: 4px;
    }

    &-info {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        line-height: 1.25;
    }

    &-name {
        font-weight: 700;
        font-size: 0.875rem;
    }

    &-title {
        color: hsl(0, 0%, 48%);
        font-size: 0.75rem;
        // padding-left: 0.25rem;
    }
}
</style>