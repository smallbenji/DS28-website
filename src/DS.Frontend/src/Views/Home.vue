<template>
    <UDashboardPanel id="home">
        <template #header>
            <UDashboardNavbar title="HQ">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto w-full max-w-5xl">
                <h1 class="text-2xl font-semibold text-highlighted">Hej {{ firstName }}</h1>
                <p class="mt-1 text-muted">Her er dine genveje i HQ.</p>

                <div v-if="loading" class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    <USkeleton v-for="index in 6" :key="index" class="h-24" />
                </div>

                <UAlert v-else-if="loadFailed" class="mt-6" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Genvejene kunne ikke hentes" description="Prøv at genindlæse siden." />

                <UEmpty v-else-if="shortcuts.length === 0" class="mt-6" icon="i-lucide-layout-grid" title="Du har ingen genveje endnu" />

                <div v-else class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    <ShortcutCard v-for="shortcut in shortcuts" :key="shortcut.to" :shortcut="shortcut" />
                </div>

                <p v-if="Version.version" class="mt-8 text-center text-sm text-muted">{{ Version.version }}</p>
            </div>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { storeToRefs } from "pinia";
import { useMeStore } from "@/Stores/MeStore";
import { useVersionStore } from "@/Stores/VersionStore";
import { toHqShortcut } from "@/lib/hqShortcuts";
import ShortcutCard from "@/Components/ShortcutCard.vue";

const meStore = useMeStore();
const versionStore = useVersionStore();
const { Me, Hq } = storeToRefs(meStore);
const { Version } = storeToRefs(versionStore);

const loading = ref(true);
const loadFailed = ref(false);

onMounted(async () => {
    try {
        await Promise.all([meStore.GET_HQ(), versionStore.GET_VERSION()]);
    } catch {
        loadFailed.value = true;
    } finally {
        loading.value = false;
    }
});

const firstName = computed(() => Me.value?.name.split(" ")[0] ?? "");
const shortcuts = computed(() => Hq.value.shortcuts.map(toHqShortcut));
</script>
