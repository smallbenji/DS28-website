<template>
    <div class="flex flex-col gap-4">
        <div class="flex flex-wrap items-start justify-between gap-3">
            <div>
                <UBadge label="UI-udkast" color="info" variant="subtle" />
                <h1 class="mt-1 text-2xl font-semibold text-highlighted">Prøv formularen</h1>
                <p class="text-sm text-muted">Udfyld felterne, og se, hvordan skabelonen bliver til en formular.</p>
            </div>
            <code class="rounded bg-elevated px-2 py-1 text-xs">{{ templateId }}</code>
        </div>

        <UAlert color="info" variant="subtle" icon="i-lucide-info" description="Dette er en forhåndsvisning. Dine svar gemmes ikke og forsvinder, når du forlader siden." />

        <USkeleton v-if="isLoading" class="h-64" />

        <UAlert
            v-else-if="error"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="error"
            :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: loadTemplate }]"
        />

        <div v-else-if="template" class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
            <div class="flex min-w-0 flex-col gap-4">
                <TemplateForm v-if="template.sections.length" v-model="values" :template="template" />
                <UCard v-else>
                    <p class="text-sm text-muted">Denne formular har endnu ingen sektioner.</p>
                </UCard>
                <div class="flex items-center justify-between gap-2">
                    <span class="text-xs text-muted">Kun til afprøvning, ingen indsendelse</span>
                    <UButton color="neutral" variant="outline" label="Ryd svar" :disabled="!hasAnswers" @click="values = {}" />
                </div>
            </div>

            <UCard class="self-start" aria-labelledby="preview-info-title">
                <h2 id="preview-info-title" class="text-sm font-semibold text-highlighted">Om denne visning</h2>
                <p class="mt-2 text-sm text-muted">Sektioner, labels og felttyper kommer fra C#-skabelonen. Svarene holdes adskilt fra definitionen.</p>
                <UCollapsible class="mt-3 border-t border-default pt-3">
                    <UButton color="neutral" variant="ghost" size="sm" block class="group justify-between" trailing-icon="i-lucide-chevron-down" :ui="{ trailingIcon: 'transition-transform group-data-[state=open]:rotate-180' }" label="Se aktuelle svar (JSON)" />
                    <template #content>
                        <pre class="mt-2 overflow-x-auto rounded bg-elevated p-2 text-xs">{{ JSON.stringify(values, null, 2) }}</pre>
                    </template>
                </UCollapsible>
            </UCard>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { storeToRefs } from "pinia";
import { useFormStore } from "@/Stores/FormStore";
import TemplateForm from "@/Components/Forms/TemplateForm.vue";
import type { FormValues } from "@/types";

const route = useRoute();
const formStore = useFormStore();
const { template, isLoading, error } = storeToRefs(formStore);
const values = ref<FormValues>({});

const templateId = computed(() => {
    const id = route.params.id;

    return (Array.isArray(id) ? id[0] : id) || "test-form-template";
});

const hasAnswers = computed(() => Object.values(values.value).some((value) => value !== null && value !== ""));

async function loadTemplate() {
    values.value = {};
    await formStore.GET_TEMPLATE(templateId.value);
}

watch(templateId, loadTemplate, { immediate: true });
onBeforeUnmount(() => formStore.RESET());
</script>
