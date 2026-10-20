<template>
    <section class="activity-review" aria-label="Godkendelse">
        <div class="activity-review-top">
            <div>
                <span class="tag" :class="status.type">{{ status.label }}</span>
                <p class="activity-review-description">{{ description }}</p>
            </div>
            <div v-if="activity.canEdit" class="activity-review-actions">
                <BButton v-if="canSubmit" type="is-primary" size="is-small" :disabled="blocked || missingFields.length > 0" :loading="busy" @click="perform('submit')">
                    {{ activity.reviewStatus === 'ChangesRequested' ? 'Genindsend til godkendelse' : 'Indsend til godkendelse' }}
                </BButton>
                <template v-if="isAdmin && activity.reviewStatus === 'Submitted'">
                    <BButton type="is-success" size="is-small" :disabled="blocked" :loading="busy" @click="perform('approve')">Godkend aktivitet</BButton>
                    <BButton size="is-small" :disabled="blocked" @click="returnOpen = !returnOpen">{{ returnOpen ? 'Luk begrundelse' : 'Send retur' }}</BButton>
                </template>
            </div>
        </div>
        <p v-if="activity.submittedAt" class="activity-review-meta">Senest indsendt {{ dateTime(activity.submittedAt) }}</p>
        <div v-if="activity.reviewedAt" class="activity-review-feedback">
            <p class="activity-review-meta">Seneste vurdering · {{ activity.reviewedByName || 'Tidligere bruger' }} · {{ dateTime(activity.reviewedAt) }}</p>
            <p v-if="activity.reviewFeedback" class="activity-review-feedback-text">{{ activity.reviewFeedback }}</p>
        </div>
        <p v-if="activity.canEdit && hasUnsavedChanges" class="activity-review-hint">Gem dine ændringer, før du indsender eller vurderer aktiviteten.</p>
        <p v-else-if="canSubmit && missingFields.length" class="activity-review-hint">Mangler før indsendelse: {{ missingFields.join(', ') }}.</p>
        <BNotification v-if="error" type="is-danger" :closable="false" class="mt-3">
            {{ error }}
            <BButton v-if="conflict" size="is-small" class="mt-2" :disabled="busy || hasUnsavedChanges" @click="reload">Genindlæs aktivitet</BButton>
        </BNotification>
        <form v-if="returnOpen && isAdmin && activity.reviewStatus === 'Submitted'" class="activity-review-return" @submit.prevent="perform('request-changes')">
            <BField label="Hvad skal teamet ændre?">
                <BInput v-model="feedback" type="textarea" :rows="3" maxlength="4000" required :disabled="blocked" placeholder="Beskriv, hvad der mangler, så teamet kan rette og genindsende." />
            </BField>
            <BButton native-type="submit" type="is-warning" size="is-small" :disabled="blocked || !feedback.trim() || feedback.trim().length > 4000" :loading="busy">Send retur med begrundelse</BButton>
        </form>
    </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { BButton, BField, BInput, BNotification, useToast } from 'buefy';
import ActivityService from '@/Services/ActivityService';
import { useActivityStore } from '@/Stores/ActivityStore';
import { activityReviewStatus } from '@/lib/activityReview';
import { getErrorMessage, getErrorStatus } from '@/lib/api';
import type { ActivityDto, ActivityReviewAction } from '@/types';

