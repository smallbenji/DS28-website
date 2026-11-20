<template>
    <div class="flex flex-col gap-6">
        <div class="flex flex-wrap items-center justify-between gap-4">
            <div class="flex min-w-0 items-center gap-4">
                <UserAvatar :user="user" size="3xl" />
                <div class="min-w-0">
                    <h2 class="flex items-center gap-2 truncate text-xl font-semibold text-highlighted">
                        {{ user.firstName }} {{ user.lastName }}
                        <UBadge v-if="isLocked" label="Låst" color="warning" variant="subtle" />
                    </h2>
                    <p class="truncate text-sm text-muted">{{ user.userName || user.id }}</p>
                </div>
            </div>
            <div class="flex flex-wrap gap-2">
                <UButton v-if="hasRole('UsersLock')" color="warning" variant="outline" :icon="isLocked ? 'i-lucide-lock-open' : 'i-lucide-lock'" :label="isLocked ? 'Lås op' : 'Lås'" @click="lockOpen = true" />
                <UButton v-if="hasRole('UsersResetPassword')" color="neutral" variant="outline" icon="i-lucide-key-round" label="Nulstil adgangskode" :loading="generatingLink" @click="generateResetLink" />
                <UButton v-if="hasRole('UsersDelete')" color="error" variant="outline" icon="i-lucide-trash-2" label="Slet" @click="deleteOpen = true" />
            </div>
        </div>

        <div class="grid gap-6 xl:grid-cols-2">
            <UCard>
                <template #header>
                    <h3 class="font-semibold text-highlighted">Stamdata</h3>
                </template>
                <form id="user-form" class="flex flex-col gap-4" @submit.prevent="save">
                    <div v-if="hasRole('UsersEditProfilePicture')" class="flex flex-wrap items-center gap-2">
                        <UButton :to="legacyUrl" color="neutral" variant="outline" size="sm" icon="i-lucide-image" label="Skift billede i nuværende HQ" />
                        <UButton v-if="user.profilePicture" color="error" variant="ghost" size="sm" label="Fjern billede" :loading="removingPicture" @click="removePicture" />
                    </div>
                    <div class="grid gap-4 sm:grid-cols-2">
                        <UFormField label="Fornavn">
                            <UInput v-model="draft.firstName" class="w-full" />
                        </UFormField>
                        <UFormField label="Efternavn">
                            <UInput v-model="draft.lastName" class="w-full" />
                        </UFormField>
                    </div>
                    <UFormField label="Email">
                        <UInput v-model="draft.email" type="email" class="w-full" />
                    </UFormField>
                    <UFormField label="Telefon">
                        <UInput v-model="draft.phone" type="tel" class="w-full" />
                    </UFormField>
                    <UFormField label="Gruppe">
                        <USelectMenu v-model="draftGroupId" :items="groupItems" value-key="value" class="w-full" />
                    </UFormField>
                </form>
                <template #footer>
                    <div class="flex justify-end gap-2">
                        <UButton v-if="isDirty" color="neutral" variant="ghost" label="Fortryd" @click="resetDraft" />
                        <UButton type="submit" form="user-form" label="Gem" :loading="saving" :disabled="!isDirty" />
                    </div>
                </template>
            </UCard>

            <div class="flex flex-col gap-6">
                <UCard :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <div class="flex items-center justify-between gap-2">
                            <h3 class="font-semibold text-highlighted">Roller</h3>
                            <UButton size="sm" icon="i-lucide-plus" label="Tildel rolle" :disabled="availableRoles.length === 0" @click="openAssignRole" />
                        </div>
                    </template>
                    <p v-if="user.roles.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen roller</p>
                    <ul v-else class="divide-y divide-default">
                        <li v-for="role in user.roles" :key="role" class="flex items-center justify-between gap-2 px-4 py-2 sm:px-6">
                            <span class="flex items-center gap-2 text-sm">
                                <UIcon name="i-lucide-shield" class="size-4 text-muted" />
                                {{ role }}
                            </span>
                            <UButton v-if="AssignableGroups.includes(role)" color="error" variant="ghost" size="xs" label="Fjern" @click="confirmRemoveRole(role)" />
                        </li>
                    </ul>
                </UCard>

                <UCard :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <h3 class="font-semibold text-highlighted">Stillinger</h3>
                    </template>
                    <p v-if="user.positions.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen stillinger</p>
                    <ul v-else class="divide-y divide-default">
                        <li v-for="position in user.positions" :key="position.id" class="flex items-center gap-2 px-4 py-2 text-sm sm:px-6">
                            <UIcon name="i-lucide-network" class="size-4 text-muted" />
                            {{ position.title }}
                        </li>
                    </ul>
                </UCard>
            </div>
        </div>

        <UModal v-model:open="assignRoleOpen" title="Tildel rolle">
            <template #body>
                <UFormField label="Rolle">
                    <USelectMenu v-model="roleToAssign" :items="availableRoles" placeholder="Vælg rolle…" class="w-full" />
                </UFormField>
            </template>
            <template #footer>
                <div class="flex w-full justify-end gap-2">
                    <UButton color="neutral" variant="outline" label="Annuller" @click="assignRoleOpen = false" />
                    <UButton label="Tildel rolle" :disabled="!roleToAssign" :loading="changingRole" @click="assignRole" />
                </div>
            </template>
        </UModal>

        <ConfirmModal
            v-model:open="removeRoleOpen"
            title="Fjern rolle"
            :description="`Fjern rollen ${roleToRemove} fra ${user.firstName} ${user.lastName}?`"
            confirm-label="Fjern rolle"
            color="error"
            :loading="changingRole"
            @confirm="removeRole"
        />

        <ConfirmModal
            v-model:open="lockOpen"
            :title="isLocked ? 'Lås bruger op' : 'Lås bruger'"
            :description="isLocked ? `Lås op for ${user.firstName} ${user.lastName}?` : `Lås ${user.firstName} ${user.lastName}? Brugeren kan ikke logge ind, mens kontoen er låst.`"
            :confirm-label="isLocked ? 'Lås op' : 'Lås bruger'"
            color="warning"
            :loading="locking"
            @confirm="toggleLock"
        />

        <ConfirmModal
            v-model:open="deleteOpen"
            title="Slet bruger"
            :description="`Slet ${user.firstName} ${user.lastName}? Det kan ikke fortrydes.`"
            confirm-label="Slet bruger"
            color="error"
            :loading="deleting"
            @confirm="deleteUser"
        />

        <UModal v-model:open="resetLinkOpen" title="Nulstil adgangskode">
            <template #body>
                <template v-if="resetLink">
                    <p class="text-sm">
                        Send linket til <strong>{{ resetLink.email }}</strong>. Det kan kun bruges én gang.
                    </p>
                    <UFieldGroup class="mt-3 w-full">
                        <UInput :model-value="resetLink.link" readonly class="w-full" aria-label="Link til nulstilling" />
                        <UButton icon="i-lucide-copy" label="Kopiér" @click="copyResetLink" />
                    </UFieldGroup>
                </template>
                <UAlert v-else color="error" variant="subtle" title="Linket kunne ikke genereres. Prøv igen." />
            </template>
            <template #footer>
                <div class="flex w-full justify-end gap-2">
                    <UButton color="neutral" variant="outline" label="Luk" @click="resetLinkOpen = false" />
                    <UButton label="Generér nyt link" :loading="generatingLink" @click="generateResetLink" />
                </div>
            </template>
        </UModal>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, toRaw, watch } from "vue";
