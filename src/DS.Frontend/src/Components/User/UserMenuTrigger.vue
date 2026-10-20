<template>
    <div class="user-menu-trigger">
        <UserAvatar :user="Me" size="small" />
        <div class="user-menu-trigger-info">
            <span class="user-menu-trigger-name">{{ Me.name }}</span>
            <span class="user-menu-trigger-title">{{ Me.title }}</span>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useMeStore } from '@/Stores/MeStore';
import UserAvatar from '@/Components/User/UserAvatar.vue';
import { storeToRefs } from 'pinia';

const meStore = useMeStore();
const { Me } = storeToRefs(meStore);
</script>
<style lang="scss">
.user-menu-trigger {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;

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