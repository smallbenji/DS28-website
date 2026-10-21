<template>
    <slot v-if="allowed" />
</template>
<script lang="ts" setup>
import { useMeStore } from '@ds28/shared-core/Stores/MeStore';
import { computed } from 'vue';

const props = defineProps<{
    role?: string;
    any?: string[];
}>();

const meStore = useMeStore();

const allowed = computed(() => {
    const roles = meStore.Me?.roles ?? [];
    const appRoles = meStore.Me?.appRoles ?? [];
    const has = (name: string) => roles.includes(name) || appRoles.includes(name);

    if (props.role) {
        return has(props.role);
    }

    if (props.any?.length) {
        return props.any.some(has);
    }

    return true;
});
</script>
