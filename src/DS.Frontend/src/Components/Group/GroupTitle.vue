<template>
    <div class="group-titles">
        <div class="group-title">
            <h1>
                {{ Group.name }}
            </h1>
            <div class="group-title-subtitle">
                {{ Group.id }}
                •
                {{ districtLabel(Group.district) }}
            </div>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useGroupStore } from '@/Stores/GroupStore';
import { districtLabel } from '@/types';
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
    border: 1px solid rgba(0, 0, 0, 0.1);
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);

    .group-title {
        padding: 1rem;

        h1 {
            font-size: 2rem;
            font-weight: 800;
        }

        .group-title-subtitle {
            font-size: 0.85rem;
            color: rgba(0, 0, 0, 0.5);
        }
    }
}
</style>
