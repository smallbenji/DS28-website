<template>
    <UModal v-model:open="open" title="Recovery codes" :dismissible="false" :close="false">
        <template #body>
            <RecoveryCodesList :codes="codes" />
        </template>
        <template #footer>
            <div class="flex w-full justify-end">
                <UButton label="Jeg har gemt koderne" @click="open = false" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import RecoveryCodesList from "@/Components/Account/RecoveryCodesList.vue";

defineProps<{ codes: string[] }>();
const open = defineModel<boolean>("open", { required: true });
</script>
