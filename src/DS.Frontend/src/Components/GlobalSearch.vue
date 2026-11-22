<template>
    <UDashboardSearch
        v-model:open="open"
        v-model:search-term="searchTerm"
        :groups="groups"
        :loading="searching"
        :color-mode="false"
        placeholder="Søg efter tavle, gruppe, aktivitet, modul eller adminside"
        title="Søg"
        description="Søg efter moduler, adminsider, tavler, grupper og aktiviteter."
    />
</template>

<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { storeToRefs } from "pinia";
import type { CommandPaletteGroup, CommandPaletteItem } from "@nuxt/ui";
import { useMeStore } from "@/Stores/MeStore";
import SearchService from "@/Services/SearchService";
import AdminService from "@/Services/AdminService";
import { toHqShortcut } from "@/lib/hqShortcuts";
import type { AdminSectionDto, HQPanelEntryDto, SearchResultDto, SearchResultType } from "@/types";

const open = defineModel<boolean>("open", { default: false });

const router = useRouter();
const meStore = useMeStore();
const { Hq, Me } = storeToRefs(meStore);
const searchService = new SearchService();
const adminService = new AdminService();

const minimumSearchLength = 2;
const searchDelay = 250;

const searchTerm = ref("");
const results = ref<SearchResultDto[]>([]);
const searching = ref(false);
const searchFailed = ref(false);
const adminSections = ref<AdminSectionDto[]>([]);
let searchTimer: ReturnType<typeof setTimeout> | undefined;
let latestSearch = 0;

const resultSections: Record<SearchResultType, { label: string; icon: string }> = {
    board: { label: "Tavler", icon: "i-lucide-columns-3" },
    group: { label: "Grupper", icon: "i-lucide-users" },
    activity: { label: "Aktiviteter", icon: "i-lucide-newspaper" }
};

async function loadAdminSections() {
    if (!Me.value?.appRoles.includes("AdminAccess")) {
        adminSections.value = [];
        return;
    }

    try {
        adminSections.value = (await adminService.get()).sections;
    } catch {
        adminSections.value = [];
    }
}

watch(open, (isOpen) => {
    if (isOpen) void loadAdminSections();
});

watch(open, async (isOpen) => {
    if (!isOpen || Hq.value.shortcuts.length > 0) return;

    try {
        await meStore.GET_HQ();
    } catch {
        searchFailed.value = true;
    }
});

async function runSearch(value: string) {
    const term = value.trim();
    const searchId = ++latestSearch;
    if (term.length < minimumSearchLength) {
        results.value = [];
        searching.value = false;
        searchFailed.value = false;
        return;
    }

    searching.value = true;
    searchFailed.value = false;
    try {
        const found = await searchService.search(term);
        if (searchId === latestSearch) results.value = found;
    } catch {
        if (searchId === latestSearch) {
            results.value = [];
            searchFailed.value = true;
        }
    } finally {
        if (searchId === latestSearch) searching.value = false;
    }
}

watch(searchTerm, (value) => {
    clearTimeout(searchTimer);
    if (value.trim().length < minimumSearchLength) {
        void runSearch(value);
        return;
    }

    searching.value = true;
    searchTimer = setTimeout(() => void runSearch(value), searchDelay);
});

onUnmounted(() => clearTimeout(searchTimer));

function navigate(url: string) {
    open.value = false;
    if (router.currentRoute.value.fullPath !== url) void router.push(url);
}

function toShortcutItem(entry: HQPanelEntryDto): CommandPaletteItem {
    const shortcut = toHqShortcut(entry);
    const isRelative = !/^https?:\/\//i.test(shortcut.to);

    return {
        label: shortcut.title,
        icon: shortcut.icon,
        suffix: shortcut.opensNewWindow ? "Åbner i nyt vindue" : undefined,
        onSelect: () => {
            open.value = false;
            if (shortcut.opensNewWindow) window.open(shortcut.to, "_blank", "noopener,noreferrer");
            else if (isRelative) navigate(shortcut.to);
            else window.location.assign(shortcut.to);
        }
    };
}

const groups = computed<CommandPaletteGroup<CommandPaletteItem>[]>(() => {
    const shortcutItems = Hq.value.shortcuts.map(toShortcutItem);

    const adminItems = searchTerm.value.trim().length > 0
        ? adminSections.value.flatMap((section) => section.entries.map(toShortcutItem))
        : [];

    const resultGroups = (Object.keys(resultSections) as SearchResultType[])
        .map((type) => ({
            id: type,
            label: resultSections[type].label,
            ignoreFilter: true,
            items: results.value
                .filter((result) => result.type === type)
                .map((result): CommandPaletteItem => ({
                    label: result.title,
                    suffix: result.subtitle ?? undefined,
                    icon: resultSections[type].icon,
                    onSelect: () => navigate(result.url)
                }))
        }))
        .filter((group) => group.items.length > 0);

    const statusGroup = searchFailed.value
        ? [{ id: "error", label: "Søgningen fejlede", ignoreFilter: true, items: [{ label: "Søgningen kunne ikke gennemføres. Prøv igen.", icon: "i-lucide-circle-alert", disabled: true }] }]
        : [];

    return [
        { id: "modules", label: "Moduler", items: shortcutItems },
        { id: "administration", label: "Administration", items: adminItems },
        ...resultGroups,
        ...statusGroup
    ];
});
</script>
