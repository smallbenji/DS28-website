<template>
    <div class="link-container">
        <div class="link-line">
            <span>
                Forhåndstilmelding
            </span>
            <span class="date">
                Åbner d. 1/7/2026
            </span>
        </div>
        <div class="link-line link-line--disabled">
            <span>
                Endelig Tilmelding
            </span>
            <span class="date">
                Åbner d. 1/7/2026
            </span>
        </div>
        <div class="link-line link-line--disabled">
            <span>
                Aktivitets Tilmelding
            </span>
            <span class="date">
                Åbner d. 1/7/2026
            </span>
        </div>
    </div>
</template>
<script lang="ts" setup>
</script>
<style lang="scss">
.link {
    &-container {
        background-color: white;
        border-radius: 10px;
        max-width: 25rem;
        overflow: hidden;
    }
    &-line {
        padding: 1rem;

        border-bottom: 1px solid rgba(0, 0, 0, 0.1);
        cursor: pointer;
        display: flex;
        justify-content: space-between;
        align-items: center;
        
        transition: 0.1s ease-in-out;
        &:last-child {
            border: none;
        }

        &:hover {
            background-color: rgba(0, 0, 0, 0.1);
        }

        .date {
            font-size: 0.75rem;
            color: rgba(0, 0, 0, 0.4);
        }

        &--disabled {
            opacity: 0.5;
            cursor: not-allowed;

            &:hover {
                background-color: transparent;
            }
        }
    }
}
</style>