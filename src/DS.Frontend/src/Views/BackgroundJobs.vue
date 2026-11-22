<template>
    <UDashboardPanel id="background-jobs">
        <template #header>
            <UDashboardNavbar title="Baggrundsjob">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/admin" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til admin" />
                </template>

                <template #right>
                    <UButton
                        color="neutral"
                        variant="ghost"
                        size="sm"
                        :aria-pressed="polling"
                        :aria-label="polling ? 'Sæt automatisk opdatering på pause' : 'Genoptag automatisk opdatering'"
                        @click="togglePolling"
                    >
                        <span class="size-2 rounded-full" :class="polling ? 'animate-pulse bg-success' : 'bg-muted'" />
                        <span class="hidden text-xs text-muted sm:inline">{{ polling ? "Opdateres hvert 5. sek." : "Opdatering på pause" }}</span>
                    </UButton>
                    <UButton href="/hangfire" target="_blank" color="neutral" variant="outline" icon="i-lucide-external-link" :label="isMobile ? undefined : 'Hangfire-dashboard'" aria-label="Åbn Hangfire-dashboard" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="flex flex-col gap-8">
                <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

                <section aria-label="Nøgletal">
                    <div v-if="!overview && loading" class="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-5">
                        <USkeleton v-for="index in 5" :key="index" class="h-28" />
                    </div>
                    <dl v-else-if="overview" class="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-5">
                        <UCard v-for="stat in stats" :key="stat.label">
                            <dt class="flex items-center gap-2 text-sm font-medium text-highlighted">
                                <UIcon :name="stat.icon" class="size-4" :class="[stat.color, { 'animate-spin': stat.spin }]" />
                                {{ stat.label }}
                            </dt>
                            <dd class="mt-1 text-3xl font-semibold tabular-nums" :class="stat.alert ? 'text-error' : 'text-highlighted'">{{ stat.value.toLocaleString("da-DK") }}</dd>
                            <p class="mt-1 text-xs text-muted">{{ stat.hint ?? "&nbsp;" }}</p>
                        </UCard>
                    </dl>
                </section>

                <div v-if="overview" class="grid gap-8 xl:grid-cols-[minmax(0,1fr)_22rem]">
                    <div class="flex min-w-0 flex-col gap-8">
                        <section>
                            <div class="mb-3 flex items-center gap-2">
                                <h2 class="flex items-center gap-2 text-sm font-semibold text-highlighted">
                                    <UIcon name="i-lucide-circle-alert" class="size-4 text-muted" />
                                    Fejlede jobs
                                </h2>
                                <UBadge v-if="overview.failed.length > 0" :label="String(overview.statistics.failed)" color="error" variant="soft" size="sm" />
                                <UButton
                                    v-if="overview.canManage && overview.failed.length > 1"
                                    class="ml-auto"
                                    size="xs"
                                    color="neutral"
                                    variant="ghost"
                                    icon="i-lucide-rotate-ccw"
                                    label="Prøv alle igen"
                                    :loading="busy === 'requeue-all'"
                                    @click="requeueAll"
                                />
                            </div>

                            <div class="divide-y divide-default overflow-hidden rounded-lg border border-default">
                                <UEmpty v-if="overview.failed.length === 0" icon="i-lucide-circle-check" title="Ingen fejlede jobs" description="Alt kører, som det skal." class="py-10" />

                                <div v-for="job in overview.failed" :key="job.id">
                                    <div class="flex items-start gap-3 px-4 py-3">
                                        <UButton
                                            color="neutral"
                                            variant="ghost"
                                            size="xs"
                                            icon="i-lucide-chevron-right"
                                            :aria-expanded="openFailed.has(job.id)"
                                            :aria-label="`Vis detaljer for ${job.title} ${job.id}`"
                                            :ui="{ leadingIcon: openFailed.has(job.id) ? 'rotate-90 transition-transform' : 'transition-transform' }"
                                            @click="toggleFailed(job.id)"
                                        />
                                        <button type="button" class="min-w-0 flex-1 text-left" @click="toggleFailed(job.id)">
                                            <p class="flex flex-wrap items-center gap-x-2 text-sm">
                                                <span class="font-medium text-highlighted">{{ job.title }}</span>
                                                <span class="font-mono text-xs text-muted">#{{ job.id }}</span>
                                            </p>
                                            <p class="truncate text-sm text-error">{{ errorLine(job.exceptionType, job.exceptionMessage) }}</p>
                                            <p class="text-xs text-muted">Fejlede {{ formatRelative(job.failedAt, now) }} · {{ job.attempts }} forsøg</p>
                                        </button>
                                        <div v-if="overview.canManage" class="flex shrink-0 gap-1">
                                            <UButton
                                                size="xs"
                                                color="neutral"
                                                variant="outline"
                                                icon="i-lucide-rotate-ccw"
                                                :label="isMobile ? undefined : 'Prøv igen'"
                                                :aria-label="`Prøv ${job.title} ${job.id} igen`"
                                                :loading="busy === `requeue-${job.id}`"
                                                @click="requeue(job.id, job.title)"
                                            />
                                            <UButton
                                                size="xs"
                                                color="error"
                                                variant="ghost"
                                                icon="i-lucide-trash-2"
                                                :aria-label="`Slet ${job.title} ${job.id}`"
                                                @click="askDelete(job.id, job.title)"
                                            />
                                        </div>
                                    </div>
                                    <div v-if="openFailed.has(job.id)" class="border-t border-default bg-elevated/30 px-4 py-4 sm:pl-14">
                                        <BackgroundJobDetails :job-id="job.id" />
                                    </div>
                                </div>
                            </div>
                        </section>

                        <section v-if="overview.recurring.length > 0">
                            <h2 class="mb-3 flex items-center gap-2 text-sm font-semibold text-highlighted">
                                <UIcon name="i-lucide-repeat" class="size-4 text-muted" />
                                Tilbagevendende jobs
                            </h2>
                            <div class="overflow-x-auto rounded-lg border border-default">
                                <table class="w-full text-sm">
                                    <thead class="bg-elevated/50 text-left text-xs text-muted">
                                        <tr>
                                            <th class="px-4 py-2.5 font-medium">Job</th>
                                            <th class="px-4 py-2.5 font-medium">Tidsplan</th>
                                            <th class="px-4 py-2.5 font-medium">Sidste kørsel</th>
                                            <th class="px-4 py-2.5 font-medium">Næste kørsel</th>
                                            <th v-if="overview.canManage" class="px-4 py-2.5"><span class="sr-only">Handlinger</span></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="job in overview.recurring" :key="job.id" class="border-t border-default">
                                            <td class="px-4 py-3">
                                                <p class="font-medium text-highlighted">{{ job.title }}</p>
                                                <p class="font-mono text-xs text-muted">{{ job.id }}</p>
                                            </td>
                                            <td class="px-4 py-3">
                                                <p class="whitespace-nowrap">{{ describeCron(job.cron, job.timeZoneId) }}</p>
                                                <p class="font-mono text-xs text-muted">{{ job.cron }}</p>
                                            </td>
                                            <td class="px-4 py-3">
                                                <template v-if="job.lastExecution">
                                                    <UBadge
                                                        v-if="job.lastJobState"
                                                        :label="jobState(job.lastJobState).label"
                                                        :icon="jobState(job.lastJobState).icon"
                                                        :color="recurringStateColor(job.lastJobState)"
                                                        variant="soft"
                                                        size="sm"
                                                    />
                                                    <p class="mt-1 text-xs whitespace-nowrap text-muted">{{ formatJobTime(job.lastExecution) }}</p>
                                                </template>
                                                <span v-else class="text-muted">Ikke kørt endnu</span>
                                            </td>
                                            <td class="px-4 py-3">
                                                <p class="whitespace-nowrap">{{ formatJobTime(job.nextExecution) }}</p>
                                                <p class="text-xs text-muted">{{ formatRelative(job.nextExecution, now) }}</p>
                                            </td>
                                            <td v-if="overview.canManage" class="px-4 py-3 text-right">
                                                <UButton
                                                    size="xs"
                                                    color="neutral"
                                                    variant="outline"
                                                    icon="i-lucide-play"
                                                    label="Kør nu"
                                                    class="whitespace-nowrap"
                                                    :loading="busy === `trigger-${job.id}`"
                                                    @click="trigger(job.id, job.title)"
                                                />
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </section>
                    </div>

                    <aside class="flex min-w-0 flex-col">
                        <h2 class="mb-3 flex items-center gap-2 text-sm font-semibold text-highlighted">
                            <UIcon name="i-lucide-activity" class="size-4 text-muted" />
                            Seneste aktivitet
                        </h2>
                        <ol class="flex flex-col divide-y divide-default rounded-lg border border-default">
                            <li v-if="overview.activity.length === 0" class="px-4 py-6 text-center text-sm text-muted">Ingen aktivitet endnu.</li>
                            <li v-for="item in overview.activity" :key="`${item.state}-${item.id}`" class="flex items-start gap-3 px-4 py-2.5">
                                <UIcon
                                    :name="jobState(item.state).icon"
                                    class="mt-0.5 size-4 shrink-0"
                                    :class="[jobState(item.state).color, { 'animate-spin': jobState(item.state).spin }]"
                                    :aria-label="jobState(item.state).label"
                                />
                                <div class="min-w-0 flex-1">
                                    <p class="truncate text-sm text-highlighted">{{ item.title }}</p>
                                    <p class="text-xs text-muted">{{ activityText(item) }}</p>
                                </div>
                                <span class="font-mono text-xs text-dimmed">#{{ item.id }}</span>
                            </li>
                        </ol>
                        <p class="mt-2 text-xs text-muted">Viser de seneste {{ overview.activity.length }} hændelser. Argumenter vises kun i jobdetaljer og er maskeret.</p>
                    </aside>
                </div>

                <BackgroundJobHistory ref="historyRef" />
            </div>

            <ConfirmModal
                v-model:open="deleteOpen"
                title="Slet job?"
                :description="deleteDescription"
                confirm-label="Slet job"
                color="error"
                :loading="busy === 'delete'"
                @confirm="confirmDelete"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useDocumentVisibility, useIntervalFn } from "@vueuse/core";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import BackgroundJobDetails from "@/Components/BackgroundJobs/BackgroundJobDetails.vue";
