<template>
    <span v-if="holders.length" class="mt-2 flex flex-col items-center gap-1">
        <span v-for="holder in holders" :key="holder.id" class="flex items-center gap-1.5 text-sm">
            <UserAvatar :user="holder" size="sm" />
            {{ holder.firstName }} {{ holder.lastName }}
        </span>
    </span>
    <span v-else class="mt-2 block text-xs text-muted">Ingen tilknyttet</span>
</template>

<script setup lang="ts">
import UserAvatar from "@/Components/User/UserAvatar.vue";
import type { UserSummaryDto } from "@/types";

defineProps<{ holders: UserSummaryDto[] }>();
</script>
