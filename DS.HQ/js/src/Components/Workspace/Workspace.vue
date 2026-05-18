<template>
    <main class="workspace" :class="filled ? 'filled' : 'dashed'">
        <div class="workspace-box" v-if="filled">
            <slot />
        </div>
    </main>
</template>
<script lang="ts" setup>
defineProps<{
    filled: boolean;
}>();
</script>
<style lang="scss">
.workspace {
    height: 100%;
    flex: 1;
    border-radius: 10px;
    overflow: hidden;

    &-box {
        display: flex;
        flex-direction: column;
        height: 100%;
        overflow-x: auto;
    }
}
</style>