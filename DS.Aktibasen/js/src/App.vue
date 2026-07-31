<script setup lang="ts">
</script>

<template>
    <router-view />
</template>

<style lang="scss">
.compact-scroll {
    $track-color: rgba(0, 0, 0, 0.1);
    $handle-color: rgba(0, 0, 0, 0.2);
    scrollbar-width: thin;
    scrollbar-color: $handle-color $track-color;
    &::-webkit-scrollbar {
        width: 6px;
        height: 6px;
    }
    &::-webkit-scrollbar-track {
        background: $track-color;
    }
    &::-webkit-scrollbar-thumb {
        background-color: $handle-color;
    }
    overflow: auto;

}
</style>