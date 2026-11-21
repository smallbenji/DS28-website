<template>
    <USlideover v-model:open="open" :title="position?.title ?? 'Stilling'" :description="isPa ? 'Personlig assistent' : undefined">
        <template #body>
            <div v-if="position" class="flex flex-col gap-6">
                <section>
                    <h3 class="mb-3 text-sm font-semibold text-highlighted">Stilling</h3>
                    <form v-if="canEdit" id="org-edit-form" class="flex flex-col gap-3" @submit.prevent="saveEdit">
                        <UFormField label="Titel" required>
                            <UInput v-model="editTitle" class="w-full" />
                        </UFormField>
                        <UFormField label="Farve">
                            <OrgColorSelect v-model="editColor" />
                        </UFormField>
                        <UFormField label="Grupper">
                            <USelectMenu v-model="editGroups" :items="groupItems" value-key="value" multiple placeholder="Vælg grupper…" class="w-full" />
                        </UFormField>
                        <div class="flex justify-end">
                            <UButton type="submit" label="Gem" :loading="busy === 'edit'" :disabled="busy != null || !editTitle.trim()" />
                        </div>
                    </form>
                    <div v-else-if="position.groups.length" class="flex flex-wrap gap-1">
                        <UBadge v-for="group in position.groups" :key="group" :label="groupTitle(group)" color="neutral" variant="outline" />
                    </div>
                    <p v-else class="text-sm text-muted">Ingen grupper</p>
                </section>

                <section>
                    <h3 class="mb-3 text-sm font-semibold text-highlighted">Indehavere</h3>
                    <ul v-if="position.holders.length" class="mb-3 flex flex-col gap-2">
                        <li v-for="holder in position.holders" :key="holder.id" class="flex items-center justify-between gap-2">
                            <span class="flex items-center gap-2 text-sm">
                                <UserAvatar :user="holder" size="sm" />
                                {{ holder.firstName }} {{ holder.lastName }}
                            </span>
                            <UButton
                                v-if="canManage && canRemoveHolder"
                                color="error"
                                variant="ghost"
                                size="xs"
                                icon="i-lucide-x"
                                :aria-label="`Fjern ${holder.firstName} ${holder.lastName}`"
                                :disabled="busy != null"
                                @click="removeHolder(holder.id)"
                            />
                        </li>
                    </ul>
                    <p v-else class="mb-3 text-sm text-muted">Ingen tilknyttet</p>

                    <UFieldGroup v-if="canManage" class="w-full">
                        <USelectMenu v-model="selectedUserId" :items="userItems" value-key="value" placeholder="Vælg bruger…" class="w-full" />
                        <UButton label="Tilknyt" :loading="busy === 'holder'" :disabled="busy != null || !selectedUserId" @click="addHolder" />
                    </UFieldGroup>
                </section>

                <section v-if="canManage">
                    <h3 class="mb-3 text-sm font-semibold text-highlighted">Inviter ny bruger</h3>
                    <form class="flex" @submit.prevent="sendInvite">
                        <UFieldGroup class="w-full">
                            <UInput v-model="inviteEmail" type="email" placeholder="eksempel@mail.dk" class="w-full" aria-label="Email" />
                            <UButton type="submit" label="Send" :loading="busy === 'invite'" :disabled="busy != null || !inviteEmail.trim()" />
                        </UFieldGroup>
                    </form>
                </section>

                <section v-if="canManage && !isPa && !hasPa">
                    <h3 class="mb-3 text-sm font-semibold text-highlighted">Personlig assistent</h3>
                    <UButton color="neutral" variant="outline" icon="i-lucide-user-plus" label="Tilføj personlig assistent" :loading="busy === 'pa'" :disabled="busy != null" @click="addPa" />
                </section>

                <section v-if="canManage && !isPa">
                    <h3 class="mb-3 text-sm font-semibold text-highlighted">Flyt</h3>
                    <UFieldGroup class="w-full">
                        <USelectMenu v-model="moveParent" :items="moveItems" value-key="value" class="w-full" />
                        <UButton label="Flyt" :loading="busy === 'move'" :disabled="busy != null" @click="saveMove" />
                    </UFieldGroup>
                </section>

                <section v-if="canCreateChild">
                    <h3 class="mb-3 text-sm font-semibold text-highlighted">Ny understilling</h3>
                    <form class="flex flex-col gap-3" @submit.prevent="createChild">
                        <UFormField label="Titel" required>
                            <UInput v-model="childTitle" placeholder="Fx Økonomiansvarlig" class="w-full" />
                        </UFormField>
                        <UFormField label="Farve">
                            <OrgColorSelect v-model="childColor" />
                        </UFormField>
                        <UFormField label="Grupper">
                            <USelectMenu v-model="childGroups" :items="groupItems" value-key="value" multiple placeholder="Vælg grupper…" class="w-full" />
                        </UFormField>
                        <div class="flex justify-end">
                            <UButton type="submit" icon="i-lucide-plus" label="Opret" :loading="busy === 'child'" :disabled="busy != null || !childTitle.trim()" />
                        </div>
                    </form>
                </section>

                <section v-if="canManage && (isPa || !position.isSystem)" class="border-t border-default pt-4">
                    <UButton color="error" variant="outline" icon="i-lucide-trash-2" :label="isPa ? 'Slet personlig assistent' : 'Slet stilling'" :disabled="busy != null" @click="deleteOpen = true" />
                </section>
            </div>

            <ConfirmModal
                v-model:open="deleteOpen"
                :title="isPa ? 'Slet personlig assistent' : 'Slet stilling'"
                :description="`Slet ${position?.title}?`"
                confirm-label="Slet"
                color="error"
                :loading="busy === 'delete'"
                @confirm="removePosition"
            />
        </template>
    </USlideover>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import OrgService from "@/Services/OrgService";
