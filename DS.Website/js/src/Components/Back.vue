<template>
    <div class="back">
        <div class="back-title">
            <router-link to="/">
                <font-awesome-icon icon="arrow-left" />
            </router-link>
            <h1 class="title is-5">
                <font-awesome-icon :icon="props.icon" /> {{props.title}}
            </h1>
        </div>
        <div class="buttons">
            <b-dropdown
                position="is-bottom-left"
            >
                <template #trigger>
                    <b-button
                        :label="ME.name"
                        size="is-small"
                        icon-right="angle-down"
                    />
                </template>
                <b-dropdown-item>
                    {{ ME.name }}
                </b-dropdown-item>
                <hr class="dropdown-divider" />
                <b-dropdown-item custom>
                    <b-button
                        type="is-danger is-light"
                        size="is-fullwidth"
                        icon-left="door-open"
                        label="Log ud"
                    />
                </b-dropdown-item>
            </b-dropdown>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useMeStore } from '@/Stores/MeStore';
import { BButton, BDropdown, BDropdownItem } from 'buefy';
import { storeToRefs } from 'pinia';

const props = defineProps<{
    icon: string;
    title: string;
}>();

const meStore = useMeStore();
const { ME } = storeToRefs(meStore);

</script>
<style lang="scss">
.back {
    background-color: #fff;
    border-radius: 10px;
    margin: 1rem;
    display: flex;
    justify-content: space-between;
    gap: 0.25rem;
    padding: 1rem;

    &-title {
        display: flex;
        gap: 0.25rem;
        align-items: center;
    }
}
</style>