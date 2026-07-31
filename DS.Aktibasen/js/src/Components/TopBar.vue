<template>
    <div class="topbar">
        <span class="a-title">
            Aktibasen
        </span>
        <div class="user">
            <!-- <span v-if="ME?.isActivityAdmin" class="role">
                <FontAwesomeIcon :icon="faShield" />
                ADMIN
            </span> -->
            {{ ME?.name }}
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useMeStore } from '@/Stores/MeStore';
// import { faShield } from '@fortawesome/free-solid-svg-icons';
// import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { storeToRefs } from 'pinia';

const meStore = useMeStore();
const { ME } = storeToRefs(meStore);
</script>
<style lang="scss">
.topbar {
    display: flex;
    justify-content: space-between;
    padding: 1rem;
    border-radius: 0px 0px 10px 10px;
    background-color: white;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);

    .a-title {
        font-weight: 600;
    }
    .user {
        .role {
            font-size: 0.6rem;
            font-weight: 800;
            display: flex;
            align-items: center;
        }
    }
}
</style>