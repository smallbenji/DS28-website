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
        <div class="activity-detail-body">
            <nav class="activity-detail-nav">
                <button
                    v-for="(tab, index) in tabs"
                    :key="tab.key"
                    type="button"
                    class="activity-detail-nav-item"
                    :class="{ 'is-active': activeTab === index }"
                    @click="activeTab = index"
                >
                    <i class="activity-detail-nav-icon" :class="tab.icon"></i>
                    <span class="activity-detail-nav-label">{{ tab.label }}</span>
                    <i class="far fa-circle-check activity-detail-nav-check" v-if="tab.filled"></i>
                </button>
            </nav>
            <div class="activity-detail-panel">
                <div class="activity-detail-form" v-if="activeTab === 0">
                    <BField label="Navn">
                        <BInput v-model="form.name" placeholder="Aktivitetens navn" />
                    </BField>
                    <BField label="Budget">
                        <BInput v-model.number="form.budget" type="number" placeholder="0" disabled />
                    </BField>
                </div>
                <div class="activity-detail-form" v-else-if="activeTab === 1">
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
            </div>
        </div>
        <div class="activity-detail-actions">
            <BButton type="is-success" icon-left="check" :loading="saving" :disabled="saving" @click="saveActivity">
                Gem
            </BButton>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { BButton, BField, BInput, useToast } from 'buefy';
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

const tabs = computed(() => [
    { key: 'activity', label: 'Aktivitet', icon: 'fas fa-calendar-days', filled: activityTabFilled.value },
    { key: 'catalog', label: 'Katalog', icon: 'fas fa-globe', filled: catalogTabFilled.value }
]);

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

    &-body {
        display: flex;
        align-items: stretch;
        gap: 2rem;
        min-height: calc(100vh - 18rem);
    }

    &-nav {
        flex: 0 0 19rem;
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
        border-right: 1px solid rgba(0, 0, 0, 0.08);
        padding-right: 1.25rem;

        &-item {
            display: flex;
            align-items: center;
            gap: 1.1rem;
            width: 100%;
            padding: 1rem 1.15rem;
            border: 0;
            border-radius: 10px;
            background: transparent;
            color: #4a4a4a;
            font-family: inherit;
            font-size: 1.15rem;
            text-align: left;
            cursor: pointer;
            transition: background-color 0.15s ease, color 0.15s ease;

            &:hover {
                background-color: #f2f2f2;
                color: #363636;

                .activity-detail-nav-icon {
                    color: #4a4a4a;
                }
            }

            &.is-active {
                background-color: #e8e8e8;
                color: #1f1f1f;
                font-weight: 600;

                .activity-detail-nav-icon {
                    color: #1f1f1f;
                }
            }
        }

        &-icon {
            flex: 0 0 1.5rem;
            font-size: 1.35rem;
            color: #7a7a7a;
            text-align: center;
            transition: color 0.15s ease;
        }

        &-label {
            flex: 1;
        }

        &-check {
            color: #34c759;
            font-size: 1.4rem;
        }
    }

    &-panel {
        flex: 1;
        min-width: 0;
    }

    &-form {
        padding: 0.25rem 0.25rem 0.5rem;
        max-width: 36rem;

        box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
        border: 1px solid rgba(0, 0, 0, 0.1);
        padding: 1rem;
        border-radius: 10px;

        .label {
            font-size: 1.05rem;
        }

        .input,
        .textarea {
            font-size: 1.05rem;
        }
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
