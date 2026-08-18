<template>
    <div class="group-users mb-auto">
        <p class="subtitle mb-2 px-4">
            Brugere
        </p>
        <div class="group-user" v-for="user in Group.users">
            <FontAwesomeIcon icon="user" />
            {{ user.firstName + " " + user.lastName }}
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useGroupStore } from '@/Stores/GroupStore';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { storeToRefs } from 'pinia';

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);
</script>
<style lang="scss">
.group {
    &-users {
        min-width: 12rem;
        background-color: white;
        padding: 1rem 0;
        border-radius: 10px;
        border: 1px solid rgba(0, 0, 0, 0.1);
        box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
    }

    &-user {
        padding: 0.25rem 1rem;
        // background-color: darkgray;
    }
}
</style>