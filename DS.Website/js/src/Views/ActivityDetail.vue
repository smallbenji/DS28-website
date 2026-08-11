<template>
    <div class="activity-detail ui-card" v-if="activity">
        <div class="activity-detail-header">
            <h1 class="title is-4 mb-0">{{ form.name || 'Aktivitet' }}</h1>
            <span
                class="tag is-success is-light is-medium"
                v-if="activityTabFilled && catalogTabFilled"
            >
                <i class="fas fa-check"></i> Udfyldt
            </span>
            <span class="tag is-danger is-light is-medium" v-else>
                <i class="fas fa-circle-exclamation"></i> Mangler udfyldning
            </span>
        </div>
        <BTabs v-model="activeTab" type="is-boxed" class="activity-detail-tabs">
            <BTabItem>
                <template #header>
                    <span>Aktivitet</span>
                    <span class="tag is-success is-light" v-if="activityTabFilled">Udfyldt</span>
                    <span class="tag is-danger is-light" v-else>Mangler</span>
                </template>
                <div class="activity-detail-form">
                    <BField label="Navn">
                        <BInput v-model="form.name" placeholder="Aktivitetens navn" />
                    </BField>
                    <BField label="Budget">
                        <BInput v-model.number="form.budget" type="number" placeholder="0" disabled />
                    </BField>
                </div>
            </BTabItem>
            <BTabItem>
                <template #header>
                    <span>Katalog</span>
                    <span class="tag is-success is-light" v-if="catalogTabFilled">Udfyldt</span>
                    <span class="tag is-danger is-light" v-else>Mangler</span>
                </template>
                <div class="activity-detail-form">
                    <BField label="Katalognavn">
                        <BInput v-model="form.catalog.name" placeholder="Katalognavn" />
                    </BField>
                    <BField label="Resumé">
                        <BInput v-model="form.catalog.summary" placeholder="Kort resumé" />
                    </BField>
                    <BField label="Beskrivelse">
                        <BInput v-model="form.catalog.description" type="textarea" placeholder="Beskrivelse" />
                    </BField>
                </div>
            </BTabItem>
        </BTabs>
        <div class="activity-detail-actions">
            <BButton type="is-success" icon-left="check" :loading="saving" :disabled="saving" @click="saveActivity">
                Gem
            </BButton>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { BButton, BField, BInput, BTabs, BTabItem, useToast } from 'buefy';
import { storeToRefs } from 'pinia';
import { reactive, ref, computed, watch } from 'vue';
import type { CatalogDataDto } from '@/types';

interface ActivityFormDto {
    id: number;
    name: string;
    budget: number;
    catalog: CatalogDataDto;
}

const Toast = useToast();
const activityStore = useActivityStore();
const { SelectedActivity } = storeToRefs(activityStore);

const activeTab = ref(0);
const saving = ref(false);

const form = reactive<ActivityFormDto>({
    id: 0,
    name: '',
    budget: 0,
    catalog: { id: 0, name: '', summary: '', description: '' }
});

watch(SelectedActivity, (activity) => {
    if (!activity) return;

    form.id = activity.id;
    form.name = activity.name;
    form.budget = activity.budget;
    form.catalog = activity.catalog ?? { id: 0, name: '', summary: '', description: '' };
}, { immediate: true });

const activity = SelectedActivity;

const activityTabFilled = computed(() => form.name.trim().length > 0);

const catalogTabFilled = computed(
    () => form.catalog.name.trim().length > 0
        || form.catalog.summary.trim().length > 0
        || form.catalog.description.trim().length > 0
);

const saveActivity = async () => {
    saving.value = true;
    const success = await activityStore.UPDATE_ACTIVITY({ ...form });
    saving.value = false;

    if (success) {
        Toast.open({
            message: 'Aktiviteten er opdateret',
            type: 'is-success'
        });
    } else {
        Toast.open({
            message: 'Der skete en fejl ved opdatering af aktiviteten',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
.activity-detail {
    width: calc(100vw - 4rem);
    min-height: calc(100vh - 7rem);
    padding: 1.5rem 2rem;
    margin: 1rem 2rem;

    &-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1rem;

        .tag i {
            margin-right: 0.35rem;
        }
    }

    &-tabs {
        min-height: calc(100vh - 18rem);

        .tabs li .tag {
            margin-left: 0.4rem;
        }
    }

    &-form {
        padding: 1.5rem 0.25rem 0.5rem;
        max-width: 32rem;
    }

    &-actions {
        border-top: 1px solid rgba(0, 0, 0, 0.1);
        padding-top: 1rem;
        margin-top: 0.5rem;
        display: flex;
        justify-content: flex-end;
    }
}
</style>
