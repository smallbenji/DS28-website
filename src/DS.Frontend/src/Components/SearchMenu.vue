<template>
    <BButton v-if="Me?.isAuthenticated" class="search-menu-trigger" size="is-small" icon-left="magnifying-glass" @click="open">Søg moduler</BButton>
    <BModal :destroy-on-hide="false" v-model="isOpen" has-modal-card aria-label="Søg moduler" close-button-aria-label="Luk modulsøgning">
        <div class="modal-card search-menu">
            <section class="modal-card-body">
                <h2 class="title is-5 mb-3">Søg moduler</h2>
                <BInput
                    ref="searchInput"
                    aria-label="Søg efter modul"
                    v-model="query"
                    icon="magnifying-glass"
                    placeholder="Eksempel: Gruppe"
                    @keydown="onInputKeydown"
                />
                <p v-if="loading" role="status" class="mt-3">Henter moduler…</p>
                <BNotification v-if="loadError" type="is-danger" :closable="false" role="alert" class="mt-3">
                    Modulerne kunne ikke hentes. Kontrollér forbindelsen, og prøv igen.
                    <BButton class="mt-2" @click="loadShortcuts">Prøv igen</BButton>
                </BNotification>
                <div class="search-menu-list">
                    <button
                        v-for="(option, index) in filteredShortcuts"
                        :key="option.url"
                        type="button"
                        class="search-menu-item"
                        :class="{ 'is-active': index === activeIndex }"
                        @mouseenter="activeIndex = index"
                        @click="onSelect(option)"
                        :tabindex="0"
                    >
                        <font-awesome-icon
                            :icon="option.icon.length == 1 ? option.icon[0] : option.icon"
                        />
                        <span>{{ option.title }}</span>
                    </button>
                    <p v-if="!loading && !loadError && filteredShortcuts.length === 0" class="search-menu-empty">
                        Ingen moduler matcher din søgning. Prøv et andet søgeord.
                    </p>
                </div>
            </section>
            <footer class="search-menu-footer">
                <span><kbd>↑</kbd><kbd>↓</kbd>Naviger</span>
                <span><kbd>⏎</kbd>Åbn</span>
                <span><kbd>Esc</kbd>Luk</span>
            </footer>
        </div>
    </BModal>
</template>

<script lang="ts" setup>
import { BButton, BInput, BModal, BNotification } from 'buefy';
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useMeStore } from '@/Stores/MeStore';
import type { HQPanelEntryDto } from '@/types';

const router = useRouter();
const meStore = useMeStore();
const { Me, Hq } = storeToRefs(meStore);

const isOpen = ref(false);
let returnFocus: HTMLElement | null = null;

watch(isOpen, async (open) => {
    if (open) return;

    await nextTick();
    if (returnFocus?.isConnected) returnFocus.focus();
});
const query = ref('');
const loading = ref(false);
const loadError = ref(false);
const activeIndex = ref(0);
const searchInput = ref<InstanceType<typeof BInput> | null>(null);

const isExternal = (url: string) => {
    return /^https?:\/\//i.test(url);
};

const filteredShortcuts = computed(() => {
    const search = query.value.trim().toLowerCase();
    if (!search) return Hq.value.shortcuts;

    return Hq.value.shortcuts.filter(x => x.title.toLowerCase().includes(search));
});

watch(filteredShortcuts, (shortcuts) => {
    if (activeIndex.value >= shortcuts.length) {
        activeIndex.value = Math.max(0, shortcuts.length - 1);
    }
});

const focusInput = () => {
    if (!searchInput.value) return;

    if (typeof searchInput.value.focus === 'function') {
        searchInput.value.focus();
    } else if (searchInput.value.$el?.querySelector('input')) {
        searchInput.value.$el.querySelector('input').focus();
    }
};