import { useNotify } from "@/lib/notify";
import { canCreateChild as canCreateChildPosition, canManagePosition } from "@/lib/org/orgAccess";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import OrgColorSelect from "@/Components/Org/OrgColorSelect.vue";
import { groupTitle, type OrgPositionDto, type UserDto } from "@/types";

type BusyAction = "edit" | "holder" | "invite" | "pa" | "move" | "child" | "delete";

const props = defineProps<{
    position: OrgPositionDto | null;
    positions: OrgPositionDto[];
    assignableGroups: string[];
    users: UserDto[];
    currentUserId: string;
    isSysAdmin: boolean;
}>();

const open = defineModel<boolean>("open", { required: true });
const emit = defineEmits<{ (e: "refresh"): void }>();

const service = new OrgService();
const notify = useNotify();

const busy = ref<BusyAction | null>(null);
const editTitle = ref("");
const editColor = ref<string | null>(null);
const editGroups = ref<string[]>([]);
const selectedUserId = ref<string | undefined>();
const inviteEmail = ref("");
const rootValue = "root";
const moveParent = ref<string>(rootValue);
const childTitle = ref("");
const childColor = ref<string | null>(null);
const childGroups = ref<string[]>([]);
const deleteOpen = ref(false);

const canManage = computed(() => props.position !== null
    && canManagePosition(props.position, props.positions, props.currentUserId, props.isSysAdmin));
const canEdit = computed(() => canManage.value && props.position !== null && !props.position.isSystem && props.position.paForPositionId === null);
const isPa = computed(() => props.position?.paForPositionId != null);
const hasPa = computed(() => props.position !== null && props.positions.some((candidate) => candidate.paForPositionId === props.position!.id));
const canCreateChild = computed(() => props.position !== null
    && canCreateChildPosition(props.position, props.positions, props.currentUserId, props.isSysAdmin));
const canRemoveHolder = computed(() => !props.position?.isSystem || (props.position?.holders.length ?? 0) > 1);

const groupItems = computed(() => props.assignableGroups.map((group) => ({ label: groupTitle(group), value: group })));

const userItems = computed(() => {
    const held = new Set(props.position?.holders.map((holder) => holder.id) ?? []);

    return props.users
        .filter((user) => !held.has(user.id))
        .map((user) => ({ label: `${user.firstName} ${user.lastName}`, value: user.id }));
});

