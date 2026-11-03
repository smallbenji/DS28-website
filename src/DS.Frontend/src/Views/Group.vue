<template>
    <div class="group-page">
        <div class="group-view">
            <GroupTitle />
            <GroupLinks />
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
.group-page {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem 2rem;

    @media (max-width: 768px) {
        flex-direction: column;
        padding: 1rem;

        .group-users {
            width: 100%;
        }
    }
}

.group-view {
    min-width: 0;
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
</style>