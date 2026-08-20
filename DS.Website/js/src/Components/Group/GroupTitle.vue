<template>
    <div class="group-titles">
        <div class="group-title">
            <h1 class="title">
                {{ Group.name }}
            </h1>
            <p class="subtitle is-6">
                {{ Group.district }}
            </p>
        </div>
        <div class="flex"></div>
        <div class="group-number">
            {{ Group.id }}
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useGroupStore } from '@/Stores/GroupStore';
import { storeToRefs } from 'pinia';

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);
</script>
<style lang="scss" scoped>
.group-titles {
    background-color: white;
    border-radius: 10px;
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    width: 100%;
    .flex {
        flex: 1;
    }

    .group-title {
        padding: 1rem;
    }
    border: 1px solid rgba(0, 0, 0, 0.1);
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);

    .group-number {
        border-left: 1px solid rgba(0, 0, 0, 0.2);
        display: flex;
        align-items: center;
        padding: 1rem;
        font-size: 1.25rem;
        color: rgba(0, 0, 0, 0.5);
    }
}
</style>