import BackgroundJobHistory from "@/Components/BackgroundJobs/BackgroundJobHistory.vue";
import BackgroundJobService from "@/Services/BackgroundJobService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import { useIsMobile } from "@/lib/useIsMobile";
import { describeCron, formatDuration, formatJobTime, formatRelative, jobState } from "@/lib/backgroundJobs";
import type { BackgroundJobActivityDto, BackgroundJobOverviewDto } from "@/types";

const pollIntervalMs = 5000;

const service = new BackgroundJobService();
const notify = useNotify();
const isMobile = useIsMobile();
const visibility = useDocumentVisibility();

const overview = ref<BackgroundJobOverviewDto | null>(null);
const loading = ref(true);
const error = ref("");
const busy = ref("");
const now = ref(Date.now());
const polling = ref(true);
const openFailed = reactive(new Set<string>());
const historyRef = ref<InstanceType<typeof BackgroundJobHistory> | null>(null);

const deleteOpen = ref(false);
const pendingDelete = ref<{ id: string; title: string } | null>(null);
const deleteDescription = computed(() => pendingDelete.value
    ? `${pendingDelete.value.title} #${pendingDelete.value.id} slettes og bliver ikke forsøgt igen.`
    : "");

const stats = computed(() => {
    const statistics = overview.value?.statistics;
    if (!statistics) return [];

    return [
        { label: "Kører", value: statistics.processing, icon: "i-lucide-loader", color: "text-info", spin: statistics.processing > 0 },
        { label: "I kø", value: statistics.enqueued, icon: "i-lucide-list-ordered", color: "text-muted" },
        { label: "Planlagt", value: statistics.scheduled, icon: "i-lucide-clock", color: "text-muted" },
        { label: "Fejlet", value: statistics.failed, icon: "i-lucide-circle-alert", color: "text-error", alert: statistics.failed > 0 },
        { label: "Gennemført", value: statistics.succeededLast24Hours, icon: "i-lucide-circle-check", color: "text-success", hint: "seneste 24 timer" }
    ];
});

