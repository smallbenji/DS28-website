<template>
    <div class="back">
        <a href="/">
            <font-awesome-icon icon="arrow-left" />
        </a>
        <h1 class="title is-5">
            <font-awesome-icon :icon="props.icon" /> {{props.title}}
        </h1>
    </div>
</template>
<script lang="ts" setup>
const props = defineProps<{
    icon: string;
    title: string;
}>();
</script>
<style lang="scss">
.back {
    background-color: #fff;
    border-radius: 10px;
    margin: 1rem;
    display: flex;
    gap: 0.25rem;
    padding: 1rem;
}
</style>