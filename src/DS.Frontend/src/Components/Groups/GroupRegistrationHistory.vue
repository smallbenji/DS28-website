<template>
    <section class="panel">
        <p class="panel-heading">Tilmeldingstal</p>
        <div class="history-body">
            <p v-if="loading">Henter historik…</p>
            <BNotification v-else-if="failed" type="is-danger" :closable="false">
                Historikken kunne ikke hentes.
                <BButton size="is-small" @click="load">Prøv igen</BButton>
            </BNotification>
            <template v-else-if="history">
                <h3 class="title is-6">Nuværende forhåndstilmelding</h3>
                <p v-if="!history.currentPresignup">Ingen nuværende forhåndstilmelding registreret.</p>
                <BTable v-else :data="currentRows">
                    <BTableColumn field="category" label="Gren" v-slot="props">{{ categoryLabel(props.row.category) }}</BTableColumn>
                    <BTableColumn field="participantCount" label="Deltagere" numeric v-slot="props">{{ props.row.participantCount }}</BTableColumn>
                </BTable>
                <h3 class="title is-6 mt-5">Forhåndstilmelding · 2024</h3>
                <p v-if="!history.presignups.length">Ingen historisk forhåndstilmelding registreret.</p>
                <BTable v-else :data="history.presignups">
                    <BTableColumn field="category" label="Gren" v-slot="props">{{ categoryLabel(props.row.category) }}</BTableColumn>
                    <BTableColumn field="participantCount" label="Deltagere" numeric v-slot="props">{{ props.row.participantCount }}</BTableColumn>
                    <BTableColumn field="guestCount" label="Venskabsgruppe" numeric v-slot="props">{{ props.row.guestCount }}</BTableColumn>
                </BTable>
                <h3 class="title is-6 mt-5">Endelig tilmelding · 2024</h3>
                <p v-if="!history.signups.length">Ingen historisk endelig tilmelding registreret.</p>
                <template v-else>
                    <p class="mb-3">Dag 0 er patruljesummer, ikke en fremmødedag. — betyder ukendt fremmøde; 0 er et registreret nul.</p>
                    <BTable :data="signupRows">
                        <BTableColumn field="category" label="Gren" v-slot="props">{{ categoryLabel(props.row.category) }}</BTableColumn>
                        <BTableColumn label="Dag 0" numeric v-slot="props">{{ props.row.days[0] ?? '—' }}</BTableColumn>
                        <BTableColumn v-for="day in 8" :key="day" :label="`Dag ${day}`" numeric v-slot="props">{{ props.row.days[day] ?? '—' }}</BTableColumn>
                    </BTable>
                </template>
            </template>
        </div>
    </section>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { BButton, BNotification, BTable, BTableColumn } from 'buefy';
import { apiGet } from '@/lib/api';

interface RegistrationHistory {
    currentPresignup: Record<string, number> | null;
    presignups: { campYear: number; category: string; participantCount: number; guestCount: number }[];
    signups: { campYear: number; category: string; dayIndex: number; participantCount: number }[];
}

const props = defineProps<{ groupId: string | number }>();
const history = ref<RegistrationHistory | null>(null);
const loading = ref(true);
const failed = ref(false);
const labels: Record<string, string> = {
    family: 'Familiespejdere', beaver: 'Bævere', wolf: 'Ulve', junior: 'Juniorer',
    trop: 'Trop', senior: 'Seniorer', rover: 'Klan', leader: 'Ledere'
};
const categoryLabel = (category: string) => labels[category] ?? category;
const currentRows = computed(() => Object.entries(history.value?.currentPresignup ?? {})
    .map(([category, participantCount]) => ({ category, participantCount })));
const signupRows = computed(() => {
    const rows = new Map<string, { category: string; days: Record<number, number> }>();
    for (const signup of history.value?.signups ?? []) {
        let row = rows.get(signup.category);
        if (!row) {
            row = { category: signup.category, days: {} };
            rows.set(signup.category, row);
        }
        row.days[signup.dayIndex] = signup.participantCount;
    }
    return [...rows.values()];
});

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
.history-body {
    padding: 1rem;
}
</style>
