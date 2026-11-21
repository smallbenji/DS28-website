<template>
    <UDashboardPanel id="exports">
        <template #header>
            <UDashboardNavbar title="Dataudtræk">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/admin" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til admin" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto flex w-full max-w-3xl flex-col gap-4">
                <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

                <template v-if="loading">
                    <USkeleton v-for="index in 3" :key="index" class="h-20" />
                </template>

                <UEmpty v-else-if="!error && exports.length === 0" icon="i-lucide-file-down" title="Der er ingen dataudtræk, du har adgang til." />

                <UCard v-for="item in exports" :key="item.key">
                    <div class="flex flex-wrap items-center justify-between gap-4">
                        <div class="flex min-w-0 flex-1 items-start gap-3">
                            <div class="rounded-md bg-primary/10 p-2">
                                <UIcon name="i-lucide-file-spreadsheet" class="size-5 text-primary" />
                            </div>
                            <div class="min-w-0">
                                <h2 class="font-semibold text-highlighted">{{ item.title }}</h2>
                                <p class="text-sm text-muted">{{ item.description }}</p>
                            </div>
                        </div>
                        <UButton
                            icon="i-lucide-download"
                            label="Hent .xlsx"
                            :loading="downloading === item.key"
                            :disabled="downloading !== '' && downloading !== item.key"
                            :aria-label="`Hent ${item.title} som .xlsx`"
                            @click="download(item)"
                        />
                    </div>
                </UCard>
            </div>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import ExportService from "@/Services/ExportService";
import { getErrorMessage } from "@/lib/api";
import type { DataExportDto } from "@/types";

const service = new ExportService();

const exports = ref<DataExportDto[]>([]);
const loading = ref(true);
const downloading = ref("");
const error = ref("");

async function load() {
    loading.value = true;
    error.value = "";
    try {
        exports.value = await service.getExports();
    } catch (e) {
        error.value = getErrorMessage(e, "Dataudtræk kunne ikke hentes. Kontrollér din adgang, og prøv igen.");
    } finally {
        loading.value = false;
    }
}

async function download(item: DataExportDto) {
    if (downloading.value) return;

    downloading.value = item.key;
    error.value = "";
    try {
        const blob = await service.download(item.key);
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = `${item.fileName}.xlsx`;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    } catch (e) {
        error.value = getErrorMessage(e, "Dataudtrækket kunne ikke hentes. Prøv igen.");
    } finally {
        downloading.value = "";
    }
}

onMounted(load);
</script>
