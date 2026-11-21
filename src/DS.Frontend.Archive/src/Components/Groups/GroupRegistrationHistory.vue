<template>
    <section class="panel">
        <div class="panel-heading history-heading">
            <h3>Tilmeldingstal</h3>
            <span class="history-year">Historik · 2024</span>
        </div>
        <div class="history-body">
            <p v-if="loading" role="status">Henter historik…</p>
            <BNotification v-else-if="failed" type="is-danger" :closable="false">
                Historikken kunne ikke hentes.
                <BButton size="is-small" @click="load">Prøv igen</BButton>
            </BNotification>
            <template v-else-if="history">
                <p v-if="!rows.length">Ingen historiske tilmeldinger registreret for 2024.</p>
                <template v-else>
                    <div class="history-toolbar">
                        <h4>Sammenligning · 2024</h4>
                        <BCheckbox v-model="showGuests" size="is-small">Venskabsgrupper</BCheckbox>
                    </div>
                    <BTable :data="rows" :mobile-cards="false" narrowed>
                        <BTableColumn field="category" label="Gren" v-slot="props">{{ categoryLabel(props.row.category) }}</BTableColumn>
                        <BTableColumn label="Forhånd" numeric v-slot="props">{{ count(props.row.presignup) }}</BTableColumn>
                        <BTableColumn :visible="showGuests" label="Venskab" numeric v-slot="props">{{ count(props.row.guests) }}</BTableColumn>
                        <BTableColumn label="Endelig" numeric v-slot="props">{{ count(props.row.signup) }}</BTableColumn>
                        <BTableColumn label="Ændring" numeric v-slot="props">{{ change(props.row.change) }}</BTableColumn>
                        <template #footer>
                            <th scope="row">I alt</th>
                            <td class="has-text-right">{{ count(totals.presignup) }}</td>
                            <td v-if="showGuests" class="has-text-right">{{ count(totals.guests) }}</td>
                            <td class="has-text-right">{{ count(totals.signup) }}</td>
                            <td class="has-text-right">{{ change(totals.change) }}</td>
                        </template>
                    </BTable>
                    <p v-if="hasUnknownCounts" class="history-note">— = ukendt. Totaler vises kun, når alle grene har tal.</p>
                    <details v-if="history.signups.length" class="history-attendance">
                        <summary>Fremmøde pr. dag <span>2024 · dag 1–8</span></summary>
                        <BTable :data="attendanceRows" :mobile-cards="false" narrowed>
                            <BTableColumn field="category" label="Gren" v-slot="props">{{ categoryLabel(props.row.category) }}</BTableColumn>
                            <BTableColumn v-for="day in 8" :key="day" :label="`Dag ${day}`" numeric v-slot="props">{{ count(props.row.days[day]) }}</BTableColumn>
                        </BTable>
                        <p class="history-note">Endelige tal er patruljesummer (dag 0). Dagsfremmøde vises separat. — = ukendt, 0 = registreret nul.</p>
                    </details>
                </template>
            </template>
        </div>
    </section>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { BButton, BCheckbox, BNotification, BTable, BTableColumn } from 'buefy';
import { apiGet } from '@/lib/api';
import { registrationHistoryRows, registrationHistoryTotals, type RegistrationHistory } from '@/lib/groupRegistrationHistory';

const props = defineProps<{ groupId: string | number }>();
const history = ref<RegistrationHistory | null>(null);
const loading = ref(true);
const failed = ref(false);
const showGuests = ref(false);
const labels: Record<string, string> = {
    family: 'Familiespejdere', beaver: 'Bævere', wolf: 'Ulve', junior: 'Juniorer',
    trop: 'Trop', senior: 'Seniorer', rover: 'Klan', leader: 'Ledere'
};
const categoryLabel = (category: string) => labels[category] ?? category;
const count = (value: number | null | undefined) => value == null ? '—' : value.toLocaleString('da-DK');
const change = (value: number | null) => value == null ? '—' : value > 0 ? `+${count(value)}` : value < 0 ? `−${count(Math.abs(value))}` : '0';
const rows = computed(() => history.value ? registrationHistoryRows(history.value) : []);
const totals = computed(() => registrationHistoryTotals(rows.value));
const hasUnknownCounts = computed(() => rows.value.some(row => row.presignup == null || row.signup == null));
const attendanceRows = computed(() => rows.value.filter(row => history.value?.signups.some(signup => signup.category === row.category)));

async function load() {
    loading.value = true;
    failed.value = false;
    try {
        history.value = await apiGet<RegistrationHistory>(`/api/v1/groups/${props.groupId}/registration-history`);
    } catch {
        failed.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>
<style scoped>
.history-heading,
.history-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 0.5rem;
}
.history-heading h3 {
    font: inherit;
}
.history-year {
    font-size: 0.8rem;
    font-weight: normal;
}
.history-body {
    padding: 0.75rem 1rem;
}
.history-toolbar {
    margin-bottom: 0.5rem;
}
.history-toolbar h4 {
    font-weight: 600;
}
.history-body :deep(table) {
    font-size: 0.85rem;
    font-variant-numeric: tabular-nums;
}
.history-body :deep(tfoot) {
    font-weight: 600;
    background: var(--bulma-scheme-main-bis);
}
.history-attendance {
    margin-top: 0.5rem;
}
.history-attendance summary {
    padding: 0.5rem 0;
    cursor: pointer;
}
.history-attendance summary span {
    margin-left: 0.5rem;
    font-size: 0.8rem;
}
.history-note {
    margin-top: 0.5rem;
    font-size: 0.8rem;
}
</style>
