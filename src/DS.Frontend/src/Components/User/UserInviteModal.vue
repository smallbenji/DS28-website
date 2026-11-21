<template>
    <UModal v-model:open="open" title="Inviter ny frivillig">
        <template #body>
            <form id="invite-user-form" class="flex flex-col gap-4" @submit.prevent="send">
                <UFormField label="Email" required>
                    <UInput v-model="email" type="email" placeholder="eksempel@mail.dk" class="w-full" autofocus />
                </UFormField>
                <UFormField label="Roller">
                    <USelectMenu v-model="roles" :items="assignableRoles" multiple placeholder="Vælg roller…" icon="i-lucide-tags" class="w-full" />
                </UFormField>
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton type="submit" form="invite-user-form" icon="i-lucide-mail" label="Send invitation" :loading="sending" :disabled="!email.trim()" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { storeToRefs } from "pinia";
import { useToast } from "@nuxt/ui/composables";
import { useUserStore } from "@/Stores/UserStore";

const open = defineModel<boolean>("open", { required: true });

const userStore = useUserStore();
const { Groups, AssignableGroups } = storeToRefs(userStore);
const toast = useToast();

const email = ref("");
const roles = ref<string[]>([]);
const sending = ref(false);

const assignableRoles = computed(() => Groups.value
    .map((group) => group.name)
    .filter((name) => AssignableGroups.value.includes(name)));

watch(open, (isOpen) => {
    if (!isOpen) return;

    email.value = "";
    roles.value = [];
});

async function send() {
    if (!email.value.trim()) return;

    sending.value = true;
    try {
        const ok = await userStore.INVITE_USER(email.value, roles.value);
        if (ok) {
            toast.add({ title: `Invitation sendt til ${email.value}`, color: "success", icon: "i-lucide-circle-check" });
            open.value = false;
        } else {
            toast.add({ title: "Invitationen kunne ikke sendes.", color: "error", icon: "i-lucide-circle-alert" });
        }
    } finally {
        sending.value = false;
    }
}
</script>
