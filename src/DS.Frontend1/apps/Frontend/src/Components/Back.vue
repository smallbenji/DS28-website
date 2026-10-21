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
            <b-dropdown
                v-if="Me"
                position="is-bottom-left"
            >
                <template #trigger>
                    <UserMenuTrigger />
                </template>
                <b-dropdown-item v-if="Me.isImpersonating" disabled>
                    <b-icon pack="fas" icon="user-shield" size="is-small" />
                    Viser som {{ Me.title || Me.roles[0] }}
                </b-dropdown-item>
                <b-dropdown-item v-if="Me.isImpersonating" @click="stopImpersonation">
                    <b-icon pack="fas" icon="xmark" size="is-small" />
                    Stop visning
                </b-dropdown-item>
                <b-dropdown-item v-else-if="Me.roles.includes('SysAdmin')" @click="pickerOpen = true">
                    <b-icon pack="fas" icon="user-shield" size="is-small" />
                    Vis som …
                </b-dropdown-item>
                <hr v-if="Me.isImpersonating || Me.roles.includes('SysAdmin')" class="dropdown-divider" />
                <b-dropdown-item @click="goToProfile">
                    <b-icon pack="fas" icon="user" size="is-small" />
                    Min profil
                </b-dropdown-item>
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
            <ImpersonationPicker v-model:open="pickerOpen" />
        </div>
    </div>
</template>
<script lang="ts" setup>
import { BButton, BDropdown, BDropdownItem } from 'buefy';
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import AuthService from '@ds28/shared-core/Services/AuthService';
import UserMenuTrigger from '@/Components/User/UserMenuTrigger.vue';
import ImpersonationPicker from '@/Components/ImpersonationPicker.vue';
import { useMeStore } from '@ds28/shared-core/Stores/MeStore';

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

function goToProfile() {
    router.push('/profile');
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
    opacity: 1;
    visibility: visible;
    transform: translateX(0);
    transition: opacity 0.25s ease-out, transform 0.25s ease-out, visibility 0s;

    &.is-hidden {
        opacity: 0;
        visibility: hidden;
        transform: translateX(-0.5rem);
        transition: opacity 0.25s ease-out, transform 0.25s ease-out, visibility 0s linear 0.25s;
    }
}

.back-title-text {
    animation: back-slide-in 0.25s ease-out;
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
