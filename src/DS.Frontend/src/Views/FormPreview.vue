<template>
    <section class="section">
        <div class="form-preview">
            <header class="preview-header">
                <div>
                    <span class="tag is-info is-light">UI-udkast</span>
                    <h1 class="title is-3">Prøv formularen</h1>
                    <p class="has-text-grey">Udfyld felterne og se, hvordan skabelonen bliver til en formular.</p>
                </div>
                <code class="template-id">{{ templateId }}</code>
            </header>

            <BNotification type="is-info" :closable="false">
                Dette er en forhåndsvisning. Dine svar gemmes ikke og forsvinder, når du forlader siden.
            </BNotification>

            <div v-if="isLoading" class="box" role="status" aria-live="polite">Henter formularen…</div>
            <BNotification v-else-if="error" type="is-danger" :closable="false" role="alert">
                <p>{{ error }}</p>
                <BButton class="retry-button" @click="loadTemplate">Prøv igen</BButton>
            </BNotification>

            <div v-else-if="template" class="preview-layout">
                <div>
                    <TemplateForm v-if="template.sections.length" v-model="values" :template="template" />
                    <div v-else class="box">Denne formular har endnu ingen sektioner.</div>
                    <div class="form-actions">
                        <span class="has-text-grey is-size-7">Kun til afprøvning · ingen indsendelse</span>
                        <BButton :disabled="!hasAnswers" @click="values = {}">Ryd svar</BButton>
                    </div>
                </div>

                <aside class="box preview-info" aria-labelledby="preview-info-title">
                    <h2 id="preview-info-title" class="title is-6">Om denne visning</h2>
                    <p>Sektioner, labels og felttyper kommer fra C#-skabelonen. Svarene holdes adskilt fra definitionen.</p>
                    <hr />
                    <details>
                        <summary>Se aktuelle svar (JSON)</summary>
                        <pre>{{ JSON.stringify(values, null, 2) }}</pre>
                    </details>
                </aside>
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { storeToRefs } from 'pinia';
import { BButton, BNotification } from 'buefy';
import TemplateForm from '@/Components/Forms/TemplateForm.vue';
import { useFormStore } from '@/Stores/FormStore';
import type { FormValues } from '@/types';

const route = useRoute();
const formStore = useFormStore();
const { template, isLoading, error } = storeToRefs(formStore);
const values = ref<FormValues>({});
const templateId = computed(() => {
    const id = route.params.id;
    return (Array.isArray(id) ? id[0] : id) || 'test-form-template';
});
const hasAnswers = computed(() => Object.values(values.value).some(value => value !== null && value !== ''));

async function loadTemplate() {
    values.value = {};
    await formStore.GET_TEMPLATE(templateId.value);
}

watch(templateId, loadTemplate, { immediate: true });
onBeforeUnmount(() => formStore.RESET());
</script>

<style scoped>
.form-preview {
    max-width: 68rem;
    margin: 0 auto;
}

.preview-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    flex-wrap: wrap;
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.preview-header .title {
    margin: 0.75rem 0;
}

.template-id {
    overflow-wrap: anywhere;
    background: #eef0f3;
    color: #576174;
    border-radius: 6px;
    padding: 0.4rem 0.6rem;
}

.preview-layout {
    display: grid;
    grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
    align-items: start;
    gap: 2rem;
    margin-top: 2rem;
}

.preview-info {
    font-size: 0.9rem;
    line-height: 1.6;
}

.preview-info summary {
    cursor: pointer;
    font-weight: 600;
}

.preview-info pre {
    margin-top: 1rem;
    max-height: 24rem;
    overflow: auto;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}

.form-actions {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 1rem;
    margin-top: 1.5rem;
}

.retry-button {
    margin-top: 1rem;
}

@media (max-width: 768px) {
    .preview-layout {
        grid-template-columns: minmax(0, 1fr);
    }
}
</style>
