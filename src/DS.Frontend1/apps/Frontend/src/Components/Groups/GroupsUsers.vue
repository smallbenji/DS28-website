<template>
        <nav class="panel">
        <p class="panel-heading">
            Brugere
        </p>
        <div class="panel-body">
            <div v-for="user in users">
                <a class="panel-block">
                    <span class="panel-icon">
                        <UserAvatar :user="user" size="xsmall"/>
                        <!-- <i class="fas fa-user" aria-hidden="true"></i> -->
                    </span>
                    {{ user.firstName + " " + user.lastName}}
                </a>
            </div>
            <div v-if="users.length <= 0" class="panel-block">
                Ingen brugere
            </div>
        </div>

    </nav>
</template>
<script lang="ts" setup>
import { useGroupsStore } from '@ds28/shared-core/Stores/GroupsStore';
import { storeToRefs } from 'pinia';
import { computed } from 'vue';
import type { GroupDto } from '@ds28/shared-core/types';
import UserAvatar from '../User/UserAvatar.vue';

const props = defineProps<{
    selectedGroup: GroupDto
}>();

const groupStore = useGroupsStore();
const { Groups: groups } = storeToRefs(groupStore);

const users = computed(() => {
    return groups.value.users[String(props.selectedGroup.id)] ?? [];
})
</script>