async function load() {
    try {
        overview.value = await service.getOverview();
        error.value = "";
    } catch (e) {
        error.value = getErrorMessage(e, "Baggrundsjob kunne ikke hentes. Kontrollér din adgang, og prøv igen.");
    } finally {
        loading.value = false;
        now.value = Date.now();
    }
}

const { pause, resume } = useIntervalFn(() => {
    if (visibility.value === "visible") void load();
}, pollIntervalMs);

function togglePolling() {
    polling.value = !polling.value;
    if (polling.value) {
        resume();
        void load();
    } else {
        pause();
    }
}

function toggleFailed(id: string) {
    if (openFailed.has(id)) openFailed.delete(id);
    else openFailed.add(id);
}

function errorLine(type: string | null, message: string | null) {
    if (type && message) return `${type}: ${message}`;

    return type ?? message ?? "Ukendt fejl";
}

function activityText(item: BackgroundJobActivityDto) {
    const parts = [jobState(item.state).label];
    if (item.at) parts.push(formatRelative(item.at, now.value));
    if (item.state === "succeeded" && item.durationMs != null) parts.push(formatDuration(item.durationMs));

    return parts.join(" · ");
}

function recurringStateColor(state: string) {
    switch (state.toLowerCase()) {
        case "succeeded": return "success";
        case "failed": return "error";
        case "processing": return "info";
        default: return "neutral";
    }
}

