<template>
    <div style="padding: 1rem 2rem; display: flex; justify-content: space-between; gap: 1rem;">
        <div class="group-view">
            <GroupTitle />
            <GroupLinks class="" />
        </div>
        <GroupUsers />
    </div>
</template>

<script setup lang="ts">
import GroupLinks from '@/Components/Group/GroupLinks.vue';
import GroupTitle from '@/Components/Group/GroupTitle.vue';
import GroupUsers from '@/Components/Group/GroupUsers.vue';

</script>
<style lang="scss">
.group-view {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
</style>