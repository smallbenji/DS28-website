<template>
    <div class="split">
        <Sidebar />
        <ActivityList />
    </div>
</template>
<script lang="ts" setup>
import ActivityList from '@/Components/Home/ActivityList.vue';
import Sidebar from '@/Components/Home/Sidebar.vue';
import { useMeStore } from '@/Stores/MeStore';
import { storeToRefs } from 'pinia';


const meStore = useMeStore()
const { ME } = storeToRefs(meStore);
</script>
<style lang="scss">
.split {
    display: flex;
    width: 100%;
    height: 100%;
}
</style>