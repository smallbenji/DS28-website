<template>
    <UModal :open="!!secret" title="Gem client secret" :dismissible="false" :close="false">
        <template #body>
            <UAlert color="warning" variant="subtle" icon="i-lucide-triangle-alert">
                <template #description>
                    Secret til <strong>{{ clientId }}</strong> vises kun nu. Gem det sikkert, og opdatér klientapplikationen.
                </template>
            </UAlert>
            <UFormField label="Client secret" class="mt-4">
                <UFieldGroup class="w-full">
                    <UInput :model-value="secret" readonly class="w-full font-mono" aria-label="Client secret" />
                    <UButton icon="i-lucide-copy" label="Kopiér" @click="copy" />
                </UFieldGroup>
            </UFormField>
        </template>
        <template #footer>
            <div class="flex w-full justify-end">
                <UButton label="Jeg har gemt secret" @click="emit('close')" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { useNotify } from "@/lib/notify";

const props = defineProps<{ secret: string; clientId: string }>();
const emit = defineEmits<{ (e: "close"): void }>();

const notify = useNotify();

async function copy() {
    try {
        await navigator.clipboard.writeText(props.secret);
        notify.success("Secret er kopieret.");
    } catch {
        notify.error("Kopiér secret manuelt fra feltet.");
    }
}
</script>
