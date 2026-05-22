<template>
    Helo {{ invitationId }}
</template>
<script lang="ts" setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const router = useRoute();

const invitationId = computed(() => router.params.id);
</script>