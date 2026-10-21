<template>
    <BModal :model-value="active" @update:model-value="$emit('update:active', $event)" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Recovery codes</p>
                <button type="button" class="delete" @click="close" />
            </header>
            <section class="modal-card-body">
                <div class="notification is-warning is-light">
                    <p class="mb-2">
                        Gem disse recovery codes et sikkert sted. De vises kun én gang og kan bruges
                        til at logge ind, hvis du mister adgang til din autentificeringsapp.
                    </p>
                    <div class="tags">
                        <span v-for="code in codes" :key="code" class="tag is-warning is-light is-medium is-family-monospace">
                            {{ code }}
                        </span>
                    </div>
                </div>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" @click="close">Jeg har gemt koderne</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>
<script setup lang="ts">
import { BButton, BModal } from 'buefy';

defineProps<{
    active: boolean;
    codes: string[];
}>();

const emit = defineEmits<{
    (e: 'update:active', value: boolean): void;
}>();

const close = () => {
    emit('update:active', false);
};
</script>
<style lang="scss">
.modal-card {
    .modal-card-head,
    .modal-card-title,
    .modal-card-body,
    .modal-card-foot {
        color: #363636;
    }
}
</style>
