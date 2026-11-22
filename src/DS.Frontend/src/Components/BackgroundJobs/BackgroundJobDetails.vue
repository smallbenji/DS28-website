<template>
    <div class="flex flex-col gap-2">
        <USkeleton v-if="loading" class="h-32" />

        <UAlert v-else-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

        <div v-else-if="details" class="grid gap-6 lg:grid-cols-2">
            <div class="min-w-0">
                <h3 class="mb-2 text-xs font-semibold tracking-wide text-muted uppercase">Detaljer</h3>
                <dl class="grid grid-cols-[6rem_minmax(0,1fr)] gap-y-1 text-sm">
                    <dt class="text-muted">Job-id</dt>
                    <dd class="font-mono">{{ details.id }}</dd>
                    <template v-if="details.method">
                        <dt class="text-muted">Metode</dt>
                        <dd class="truncate font-mono text-xs leading-5">{{ details.method }}</dd>
                    </template>
                    <dt class="text-muted">Kø</dt>
                    <dd>{{ details.queue }}</dd>
                    <dt class="text-muted">Oprettet</dt>
                    <dd>{{ formatJobDateTime(details.createdAt) }}</dd>
                    <template v-if="details.startedAt">
                        <dt class="text-muted">Startet</dt>
                        <dd>{{ formatJobDateTime(details.startedAt) }}</dd>
                    </template>
                    <template v-if="details.finishedAt">
                        <dt class="text-muted">{{ finishedLabel }}</dt>
                        <dd>{{ formatJobDateTime(details.finishedAt) }}</dd>
                    </template>
                    <template v-if="details.server">
                        <dt class="text-muted">Server</dt>
                        <dd class="truncate font-mono text-xs leading-5">{{ details.server }}</dd>
                    </template>
                    <dt class="text-muted">Forsøg</dt>
                    <dd>{{ details.attempts }}</dd>
                </dl>

                <h3 class="mt-5 mb-1 text-xs font-semibold tracking-wide text-muted uppercase">Argumenter</h3>
                <p v-if="details.argumentsUnavailable" class="text-sm text-muted">Argumenterne kunne ikke indlæses.</p>
                <p v-else-if="details.arguments.length === 0" class="text-sm text-muted">Ingen argumenter</p>
                <dl v-else>
                    <div v-for="argument in details.arguments" :key="argument.name" class="flex flex-wrap items-baseline gap-x-3 py-1">
                        <dt class="w-20 shrink-0 font-mono text-xs text-muted">{{ argument.name }}</dt>
                        <dd class="min-w-0 text-sm">
                            <span v-if="argument.hidden" class="inline-flex items-center gap-1 text-muted">
                                <UIcon name="i-lucide-eye-off" class="size-3.5" />
                                Skjult{{ argument.size != null ? ` (${formatSize(argument.size)})` : "" }}
                            </span>
                            <span v-else class="font-mono break-all text-highlighted">{{ argument.value }}</span>
                            <UBadge v-if="argument.masked" label="Maskeret" color="neutral" variant="soft" size="sm" class="ml-2" />
                        </dd>
                    </div>
                </dl>
            </div>

            <div class="min-w-0">
                <template v-if="details.attemptHistory.length > 0">
                    <h3 class="mb-2 text-xs font-semibold tracking-wide text-muted uppercase">Fejlede forsøg</h3>
                    <ol class="flex flex-col gap-1.5 text-sm">
                        <li v-for="attempt in visibleAttempts" :key="attempt.number" class="flex items-baseline gap-2">
                            <UIcon name="i-lucide-x" class="size-3.5 shrink-0 translate-y-0.5 text-error" />
                            <span class="w-8 shrink-0 text-muted tabular-nums">#{{ attempt.number }}</span>
                            <span class="w-28 shrink-0 tabular-nums">{{ formatJobTime(attempt.at) }}</span>
                            <span class="truncate text-muted" :title="attempt.text ?? ''">{{ attempt.text }}</span>
                        </li>
                        <li v-if="hiddenAttempts > 0" class="pl-6 text-xs text-muted">+ {{ hiddenAttempts }} tidligere forsøg</li>
                    </ol>
                </template>

                <template v-if="details.stackTrace">
                    <div class="mt-5 mb-2 flex items-center gap-2">
                        <h3 class="text-xs font-semibold tracking-wide text-muted uppercase">Stacktrace</h3>
                        <UButton class="ml-auto" size="xs" color="neutral" variant="ghost" icon="i-lucide-copy" label="Kopiér hele" @click="copyStackTrace" />
                    </div>
                    <pre class="overflow-auto rounded-md bg-inverted/95 p-3 font-mono text-[11px] leading-5 text-inverted" :class="showFullStack ? 'max-h-96' : 'max-h-40'">{{ stackText }}</pre>
                    <UButton v-if="shortStack.hiddenFrames > 0" size="xs" variant="link" class="px-0" :label="showFullStack ? 'Vis forkortet' : `Vis hele (${stackLineCount} linjer)`" @click="showFullStack = !showFullStack" />
                </template>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useClipboard } from "@vueuse/core";
import BackgroundJobService from "@/Services/BackgroundJobService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import { formatJobDateTime, formatJobTime, formatSize, shortenStackTrace } from "@/lib/backgroundJobs";
import type { BackgroundJobDetailsDto } from "@/types";

const props = defineProps<{ jobId: string }>();

const service = new BackgroundJobService();
const notify = useNotify();
const { copy } = useClipboard({ legacy: true });

const details = ref<BackgroundJobDetailsDto | null>(null);
const loading = ref(true);
const error = ref("");
const showFullStack = ref(false);

const maxAttempts = 3;
const visibleAttempts = computed(() => details.value?.attemptHistory.slice(0, maxAttempts) ?? []);
const hiddenAttempts = computed(() => Math.max(0, (details.value?.attemptHistory.length ?? 0) - maxAttempts));

const finishedLabel = computed(() => {
    switch (details.value?.state) {
        case "failed": return "Fejlede";
        case "deleted": return "Slettet";
        default: return "Afsluttet";
    }
});

const shortStack = computed(() => shortenStackTrace(details.value?.stackTrace ?? ""));
const stackLineCount = computed(() => (details.value?.stackTrace ?? "").split("\n").length);
const stackText = computed(() => {
    if (showFullStack.value) return details.value?.stackTrace ?? "";
    if (shortStack.value.hiddenFrames === 0) return shortStack.value.text;

    return `${shortStack.value.text}\n   … ${shortStack.value.hiddenFrames} rammer fra biblioteker skjult`;
});

async function load() {
    loading.value = true;
    error.value = "";
    try {
        details.value = await service.getDetails(props.jobId);
    } catch (e) {
        error.value = getErrorMessage(e, "Jobbets detaljer kunne ikke hentes.");
    } finally {
        loading.value = false;
    }
}

async function copyStackTrace() {
    try {
        await copy(details.value?.stackTrace ?? "");
        notify.success("Stacktrace kopieret");
    } catch {
        notify.error("Stacktrace kunne ikke kopieres.");
    }
}

onMounted(load);
</script>
