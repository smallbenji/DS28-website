<template>
    <div class="back">
        <div class="back-title">
            <button v-if="!props.hideBack" class="back-button" @click="goBack" aria-label="Tilbage">
                <font-awesome-icon icon="arrow-left" />
            </button>
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
                <b-dropdown-item>
                    <router-link to="/profile" class="has-text-dark">
                        <b-icon pack="fas" icon="user" size="is-small" />
                        Min profil
                    </router-link>
                </b-dropdown-item>
                <hr class="dropdown-divider" />
                <b-dropdown-item custom>
                    <b-button
                        type="is-danger is-light"
                        size="is-fullwidth"
                        icon-left="door-open"
                        label="Log ud"
                        @click="logout"
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
import { useRouter } from 'vue-router';
import AuthService from '@/Services/AuthService';

const props = defineProps<{
    icon: string;
    title: string;
    hideBack?: boolean;
}>();

const meStore = useMeStore();
const { ME } = storeToRefs(meStore);

const authService = new AuthService();

const router = useRouter();

function goBack() {
    const back = router.options.history.state.back as string | null;
    if (back) {
        router.back();
    } else {
        router.push('/');
    }
}


async function logout() {
    await authService.logout();
    window.location.href = "/login";
}

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

.back-button {
    background: none;
    border: none;
    cursor: pointer;
    font-size: 1rem;
    color: inherit;
    padding: 0 0.25rem;
}
</style>