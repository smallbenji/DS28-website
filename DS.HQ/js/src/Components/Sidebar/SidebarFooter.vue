<template>
    <div class="sidebar-footer">
        <slot />
    </div>
</template>
<style lang="scss">
.sidebar-footer {
    padding: 1rem;
    display: flex;
    justify-content: space-between;
    border-top: 1px solid rgba(0, 0, 0, 0.1);
    box-shadow: -5px 0 5px 0 rgba(0, 0, 0, 0.1);
}
</style>