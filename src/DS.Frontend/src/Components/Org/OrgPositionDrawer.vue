<template>
    <USlideover v-model:open="open" :side="side" :title="isPa ? 'Personlig assistent' : (position?.title ?? 'Stilling')" :ui="side === 'bottom' ? { content: 'max-h-[88dvh]' } : undefined">
        <template #description>
            <span v-if="position" class="flex flex-wrap items-center gap-x-1 text-sm text-muted">
                <template v-if="isPa && owner">
                    <span>for</span>
                    <button type="button" class="underline decoration-accented underline-offset-2 hover:text-primary" @click="emit('select', owner.id)">{{ owner.title }}</button>
                </template>
                <span v-else-if="ancestors.length === 0">Rodstilling</span>
                <template v-for="(ancestor, index) in ancestors" v-else :key="ancestor.id">
                    <UIcon v-if="index > 0" name="i-lucide-chevron-right" class="size-3" />
                    <button type="button" class="underline decoration-accented underline-offset-2 hover:text-primary" @click="emit('select', ancestor.id)">{{ ancestor.title }}</button>
                </template>
            </span>
        </template>

        <template #actions>
            <UDropdownMenu v-if="canDelete" :items="menuItems" :content="{ align: 'end' }">
                <UButton icon="i-lucide-ellipsis-vertical" color="neutral" variant="ghost" aria-label="Flere handlinger" />
            </UDropdownMenu>
        </template>

        <template #body>
            <div v-if="position" class="flex flex-col gap-5">
                <UTabs v-model="tab" :items="tabItems" :content="false" variant="link" class="w-full" />

                <div v-if="tab === 'position'" class="flex flex-col gap-3">
                    <form v-if="canEdit" id="org-edit-form" class="flex flex-col gap-3" @submit.prevent="saveEdit">
                        <UFormField label="Titel" required>
                            <UInput v-model="editTitle" class="w-full" />
                        </UFormField>
                        <UFormField label="Farve">
                            <OrgColorSelect v-model="editColor" />
                        </UFormField>
                        <UFormField label="Grupper" help="Grupperne giver indehaverne adgang til de tilhørende dele af HQ.">
                            <USelectMenu v-model="editGroups" :items="groupItems" value-key="value" multiple placeholder="Vælg grupper…" class="w-full" />
                        </UFormField>
                        <div class="flex justify-end">
                            <UButton type="submit" label="Gem" :loading="busy === 'edit'" :disabled="busy != null || !editTitle.trim()" />
                        </div>
                    </form>
                    <template v-else>
                        <UAlert
                            color="neutral"
                            variant="subtle"
                            icon="i-lucide-info"
                            :title="position.isSystem ? 'Systemstilling. Titel og grupper kan ikke ændres.' : 'Du kan se stillingen, men ikke redigere den.'"
                        />
                        <div v-if="position.groups.length" class="flex flex-wrap gap-1">
                            <UBadge v-for="group in position.groups" :key="group" :label="groupTitle(group)" color="neutral" variant="outline" />
                        </div>
                        <p v-else class="text-sm text-muted">Ingen grupper</p>
                    </template>
                </div>

                <div v-else-if="tab === 'people'" class="flex flex-col gap-6">
                    <section>
                        <h3 class="mb-3 text-sm font-semibold text-highlighted">{{ isPa ? "Personlig assistent" : "Indehavere" }}</h3>
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

                    <section v-if="canManage" class="border-t border-default pt-5">
                        <h3 class="mb-3 text-sm font-semibold text-highlighted">Inviter ny bruger</h3>
                        <form class="flex" @submit.prevent="sendInvite">
                            <UFieldGroup class="w-full">
                                <UInput v-model="inviteEmail" type="email" placeholder="eksempel@mail.dk" class="w-full" aria-label="Email" />
                                <UButton type="submit" color="neutral" variant="outline" label="Send" :loading="busy === 'invite'" :disabled="busy != null || !inviteEmail.trim()" />
                            </UFieldGroup>
                        </form>
                    </section>

                    <section v-if="!isPa" class="border-t border-default pt-5">
                        <h3 class="mb-3 text-sm font-semibold text-highlighted">Personlig assistent</h3>
                        <UButton
                            v-if="pa"
                            color="neutral"
                            variant="outline"
                            icon="i-lucide-users"
                            :label="`Åbn personlig assistent (${pa.holders.length})`"
                            @click="emit('select', pa.id)"
                        />
                        <UButton
                            v-else-if="canManage"
                            color="neutral"
                            variant="outline"
                            icon="i-lucide-user-plus"
                            label="Tilføj personlig assistent"
                            :loading="busy === 'pa'"
                            :disabled="busy != null"
                            @click="addPa"
                        />
                        <p v-else class="text-sm text-muted">Ingen personlig assistent</p>
                    </section>
                </div>

                <div v-else-if="tab === 'structure'" class="flex flex-col gap-6">
                    <section v-if="canManage">
                        <h3 class="mb-3 text-sm font-semibold text-highlighted">Overordnet stilling</h3>
                        <OrgParentPicker v-model="moveParent" :position="position" :positions="positions" :roots="roots" :is-sys-admin="isSysAdmin" :can-select="canSelectParent" />
                        <div class="mt-3 flex justify-end">
                            <UButton icon="i-lucide-move" label="Flyt hertil" :loading="busy === 'move'" :disabled="busy != null || moveParent === currentParentValue" @click="saveMove" />
                        </div>
                        <p v-if="side === 'right'" class="mt-2 text-xs text-muted">Du kan også trække stillingen hen på en anden stilling i diagrammet eller listen.</p>
                    </section>

                    <section :class="{ 'border-t border-default pt-5': canManage }">
                        <h3 class="mb-3 text-sm font-semibold text-highlighted">Understillinger</h3>
                        <ul v-if="children.length" class="mb-3 flex flex-col gap-1">
                            <li v-for="child in children" :key="child.id">
                                <button
                                    type="button"
                                    class="flex w-full items-center gap-2 rounded-md border border-default px-3 py-2 text-left text-sm transition hover:bg-elevated/50 focus-visible:outline-2 focus-visible:outline-primary"
                                    @click="emit('select', child.id)"
                                >
                                    <span class="size-2.5 shrink-0 rounded-full border border-accented" :style="child.color ? { backgroundColor: child.color, borderColor: child.color } : undefined" />
                                    <span class="flex-1 truncate">{{ child.title }}</span>
                                    <span class="text-xs text-muted">{{ child.holders.length }} {{ child.holders.length === 1 ? "person" : "personer" }}</span>
                                    <UIcon name="i-lucide-chevron-right" class="size-4 text-muted" />
                                </button>
                            </li>
                        </ul>
                        <p v-else class="mb-3 text-sm text-muted">Ingen understillinger</p>
                        <UButton v-if="canCreateChild" color="neutral" variant="outline" icon="i-lucide-plus" label="Ny understilling" @click="emit('createChild', position.id)" />
                    </section>
                </div>
            </div>

            <ConfirmModal
                v-model:open="deleteOpen"
                :title="isPa ? 'Slet personlig assistent' : 'Slet stilling'"
                :description="`Slet ${isPa && owner ? `den personlige assistent for ${owner.title}` : position?.title}?`"
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
import type { DropdownMenuItem, TabsItem } from "@nuxt/ui";
import OrgService from "@/Services/OrgService";
import { useNotify } from "@/lib/notify";
import { canCreateChild as canCreateChildPosition, canManagePosition } from "@/lib/org/orgAccess";
import { collectAncestors, collectDescendantIds } from "@/lib/org/orgTree";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import OrgColorSelect from "@/Components/Org/OrgColorSelect.vue";
import OrgParentPicker from "@/Components/Org/OrgParentPicker.vue";
import { groupTitle, type OrgNodeModel, type OrgPositionDto, type UserDto } from "@/types";

