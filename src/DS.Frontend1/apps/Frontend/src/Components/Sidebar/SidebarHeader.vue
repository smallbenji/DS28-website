<template>
    <div class="sidebar-header">
        <slot />
    </div>
</template>
<style lang="scss">
.sidebar-header {
    padding: 1rem;
    border-bottom: 1px solid rgba(0, 0, 0, 0.1);
    box-shadow: 5px 0 5px 0 rgba(0, 0, 0, 0.1);
}
</style>