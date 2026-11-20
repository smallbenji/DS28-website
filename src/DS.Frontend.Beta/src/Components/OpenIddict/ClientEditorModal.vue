<template>
    <UModal v-model:open="open" :title="client ? 'Rediger klient' : 'Opret klient'" :dismissible="!busy" :close="!busy" :ui="{ content: 'sm:max-w-xl' }">
        <template #body>
            <form id="client-editor-form" class="flex flex-col gap-4" @submit.prevent="submit">
                <UAlert v-if="saveError" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="saveError" />

                <UFormField label="Navn" required>
                    <UInput v-model="form.displayName" required maxlength="200" :disabled="busy" class="w-full" />
                </UFormField>

                <UFormField label="Client ID" required help="Unikt ID, som applikationen bruger ved login. Ændringer kræver opdatering i applikationen.">
                    <UInput v-model="form.clientId" required maxlength="100" :disabled="busy" class="w-full font-mono" />
                </UFormField>

                <UFormField label="Klienttype">
                    <USelect v-model="form.clientType" :items="clientTypeItems" :disabled="busy" class="w-full" @update:model-value="onTypeChange" />
                </UFormField>

                <UFormField label="Loginrettigheder" required help="Brugeren skal have mindst én af de valgte rettigheder. Klienten tildeler dem ikke.">
                    <USelectMenu v-model="form.requiredRoles" :items="availableRoles" multiple placeholder="Søg og vælg rettigheder…" :disabled="busy" class="w-full" />
                </UFormField>

                <UFormField label="Redirect-URL'er" required help="Én præcis callback-URL pr. linje. HTTPS kræves, undtagen HTTP på loopback (fx localhost). Ingen wildcards eller fragmenter.">
                    <UTextarea v-model="redirectText" :rows="4" required :disabled="busy" class="w-full font-mono" autoresize />
                </UFormField>

                <UFormField label="Scopes">
                    <div class="flex flex-wrap gap-4">
                        <UCheckbox :model-value="true" disabled label="openid" />
                        <UCheckbox
                            v-for="scope in editableScopes"
                            :key="scope"
                            :model-value="form.scopes.includes(scope)"
                            :label="scope"
                            :disabled="busy"
                            @update:model-value="(checked: boolean | 'indeterminate') => toggleScope(scope, checked === true)"
                        />
                    </div>
                </UFormField>

                <UCheckbox v-model="form.requirePkce" label="Kræv PKCE" :description="form.clientType === 'public' ? 'Public-klienter skal altid bruge PKCE.' : undefined" :disabled="busy || form.clientType === 'public'" />

                <p class="text-xs text-muted">
                    Nye confidential-klienter får et automatisk genereret secret. Eksisterende secrets bevares ved almindelig redigering. Øvrige avancerede indstillinger bevares.
                </p>
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" :disabled="busy" @click="open = false" />
                <UButton type="submit" form="client-editor-form" :label="client ? 'Gem ændringer' : 'Opret klient'" :loading="busy" />
            </div>
        </template>
    </UModal>

    <ConfirmModal
        v-model:open="typeChangeOpen"
        title="Skift klienttype"
        description="Skift af klienttype ændrer kravene til klientens login. Et eksisterende secret fjernes ved skift til public, og et nyt genereres ved skift til confidential."
        confirm-label="Skift og gem"
        color="warning"
        :loading="busy"
        @confirm="save"
    />
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import OpenIddictService from "@/Services/OpenIddictService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { OpenIddictClientDto, OpenIddictClientSavedDto, OpenIddictClientUpdateDto } from "@/types";

const props = defineProps<{ client: OpenIddictClientDto | null; availableRoles: string[] }>();
const open = defineModel<boolean>("open", { required: true });
const emit = defineEmits<{ (e: "saved", result: OpenIddictClientSavedDto): void }>();

const service = new OpenIddictService();
const notify = useNotify();

const editableScopes = ["profile", "email", "roles"];
const clientTypeItems = [
    { label: "Confidential: serverapplikation med secret", value: "confidential" },
    { label: "Public: browserapplikation uden secret", value: "public" }
];

const form = ref<OpenIddictClientUpdateDto>(emptyForm());
const redirectText = ref("");
const busy = ref(false);
const saveError = ref("");
const typeChangeOpen = ref(false);

function emptyForm(): OpenIddictClientUpdateDto {
    return { clientId: "", displayName: "", clientType: "confidential", redirectUris: [], scopes: ["openid"], requiredRoles: [], requirePkce: true };
}

watch(open, (isOpen) => {
    if (!isOpen) return;

    const client = props.client;
    form.value = client
        ? {
            clientId: client.clientId,
            displayName: client.displayName ?? "",
            clientType: client.clientType,
            redirectUris: [...client.redirectUris],
            scopes: [...client.scopes],
            requiredRoles: [...client.requiredRoles],
            requirePkce: client.requirePkce || client.clientType === "public"
        }
        : emptyForm();
    redirectText.value = client ? client.redirectUris.join("\n") : "";
    saveError.value = "";
});

function onTypeChange() {
    if (form.value.clientType === "public") form.value.requirePkce = true;
}

function toggleScope(scope: string, checked: boolean) {
    form.value.scopes = checked
        ? [...new Set([...form.value.scopes, scope])]
        : form.value.scopes.filter((value) => value !== scope);
}

function submit() {
    if (busy.value) return;

    if (form.value.requiredRoles.length === 0) {
        saveError.value = "Vælg mindst én loginrettighed.";
        return;
    }

    if (props.client && props.client.clientType !== form.value.clientType) {
        typeChangeOpen.value = true;
        return;
    }

    void save();
}

async function save() {
    if (busy.value) return;

    busy.value = true;
    saveError.value = "";
    const data: OpenIddictClientUpdateDto = {
        ...form.value,
        clientId: form.value.clientId.trim(),
        displayName: form.value.displayName.trim(),
        redirectUris: [...new Set(redirectText.value.split(/\r?\n/).map((value) => value.trim()).filter(Boolean))]
    };

    try {
        const result = props.client ? await service.update(props.client.id, data) : await service.create(data);
        typeChangeOpen.value = false;
        open.value = false;
        notify.success("Klienten er gemt.");
        emit("saved", result);
    } catch (exception) {
        typeChangeOpen.value = false;
        saveError.value = getErrorMessage(exception, "Klienten kunne ikke gemmes. Prøv igen.");
    } finally {
        busy.value = false;
    }
}
</script>
