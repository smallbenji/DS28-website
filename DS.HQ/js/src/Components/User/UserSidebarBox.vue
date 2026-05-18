<template>
    <div class="user-sidebar-box" :class="{active: selected}">
        <div class="user-sidebar-box-name">
            <span>{{ user.user.firstName }} {{ user.user.lastName }}</span>
        </div>
        <div class="user-sidebar-box-role-pills">
            <span v-for="role in user.roles" :key="role.id" class="tag is-dark">
                {{ role.name }}
            </span>
        </div>
        <div class="user-sidebar-box-group-info" v-if="user.group?.name">
            {{ user.group.name }}
        </div>
    </div>
</template>
<script lang="ts" setup>
const props = defineProps<{
    user: DSUser;
    selected: boolean;
}>();
</script>
<style lang="scss">
.user-sidebar-box {
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

    &-role-pills {
        display: flex;
        gap: 0.4rem;
        margin-bottom: 0.5rem;
    }

    &-group-info {
        font-size: 0.8rem;
    }

    &.active {
        background: #ececec;
        border: 1px solid #696969;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);
    }
}
</style>