<template>
    <section class="group-users mb-auto" aria-labelledby="group-users-title">
        <h2 id="group-users-title" class="subtitle mb-2 px-4">
            Brugere
        </h2>
        <div class="px-4 mb-3">
            <BButton size="is-small" @click="router.push('/group/members')">Administrer brugere</BButton>
        </div>
        <ul>
            <li class="group-user" v-for="user in Group.users" :key="user.id">
                <UserAvatar :user="user" size="small"/>
                <span>{{ user.firstName + " " + user.lastName }}</span>
            </li>
        </ul>
        <p v-if="Group.users.length === 0" class="px-4">Ingen brugere i gruppen endnu.</p>
    </section>
</template>
<script lang="ts" setup>
import { BButton } from 'buefy';
import { useRouter } from 'vue-router';
const router = useRouter();
import { useGroupStore } from '@/Stores/GroupStore';
import { storeToRefs } from 'pinia';
import UserAvatar from '../User/UserAvatar.vue';

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);
</script>
<style lang="scss">
.group {
    &-users {
        flex: 0 0 14rem;
        min-width: 0;
        max-width: 100%;

        @media (max-width: 768px) {
            flex-basis: auto;
        }
        background-color: white;
        padding: 1rem 0;
        border-radius: 10px;
        border: 1px solid rgba(0, 0, 0, 0.1);
        box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
    }

    &-user {
        padding: 0.25rem 1rem;
        display: flex;
        gap: 0.5rem;
        align-items: center;
        span {
            min-width: 0;
            overflow-wrap: anywhere;
        }

        .user-avatar {
            flex-shrink: 0;
        }
    }
}
</style>
