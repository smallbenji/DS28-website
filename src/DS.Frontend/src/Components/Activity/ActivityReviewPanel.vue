<template>
    <UCard>
        <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="min-w-0 flex-1">
                <UBadge :label="status.label" :color="activityReviewBadgeColor(activity.reviewStatus)" variant="subtle" />
                <p class="mt-2 text-sm text-muted">{{ description }}</p>
            </div>
            <div v-if="activity.canEdit" class="flex flex-wrap gap-2">
                <UButton
                    v-if="canSubmit"
                    size="sm"
                    icon="i-lucide-send"
                    :label="activity.reviewStatus === 'ChangesRequested' ? 'Genindsend til godkendelse' : 'Indsend til godkendelse'"
                    :disabled="blocked || missingFields.length > 0"
                    :loading="busy && pendingAction === 'submit'"
                    @click="perform('submit')"
                />
                <template v-if="isAdmin && activity.reviewStatus === 'Submitted'">
                    <UButton size="sm" color="success" icon="i-lucide-check" label="Godkend aktivitet" :disabled="blocked" :loading="busy && pendingAction === 'approve'" @click="perform('approve')" />
                    <UButton size="sm" color="neutral" variant="outline" :label="returnOpen ? 'Luk begrundelse' : 'Send retur'" :disabled="blocked" @click="returnOpen = !returnOpen" />
                </template>
            </div>
        </div>

        <p v-if="activity.submittedAt" class="mt-3 text-xs text-muted">Senest indsendt {{ dateTime(activity.submittedAt) }}</p>
        <div v-if="activity.reviewedAt" class="mt-2 rounded-md bg-elevated px-3 py-2">
            <p class="text-xs text-muted">Seneste vurdering: {{ activity.reviewedByName || 'Tidligere bruger' }}, {{ dateTime(activity.reviewedAt) }}</p>
            <p v-if="activity.reviewFeedback" class="mt-1 whitespace-pre-line text-sm">{{ activity.reviewFeedback }}</p>
        </div>

        <p v-if="activity.canEdit && hasUnsavedChanges" class="mt-3 text-sm text-warning">Gem dine ændringer, før du indsender eller vurderer aktiviteten.</p>
        <p v-else-if="canSubmit && missingFields.length" class="mt-3 text-sm text-warning">Mangler før indsendelse: {{ missingFields.join(', ') }}.</p>

        <UAlert
            v-if="error"
            class="mt-3"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="error"
            :actions="conflict ? [{ label: 'Genindlæs aktivitet', color: 'error', variant: 'outline', disabled: busy || hasUnsavedChanges, onClick: reload }] : undefined"
        />

        <form v-if="returnOpen && isAdmin && activity.reviewStatus === 'Submitted'" class="mt-4 flex flex-col gap-3" @submit.prevent="perform('request-changes')">
            <UFormField label="Hvad skal teamet ændre?" required>
                <UTextarea
                    v-model="feedback"
                    :rows="3"
                    :maxlength="4000"
                    :disabled="blocked"
                    placeholder="Beskriv, hvad der mangler, så teamet kan rette og genindsende."
                    class="w-full"
                />
            </UFormField>
            <UButton
                type="submit"
                size="sm"
                color="warning"
                label="Send retur med begrundelse"
                class="self-start"
                :disabled="blocked || !feedback.trim() || feedback.trim().length > 4000"
                :loading="busy && pendingAction === 'request-changes'"
            />
        </form>
    </UCard>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import ActivityService from "@/Services/ActivityService";
import { useActivityStore } from "@/Stores/ActivityStore";
import { activityReviewStatus } from "@/lib/activityReview";
import { activityReviewBadgeColor } from "@/lib/activityReviewBadge";
import { getErrorMessage, getErrorStatus } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import type { ActivityDto, ActivityReviewAction } from "@/types";

const props = defineProps<{ activity: ActivityDto; isAdmin: boolean; hasUnsavedChanges: boolean }>();
const emit = defineEmits<{ busy: [value: boolean] }>();

const store = useActivityStore();
const service = new ActivityService();
const notify = useNotify();

const busy = ref(false);
const pendingAction = ref<ActivityReviewAction | null>(null);
const feedback = ref("");
const returnOpen = ref(false);
const error = ref("");
const conflict = ref(false);

const status = computed(() => activityReviewStatus(props.activity.reviewStatus));
const canSubmit = computed(() => props.activity.canEdit && ["Draft", "ChangesRequested"].includes(props.activity.reviewStatus));
const missingFields = computed(() => props.activity.reviewMissingFields ?? []);
const blocked = computed(() => busy.value || props.hasUnsavedChanges || conflict.value);
const description = computed(() => {
    switch (props.activity.reviewStatus) {
        case "Submitted": return "Afventer vurdering. Ændringer i navn eller katalogtekst kræver ny indsendelse.";
        case "ChangesRequested": return "Ret aktiviteten ud fra begrundelsen, og indsend den igen.";
        case "Approved": return "Godkendt internt. Ændringer i navn eller katalogtekst kræver ny indsendelse. Godkendelse udgiver ikke aktiviteten i kataloget.";
        default: return "Aktiviteten er under udarbejdelse. Indsend den, når navn og katalogtekst er klar.";
    }
});

function dateTime(value: string) {
    return new Intl.DateTimeFormat("da-DK", { dateStyle: "medium", timeStyle: "short", timeZone: "Europe/Copenhagen" }).format(new Date(value));
}

watch(() => props.activity.id, () => {
    error.value = "";
    conflict.value = false;
    feedback.value = "";
    returnOpen.value = false;
});

function setBusy(value: boolean) {
    busy.value = value;
    emit("busy", value);
}

async function reload() {
    setBusy(true);
    try {
        const activity = await service.getActivity(props.activity.id);
        if (!activity) throw new Error();
        store.SelectedActivity = activity;
        await store.GET_TEAMS();
        error.value = "";
        conflict.value = false;
    } catch {
        error.value = "Aktiviteten kunne ikke genindlæses. Prøv igen.";
    } finally {
        setBusy(false);
    }
}

async function perform(action: ActivityReviewAction) {
    if (blocked.value || !props.activity.canEdit) return;
    if (action === "request-changes" && (!feedback.value.trim() || feedback.value.trim().length > 4000)) return;

    pendingAction.value = action;
    setBusy(true);
    error.value = "";
    conflict.value = false;
    try {
        await service.reviewActivity(props.activity.id, action, props.activity.reviewVersion, feedback.value.trim());
        const activity = await service.getActivity(props.activity.id);
        if (!activity) {
            error.value = "Handlingen er gemt, men den opdaterede aktivitet kunne ikke hentes. Genindlæs, før du fortsætter.";
            conflict.value = true;
            return;
        }
        store.SelectedActivity = activity;
        await store.GET_TEAMS();
        returnOpen.value = false;
        feedback.value = "";
        notify.success(action === "submit"
            ? "Aktiviteten er indsendt til godkendelse."
            : action === "approve" ? "Aktiviteten er godkendt." : "Aktiviteten er sendt retur til teamet.");
    } catch (reason) {
        conflict.value = getErrorStatus(reason) === 409;
        error.value = getErrorMessage(reason, "Handlingen kunne ikke gennemføres. Prøv igen.");
    } finally {
        pendingAction.value = null;
        setBusy(false);
    }
}
</script>
