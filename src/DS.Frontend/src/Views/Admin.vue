<template>
    <UDashboardPanel id="admin">
        <template #header>
            <UDashboardNavbar title="Admin">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

            <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
                <aside class="flex flex-col gap-4 lg:order-2">
                    <h2 class="flex items-center gap-2 text-sm font-semibold text-highlighted">
                        <UIcon name="i-lucide-chart-column" class="size-4 text-muted" />
                        Tilmeldingsstatistik
                    </h2>

                    <template v-if="loading && signupProgress.length === 0">
                        <USkeleton v-for="index in 3" :key="index" class="h-28" />
                    </template>

                    <UCard v-for="item in signupProgress" :key="item.key">
                        <p class="text-sm font-medium text-highlighted">{{ item.label }}</p>
                        <p class="mt-1 text-3xl font-semibold text-highlighted tabular-nums">
                            {{ item.current }}
                            <span class="text-base font-normal text-muted">af {{ item.target }}</span>
                        </p>
                        <UProgress
                            class="mt-3"
                            size="sm"
                            :model-value="Math.min(item.current, item.target)"
                            :max="item.target"
                            :color="progressColor(item)"
                            :aria-label="item.label"
                            :aria-valuetext="`${item.current} af ${item.target}. ${statusText(item)}`"
                        />
                        <p class="mt-2 text-xs text-muted">{{ statusText(item) }}</p>
                    </UCard>

                    <p v-if="signupProgress.length > 0" class="text-xs text-muted">Måltallet er deltagerantallet ved sidste lejr.</p>

                    <h2 class="mt-2 flex items-center gap-2 text-sm font-semibold text-highlighted">
                        <UIcon name="i-lucide-database" class="size-4 text-muted" />
                        I systemet
                    </h2>

                    <USkeleton v-if="loading && !systemCounts" class="h-20" />

                    <UCard v-else-if="systemCounts">
                        <dl class="grid grid-cols-2 gap-4">
                            <div>
                                <dt class="text-sm font-medium text-highlighted">Brugere</dt>
                                <dd class="mt-1 text-3xl font-semibold text-highlighted tabular-nums">{{ systemCounts.users }}</dd>
                            </div>
                            <div>
                                <dt class="text-sm font-medium text-highlighted">Grupper</dt>
                                <dd class="mt-1 text-3xl font-semibold text-highlighted tabular-nums">{{ systemCounts.groups }}</dd>
                            </div>
                        </dl>
                    </UCard>
                </aside>

                <div class="flex min-w-0 flex-col gap-8">
                    <div v-if="loading && sections.length === 0" class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                        <USkeleton v-for="index in 6" :key="index" class="h-20" />
                    </div>

                    <section v-for="section in sections" :key="section.title">
                        <h2 class="mb-3 flex items-center gap-2 text-sm font-semibold text-highlighted">
                            <UIcon :name="lucideIcon(section.icon)" class="size-4 text-muted" />
                            {{ section.title }}
                        </h2>
                        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                            <ShortcutCard v-for="shortcut in section.shortcuts" :key="shortcut.to" :shortcut="shortcut" />
                        </div>
                    </section>
                </div>
            </div>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import AdminService from "@/Services/AdminService";
import { getErrorMessage } from "@/lib/api";
import { lucideIcon, toHqShortcut } from "@/lib/hqShortcuts";
import ShortcutCard from "@/Components/ShortcutCard.vue";
import type { AdminSectionDto, SignupProgressDto, SystemCountsDto } from "@/types";

type ProgressColor = "success" | "warning" | "error";

const service = new AdminService();

const rawSections = ref<AdminSectionDto[]>([]);
const signupProgress = ref<SignupProgressDto[]>([]);
const systemCounts = ref<SystemCountsDto | null>(null);
const loading = ref(true);
const error = ref("");

const sections = computed(() => rawSections.value
    .filter((section) => section.entries.length > 0)
    .map((section) => ({
        title: section.title,
        icon: section.icon,
        shortcuts: section.entries.map(toHqShortcut)
    })));

function percent(item: SignupProgressDto): number {
    if (item.target <= 0) return 0;

    return Math.round((item.current / item.target) * 100);
}

function progressColor(item: SignupProgressDto): ProgressColor {
    if (item.status === "reached") return "success";
    if (item.status === "halfway") return "warning";

    return "error";
}

function statusText(item: SignupProgressDto): string {
    if (item.status === "reached") return "Målet er nået";
    if (item.status === "halfway") return `Halvejs (${percent(item)} %)`;

    return `Under halvejs (${percent(item)} %)`;
}

async function load() {
    loading.value = true;
    error.value = "";

    try {
        const data = await service.get();
        rawSections.value = data.sections;
        signupProgress.value = await service.getSignupProgress();
        systemCounts.value = await service.getSystemCounts();
    } catch (e) {
        error.value = getErrorMessage(e, "Admin kunne ikke hentes. Kontrollér din adgang, og prøv igen.");
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>
