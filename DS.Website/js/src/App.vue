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
// Flis-sproget er delt mellem HQ og /admin, og skal derfor ligge i en
// altid indlæst fil. Lægtes disse regler i Home.vue ville de kun findes
// efter at den view var blevet indlæst.
.center {
    margin: 0 auto;
    color: rgba(0, 0, 0, 0.1);
}

.grid {
    display: grid;
    gap: 1rem;
    grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
    max-width: 52rem;
    margin: auto;
    margin-top: 1rem;

    @media (max-width: 768px) {
        margin-left: 1rem;
        margin-right: 1rem;
        display: flex;
        flex-direction: column;
        height: 100%;
        overflow-y: auto;
    }
}

.link-box {
    height: 8rem;
    width: 100%;
    background-color: #fff;
    border-radius: 10px;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.05);
    transition: 0.2s ease-in;
    cursor: pointer;
    text-decoration: none;
    color: black;

    @media (max-width: 768px) {
        height: auto;
        padding: 1rem;
    }

    div {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        height: 100%;

        @media (max-width: 768px) {
            flex-direction: row;
            width: 100%;
            justify-content: space-between;
        }

        svg {
            font-size: 44px;
            color: rgba(0, 0, 0, 0.4);
            transition: ease-in 0.2s;
        }

        svg:hover {
            color: rgb(59, 130, 246);
        }

        p {
            text-align: center;
            margin: 0;
            font-size: 18px;
            padding: 0;
            color: rgba(0, 0, 0, 0.4);
            transition: ease-in 0.2s;
        }
    }

    &:hover {
        background-color: #eee;

        p {
            color: #000;
        }

        svg {
            color: rgb(59, 130, 246);
        }
    }
}

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