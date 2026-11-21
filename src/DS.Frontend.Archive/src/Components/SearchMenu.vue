<template>
    <BButton v-if="Me?.isAuthenticated" class="search-menu-trigger" size="is-small" icon-left="magnifying-glass" @click="open">Søg</BButton>
    <BModal :destroy-on-hide="false" v-model="isOpen" has-modal-card aria-label="Søg" close-button-aria-label="Luk søgning">
        <div class="modal-card search-menu">
            <section class="modal-card-body">
                <div class="search-menu-header">
                    <BInput
                        ref="searchInput"
                        class="search-menu-input"
                        aria-label="Søg efter moduler, tavler, grupper eller aktiviteter"
                        v-model="query"
                        icon="magnifying-glass"
                        placeholder="Søg efter tavle, gruppe eller modul"
                        @keydown="onInputKeydown"
                    />
                    <span class="search-menu-shortcut" aria-hidden="true"><kbd>{{ modifierKey }}</kbd><kbd>Space</kbd></span>
                </div>
                <p v-if="loading" role="status" class="mt-3">Henter moduler…</p>
                <BNotification v-if="loadError" type="is-danger" :closable="false" role="alert" class="mt-3">
                    Modulerne kunne ikke hentes. Kontrollér forbindelsen, og prøv igen.
                    <BButton class="mt-2" @click="loadShortcuts">Prøv igen</BButton>
                </BNotification>
                <div class="search-menu-list">
                    <div v-for="section in sections" :key="section.title" class="search-menu-section">
                        <p class="search-menu-section-title">{{ section.title }}</p>
                        <button
                            v-for="entry in section.entries"
                            :key="entry.item.key"
                            type="button"
                            class="search-menu-item"
                            :class="{ 'is-active': entry.index === activeIndex }"
                            @mouseenter="activeIndex = entry.index"
                            @click="onSelect(entry.item)"
                        >
                            <span class="search-menu-icon">
                                <font-awesome-icon :icon="entry.item.icon.length === 1 ? entry.item.icon[0] : entry.item.icon" />
                            </span>
                            <span class="search-menu-text">
                                <span class="search-menu-title">
                                    <template v-for="(part, partIndex) in highlight(entry.item.title)" :key="partIndex">
                                        <mark v-if="part.match">{{ part.text }}</mark>
                                        <template v-else>{{ part.text }}</template>
                                    </template>
                                </span>
                                <span v-if="entry.item.subtitle" class="search-menu-subtitle">{{ entry.item.subtitle }}</span>
                            </span>
                            <span v-if="entry.item.external" class="search-menu-end">
                                <font-awesome-icon icon="up-right-from-square" />
                                <span class="is-sr-only">Åbner i ny fane</span>
                            </span>
                            <span v-else-if="entry.index === activeIndex" class="search-menu-end" aria-hidden="true"><kbd>⏎</kbd></span>
                        </button>
                    </div>
                    <p v-if="searching" role="status" class="search-menu-status">Søger…</p>
                    <p v-else-if="searchError" role="alert" class="search-menu-status">
                        Søgningen kunne ikke gennemføres.
                        <BButton size="is-small" type="is-text" @click="runSearch(query)">Prøv igen</BButton>
                    </p>
                    <p v-else-if="showEmptyState" class="search-menu-status">
                        Ingen resultater for "{{ query.trim() }}". Du ser kun det, du har adgang til.
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
import SearchService from '@/Services/SearchService';
import type { SearchResultDto, SearchResultType } from '@/types';

interface SearchMenuItem {
    key: string;
    section: string;
    title: string;
    subtitle: string | null;
    icon: string[];
    url: string;
    external: boolean;
}

const minimumSearchLength = 2;
const searchDelay = 250;

const resultSections: Record<SearchResultType, { title: string; icon: string[] }> = {
    board: { title: 'Tavler', icon: ['table-columns'] },
    group: { title: 'Grupper', icon: ['users'] },
    activity: { title: 'Aktiviteter', icon: ['newspaper'] },
};

const router = useRouter();
const meStore = useMeStore();
const { Me, Hq } = storeToRefs(meStore);
const searchService = new SearchService();

const modifierKey = /Mac|iPhone|iPad/i.test(navigator.userAgent) ? '⌘' : 'Ctrl';

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

const results = ref<SearchResultDto[]>([]);
const searching = ref(false);
const searchError = ref(false);
let searchTimer: ReturnType<typeof setTimeout> | undefined;
let latestSearch = 0;

const isExternal = (url: string) => {
    return /^https?:\/\//i.test(url);
};

const normalizedQuery = computed(() => query.value.trim().toLowerCase());

const items = computed<SearchMenuItem[]>(() => {
    const search = normalizedQuery.value;
    const shortcuts = search
        ? Hq.value.shortcuts.filter(x => x.title.toLowerCase().includes(search))
        : Hq.value.shortcuts;

    const shortcutItems = shortcuts.map(shortcut => ({
        key: `shortcut:${shortcut.url}`,
        section: 'Moduler',
        title: shortcut.title,
        subtitle: null,
        icon: shortcut.icon,
        url: shortcut.url,
        external: isExternal(shortcut.url),
    }));

    const resultItems = (Object.keys(resultSections) as SearchResultType[]).flatMap(type =>
        results.value
            .filter(result => result.type === type)
            .map(result => ({
                key: `${result.type}:${result.id}`,
                section: resultSections[type].title,
                title: result.title,
                subtitle: result.subtitle,
                icon: resultSections[type].icon,
                url: result.url,
                external: false,
            }))
    );

    return [...shortcutItems, ...resultItems];
});

