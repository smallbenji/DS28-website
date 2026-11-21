<template>
    <UAlert
        color="warning"
        variant="subtle"
        icon="i-lucide-hard-drive"
        title="Google Drev er ikke forbundet"
        :actions="canManage ? [{ label: 'Forbind Google Drev', color: 'warning', variant: 'outline', to: '/admin/camp-settings' }] : undefined"
    />
</template>

<script setup lang="ts">
defineProps<{ canManage: boolean }>();
</script>