const props = defineProps<{ activity: ActivityDto; isAdmin: boolean; hasUnsavedChanges: boolean }>();
const emit = defineEmits<{ busy: [value: boolean] }>();
const store = useActivityStore();
const service = new ActivityService();
const Toast = useToast();
const busy = ref(false);
const feedback = ref('');
const returnOpen = ref(false);
const error = ref('');
const conflict = ref(false);
const status = computed(() => activityReviewStatus(props.activity.reviewStatus));
const canSubmit = computed(() => props.activity.canEdit && ['Draft', 'ChangesRequested'].includes(props.activity.reviewStatus));
const missingFields = computed(() => props.activity.reviewMissingFields ?? []);
const blocked = computed(() => busy.value || props.hasUnsavedChanges || conflict.value);
const description = computed(() => {
    switch (props.activity.reviewStatus) {
        case 'Submitted': return 'Afventer vurdering. Ændringer i navn eller katalogtekst kræver ny indsendelse.';
        case 'ChangesRequested': return 'Ret aktiviteten ud fra begrundelsen og indsend den igen.';
        case 'Approved': return 'Godkendt internt. Ændringer i navn eller katalogtekst kræver ny indsendelse. Godkendelse udgiver ikke aktiviteten i kataloget.';
        default: return 'Aktiviteten er under udarbejdelse. Indsend den, når navn og katalogtekst er klar.';
    }
});

function dateTime(value: string) {
    return new Intl.DateTimeFormat('da-DK', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Copenhagen' }).format(new Date(value));
}

watch(() => props.activity.id, () => {
    error.value = '';
    conflict.value = false;
    feedback.value = '';
    returnOpen.value = false;
});

async function reload() {
    busy.value = true;
    emit('busy', true);
    try {
        const activity = await service.getActivity(props.activity.id);
        if (!activity) throw new Error();
        store.SelectedActivity = activity;
        await store.GET_TEAMS();
        error.value = '';
        conflict.value = false;
    } catch {
        error.value = 'Aktiviteten kunne ikke genindlæses. Prøv igen.';
    } finally {
        busy.value = false;
        emit('busy', false);
    }
}

async function perform(action: ActivityReviewAction) {
    if (blocked.value || !props.activity.canEdit) return;
    if (action === 'request-changes' && (!feedback.value.trim() || feedback.value.trim().length > 4000)) return;

    busy.value = true;
    emit('busy', true);
    error.value = '';
    conflict.value = false;
    try {
        await service.reviewActivity(props.activity.id, action, props.activity.reviewVersion, feedback.value.trim());
        const activity = await service.getActivity(props.activity.id);
        if (!activity) {
            error.value = 'Handlingen er gemt, men den opdaterede aktivitet kunne ikke hentes. Genindlæs, før du fortsætter.';
            conflict.value = true;
            return;
        }
        store.SelectedActivity = activity;
        await store.GET_TEAMS();
        returnOpen.value = false;
        feedback.value = '';
        Toast.open({ message: action === 'submit' ? 'Aktiviteten er indsendt til godkendelse.' : action === 'approve' ? 'Aktiviteten er godkendt.' : 'Aktiviteten er sendt retur til teamet.', type: 'is-success' });
    } catch (reason) {
        conflict.value = getErrorStatus(reason) === 409;
        error.value = getErrorMessage(reason, 'Handlingen kunne ikke gennemføres. Prøv igen.');
    } finally {
        busy.value = false;
        emit('busy', false);
    }
}
</script>

<style scoped>
.activity-review {
    border: 1px solid #e5eaf0;
    background: #fafbfc;
    border-radius: 8px;
    padding: 1rem 1.25rem;
    margin-bottom: 1.5rem;
}

.activity-review-top,
.activity-review-actions {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.75rem;
}

.activity-review-top {
    justify-content: space-between;
    align-items: flex-start;
}

.activity-review-description {
    margin-top: 0.5rem;
    font-size: 0.85rem;
    color: #596575;
    max-width: 48rem;
}

.activity-review-meta,
.activity-review-hint {
    margin-top: 0.5rem;
    font-size: 0.8rem;
    color: #68717d;
}

.activity-review-feedback {
    margin-top: 0.75rem;
    border-left: 2px solid #d5dde6;
    padding-left: 0.75rem;
}

.activity-review-feedback-text {
    white-space: pre-wrap;
    overflow-wrap: anywhere;
    margin-top: 0.35rem;
}

.activity-review-return {
    max-width: 40rem;
    margin-top: 1rem;
}
</style>
