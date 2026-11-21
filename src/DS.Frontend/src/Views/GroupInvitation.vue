<template>
    <UCard>
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Invitation til en gruppe</h1>
        </template>

        <div v-if="loading" class="flex flex-col gap-3">
            <USkeleton v-for="index in 3" :key="index" class="h-10" />
        </div>

        <UAlert v-if="error" class="mb-4" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

        <template v-if="invitation && meStore.Me">
            <p class="mb-4 text-sm">
                Du er inviteret til <strong>{{ invitation.groupName }}</strong> som <strong>{{ invitation.email }}</strong>.
            </p>

            <div v-if="invitation.existingUser && !meStore.Me.isAuthenticated" class="flex flex-col gap-3">
                <p class="text-sm">Log ind med den inviterede email for at acceptere invitationen.</p>
                <UButton icon="i-lucide-log-in" label="Log ind" block @click="login" />
            </div>

            <div v-else-if="!invitation.existingUser && meStore.Me.isAuthenticated" class="flex flex-col gap-3">
                <p class="text-sm">Log ud, før du opretter en konto med den inviterede email.</p>
                <UButton color="neutral" variant="outline" icon="i-lucide-log-out" label="Log ud" block :loading="busy" :disabled="busy" @click="logout" />
            </div>

            <form v-else class="flex flex-col gap-4" @submit.prevent="accept">
                <template v-if="!invitation.existingUser">
                    <div class="grid gap-4 sm:grid-cols-2">
                        <UFormField label="Fornavn" required>
                            <UInput v-model="firstName" autocomplete="given-name" :disabled="busy" class="w-full" />
                        </UFormField>
                        <UFormField label="Efternavn" required>
                            <UInput v-model="lastName" autocomplete="family-name" :disabled="busy" class="w-full" />
                        </UFormField>
                    </div>
                    <PasswordFields v-model:password="password" v-model:repeat-password="repeatPassword" :disabled="busy" />
                </template>
                <p v-else class="text-sm">
                    Du er logget ind som {{ meStore.Me.name }}. Kontoen skal have den inviterede email og må ikke allerede tilhøre en gruppe.
                </p>

                <UButton
                    type="submit"
                    :label="invitation.existingUser ? 'Accepter invitation' : 'Opret bruger og tilslut gruppen'"
                    block
                    :loading="busy"
                    :disabled="busy || !valid"
                />
                <UButton
                    v-if="invitation.existingUser"
                    color="neutral"
                    variant="outline"
                    label="Log ind med en anden konto"
                    block
                    :disabled="busy"
                    @click="logout"
                />
            </form>
        </template>
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import AuthService from "@/Services/AuthService";
import GroupInvitationService, { type GroupInvitationDto } from "@/Services/GroupInvitationService";
import { useMeStore } from "@/Stores/MeStore";
import { getErrorMessage } from "@/lib/api";
import PasswordFields from "@/Components/Auth/PasswordFields.vue";

const route = useRoute();
const router = useRouter();
const meStore = useMeStore();
const service = new GroupInvitationService();
const authService = new AuthService();

const invitation = ref<GroupInvitationDto | null>(null);
const firstName = ref("");
const lastName = ref("");
const password = ref("");
const repeatPassword = ref("");
const busy = ref(false);
const loading = ref(true);
const error = ref("");

const invitationId = computed(() => String(route.params.id));
const returnPath = computed(() => route.fullPath);

const valid = computed(() => invitation.value?.existingUser
    || (firstName.value.trim() !== "" && lastName.value.trim() !== "" && password.value.length >= 4 && password.value === repeatPassword.value));

function fail(cause: unknown) {
    error.value = getErrorMessage(cause, "Der opstod en fejl. Prøv igen.");
}

function login() {
    router.push({ path: "/login", query: { returnUrl: returnPath.value } });
}

async function logout() {
    busy.value = true;
    try {
        await authService.logout();
        window.location.assign(`/login?returnUrl=${encodeURIComponent(returnPath.value)}`);
    } catch (cause) {
        fail(cause);
        busy.value = false;
    }
}

async function accept() {
    if (!valid.value || busy.value) return;

    busy.value = true;
    error.value = "";
    try {
        await service.accept(invitationId.value, { firstName: firstName.value.trim(), lastName: lastName.value.trim(), password: password.value });
        window.location.assign("/group");
    } catch (cause) {
        fail(cause);
        busy.value = false;
    }
}

onMounted(async () => {
    try {
        if (!meStore.Me) await meStore.GET_ME();
        invitation.value = await service.get(invitationId.value);
    } catch (cause) {
        fail(cause);
    } finally {
        loading.value = false;
    }
});
</script>
