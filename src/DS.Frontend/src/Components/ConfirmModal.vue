<template>
    <UModal v-model:open="open" :title="title" :description="description">
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton :color="color" :label="confirmLabel" :loading="loading" @click="emit('confirm')" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
    title: string;
    description: string;
    confirmLabel: string;
    color?: "primary" | "warning" | "error";
    loading?: boolean;
}>(), {
    color: "primary",
    loading: false
});

const open = defineModel<boolean>("open", { required: true });
const emit = defineEmits<{ (e: "confirm"): void }>();
</script>
