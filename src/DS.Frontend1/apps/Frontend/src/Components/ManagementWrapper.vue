<template>
    <div class="management-wrapper">
        <slot />
    </div>
</template>
<style lang="scss">
.management-wrapper {
    display: flex;
    height: 100%;
    width: 100%;
    padding: 1rem;
    padding-top: 0;
    gap: 1rem;
    min-height: 0;
    scrollbar-width: thin;

    .mobile-back {
        display: none;
        align-items: center;
        gap: 0.5rem;
        background: none;
        border: none;
        color: #3273dc;
        cursor: pointer;
        font-weight: 600;
        font-size: 1rem;
        padding: 0.5rem 0.25rem;
    }
}

@media (max-width: 768px) {
    .management-wrapper {
        .sidebar {
            width: 100%;
        }

        &.has-selection {
            .sidebar {
                display: none;
            }

            .mobile-back {
                display: inline-flex;
            }
        }

        &:not(.has-selection) {
            .workspace {
                display: none;
            }
        }
    }
}
</style>