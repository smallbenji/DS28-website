<template>
    <div class="group-sidebar-box" :class="{active: selected}">
        <div class="group-sidebar-box-name">
            <span>{{ group.name }}</span>
        </div>
        <div class="group-sidebar-box-info">
            ID: {{ group.id }}
        </div>
    </div>
</template>
<script lang="ts" setup>
defineProps<{
    group: DSGroup;
    selected: boolean;
}>();
</script>
<style lang="scss">
.group-sidebar-box {
    border: 1px solid rgba(0, 0, 0, 0.1);
    padding: 1rem 1.2rem;
    border-radius: 10px;
    margin-bottom: 1rem;
    cursor: pointer;

    &-name {
        font-weight: 600;
        font-size: 1rem;
        margin-bottom: 0.4rem;
    }

    &-info {
        font-size: 0.8rem;
        color: #64748b;
    }

    &.active {
        background: #ececec;
        border: 1px solid #696969;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);
    }
}
</style>
