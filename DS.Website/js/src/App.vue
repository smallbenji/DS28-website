<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import Back from '@/Components/Back.vue';

const route = useRoute();
const showBack = computed(() => !!route.meta.pageTitle);
</script>

<template>
    <template v-if="showBack">
        <Back />
        <router-view />
    </template>
    <router-view v-else />
</template>

<style lang="scss">
.compact-scroll {
    $track-color: rgba(0, 0, 0, 0.1);
    $handle-color: rgba(0, 0, 0, 0.2);
    scrollbar-width: thin;
    scrollbar-color: $handle-color $track-color;
    &::-webkit-scrollbar {
        width: 6px;
        height: 6px;
    }
    &::-webkit-scrollbar-track {
        background: $track-color;
    }
    &::-webkit-scrollbar-thumb {
        background-color: $handle-color;
    }
    overflow: auto;

}

.ui-card {
    background-color: #fff;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.flex {
    flex: 1;
}

.ui-table {
    width: 100%;
    border-collapse: collapse;

    th,
    td {
        text-align: left;
        padding: 0.5rem 1rem;
        border-bottom: 1px solid rgba(0, 0, 0, 0.08);
    }

    th {
        font-size: 0.7rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #6b7280;
        font-weight: 600;
    }

    tbody tr {
        cursor: pointer;
        transition: background-color 0.15s ease;

        &:hover {
            background-color: #f7f8fa;
        }

        &:last-child td {
            border-bottom: none;
        }
    }
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
    padding: 2.5rem 1rem;
    text-align: center;
    color: #6b7280;

    > i {
        font-size: 1.75rem;
        color: #cbd5e1;
    }

    &-title {
        font-weight: 600;
        color: #4b5563;
    }
}

.workspace-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    height: 100%;
    padding: 2rem;
    text-align: center;

    > i {
        font-size: 2.5rem;
        color: #cbd5e1;
        margin-bottom: 0.25rem;
    }

    &-title {
        font-weight: 600;
        font-size: 1.1rem;
        color: #4b5563;
    }

    &-subtitle {
        color: #6b7280;
        margin-bottom: 1rem;
    }
}
</style>