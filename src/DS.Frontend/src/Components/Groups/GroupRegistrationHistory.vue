<template>
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
        <template #header>
            <div class="flex items-center justify-between gap-2">
                <h3 class="font-semibold text-highlighted">Tilmeldingstal</h3>
                <span class="text-xs text-muted">Historik 2024</span>
            </div>
        </template>

        <div v-if="loading" class="flex flex-col gap-2 p-4 sm:p-6">
            <USkeleton v-for="index in 4" :key="index" class="h-6" />
        </div>

        <div v-else-if="failed" class="p-4 sm:p-6">
            <UAlert color="error" variant="subtle" title="Historikken kunne ikke hentes." :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />
        </div>

        <p v-else-if="rows.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen historiske tilmeldinger registreret for 2024.</p>

        <template v-else>
            <div class="flex items-center justify-between gap-2 px-4 py-2 sm:px-6">
                <span class="text-sm font-medium">Sammenligning 2024</span>
                <USwitch v-model="showGuests" label="Venskabsgrupper" size="sm" />
            </div>
            <div class="overflow-x-auto">
                <table class="w-full text-sm">
                    <thead class="border-y border-default text-left text-xs text-muted">
                        <tr>
                            <th scope="col" class="px-4 py-2 font-medium sm:px-6">Gren</th>
                            <th scope="col" class="px-2 py-2 text-right font-medium">Forhånd</th>
                            <th v-if="showGuests" scope="col" class="px-2 py-2 text-right font-medium">Venskab</th>
                            <th scope="col" class="px-2 py-2 text-right font-medium">Endelig</th>
                            <th scope="col" class="px-4 py-2 text-right font-medium sm:px-6">Ændring</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-default tabular-nums">
                        <tr v-for="row in rows" :key="row.category">
                            <th scope="row" class="px-4 py-2 text-left font-normal sm:px-6">{{ categoryLabel(row.category) }}</th>
                            <td class="px-2 py-2 text-right">{{ count(row.presignup) }}</td>
                            <td v-if="showGuests" class="px-2 py-2 text-right">{{ count(row.guests) }}</td>
                            <td class="px-2 py-2 text-right">{{ count(row.signup) }}</td>
                            <td class="px-4 py-2 text-right sm:px-6" :class="changeClass(row.change)">{{ change(row.change) }}</td>
                        </tr>
                    </tbody>
                    <tfoot class="border-t border-default font-semibold tabular-nums">
                        <tr>
                            <th scope="row" class="px-4 py-2 text-left sm:px-6">I alt</th>
                            <td class="px-2 py-2 text-right">{{ count(totals.presignup) }}</td>
                            <td v-if="showGuests" class="px-2 py-2 text-right">{{ count(totals.guests) }}</td>
                            <td class="px-2 py-2 text-right">{{ count(totals.signup) }}</td>
                            <td class="px-4 py-2 text-right sm:px-6" :class="changeClass(totals.change)">{{ change(totals.change) }}</td>
                        </tr>
                    </tfoot>
                </table>
            </div>
            <p v-if="hasUnknownCounts" class="px-4 py-2 text-xs text-muted sm:px-6">- betyder ukendt. Totaler vises kun, når alle grene har tal.</p>

            <UCollapsible v-if="attendanceRows.length" class="border-t border-default">
                <UButton color="neutral" variant="ghost" block class="group justify-between rounded-none px-4 sm:px-6" trailing-icon="i-lucide-chevron-down" :ui="{ trailingIcon: 'transition-transform group-data-[state=open]:rotate-180' }" label="Fremmøde pr. dag (dag 1-8)" />
                <template #content>
                    <div class="overflow-x-auto">
                        <table class="w-full text-sm">
                            <thead class="border-y border-default text-left text-xs text-muted">
                                <tr>
                                    <th scope="col" class="px-4 py-2 font-medium sm:px-6">Gren</th>
                                    <th v-for="day in 8" :key="day" scope="col" class="whitespace-nowrap px-2 py-2 text-right font-medium">Dag {{ day }}</th>
                                </tr>
                            </thead>
                            <tbody class="divide-y divide-default tabular-nums">
                                <tr v-for="row in attendanceRows" :key="row.category">
                                    <th scope="row" class="px-4 py-2 text-left font-normal sm:px-6">{{ categoryLabel(row.category) }}</th>
                                    <td v-for="day in 8" :key="day" class="px-2 py-2 text-right">{{ count(row.days[day]) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    <p class="px-4 py-2 text-xs text-muted sm:px-6">Endelige tal er patruljesummer (dag 0). - betyder ukendt, 0 er et registreret nul.</p>
                </template>
            </UCollapsible>
        </template>
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { apiGet } from "@/lib/api";
import { registrationHistoryRows, registrationHistoryTotals, type RegistrationHistory } from "@/lib/groupRegistrationHistory";

const props = defineProps<{ groupId: string }>();

const history = ref<RegistrationHistory | null>(null);
const loading = ref(true);
const failed = ref(false);
const showGuests = ref(false);

const labels: Record<string, string> = {
    family: "Familiespejdere",
    beaver: "Bævere",
    wolf: "Ulve",
    junior: "Juniorer",
    trop: "Trop",
    senior: "Seniorer",
    rover: "Klan",
    leader: "Ledere"
};

function categoryLabel(category: string): string {
    return labels[category] ?? category;
}

function count(value: number | null | undefined): string {
    return value == null ? "-" : value.toLocaleString("da-DK");
}

function change(value: number | null): string {
    if (value == null) return "-";
    if (value > 0) return `+${count(value)}`;
    if (value < 0) return `−${count(Math.abs(value))}`;

    return "0";
}

function changeClass(value: number | null): string {
    if (value == null || value === 0) return "";

    return value > 0 ? "text-success" : "text-error";
}

const rows = computed(() => history.value ? registrationHistoryRows(history.value) : []);
const totals = computed(() => registrationHistoryTotals(rows.value));
const hasUnknownCounts = computed(() => rows.value.some((row) => row.presignup == null || row.signup == null));
const attendanceRows = computed(() => rows.value.filter((row) => history.value?.signups.some((signup) => signup.category === row.category)));

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
