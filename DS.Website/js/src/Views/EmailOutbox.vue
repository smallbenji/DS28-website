<template>
    <section class="section">
        <div class="container">
            <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>

            <div class="level">
                <div class="level-left">
                    <div class="level-item">
                        <BTag type="is-info is-light">{{ page.pendingCount }} i kø</BTag>
                    </div>
                    <div class="level-item">
                        <BTag type="is-warning is-light">{{ page.lockedCount }} låste</BTag>
                    </div>
                    <div class="level-item">
                        <BTag type="is-danger is-light">{{ page.failedCount }} fejlede</BTag>
                    </div>
                </div>
                <div class="level-right">
                    <div class="level-item">
                        <BSwitch v-model="autoRefresh">Opdatér automatisk</BSwitch>
                    </div>
                    <div class="level-item">
                        <BButton size="is-small" :loading="loading" @click="load">Opdatér</BButton>
                    </div>
                </div>
            </div>

            <div class="box">
                <BField label="Vis">
                    <BSelect v-model="status" @change="reload">
                        <option value="">Alle</option>
                        <option value="pending">I kø</option>
                        <option value="locked">Låste</option>
                        <option value="failed">Fejlede</option>
                        <option value="sent">Sendte</option>
                    </BSelect>
                </BField>
            </div>

            <p v-if="loading && page.items.length === 0" role="status">Henter mailkøen…</p>

            <template v-else>
                <BTable :data="page.items" :mobile-cards="true" :expanded="expanded" @row-expanded="onRowExpanded">
                    <BTableColumn field="id" label="ID" width="64" v-slot="props">{{ props.row.id }}</BTableColumn>
                    <BTableColumn label="Status" width="130" v-slot="props">
                        <span class="tag" :class="statusClass(props.row)">{{ statusLabel(props.row) }}</span>
                    </BTableColumn>
                    <BTableColumn field="eventType" label="Hændelse" v-slot="props">
                        {{ props.row.eventType || '—' }}
                    </BTableColumn>
                    <BTableColumn field="toEmail" label="Modtager" v-slot="props">{{ props.row.toEmail }}</BTableColumn>
                    <BTableColumn field="subject" label="Emne" v-slot="props">{{ props.row.subject }}</BTableColumn>
                    <BTableColumn field="createdAt" label="Oprettet" width="150" v-slot="props">
                        {{ formatDateTime(props.row.createdAt) }}
                    </BTableColumn>
                    <BTableColumn label="Forsøg" width="80" centered v-slot="props">{{ props.row.attempts }}</BTableColumn>

                    <BTableColumn v-slot="props" width="56">
                        <BButton size="is-small" type="is-light" @click="toggle(props.row)">
                            {{ isExpanded(props.row) ? 'Luk' : 'Vis' }}
                        </BButton>
                    </BTableColumn>

                    <template #row-details="props">
                        <tr>
                            <td :colspan="7">
                                <p v-if="props.row.sentAt"><strong>Sendt:</strong> {{ formatDateTime(props.row.sentAt) }}</p>
                                <p v-if="props.row.nextAttemptAt">
                                    <strong>Næste forsøg:</strong> {{ formatDateTime(props.row.nextAttemptAt) }}
                                </p>
                                <p v-if="props.row.failedAt">
                                    <strong>Opgivet:</strong> {{ formatDateTime(props.row.failedAt) }}
                                </p>
                                <p v-if="props.row.lockedAt">
                                    <strong>Låst af:</strong> {{ props.row.lockedBy }} kl. {{ formatDateTime(props.row.lockedAt) }}
                                </p>
                                <p v-if="props.row.correlationId">
                                    <strong>Korrelations-id:</strong> {{ props.row.correlationId }}
                                </p>
                                <p v-if="props.row.lastError" class="has-text-danger">
                                    <strong>Fejl:</strong> {{ props.row.lastError }}
                                </p>
                                <pre class="mail-body">{{ props.row.body }}</pre>
                            </td>
                        </tr>
                    </template>
                </BTable>

                <p v-if="page.items.length === 0" class="empty-state">Ingen mails i køen.</p>

                <BPagination
                    v-if="page.total > pageSize"
                    :total="page.total"
                    :current="pageNumber"
                    :page-size="pageSize"
                    @change="onPageChange"
                />
            </template>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import {
    BButton,
    BField,
    BNotification,
    BPagination,
    BSelect,
    BSwitch,
    BTable,
    BTableColumn,
    BTag
} from 'buefy';
import axios from 'axios';
import type { EmailOutboxDto, EmailOutboxPageDto } from '@/types';

