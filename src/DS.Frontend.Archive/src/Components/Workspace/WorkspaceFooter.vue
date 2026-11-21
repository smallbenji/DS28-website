<template>
    <div class="flex"></div>
    <div class="workspace-footer">
        <slot />
    </div>
</template>
<style lang="scss">
.flex {
    flex: 1;
}
.workspace-footer {
    display: flex;
    border-top: 1px solid rgba(0, 0, 0, 0.1);
    padding: 0.5rem 1rem;
    flex-direction: row-reverse;
}
</style>