<template>
    <div class="group-links">
        <component :is="link.to && !isLocked(link.date) ? RouterLink : 'div'"
            v-for="link in links" :key="link.name" :to="link.to"
            class="link has-text-dark" :class="{'is-inactive': isLocked(link.date)}">
            <span class="mb-1 has-text-weight-bold">
                {{ link.name }}
            </span>
            <span v-if="link.date && isLocked(link.date)">
                {{ link.date.getDate() + "/" + link.date.getMonth() + "/" + link.date.getFullYear() }}
            </span>
        </component>
    </div>
</template>
<script lang="ts" setup>
import { RouterLink } from 'vue-router';

const links = [
    {
        name: "Forhåndstilmelding",
        to: "/group/pre-signup",
        date: null
    },
    {
        name: "Endelig Tilmelding",
        to: "/group/final-signup",
        date: null
    },
    {
        name: "Aktivitets ønskerunde",
        date: new Date(2026, 10, 5)
    },
    {
        name: "Aktivitetstilmelding",
        date: new Date(2026, 10, 5)
    },
]

const isLocked = (targetDate: Date | null) => {
    if (!targetDate) return false;
    const today = new Date();
    return today < targetDate;
}
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
        height: 8rem;
        min-width: 14rem;
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
        position: relative; /* Essential to anchor the huge CSS cross lines */
        overflow: hidden;

        &:hover {
            cursor: pointer;
            background-color: rgba(0, 0, 0, 0.1);
        }

        &.is-locked {
            cursor: not-allowed;
            background-color: rgba(0, 0, 0, 0.2);
        }
        &.is-inactive {
            background-color: #fafafa;
            opacity: 0.7;
            cursor: not-allowed;

        }
    }
}
</style>
