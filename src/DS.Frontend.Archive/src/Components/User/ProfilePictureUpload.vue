<template>
    <BUpload v-model="selectedFile" accept="image/*" :disabled="disabled || !!imageUrl">
        <BButton tag="a" type="is-primary" :loading="disabled">
            {{ hasPicture ? 'Vælg nyt billede' : 'Vælg billede' }}
        </BButton>
    </BUpload>
    <BModal v-model="active" has-modal-card aria-role="dialog" aria-label="Beskær profilbillede" aria-modal
        :can-cancel="saving ? false : ['escape', 'outside']" @after-leave="clearImage">
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Beskær profilbillede</p>
            </header>
            <section class="modal-card-body">
                <p class="mb-4">Træk billedet, og zoom med to fingre eller musehjulet. Du kan også bruge knapperne.
                    Kun det kvadratiske udsnit bliver uploadet.</p>
                <Cropper v-if="imageUrl" ref="cropper" :src="imageUrl" class="profile-crop-preview mb-4"
                    :stencil-props="{ aspectRatio: 1, movable: false, resizable: false, handlers: {} }"
                    :stencil-size="stencilSize" :resize-image="{ adjustStencil: false }"
                    :canvas="{ width: 512, height: 512 }" image-restriction="stencil" :transitions="false"
                    @ready="ready = true" @error="imageFailed" />
                <fieldset :disabled="!ready || saving" class="profile-crop-controls">
                    <legend class="is-sr-only">Justér profilbillede</legend>
                    <div class="buttons" role="group" aria-label="Zoom">
                        <BButton @click="cropper?.zoom(1.2)">Zoom ind</BButton>
                        <BButton @click="cropper?.zoom(1 / 1.2)">Zoom ud</BButton>
                    </div>
                    <div class="buttons" role="group" aria-label="Flyt billedet">
                        <BButton @click="cropper?.move(-20, 0)">Venstre</BButton>
                        <BButton @click="cropper?.move(20, 0)">Højre</BButton>
                        <BButton @click="cropper?.move(0, -20)">Op</BButton>
                        <BButton @click="cropper?.move(0, 20)">Ned</BButton>
                    </div>
                    <div class="buttons" role="group" aria-label="Rotation og spejling">
                        <BButton @click="cropper?.rotate(90)">Drej 90°</BButton>
                        <BButton @click="cropper?.flip(true, false)">Spejlvend</BButton>
                        <BButton @click="cropper?.reset()">Nulstil</BButton>
                    </div>
                </fieldset>
                <p v-if="error" role="alert" class="has-text-danger">{{ error }}</p>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :disabled="!ready" :loading="saving" @click="confirm">Beskær og upload</BButton>
                    <BButton :disabled="saving" @click="active = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue';
import { BButton, BModal, BUpload } from 'buefy';
import { Cropper, type Boundaries } from 'vue-advanced-cropper';
import 'vue-advanced-cropper/dist/style.css';

defineProps<{ hasPicture: boolean; disabled: boolean }>();
const emit = defineEmits<{ confirm: [file: File] }>();
const selectedFile = ref<File | null>(null);
const active = ref(false);
const saving = ref(false);
const cropper = ref<InstanceType<typeof Cropper> | null>(null);
const ready = ref(false);
const error = ref('');
const imageUrl = ref<string | null>(null);

function clearImage() {
    if (imageUrl.value) URL.revokeObjectURL(imageUrl.value);
    imageUrl.value = null;
    ready.value = false;
}

function stencilSize({ boundaries }: { boundaries: Boundaries }) {
    const size = Math.min(boundaries.width, boundaries.height) * 0.8;
    return { width: size, height: size };
}

function imageFailed() {
    ready.value = false;
    error.value = 'Billedet kunne ikke åbnes. Vælg et andet billede.';
}

watch(selectedFile, (file) => {
    if (!file) return;
    selectedFile.value = null;
    clearImage();
    imageUrl.value = URL.createObjectURL(file);
    error.value = '';
    active.value = true;
});

async function confirm() {
    if (!ready.value || saving.value) return;
    saving.value = true;
    try {
        const canvas = cropper.value?.getResult().canvas;
        if (!canvas) throw new Error('No cropped image');
        const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/png'));
        if (!blob) throw new Error('Image conversion failed');
        emit('confirm', new File([blob], 'profilbillede.png', { type: 'image/png' }));
        active.value = false;
    } catch {
        error.value = 'Billedet kunne ikke beskæres. Prøv igen.';
    } finally {
        saving.value = false;
    }
}

onBeforeUnmount(clearImage);
</script>

<style scoped>
.profile-crop-preview {
    height: 320px;
}

.profile-crop-controls {
    border: 0;
    padding: 0;
    min-width: 0;
}
</style>