const pageSize = 50;
const autoRefreshInterval = 15000;

const emptyPage = (): EmailOutboxPageDto => ({
    items: [],
    total: 0,
    pendingCount: 0,
    lockedCount: 0,
    failedCount: 0
});

const page = ref<EmailOutboxPageDto>(emptyPage());
const status = ref('');
const pageNumber = ref(1);
const loading = ref(false);
const autoRefresh = ref(false);
const expanded = ref<EmailOutboxDto[]>([]);
const error = ref('');

let timer: ReturnType<typeof setInterval> | null = null;

const pageCount = computed(() => Math.max(1, Math.ceil(page.value.total / pageSize)));

function fail(e: unknown) {
    error.value =
        axios.isAxiosError(e) && typeof e.response?.data === 'string'
            ? e.response.data
            : 'Mailkøen kunne ikke hentes. Kontrollér din adgang, og prøv igen.';
}

function formatDateTime(value: string) {
    return new Date(value).toLocaleString('da-DK', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function statusLabel(row: EmailOutboxDto) {
    if (row.failedAt) return 'Fejlet';
    if (row.sentAt) return 'Sendt';
    if (row.lockedAt) return 'Låst';
    return 'I kø';
}

function statusClass(row: EmailOutboxDto) {
    if (row.failedAt) return 'is-danger is-light';
    if (row.sentAt) return 'is-success is-light';
    if (row.lockedAt) return 'is-warning is-light';
    return 'is-info is-light';
}

function isExpanded(row: EmailOutboxDto) {
    return expanded.value.some(r => r.id === row.id);
}

function toggle(row: EmailOutboxDto) {
    expanded.value = isExpanded(row)
        ? expanded.value.filter(r => r.id !== row.id)
        : [...expanded.value, row];
}

function onRowExpanded(row: EmailOutboxDto, value: boolean) {
    if (value && !isExpanded(row)) expanded.value = [...expanded.value, row];
    if (!value) expanded.value = expanded.value.filter(r => r.id !== row.id);
}

function onPageChange(value: number) {
    pageNumber.value = value;
    load();
}

function reload() {
    pageNumber.value = 1;
    load();
}

async function load() {
    if (loading.value) return;

    loading.value = true;
    error.value = '';

    try {
        const { data } = await axios.get<EmailOutboxPageDto>('/api/v1/email-outbox', {
            params: {
                status: status.value || undefined,
                page: pageNumber.value,
                pageSize
            }
        });

        page.value = data;

        if (pageNumber.value > pageCount.value) {
            pageNumber.value = pageCount.value;
        }
    } catch (e) {
        fail(e);
    } finally {
        loading.value = false;
    }
}

function syncTimer() {
    if (timer) {
        clearInterval(timer);
        timer = null;
    }

    if (autoRefresh.value) {
        timer = setInterval(load, autoRefreshInterval);
    }
}

function onAutoRefreshChange() {
    syncTimer();
    if (autoRefresh.value) load();
}

watch(autoRefresh, onAutoRefreshChange);

onMounted(() => {
    load();
});

onUnmounted(() => {
    if (timer) clearInterval(timer);
});
</script>

<style scoped>
.mail-body {
    white-space: pre-wrap;
    word-break: break-word;
    background: #f5f5f5;
    padding: 0.75rem;
    margin-top: 0.5rem;
    max-height: 20rem;
    overflow: auto;
}
</style>
