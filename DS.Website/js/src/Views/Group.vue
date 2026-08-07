<template>
    <Back title="Gruppe" icon="users"/>
    <div style="padding: 1rem 2rem; display: flex; justify-content: space-between;">
        <h1 class="title">
            {{ Group.name }}
        </h1>
        <GroupLinks class="" />
    </div>
</template>

<script setup lang="ts">
import Back from '@/Components/Back.vue';
import GroupLinks from '@/Components/Group/GroupLinks.vue';
import { useGroupStore } from '@/Stores/GroupStore';
import { storeToRefs } from 'pinia';

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);

</script>