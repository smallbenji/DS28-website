<template>
    <div class="group-links">
        <div v-for="link in links" class="link" :class="{'is-inactive': isLocked(link.date)}">
            <span class="subtitle is-5 mb-1 has-text-weight-bold">
                {{ link.name }}
            </span>
            <span v-if="isLocked(link.date)">
                {{ link.date.getDate() + "/" + link.date.getMonth() + "/" + link.date.getFullYear() }}
            </span>
        </div>
    </div>
</template>
<script lang="ts" setup>
const links = [
    {
        name: "Forhåndstilmelding",
        date: new Date(2026, 4, 5)
    },
    {
        name: "Endelig Tilmelding",
        date: new Date(2026, 10, 5)
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

const isLocked = (targetDate: Date) => {
    const today = new Date();
    return today < targetDate;
}
</script>
<style lang="scss">
.group-links {
    background-color: white;
    border-radius: 10px;
    border: 1px solid rgba(0, 0, 0, 0.1);
    display: flex;
    gap: 1rem;
    padding: 1rem;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);

    .link {
        height: 8rem;
        min-width: 12rem;
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
            opacity: 0.7;
            background-color: #fafafa;
            cursor: not-allowed;
            pointer-events: none;
            overflow: hidden;

            /* This creates the giant diagonal line going top-left to bottom-right */
            &::before {
                content: "";
                position: absolute;
                width: 150%; /* wider than 100% to fully cover the diagonal span */
                height: 3px;  /* Thickness of the cross line */
                background-color: rgba(255, 56, 96, 0.6); /* Red color with transparency */
                transform: rotate(33deg); /* Perfectly fits a 12x8rem box aspect ratio */
            }

            /* This creates the giant diagonal line going bottom-left to top-right */
            &::after {
                content: "";
                position: absolute;
                width: 150%;
                height: 3px;
                background-color: rgba(255, 56, 96, 0.6);
                transform: rotate(-33deg);
            }
        }
    }
}
</style>