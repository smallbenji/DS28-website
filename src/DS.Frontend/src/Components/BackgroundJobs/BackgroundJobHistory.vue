<template>
    <section>
        <div class="mb-3 flex flex-wrap items-center gap-x-3 gap-y-2">
            <h2 class="flex items-center gap-2 text-sm font-semibold text-highlighted">
                <UIcon name="i-lucide-history" class="size-4 text-muted" />
                Jobhistorik
            </h2>
            <UFieldGroup size="sm">
                <UButton
                    v-for="option in stateOptions"
                    :key="option.value"
                    :label="option.label"
                    :color="query.state === option.value ? 'primary' : 'neutral'"
                    :variant="query.state === option.value ? 'solid' : 'outline'"
                    @click="setState(option.value)"
                />
            </UFieldGroup>
            <div class="ml-auto flex flex-wrap items-center gap-2">
                <USelect v-model="typeFilter" :items="typeItems" size="sm" class="w-60" aria-label="Jobtype" />
                <UInput v-model="search" icon="i-lucide-search" placeholder="Job-id" size="sm" class="w-32" aria-label="Søg på job-id" />
            </div>
        </div>

        <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" class="mb-3" :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

        <div class="overflow-x-auto rounded-lg border border-default">
            <table class="w-full text-sm">
                <thead class="bg-elevated/50 text-left text-xs text-muted">
                    <tr>
                        <th class="w-8 px-2 py-2.5"><span class="sr-only">Vis detaljer</span></th>
                        <th class="px-2 py-2.5 font-medium">Job</th>
                        <th class="px-4 py-2.5 font-medium">Status</th>
                        <th class="px-4 py-2.5 font-medium">Afsluttet</th>
                        <th class="px-4 py-2.5 text-right font-medium">Varighed</th>
                        <th class="px-4 py-2.5 text-right font-medium">Job-id</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-if="loading && !history">
                        <td colspan="6" class="p-4"><USkeleton class="h-24" /></td>
                    </tr>
                    <tr v-else-if="history && history.items.length === 0">
                        <td colspan="6" class="px-4 py-10 text-center text-sm text-muted">Ingen jobs matcher filtrene.</td>
                    </tr>
                    <template v-for="item in history?.items ?? []" :key="item.id">
                        <tr class="cursor-pointer border-t border-default hover:bg-elevated/50" @click="toggle(item.id)">
                            <td class="px-2 py-2.5 text-muted">
                                <UButton
                                    color="neutral"
                                    variant="ghost"
                                    size="xs"
                                    icon="i-lucide-chevron-right"
                                    :aria-expanded="open.has(item.id)"
                                    :aria-label="`Vis detaljer for job ${item.id}`"
                                    :ui="{ leadingIcon: open.has(item.id) ? 'rotate-90 transition-transform' : 'transition-transform' }"
                                    @click.stop="toggle(item.id)"
                                />
                            </td>
                            <td class="px-2 py-2.5">
                                <span class="text-highlighted">{{ item.title }}</span>
                                <UBadge v-if="item.attempts > 1" :label="`${item.attempts} forsøg`" color="warning" variant="soft" size="sm" class="ml-2" />
                            </td>
                            <td class="px-4 py-2.5 whitespace-nowrap">
                                <span class="inline-flex items-center gap-1.5 text-xs">
                                    <UIcon :name="jobState(item.state).icon" class="size-3.5" :class="jobState(item.state).color" />
                                    {{ jobState(item.state).label }}
                                </span>
                            </td>
                            <td class="px-4 py-2.5 whitespace-nowrap">{{ formatJobTime(item.finishedAt) }}</td>
                            <td class="px-4 py-2.5 text-right whitespace-nowrap tabular-nums">{{ item.state === "succeeded" ? formatDuration(item.durationMs) : "-" }}</td>
                            <td class="px-4 py-2.5 text-right font-mono text-xs text-muted">#{{ item.id }}</td>
                        </tr>
                        <tr v-if="open.has(item.id)" class="bg-elevated/30">
                            <td />
                            <td colspan="5" class="px-2 pt-1 pb-4">
                                <BackgroundJobDetails :job-id="item.id" />
                            </td>
                        </tr>
                    </template>
                </tbody>
            </table>
        </div>

        <div class="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted">
            <span v-if="history">{{ rangeText }} · Afsluttede jobs gemmes i 24 timer</span>
            <div class="ml-auto flex gap-1">
                <UButton size="xs" color="neutral" variant="outline" icon="i-lucide-chevron-left" label="Forrige" :disabled="!history || history.page === 0" @click="goTo(query.page - 1)" />
                <UButton size="xs" color="neutral" variant="outline" trailing-icon="i-lucide-chevron-right" label="Næste" :disabled="!history || !hasNext" @click="goTo(query.page + 1)" />
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";
import { watchDebounced } from "@vueuse/core";
import BackgroundJobDetails from "@/Components/BackgroundJobs/BackgroundJobDetails.vue";
import BackgroundJobService from "@/Services/BackgroundJobService";
import { getErrorMessage } from "@/lib/api";
import { formatDuration, formatJobTime, jobState } from "@/lib/backgroundJobs";
import type { BackgroundJobHistoryDto, BackgroundJobHistoryQuery } from "@/types";

const allTypes = "all";

const service = new BackgroundJobService();

const stateOptions = [
    { value: "all", label: "Alle" },
    { value: "succeeded", label: "Gennemført" },
    { value: "failed", label: "Fejlet" },
    { value: "deleted", label: "Slettet" }
];

const query = reactive<BackgroundJobHistoryQuery>({ state: "all", type: "", search: "", page: 0 });
const typeFilter = ref(allTypes);
const search = ref("");
const history = ref<BackgroundJobHistoryDto | null>(null);
const loading = ref(true);
const error = ref("");
const open = reactive(new Set<string>());

const typeItems = computed(() => [
    { label: "Alle jobtyper", value: allTypes },
    ...(history.value?.types ?? []).map(type => ({ label: type, value: type }))
]);

const hasNext = computed(() => history.value != null && (history.value.page + 1) * history.value.pageSize < history.value.total);

const rangeText = computed(() => {
    if (!history.value || history.value.total === 0) return "0 jobs";

    const from = history.value.page * history.value.pageSize + 1;
    const to = Math.min(history.value.total, from + history.value.items.length - 1);

    return `${from}-${to} af ${history.value.total.toLocaleString("da-DK")} jobs`;
});

async function load() {
    loading.value = true;
    error.value = "";
    try {
        history.value = await service.getHistory(query);
        query.page = history.value.page;
    } catch (e) {
        error.value = getErrorMessage(e, "Jobhistorikken kunne ikke hentes.");
    } finally {
        loading.value = false;
    }
}

function setState(state: string) {
    query.state = state;
    query.page = 0;
    open.clear();
    void load();
}

function goTo(page: number) {
    query.page = page;
    open.clear();
    void load();
}

function toggle(id: string) {
    if (open.has(id)) open.delete(id);
    else open.add(id);
}

watch(typeFilter, value => {
    query.type = value === allTypes ? "" : value;
    query.page = 0;
    open.clear();
    void load();
});

watchDebounced(search, value => {
    query.search = value.trim();
    query.page = 0;
    open.clear();
    void load();
}, { debounce: 300 });

onMounted(load);

defineExpose({ reload: load });
</script>
