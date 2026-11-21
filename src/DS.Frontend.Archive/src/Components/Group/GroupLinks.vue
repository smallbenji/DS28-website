<template>
    <div class="group-links">
        <RouterLink v-for="link in links" :key="link.to" :to="link.to" class="link has-text-dark">
            <span class="mb-1 has-text-weight-bold">{{ link.name }}</span>
        </RouterLink>
        <div v-for="name in unavailableLinks" :key="name" class="link is-inactive has-text-dark">
            <span class="mb-1 has-text-weight-bold">{{ name }}</span>
            <span>Ikke tilgængelig endnu</span>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { RouterLink } from 'vue-router';

const links = [
    { name: 'Forhåndstilmelding', to: '/group/pre-signup' },
    { name: 'Endelig tilmelding', to: '/group/final-signup' }
];
const unavailableLinks = ['Aktivitetsønskerunde', 'Aktivitetstilmelding'];
</script>
<style lang="scss">
.group-links {
    background-color: white;
    border-radius: 10px;
    border: 1px solid rgba(0, 0, 0, 0.1);
    flex-wrap: wrap;
    display: flex;
    gap: 1rem;
    padding: 1rem;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);

    .link {
        min-height: 8rem;
        min-width: min(14rem, 100%);
        overflow-wrap: anywhere;
        text-align: center;
        flex: 1;
        border-radius: 10px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        border: 1px solid rgba(0, 0, 0, 0.1);
        background-color: rgba(255,255,255,1);
        transition: 0.2s ease-out;
        box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.05);
        padding: 1rem;
        position: relative;


        &:not(.is-inactive):hover {
            cursor: pointer;
            background-color: rgba(0, 0, 0, 0.1);
        }

        &.is-inactive {
            background-color: #fafafa;
            opacity: 0.7;
            cursor: default;

        }
    }
}
</style>
