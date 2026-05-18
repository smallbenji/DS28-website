<template>
    <aside class="sidebar">
        <slot />
    </aside>
</template>
<style lang="scss">
.sidebar {
    width: 380px;
    display: flex;
    flex-direction: column;
    background-color: #fff;
    height: 100%;
    flex-shrink: 0;
    border-radius: 10px;
    overflow: hidden;
}
</style>