async function refreshAll() {
    await load();
    await historyRef.value?.reload();
}

async function runAction(key: string, action: () => Promise<unknown>, success: string, failure: string) {
    if (busy.value) return;

    busy.value = key;
    try {
        await action();
        notify.success(success);
        await refreshAll();
    } catch (e) {
        notify.error(getErrorMessage(e, failure));
    } finally {
        busy.value = "";
    }
}

function requeue(id: string, title: string) {
    void runAction(`requeue-${id}`, () => service.requeue(id), `${title} #${id} er sat i kø igen`, "Jobbet kunne ikke sættes i kø igen.");
}

async function requeueAll() {
    if (busy.value) return;

    busy.value = "requeue-all";
    try {
        const count = await service.requeueFailed();
        notify.success(count === 1 ? "1 job er sat i kø igen" : `${count} jobs er sat i kø igen`);
        await refreshAll();
    } catch (e) {
        notify.error(getErrorMessage(e, "Jobbene kunne ikke sættes i kø igen."));
    } finally {
        busy.value = "";
    }
}

function askDelete(id: string, title: string) {
    pendingDelete.value = { id, title };
    deleteOpen.value = true;
}

async function confirmDelete() {
    const job = pendingDelete.value;
    if (!job) return;

    await runAction("delete", () => service.delete(job.id), `${job.title} #${job.id} er slettet`, "Jobbet kunne ikke slettes.");
    deleteOpen.value = false;
    pendingDelete.value = null;
}

function trigger(id: string, title: string) {
    void runAction(`trigger-${id}`, () => service.triggerRecurring(id), `${title} er startet`, "Jobbet kunne ikke startes.");
}

onMounted(load);
</script>
