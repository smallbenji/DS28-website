<template>
    <UModal v-model:open="open" :dismissible="!busy" :close="false" title="Kort" :ui="{ content: 'sm:max-w-5xl overflow-hidden' }">
        <template #content>
            <BoardCardDetail
                v-model:busy="busy"
                variant="modal"
                :board-id="boardId"
                :column-id="columnId"
                :card="card"
                :can-manage="canManage"
                @close="open = false"
                @removed="open = false"
                @open-card="(other) => emit('openCard', other)"
            />
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import BoardCardDetail from "@/Components/Board/BoardCardDetail.vue";
import type { WorkItemDto } from "@/types";

defineProps<{ boardId: number; columnId: number; card: WorkItemDto | null; canManage: boolean }>();
const open = defineModel<boolean>("open", { required: true });
const emit = defineEmits<{ openCard: [card: WorkItemDto] }>();

const busy = ref(false);
</script>