type OrgDrawerTab = "position" | "people" | "structure";

type BusyAction = "edit" | "holder" | "invite" | "pa" | "move" | "delete";

const props = defineProps<{
    position: OrgPositionDto | null;
    positions: OrgPositionDto[];
    roots: OrgNodeModel[];
    assignableGroups: string[];
    users: UserDto[];
    currentUserId: string;
    isSysAdmin: boolean;
    side: "right" | "bottom";
    move: (positionId: number, parentPositionId: number | null) => Promise<boolean>;
}>();

const open = defineModel<boolean>("open", { required: true });
const tab = defineModel<OrgDrawerTab>("tab", { required: true });
const emit = defineEmits<{
    (e: "refresh"): void;
    (e: "select", positionId: number): void;
    (e: "createChild", positionId: number): void;
}>();

const service = new OrgService();
const notify = useNotify();

const rootValue = "root";
const busy = ref<BusyAction | null>(null);
const editTitle = ref("");
const editColor = ref<string | null>(null);
const editGroups = ref<string[]>([]);
const selectedUserId = ref<string | undefined>();
const inviteEmail = ref("");
const moveParent = ref<string>(rootValue);
const deleteOpen = ref(false);

const isPa = computed(() => props.position?.paForPositionId != null);
const owner = computed(() => isPa.value ? props.positions.find((candidate) => candidate.id === props.position!.paForPositionId) ?? null : null);
const pa = computed(() => props.position === null ? null : props.positions.find((candidate) => candidate.paForPositionId === props.position!.id) ?? null);
const ancestors = computed(() => props.position === null ? [] : collectAncestors(props.position.id, props.positions));
const children = computed(() => props.position === null
    ? []
    : props.positions.filter((candidate) => candidate.parentPositionId === props.position!.id && candidate.paForPositionId === null));