const open = async () => {
    if (!Me.value?.isAuthenticated) return;

    returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    query.value = '';
    activeIndex.value = 0;
    isOpen.value = true;
    await nextTick();
    focusInput();

    if (Hq.value.shortcuts.length > 0) return;

    await loadShortcuts();
};

const loadShortcuts = async () => {
    loading.value = true;
    loadError.value = false;
    try {
        await meStore.GET_HQ();
    } catch {
        loadError.value = true;
    } finally {
        loading.value = false;
    }
};

const moveActive = (step: number) => {
    const count = filteredShortcuts.value.length;
    if (count === 0) return;

    activeIndex.value = (activeIndex.value + step + count) % count;
};

const onInputKeydown = (event: KeyboardEvent) => {
    if (event.key === 'ArrowDown') {
        event.preventDefault();
        moveActive(1);
    } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        moveActive(-1);
    } else if (event.key === 'Enter') {
        event.preventDefault();
        const option = filteredShortcuts.value[activeIndex.value];
        if (option) onSelect(option);
    }
};

const onSelect = (option: HQPanelEntryDto) => {
    isOpen.value = false;
    if (isExternal(option.url)) {
        window.open(option.url, '_blank', 'noopener,noreferrer');
        return;
    }
    if (router.currentRoute.value.fullPath !== option.url) {
        router.push(option.url);
    }
};

const handleGlobalKeyDown = (event: KeyboardEvent) => {
    if (!(event.ctrlKey || event.metaKey)) return;
    if (event.key !== ' ' && event.code !== 'Space') return;

    event.preventDefault();
    if (isOpen.value) {
        isOpen.value = false;
    } else {
        open();
    }
};

onMounted(() => window.addEventListener('keydown', handleGlobalKeyDown));
onUnmounted(() => window.removeEventListener('keydown', handleGlobalKeyDown));
</script>

<style lang="scss" scoped>
.search-menu-trigger {
    margin-inline-end: 0.5rem;
}

.search-menu {
    width: calc(100% - 2rem);
    max-width: 32rem;
    overflow: hidden;
    border-radius: 10px;

    .modal-card-body {
        padding: 1rem;
    }
}

.search-menu-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    margin-top: 0.75rem;
    max-height: 50vh;
    overflow-y: auto;
    overscroll-behavior: contain;
}

.search-menu-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    width: 100%;
    padding: 0.6rem 0.75rem;
    font-size: 1rem;
    text-align: left;
    color: rgba(0, 0, 0, 0.7);
    background: none;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    transition: background-color 0.15s ease-in, color 0.15s ease-in;

    svg {
        width: 1.25rem;
        color: rgba(0, 0, 0, 0.6);
    }

    &:hover,
    &.is-active {
        background-color: rgba(59, 130, 246, 0.1);
        color: rgba(0, 0, 0, 0.9);

        svg {
            color: rgb(59, 130, 246);
        }
    }

    &:focus-visible {
        outline: 2px solid rgb(59, 130, 246);
        outline-offset: 2px;
        background-color: rgba(59, 130, 246, 0.1);
    }
}

.search-menu-empty {
    margin: 0;
    padding: 1rem 0.75rem;
    text-align: center;
    color: #595959;
}

.search-menu-footer {
    display: flex;
    flex-wrap: wrap;
    justify-content: flex-end;
    gap: 0.5rem 0.75rem;
    padding: 0.5rem 0.75rem;
    font-size: 0.75rem;
    color: rgba(0, 0, 0, 0.6);
    background-color: #fff;
    border-top: 1px solid rgba(0, 0, 0, 0.06);

    span {
        white-space: nowrap;
    }

    kbd {
        font-family: inherit;
        margin-right: 0.25rem;
        padding: 0.05rem 0.35rem;
        font-size: 0.7rem;
        background-color: rgba(0, 0, 0, 0.04);
        border: 1px solid rgba(0, 0, 0, 0.15);
        border-radius: 4px;
        box-shadow: 0 1px 0 rgba(0, 0, 0, 0.1);
    }
}
</style>
