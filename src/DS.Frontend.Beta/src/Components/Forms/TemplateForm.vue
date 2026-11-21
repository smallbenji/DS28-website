<template>
    <div class="flex flex-col gap-4">
        <UCard v-for="(section, sectionIndex) in template.sections" :key="sectionIndex" as="fieldset">
            <template #header>
                <legend class="flex items-center gap-2 font-semibold text-highlighted">
                    <span class="flex size-6 items-center justify-center rounded-full bg-primary/10 text-xs text-primary" aria-hidden="true">{{ sectionIndex + 1 }}</span>
                    {{ section.title }}
                </legend>
            </template>

            <div class="grid gap-4 sm:grid-cols-2">
                <UFormField v-for="field in section.fields" :key="field.key" :label="field.label" :name="field.key">
                    <UInput
                        v-if="field.type === FormFieldType.String || field.type === FormFieldType.Date"
                        :name="field.key"
                        :type="field.type === FormFieldType.Date ? 'date' : 'text'"
                        :placeholder="field.placeholder ?? undefined"
                        :model-value="String(values[field.key] ?? '')"
                        class="w-full"
                        @update:model-value="(value: string | number) => setValue(field.key, value === '' ? null : String(value))"
                    />
                    <UInput
                        v-else-if="field.type === FormFieldType.Number"
                        :name="field.key"
                        type="number"
                        step="any"
                        :placeholder="field.placeholder ?? undefined"
                        :model-value="values[field.key] == null ? '' : String(values[field.key])"
                        class="w-full"
                        @update:model-value="(value: string | number) => setNumber(field.key, value)"
                    />
                    <p v-else class="text-sm text-error">Denne felttype understøttes endnu ikke.</p>
                </UFormField>
            </div>
        </UCard>
    </div>
</template>

<script setup lang="ts">
import { FormFieldType, type FormTemplate, type FormValue, type FormValues } from "@/types";

defineProps<{ template: FormTemplate }>();
const values = defineModel<FormValues>({ required: true });

function setValue(key: string, value: FormValue) {
    values.value = { ...values.value, [key]: value };
}

function setNumber(key: string, value: string | number | null | undefined) {
    const number = value === "" || value == null ? null : Number(value);
    setValue(key, number !== null && Number.isFinite(number) ? number : null);
}
</script>