const canManage = computed(() => props.position !== null
    && canManagePosition(props.position, props.positions, props.currentUserId, props.isSysAdmin));
const canEdit = computed(() => canManage.value && props.position !== null && !props.position.isSystem && !isPa.value);
const canCreateChild = computed(() => props.position !== null
    && canCreateChildPosition(props.position, props.positions, props.currentUserId, props.isSysAdmin));
const canRemoveHolder = computed(() => !props.position?.isSystem || (props.position?.holders.length ?? 0) > 1);
const canDelete = computed(() => canManage.value && (isPa.value || !props.position?.isSystem));
const deleteBlockedReason = computed(() => {
    if (props.position === null) return null;
    if (!isPa.value && collectDescendantIds(props.position.id, props.positions).size > 0) return "Flyt eller slet understillingerne først";
    if (props.position.holders.length > 0) return "Fjern personerne først";

    return null;
});
const currentParentValue = computed(() => props.position?.parentPositionId != null ? String(props.position.parentPositionId) : rootValue);

const groupItems = computed(() => props.assignableGroups.map((group) => ({ label: groupTitle(group), value: group })));

const userItems = computed(() => {
    const held = new Set(props.position?.holders.map((holder) => holder.id) ?? []);

    return props.users
        .filter((user) => !held.has(user.id))
        .map((user) => ({ label: `${user.firstName} ${user.lastName}`, value: user.id }));
});

const tabItems = computed<TabsItem[]>(() => {
    if (isPa.value) return [{ label: "Personer", value: "people", badge: props.position?.holders.length ?? 0 }];

    return [
        { label: "Stilling", value: "position" },
        { label: "Personer", value: "people", badge: props.position?.holders.length ?? 0 },
        ...(canManage.value || canCreateChild.value ? [{ label: "Struktur", value: "structure" }] : [])
    ];
});

const menuItems = computed<DropdownMenuItem[][]>(() => [[{
    label: isPa.value ? "Slet personlig assistent" : "Slet stilling",
    description: deleteBlockedReason.value ?? undefined,
    icon: "i-lucide-trash-2",
    color: "error",
    disabled: deleteBlockedReason.value !== null,
    onSelect: () => { deleteOpen.value = true; }
}]]);

function canSelectParent(target: OrgPositionDto): boolean {
    return canCreateChildPosition(target, props.positions, props.currentUserId, props.isSysAdmin);
}

watch(() => props.position?.id, () => {
    const current = props.position;
    editTitle.value = current?.title ?? "";
    editColor.value = current?.color ?? null;
    editGroups.value = current ? [...current.groups] : [];
    moveParent.value = currentParentValue.value;
    selectedUserId.value = undefined;
    inviteEmail.value = "";
}, { immediate: true });

watch(currentParentValue, (value) => {
    moveParent.value = value;
});

watch(tabItems, (items) => {
    if (!items.some((item) => item.value === tab.value)) tab.value = items[0]!.value as OrgDrawerTab;
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
    const parent = props.positions.find((candidate) => String(candidate.id) === moveParent.value);
    const parentId = parent?.id ?? null;
    if (parentId === null && moveParent.value !== rootValue) return;

    busy.value = "move";
    try {
        await props.move(id, parentId);
    } finally {
        busy.value = null;
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
