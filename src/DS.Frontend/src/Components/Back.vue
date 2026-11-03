<template>
    <div class="back">
        <div class="back-title">
            <button
                class="back-button"
                :class="{ 'is-hidden': hideBack }"
                :tabindex="hideBack ? -1 : 0"
                @click="goBack"
                aria-label="Tilbage"
            >
                <font-awesome-icon icon="arrow-left" />
            </button>
            <h1 class="title is-5 back-title-text">
                <font-awesome-icon :icon="icon" /> {{ title }}
            </h1>
        </div>
        <div class="buttons">
            <SearchMenu />
            <b-dropdown
                v-if="Me"
                position="is-bottom-left"
                :trigger-tabindex="-1"
            >
                <template #trigger="{ active }">
                    <UserMenuTrigger :active="active" />
                </template>
                <b-dropdown-item v-if="Me.isImpersonating" disabled>
                    <b-icon pack="fas" icon="user-shield" size="is-small" />
                    Viser som {{ Me.title || Me.roles[0] }}
                </b-dropdown-item>
                <b-dropdown-item v-if="Me.isImpersonating" has-link @click="stopImpersonation">
                    <button type="button" class="user-menu-action">
                        <b-icon pack="fas" icon="xmark" size="is-small" />
                        Stop visning
                    </button>
                </b-dropdown-item>
                <b-dropdown-item v-else-if="Me.roles.includes('SysAdmin')" has-link @click="pickerOpen = true">
                    <button type="button" class="user-menu-action">
                        <b-icon pack="fas" icon="user-shield" size="is-small" />
                        Vis som …
                    </button>
                </b-dropdown-item>
                <hr v-if="Me.isImpersonating || Me.roles.includes('SysAdmin')" class="dropdown-divider" />
                <b-dropdown-item has-link>
                    <router-link to="/profile">
                        <b-icon pack="fas" icon="user" size="is-small" />
                        Min profil
                    </router-link>
                </b-dropdown-item>
                <b-dropdown-item custom :focusable="false">
                    <b-button
                        type="is-danger is-light"
                        size="is-fullwidth"
                        icon-left="door-open"
                        label="Log ud"
                        @click="logout"
                    />
                </b-dropdown-item>
            </b-dropdown>
            <ImpersonationPicker v-model:open="pickerOpen" />
        </div>
    </div>
</template>
<script lang="ts" setup>
import { BButton, BDropdown, BDropdownItem } from 'buefy';
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import AuthService from '@/Services/AuthService';
import SearchMenu from '@/Components/SearchMenu.vue';
import UserMenuTrigger from '@/Components/User/UserMenuTrigger.vue';
import ImpersonationPicker from '@/Components/ImpersonationPicker.vue';
import { useMeStore } from '@/Stores/MeStore';

const meStore = useMeStore();
const { Me } = storeToRefs(meStore);

const pickerOpen = ref(false);

const authService = new AuthService();

const route = useRoute();
const router = useRouter();

const title = computed(() => (route.meta.pageTitle as string | undefined) ?? '');
const icon = computed(() => (route.meta.pageIcon as string | undefined) ?? '');
const hideBack = computed(() => route.meta.hideBack === true);

function goBack() {
    const back = router.options.history.state.back as string | null;
    if (back) {
        router.back();
    } else {
        router.push('..');
        // router.push('/');
    }
}

async function stopImpersonation() {
    await meStore.STOP_IMPERSONATION();
}

async function logout() {
    await authService.logout();
    window.location.href = "/login";
}

</script>
<style lang="scss">
.user-menu-action {
    width: 100%;
    border: none;
    background: transparent;
    color: inherit;
    font: inherit;
    text-align: start;
    padding: 0.375rem 1rem;
    cursor: pointer;

    &:focus-visible {
        outline: 2px solid #3273dc;
        outline-offset: -2px;
    }
}

.back {
    background-color: #fff;
    border-radius: 10px;
    margin: 1rem;
    display: flex;
    justify-content: space-between;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    flex-wrap: wrap;

    &-title {
        display: flex;
        gap: 0.25rem;
        align-items: center;
        min-width: 0;
    }
}

.back-button {
    background: none;
    border: none;
    cursor: pointer;
    font-size: 1rem;
    color: inherit;
    padding: 0.25rem 0.375rem;
    opacity: 1;
    visibility: visible;
    transform: translateX(0);
    border-radius: 4px;
    @media (prefers-reduced-motion: no-preference) {
        transition: opacity 0.25s ease-out, transform 0.25s ease-out, visibility 0s;
    }

    &:focus-visible {
        outline: 2px solid rgb(59, 130, 246);
        outline-offset: 2px;
    }

    &.is-hidden {
        opacity: 0;
        visibility: hidden;
        @media (prefers-reduced-motion: no-preference) {
            transform: translateX(-0.5rem);
            transition: opacity 0.25s ease-out, transform 0.25s ease-out, visibility 0s linear 0.25s;
        }
    }
}

.back-title-text {
    @media (prefers-reduced-motion: no-preference) {
        animation: back-slide-in 0.25s ease-out;
    }
}

@keyframes back-slide-in {
    from {
        opacity: 0;
        transform: translateX(-0.5rem);
    }
    to {
        opacity: 1;
        transform: translateX(0);
    }
}
</style>
