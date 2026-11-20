<template>
    <UFormField :label="label" :help="help" required>
        <UInput
            v-model="password"
            :type="showPassword ? 'text' : 'password'"
            icon="i-lucide-lock"
            autocomplete="new-password"
            :disabled="disabled"
            class="w-full"
            :ui="{ trailing: 'pe-1' }"
        >
            <template #trailing>
                <UButton
                    color="neutral"
                    variant="link"
                    size="sm"
                    :icon="showPassword ? 'i-lucide-eye-off' : 'i-lucide-eye'"
                    :aria-label="showPassword ? 'Skjul adgangskode' : 'Vis adgangskode'"
                    :aria-pressed="showPassword"
                    @click="showPassword = !showPassword"
                />
            </template>
        </UInput>
    </UFormField>
    <UFormField label="Gentag adgangskode" required :error="mismatch ? 'Adgangskoderne skal være ens.' : undefined">
        <UInput
            v-model="repeatPassword"
            :type="showPassword ? 'text' : 'password'"
            icon="i-lucide-lock"
            autocomplete="new-password"
            :disabled="disabled"
            class="w-full"
        />
    </UFormField>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";

withDefaults(defineProps<{ label?: string; help?: string; disabled?: boolean }>(), {
    label: "Adgangskode",
    help: "Mindst 4 tegn.",
    disabled: false
});

const password = defineModel<string>("password", { required: true });
const repeatPassword = defineModel<string>("repeatPassword", { required: true });

const showPassword = ref(false);
const mismatch = computed(() => repeatPassword.value.length > 0 && password.value !== repeatPassword.value);
</script>
