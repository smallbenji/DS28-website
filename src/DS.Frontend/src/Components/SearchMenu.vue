<template>
    <BModal v-model="isOpen" has-modal-card>
        <div class="modal-card search-menu">
            <section class="modal-card-body">
                <BInput
                    ref="searchInput"
                    v-model="query"
                    icon="magnifying-glass"
                    placeholder="Søg efter modul..."
                    @keydown="onInputKeydown"
                />
                <div class="search-menu-list">
                    <button
                        v-for="(option, index) in filteredShortcuts"
                        :key="option.url"
                        type="button"
                        class="search-menu-item"
                        :class="{ 'is-active': index === activeIndex }"
                        @mouseenter="activeIndex = index"
                        @click="onSelect(option)"
                    >
                        <font-awesome-icon
                            :icon="option.icon.length == 1 ? option.icon[0] : option.icon"
                        />
                        <span>{{ option.title }}</span>
                    </button>
                    <p v-if="filteredShortcuts.length === 0" class="search-menu-empty">
                        Ingen moduler matcher din søgning
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
import { BInput, BModal } from 'buefy';
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useMeStore } from '@/Stores/MeStore';
import type { HQPanelEntryDto } from '@/types';

const router = useRouter();
const meStore = useMeStore();
const { ME, HQ } = storeToRefs(meStore);

const isOpen = ref(false);
const query = ref('');
const activeIndex = ref(0);
const searchInput = ref<InstanceType<typeof BInput> | null>(null);

const isExternal = (url: string) => {
    return /^https?:\/\//i.test(url);
};

const filteredShortcuts = computed(() => {
    const search = query.value.trim().toLowerCase();
    if (!search) return HQ.value.shortcuts;

    return HQ.value.shortcuts.filter(x => x.title.toLowerCase().includes(search));
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
    if (!ME.value.isAuthenticated) return;

    query.value = '';
    activeIndex.value = 0;
    isOpen.value = true;
    await nextTick();
    focusInput();

    if (HQ.value.shortcuts.length > 0) return;

    try {
        await meStore.GET_HQ();
    } catch (error) {
        console.error('Could not load home shortcuts:', error);
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
.search-menu {
    width: calc(100% - 2.5rem);
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
    transition: 0.15s ease-in;

    svg {
        width: 1.25rem;
        color: rgba(0, 0, 0, 0.4);
    }

    &:hover,
    &.is-active {
        background-color: rgba(59, 130, 246, 0.1);

        svg {
            color: rgb(59, 130, 246);
        }
    }
}

.search-menu-empty {
    margin: 0;
    padding: 1rem 0.75rem;
    text-align: center;
    color: rgba(0, 0, 0, 0.4);
}

.search-menu-footer {
    display: flex;
    flex-wrap: wrap;
    justify-content: flex-end;
    gap: 0.5rem 1rem;
    padding: 0.5rem 1rem;
    font-size: 0.75rem;
    color: rgba(0, 0, 0, 0.4);
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