const sections = computed(() => {
    const grouped: { title: string; entries: { item: SearchMenuItem; index: number }[] }[] = [];
    items.value.forEach((item, index) => {
        let section = grouped.find(x => x.title === item.section);
        if (!section) {
            section = { title: item.section, entries: [] };
            grouped.push(section);
        }
        section.entries.push({ item, index });
    });

    return grouped;
});

const showEmptyState = computed(() => {
    return !loading.value && !loadError.value && items.value.length === 0;
});

const highlight = (text: string) => {
    const search = normalizedQuery.value;
    const start = search ? text.toLowerCase().indexOf(search) : -1;
    if (start < 0) return [{ text, match: false }];

    const end = start + search.length;
    return [
        { text: text.slice(0, start), match: false },
        { text: text.slice(start, end), match: true },
        { text: text.slice(end), match: false },
    ].filter(part => part.text.length > 0);
};

const runSearch = async (value: string) => {
    const term = value.trim();
    const searchId = ++latestSearch;
    if (term.length < minimumSearchLength) {
        results.value = [];
        searching.value = false;
        searchError.value = false;
        return;
    }

    searching.value = true;
    searchError.value = false;
    try {
        const found = await searchService.search(term);
        if (searchId !== latestSearch) return;

        results.value = found;
    } catch {
        if (searchId !== latestSearch) return;

        results.value = [];
        searchError.value = true;
    } finally {
        if (searchId === latestSearch) searching.value = false;
    }
};

watch(query, (value) => {
    activeIndex.value = 0;
    clearTimeout(searchTimer);
    if (value.trim().length < minimumSearchLength) {
        runSearch(value);
        return;
    }

    searching.value = true;
    searchTimer = setTimeout(() => runSearch(value), searchDelay);
});

watch(items, (list) => {
    if (activeIndex.value >= list.length) {
        activeIndex.value = Math.max(0, list.length - 1);
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
    const count = items.value.length;
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
        const item = items.value[activeIndex.value];
        if (item) onSelect(item);
    }
};

const onSelect = (item: SearchMenuItem) => {
    isOpen.value = false;
    if (item.external) {
        window.open(item.url, '_blank', 'noopener,noreferrer');
        return;
    }
    if (router.currentRoute.value.fullPath !== item.url) {
        router.push(item.url);
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
onUnmounted(() => {
    window.removeEventListener('keydown', handleGlobalKeyDown);
    clearTimeout(searchTimer);
});
</script>

<style lang="scss" scoped>
.search-menu-trigger {
    margin-inline-end: 0.5rem;
}

.search-menu {
    width: 40rem;
    max-width: calc(100vw - 2rem);
    overflow: hidden;
    border-radius: 10px;

    .modal-card-body {
        padding: 1rem;
    }
}

.search-menu-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.search-menu-input {
    flex: 1;
}

.search-menu-shortcut {
    display: flex;
    gap: 0.25rem;
    white-space: nowrap;

    @media (max-width: 600px) {
        display: none;
    }
}

.search-menu-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    margin-top: 0.5rem;
    max-height: 60vh;
    overflow-y: auto;
    overscroll-behavior: contain;
}

.search-menu-section {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.search-menu-section-title {
    margin: 0;
    padding: 0.5rem 0.75rem 0.25rem;
    font-size: 0.75rem;
    color: #595959;
}

.search-menu-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    width: 100%;
    padding: 0.5rem 0.75rem;
    font-size: 1rem;
    text-align: left;
    color: rgba(0, 0, 0, 0.7);
    background: none;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    transition: background-color 0.15s ease-in, color 0.15s ease-in;

    &:hover,
    &.is-active {
        background-color: rgba(59, 130, 246, 0.1);
        color: rgba(0, 0, 0, 0.9);

        .search-menu-icon {
            background-color: #fff;
            color: rgb(59, 130, 246);
        }
    }

    &:focus-visible {
        outline: 2px solid rgb(59, 130, 246);
        outline-offset: 2px;
        background-color: rgba(59, 130, 246, 0.1);
    }
}

.search-menu-icon {
    display: flex;
    flex-shrink: 0;
    align-items: center;
    justify-content: center;
    width: 2rem;
    height: 2rem;
    color: rgba(0, 0, 0, 0.6);
    background-color: rgba(0, 0, 0, 0.04);
    border-radius: 6px;
}

.search-menu-text {
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.search-menu-title {
    line-height: 1.3;

    mark {
        padding: 0;
        color: inherit;
        background-color: rgba(250, 204, 21, 0.4);
        border-radius: 2px;
    }
}

.search-menu-subtitle {
    overflow: hidden;
    font-size: 0.8rem;
    line-height: 1.3;
    color: #595959;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.search-menu-end {
    margin-inline-start: auto;
    padding-inline-start: 0.5rem;
    font-size: 0.75rem;
    color: rgba(0, 0, 0, 0.5);
}

.search-menu-status {
    margin: 0;
    padding: 1rem 0.75rem;
    text-align: center;
    color: #595959;
}

.search-menu-footer {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem 1rem;
    padding: 0.5rem 1rem;
    font-size: 0.75rem;
    color: rgba(0, 0, 0, 0.6);
    background-color: #fff;
    border-top: 1px solid rgba(0, 0, 0, 0.06);

    span {
        white-space: nowrap;
    }
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
</style>
