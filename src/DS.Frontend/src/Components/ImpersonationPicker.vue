<template>
    <b-modal
        v-model="open"
        has-modal-card
        trap-focus
    >
        <div class="modal-card impersonation-picker">
            <header class="modal-card-head">
                <p class="modal-card-title">Vis som …</p>
                <button class="delete" aria-label="close" @click="open = false"></button>
            </header>
            <section class="modal-card-body">
                <p v-if="Me.isImpersonating" class="impersonation-picker-note">
                    Du vises allerede som <strong>{{ Me.title || Me.roles[0] }}</strong>.
                </p>
                <p v-else class="impersonation-picker-note">
                    Vælg en rolle for at se systemet, som udvalget selv ser det.
                </p>
                <ul class="impersonation-picker-list">
                    <li v-for="group in groups" :key="group.name">
                        <button
                            type="button"
                            class="impersonation-picker-item"
                            :disabled="Me.isImpersonating"
                            @click="select(group.name)"
                        >
                            <span>
                                <strong>{{ group.title }}</strong>
                                <small>{{ group.name }}</small>
                            </span>
                            <b-icon pack="fas" icon="chevron-right" size="is-small" />
                        </button>
                    </li>
                </ul>
            </section>
        </div>
    </b-modal>
</template>
<script lang="ts" setup>
import { BIcon, BModal } from 'buefy';
import { computed, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useMeStore } from '@/Stores/MeStore';
import ImpersonationService from '@/Services/ImpersonationService';
import type { ImpersonationGroupDto } from '@/types';

const props = defineProps<{
    open: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:open', value: boolean): void;
}>();

const open = computed({
    get: () => props.open,
    set: (value: boolean) => emit('update:open', value),
});

const meStore = useMeStore();
const { Me } = storeToRefs(meStore);
const impersonationService = new ImpersonationService();

const groups = ref<ImpersonationGroupDto[]>([]);

watch(
    () => props.open,
    async (isOpen) => {
        if (isOpen) {
            groups.value = await impersonationService.getGroups();
        }
    }
);

async function select(group: string) {
    const ok = await meStore.START_IMPERSONATION(group);
    if (ok) open.value = false;
}
</script>
<style lang="scss" scoped>
.impersonation-picker {
    &-note {
        margin-bottom: 1rem;
    }

    &-list {
        list-style: none;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    &-item {
        display: flex;
        align-items: center;
        justify-content: space-between;
        width: 100%;
        padding: 0.75rem 1rem;
        border: none;
        border-radius: 10px;
        background-color: #f5f5f5;
        cursor: pointer;
        transition: background-color 0.15s ease;
        text-align: left;
        color: inherit;

        &:hover:not(:disabled) {
            background-color: #e8e8e8;
        }

        &:disabled {
            opacity: 0.5;
            cursor: default;
        }

        span {
            display: flex;
            flex-direction: column;
        }

        small {
            color: hsl(0, 0%, 48%);
        }
    }
}
</style>