import { storeToRefs } from "pinia";
import { useToast } from "@nuxt/ui/composables";
import { useUserStore } from "@/Stores/UserStore";
import { useHasRole } from "@/lib/permissions";
import { useGroupOptions } from "@/lib/groupOptions";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import type { ResetPasswordLinkDto, UserDto } from "@/types";

const props = defineProps<{ user: UserDto }>();
const emit = defineEmits<{ (e: "deleted"): void }>();

const userStore = useUserStore();
const { Groups, AssignableGroups } = storeToRefs(userStore);
const toast = useToast();
const hasRole = useHasRole();
const { groupItems, findGroup, groupValue } = useGroupOptions();

const legacyUrl = computed(() => new URL(`/admin/user/${props.user.id}`, window.location.origin).href);

const isLocked = computed(() => !!props.user.lockoutEnd && new Date(props.user.lockoutEnd).getTime() > Date.now());

const draft = ref<UserDto>(structuredClone(toRaw(props.user)));
const saving = ref(false);

function resetDraft() {
    draft.value = structuredClone(toRaw(props.user));
}

watch(() => props.user.id, resetDraft);

const draftGroupId = computed({
    get: () => groupValue(draft.value.group),
    set: (value: string) => {
        draft.value.group = findGroup(value);
    }
});

