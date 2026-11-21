<template>
    <UAvatar :src="user.profilePicture?.url" :alt="`${user.firstName} ${user.lastName}`" :text="initials" :size="size" />
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { UserDto } from "@/types";

const props = withDefaults(defineProps<{
    user: Pick<UserDto, "firstName" | "lastName" | "profilePicture">;
    size?: "sm" | "md" | "lg" | "xl" | "3xl";
}>(), {
    size: "md"
});

const initials = computed(() => `${props.user.firstName?.[0] ?? ""}${props.user.lastName?.[0] ?? ""}`.toUpperCase() || "?");
</script>
