<template>
    <div class="template-form">
        <fieldset v-for="(section, sectionIndex) in template.sections" :key="sectionIndex" class="box form-section">
            <legend class="section-heading">
                <span class="section-number" aria-hidden="true">{{ sectionIndex + 1 }}</span>
                {{ section.title }}
            </legend>

            <div class="section-fields">
                <BField
                    v-for="(field, fieldIndex) in section.fields"
                    :key="field.key"
                    :label="field.label"
                    :label-for="`${formId}-${sectionIndex}-${fieldIndex}`"
                >
                    <BInput
                        v-if="field.type === FormFieldType.String || field.type === FormFieldType.Date"
                        :id="`${formId}-${sectionIndex}-${fieldIndex}`"
                        :name="field.key"
                        :type="field.type === FormFieldType.Date ? 'date' : 'text'"
                        :placeholder="field.placeholder ?? undefined"
                        :model-value="values[field.key] ?? ''"
                        @update:model-value="setValue(field.key, $event === '' ? null : $event)"
                    />
                    <BInput
                        v-else-if="field.type === FormFieldType.Number"
                        :id="`${formId}-${sectionIndex}-${fieldIndex}`"
                        :name="field.key"
                        type="number"
                        step="any"
                        :placeholder="field.placeholder ?? undefined"
                        :model-value="values[field.key] ?? ''"
                        @update:model-value="setNumber(field.key, $event)"
                    />
                    <p v-else class="help is-danger">Denne felttype understøttes endnu ikke.</p>
                </BField>
            </div>
        </fieldset>
    </div>
</template>

<script setup lang="ts">
import { useId } from 'vue';
import { BField, BInput } from 'buefy';
import { FormFieldType, type FormTemplate, type FormValue, type FormValues } from '@/types';

defineProps<{ template: FormTemplate }>();
const values = defineModel<FormValues>({ required: true });
const formId = useId();

function setValue(key: string, value: FormValue) {
    values.value = { ...values.value, [key]: value };
}

function setNumber(key: string, value: string | number | null | undefined) {
    const number = value === '' || value == null ? null : Number(value);
    setValue(key, number !== null && Number.isFinite(number) ? number : null);
}
</script>

<style scoped>
.template-form {
    display: grid;
    gap: 1.5rem;
}

.form-section {
    min-width: 0;
    margin: 0;
    padding: 0 1.5rem 1.5rem;
    border: 1px solid #e7e9ed;
    box-shadow: 0 2px 8px rgb(0 0 0 / 3%);
}

.section-heading {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0 0.5rem;
    font-size: 1.1rem;
    font-weight: 600;
}

.section-number {
    display: grid;
    place-items: center;
    width: 1.75rem;
    height: 1.75rem;
    border-radius: 50%;
    background: #eef3fa;
    color: #355783;
    font-size: 0.8rem;
}

.section-fields {
    display: grid;
    gap: 1.25rem;
    padding-top: 1rem;
}

.section-fields :deep(.field) {
    margin-bottom: 0;
}
</style>