const descendantIds = computed(() => {
    const result = new Set<number>();
    const positionId = props.position?.id;
    if (positionId === undefined) return result;

    const stack = [positionId];
    while (stack.length > 0) {
        const current = stack.pop() as number;
        props.positions
            .filter((candidate) => candidate.parentPositionId === current)
            .forEach((candidate) => {
                if (!result.has(candidate.id)) {
                    result.add(candidate.id);
                    stack.push(candidate.id);
                }
            });
    }

    return result;
});

const moveItems = computed(() => {
    const positionId = props.position?.id;
    const candidates = props.positions
        .filter((candidate) => candidate.id !== positionId
            && candidate.paForPositionId === null
            && !descendantIds.value.has(candidate.id))
        .sort((a, b) => a.title.localeCompare(b.title, "da"))
        .map((candidate) => ({ label: candidate.title, value: String(candidate.id) }));

    return props.isSysAdmin ? [{ label: "Ingen (rodstilling)", value: rootValue }, ...candidates] : candidates;
});

watch(() => props.position?.id, () => {
    const current = props.position;
    editTitle.value = current?.title ?? "";
    editColor.value = current?.color ?? null;
    editGroups.value = current ? [...current.groups] : [];
    moveParent.value = current?.parentPositionId != null ? String(current.parentPositionId) : rootValue;
    selectedUserId.value = undefined;
    inviteEmail.value = "";
    childTitle.value = "";
    childColor.value = null;
    childGroups.value = [];
}, { immediate: true });

async function run(action: BusyAction, call: () => Promise<boolean>, success: string, failure: string): Promise<boolean> {
    busy.value = action;
    try {
        const ok = await call();
        notify.result(ok, success, failure);
        if (ok) emit("refresh");

        return ok;
    } finally {
        busy.value = null;
    }
}

async function saveEdit() {
    if (!props.position || !editTitle.value.trim()) return;

    const id = props.position.id;
    await run("edit", () => service.updatePosition(id, { title: editTitle.value.trim(), groups: editGroups.value, color: editColor.value }),
        "Stillingen er opdateret.", "Stillingen kunne ikke opdateres.");
}

async function addHolder() {
    if (!props.position || !selectedUserId.value) return;

    const id = props.position.id;
    const userId = selectedUserId.value;
    if (await run("holder", () => service.addHolder(id, userId), "Brugeren er tilknyttet.", "Brugeren kunne ikke tilknyttes.")) {
        selectedUserId.value = undefined;
    }
}

async function removeHolder(userId: string) {
    if (!props.position) return;

    const id = props.position.id;
    await run("holder", () => service.removeHolder(id, userId), "Brugeren er fjernet.", "Brugeren kunne ikke fjernes.");
}

async function sendInvite() {
    if (!props.position || !inviteEmail.value.trim()) return;

    const id = props.position.id;
    const email = inviteEmail.value.trim();
    busy.value = "invite";
    try {
        const ok = await service.invite(id, email);
        notify.result(ok, "Invitationen er sendt.", "Invitationen kunne ikke sendes.");
        if (ok) inviteEmail.value = "";
    } finally {
        busy.value = null;
    }
}

async function addPa() {
    if (!props.position) return;

    const id = props.position.id;
    await run("pa", () => service.createPa(id), "Den personlige assistent er tilføjet.", "Den personlige assistent kunne ikke tilføjes.");
}

async function saveMove() {
    if (!props.position) return;

    const id = props.position.id;
    const parentId = moveParent.value === rootValue ? null : Number(moveParent.value);
    await run("move", () => service.movePosition(id, parentId), "Stillingen er flyttet.", "Stillingen kunne ikke flyttes.");
}

async function createChild() {
    if (!props.position || !childTitle.value.trim()) return;

    const id = props.position.id;
    const created = await run("child", () => service.createPosition({ title: childTitle.value.trim(), parentPositionId: id, groups: childGroups.value, color: childColor.value }),
        "Stillingen er oprettet.", "Stillingen kunne ikke oprettes.");
    if (created) {
        childTitle.value = "";
        childColor.value = null;
        childGroups.value = [];
    }
}

async function removePosition() {
    if (!props.position) return;

    const id = props.position.id;
    if (await run("delete", () => service.deletePosition(id), "Stillingen er slettet.", "Stillingen kunne ikke slettes.")) {
        deleteOpen.value = false;
        open.value = false;
    }
}
</script>