const isDirty = computed(() => draft.value.firstName !== props.user.firstName
    || draft.value.lastName !== props.user.lastName
    || draft.value.email !== props.user.email
    || draft.value.phone !== props.user.phone
    || groupValue(draft.value.group) !== groupValue(props.user.group));

function notify(ok: boolean, success: string, failure: string) {
    toast.add(ok
        ? { title: success, color: "success", icon: "i-lucide-circle-check" }
        : { title: failure, color: "error", icon: "i-lucide-circle-alert" });
}

async function save() {
    if (!isDirty.value) return;

    saving.value = true;
    try {
        const ok = await userStore.UPDATE_USER({ ...draft.value, roles: props.user.roles });
        notify(ok, "Brugeren er gemt.", "Brugeren kunne ikke gemmes.");
    } finally {
        saving.value = false;
    }
}

watch(() => props.user, (user) => {
    if (!isDirty.value) return;
    if (user.firstName === draft.value.firstName && user.lastName === draft.value.lastName
        && user.email === draft.value.email && user.phone === draft.value.phone
        && groupValue(user.group) === groupValue(draft.value.group)) {
        resetDraft();
    }
});

const removingPicture = ref(false);

async function removePicture() {
    removingPicture.value = true;
    try {
        const ok = await userStore.UPDATE_USER_PROFILE_PICTURE(props.user.id, null);
        notify(ok, "Profilbilledet er fjernet.", "Profilbilledet kunne ikke fjernes.");
    } finally {
        removingPicture.value = false;
    }
}

const availableRoles = computed(() => Groups.value
    .map((group) => group.name)
    .filter((name) => AssignableGroups.value.includes(name) && !props.user.roles.includes(name)));

const assignRoleOpen = ref(false);
const removeRoleOpen = ref(false);
const roleToAssign = ref<string | undefined>();
const roleToRemove = ref("");
const changingRole = ref(false);

function openAssignRole() {
    roleToAssign.value = undefined;
    assignRoleOpen.value = true;
}

async function assignRole() {
    if (!roleToAssign.value) return;

    changingRole.value = true;
    try {
        const ok = await userStore.ADD_USER_TO_ROLE(props.user, roleToAssign.value);
        notify(ok, `${roleToAssign.value} er tildelt.`, "Rollen kunne ikke tildeles.");
        if (ok) assignRoleOpen.value = false;
    } finally {
        changingRole.value = false;
    }
}

function confirmRemoveRole(role: string) {
    roleToRemove.value = role;
    removeRoleOpen.value = true;
}

async function removeRole() {
    changingRole.value = true;
    try {
        const ok = await userStore.REMOVE_USER_FROM_ROLE(props.user, roleToRemove.value);
        notify(ok, `${roleToRemove.value} er fjernet.`, "Rollen kunne ikke fjernes.");
        if (ok) removeRoleOpen.value = false;
    } finally {
        changingRole.value = false;
    }
}

const lockOpen = ref(false);
const locking = ref(false);

async function toggleLock() {
    const unlocking = isLocked.value;

    locking.value = true;
    try {
        const ok = unlocking ? await userStore.UNLOCK_USER(props.user) : await userStore.LOCK_USER(props.user);
        notify(ok, unlocking ? "Brugeren er låst op." : "Brugeren er låst.", unlocking ? "Brugeren kunne ikke låses op." : "Brugeren kunne ikke låses.");
        if (ok) lockOpen.value = false;
    } finally {
        locking.value = false;
    }
}

const deleteOpen = ref(false);
const deleting = ref(false);

async function deleteUser() {
    deleting.value = true;
    try {
        const ok = await userStore.DELETE_USER(props.user);
        notify(ok, "Brugeren er slettet.", "Brugeren kunne ikke slettes.");
        if (ok) {
            deleteOpen.value = false;
            emit("deleted");
        }
    } finally {
        deleting.value = false;
    }
}

const resetLinkOpen = ref(false);
const resetLink = ref<ResetPasswordLinkDto | null>(null);
const generatingLink = ref(false);

async function generateResetLink() {
    generatingLink.value = true;
    resetLink.value = null;
    try {
        resetLink.value = await userStore.CREATE_RESET_PASSWORD_LINK(props.user);
        resetLinkOpen.value = true;
    } finally {
        generatingLink.value = false;
    }
}

async function copyResetLink() {
    if (!resetLink.value) return;

    try {
        await navigator.clipboard.writeText(resetLink.value.link);
        notify(true, "Linket er kopieret.", "");
    } catch {
        notify(false, "", "Linket kunne ikke kopieres.");
    }
}
</script>
