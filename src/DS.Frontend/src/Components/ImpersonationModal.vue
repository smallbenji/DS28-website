<template>
    <UModal v-model:open="open" title="Vis som …" description="Vælg en rolle for at se systemet, som udvalget selv ser det.">
        <template #body>
            <div v-if="loading" class="flex flex-col gap-2">
                <USkeleton v-for="index in 5" :key="index" class="h-12" />
            </div>
            <p v-else-if="groups.length === 0" class="text-sm text-muted">Der er ingen roller at vise som.</p>
            <ul v-else class="flex flex-col gap-1">
                <li v-for="group in groups" :key="group.name">
                    <button
                        type="button"
                        class="flex w-full items-center justify-between gap-2 rounded-md px-3 py-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary disabled:opacity-50"
                        :disabled="starting != null"
                        @click="select(group.name)"
                    >
                        <span class="min-w-0">
                            <span class="block truncate text-sm font-medium text-highlighted">{{ group.title }}</span>
                            <span class="block truncate font-mono text-xs text-muted">{{ group.name }}</span>
                        </span>
                        <UIcon :name="starting === group.name ? 'i-lucide-loader-circle' : 'i-lucide-chevron-right'" class="size-4 shrink-0 text-muted" :class="{ 'animate-spin': starting === group.name }" />
                    </button>
                </li>
            </ul>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useRouter } from "vue-router";
import { useMeStore } from "@/Stores/MeStore";
import ImpersonationService from "@/Services/ImpersonationService";
import { useNotify } from "@/lib/notify";
import type { ImpersonationGroupDto } from "@/types";

const open = defineModel<boolean>("open", { required: true });

const router = useRouter();
const meStore = useMeStore();
const impersonationService = new ImpersonationService();
const notify = useNotify();

const groups = ref<ImpersonationGroupDto[]>([]);
const loading = ref(false);
const starting = ref<string | null>(null);

watch(open, async (isOpen) => {
    if (!isOpen) return;

    loading.value = true;
    try {
        groups.value = await impersonationService.getGroups();
    } finally {
        loading.value = false;
    }
});

async function select(group: string) {
    starting.value = group;
    try {
        const ok = await meStore.START_IMPERSONATION(group);
        if (!ok) {
            notify.error("Visningen kunne ikke startes.");
            return;
        }

        open.value = false;
        await router.push("/");
    } finally {
        starting.value = null;
    }
}
</script>
