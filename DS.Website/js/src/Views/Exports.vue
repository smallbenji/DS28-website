<template>
    <section class="section">
        <div class="container">
            <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>

            <p v-if="loading && exports.length === 0" role="status">Indlæser dataudtræk…</p>

            <p v-if="!loading && !error && exports.length === 0" class="empty-state">
                Der er ingen dataudtræk, du har adgang til.
            </p>

            <div v-for="item in exports" :key="item.key" class="box export-card">
                <div class="export-main">
                    <p class="title is-5 export-title">
                        <font-awesome-icon icon="file-arrow-down" />
                        {{ item.title }}
                    </p>
                    <p class="export-description">{{ item.description }}</p>
                </div>

                <BButton
                    type="is-primary"
                    :loading="downloading === item.key"
                    @click="download(item)"
                >
                    <font-awesome-icon icon="download" />
                    <span>Hent .xlsx</span>
                </BButton>
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { BButton, BNotification } from 'buefy';
import axios from 'axios';
import type { DataExportDto } from '@/types';

const exports = ref<DataExportDto[]>([]);
const loading = ref(false);
const downloading = ref('');
const error = ref('');

function fail(e: unknown, fallback: string) {
    error.value =
        axios.isAxiosError(e) && typeof e.response?.data === 'string'
            ? e.response.data
            : fallback;
}

async function load() {
    if (loading.value) return;

    loading.value = true;
    error.value = '';

    try {
        const { data } = await axios.get<DataExportDto[]>('/api/v1/exports');
        exports.value = data;
    } catch (e) {
        fail(e, 'Dataudtræk kunne ikke hentes. Kontrollér din adgang, og prøv igen.');
    } finally {
        loading.value = false;
    }
}

async function download(item: DataExportDto) {
    if (downloading.value) return;

    downloading.value = item.key;
    error.value = '';

    try {
        const response = await axios.get(`/api/v1/exports/${encodeURIComponent(item.key)}`, {
            responseType: 'blob'
        });

        const url = URL.createObjectURL(response.data);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${item.fileName}.xlsx`;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    } catch (e) {
        fail(e, 'Dataudtrækket kunne ikke hentes. Prøv igen.');
    } finally {
        downloading.value = '';
    }
}

onMounted(load);
</script>

<style scoped>
.export-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1.5rem;
    flex-wrap: wrap;
}

.export-main {
    flex: 1 1 20rem;
    min-width: 0;
}

.export-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.35rem;
}

.export-title svg {
    color: rgba(0, 0, 0, 0.4);
}

.export-description {
    color: rgba(0, 0, 0, 0.6);
}

.export-card .button svg {
    margin-right: 0.4rem;
}
</style>
