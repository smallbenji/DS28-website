<template>
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
        <template #header>
            <div class="flex items-start justify-between gap-2">
                <div>
                    <h2 class="font-semibold text-highlighted">Lejrdage</h2>
                    <p class="text-sm text-muted">Grupperne vælger deltagelsesdage ud fra disse dage. En skiftedag kan høre til begge halvdele.</p>
                </div>
                <p v-if="days.length" class="shrink-0 text-sm text-muted">{{ summary }}</p>
            </div>
        </template>

        <div v-if="loading" class="flex flex-col gap-3 p-4 sm:p-6">
            <USkeleton v-for="index in 3" :key="index" class="h-10" />
        </div>

        <UAlert
            v-else-if="loadFailed"
            class="m-4 sm:m-6"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            title="Lejrdagene kunne ikke hentes."
            :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]"
        />

        <p v-else-if="days.length === 0" class="px-4 py-3 text-sm italic text-muted sm:px-6">Ingen lejrdage endnu. Tilføj lejrens første dag nedenfor.</p>

        <ul v-else class="divide-y divide-default">
            <li v-for="day in days" :key="day.id" class="flex flex-wrap items-center justify-between gap-2 px-4 py-3 sm:px-6">
                <p class="font-medium text-highlighted">{{ formatDay(day.date) }}</p>
                <div class="flex items-center gap-1">
                    <USelect
                        :model-value="day.half"
                        :items="halfItems"
                        :loading="savingId === day.id"
                        :disabled="busy"
                        :aria-label="`Halvdel for ${formatDay(day.date, false)}`"
                        class="w-44"
                        @update:model-value="(half: CampHalf) => changeHalf(day, half)"
                    />
                    <UButton color="error" variant="ghost" icon="i-lucide-trash-2" :disabled="busy" :aria-label="`Slet ${formatDay(day.date, false)}`" @click="confirmDelete(day)" />
                </div>
            </li>
        </ul>

        <template #footer>
            <form class="flex flex-wrap items-end gap-2" @submit.prevent="addDay">
                <UFormField label="Dato" :error="addError || undefined">
                    <UInput v-model="newDate" type="date" :disabled="busy" @update:model-value="addError = ''" />
                </UFormField>
                <UFormField label="Halvdel">
                    <USelect v-model="newHalf" :items="halfItems" :disabled="busy" class="w-44" />
                </UFormField>
                <UButton type="submit" icon="i-lucide-plus" label="Tilføj dag" :loading="adding" :disabled="busy" />
            </form>
        </template>

        <ConfirmModal
            v-model:open="deleteOpen"
            title="Slet lejrdag"
            :description="`Slet ${dayToDelete ? formatDay(dayToDelete.date, false) : ''} fra lejrens dage?`"
            confirm-label="Slet dag"
            color="error"
            :loading="deleting"
            @confirm="deleteDay"
        />
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import CampDayService, { type CampDay, type CampHalf } from "@/Services/CampDayService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";

const service = new CampDayService();
const notify = useNotify();

const halfItems: { label: string; value: CampHalf }[] = [
    { label: "1. halvdel", value: "First" },
    { label: "2. halvdel", value: "Second" },
    { label: "Begge halvdele", value: "Both" }
];

const days = ref<CampDay[]>([]);
const loading = ref(true);
const loadFailed = ref(false);
const savingId = ref<number | null>(null);
const adding = ref(false);
const deleting = ref(false);
const busy = computed(() => savingId.value != null || adding.value || deleting.value);

const newDate = ref("");
const newHalf = ref<CampHalf>("First");
const addError = ref("");

const deleteOpen = ref(false);
const dayToDelete = ref<CampDay | null>(null);

const summary = computed(() => {
    const first = days.value.filter((day) => day.half !== "Second").length;
    const second = days.value.filter((day) => day.half !== "First").length;
    return `${days.value.length} ${days.value.length === 1 ? "dag" : "dage"} · 1. halvdel: ${first} · 2. halvdel: ${second}`;
});

function parseDate(value: string): Date {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day);
}

function toIsoDate(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${date.getFullYear()}-${month}-${day}`;
}

const dayFormat = new Intl.DateTimeFormat("da-DK", { weekday: "long", day: "numeric", month: "long", year: "numeric" });

function formatDay(value: string, capitalize = true): string {
    const text = dayFormat.format(parseDate(value));
    return capitalize ? text.charAt(0).toUpperCase() + text.slice(1) : text;
}

function sortDays(list: CampDay[]): CampDay[] {
    return [...list].sort((a, b) => a.date.localeCompare(b.date));
}

function suggestNextDate() {
    const last = days.value.at(-1);
    if (!last) return;

    const next = parseDate(last.date);
    next.setDate(next.getDate() + 1);
    newDate.value = toIsoDate(next);
    newHalf.value = last.half === "First" ? "First" : "Second";
}

async function load() {
    loading.value = true;
    loadFailed.value = false;
    try {
        days.value = sortDays(await service.list());
        suggestNextDate();
    } catch {
        loadFailed.value = true;
    } finally {
        loading.value = false;
    }
}

async function addDay() {
    if (busy.value) return;
    if (!newDate.value) {
        addError.value = "Vælg en dato.";
        return;
    }

    adding.value = true;
    addError.value = "";
    try {
        const created = await service.create({ date: newDate.value, half: newHalf.value });
        days.value = sortDays([...days.value, created]);
        notify.success(`${formatDay(created.date)} er tilføjet.`);
        suggestNextDate();
    } catch (cause) {
        addError.value = getErrorMessage(cause, "Dagen kunne ikke tilføjes.");
    } finally {
        adding.value = false;
    }
}

async function changeHalf(day: CampDay, half: CampHalf) {
    if (busy.value || day.half === half) return;

    savingId.value = day.id;
    try {
        const updated = await service.update(day.id, { date: day.date, half });
        days.value = days.value.map((candidate) => (candidate.id === updated.id ? updated : candidate));
        notify.success(`${formatDay(updated.date)} er opdateret.`);
    } catch (cause) {
        notify.error(getErrorMessage(cause, "Halvdelen kunne ikke gemmes."));
    } finally {
        savingId.value = null;
    }
}

function confirmDelete(day: CampDay) {
    dayToDelete.value = day;
    deleteOpen.value = true;
}

async function deleteDay() {
    const day = dayToDelete.value;
    if (!day || busy.value) return;

    deleting.value = true;
    try {
        await service.remove(day.id);
        days.value = days.value.filter((candidate) => candidate.id !== day.id);
        notify.success(`${formatDay(day.date)} er slettet.`);
        deleteOpen.value = false;
    } catch (cause) {
        notify.error(getErrorMessage(cause, "Dagen kunne ikke slettes."));
    } finally {
        deleting.value = false;
    }
}

onMounted(load);
</script>
