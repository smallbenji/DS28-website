<template>
    <main class="workspace" :class="filled ? 'filled' : 'dashed'">
        <div class="workspace-box" v-if="filled">
            <slot />
        </div>
        <div v-else class="is-flex is-justify-content-center is-align-items-center" style="height: 100%;">
            <p class="is-italic has-text-grey">Vælg en resource for at se detaljer</p>
        </div>
    </main>
</template>
<script lang="ts" setup>
defineProps<{
    filled: boolean;
}>();
</script>
<style lang="scss">
*, *::before, *::after {
    box-sizing: border-box;
}

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

    &.filled {
        background-color: #fff;
    }

    &.dashed {
        border: 3px dashed rgba(0, 0, 0, 0.1);
    }
}
</style>