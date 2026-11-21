<template>
    <div class="sidebar-content compact-scroll">
        <slot />
    </div>
</template>
<style lang="scss">
.sidebar-content {
    flex: 1;
    padding: 1rem;
    overflow-y: auto;
    min-height: 0;
}
</style>