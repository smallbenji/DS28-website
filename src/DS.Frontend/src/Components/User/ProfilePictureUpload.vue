<template>
    <input ref="fileInput" type="file" accept="image/*" class="sr-only" tabindex="-1" aria-hidden="true" @change="onFileSelected" />
    <UButton
        :size="size"
        icon="i-lucide-image-up"
        :label="hasPicture ? 'Vælg nyt billede' : 'Vælg billede'"
        :loading="disabled"
        :disabled="disabled || !!imageUrl"
        @click="fileInput?.click()"
    />

    <UModal v-model:open="active" title="Beskær profilbillede" description="Træk billedet, og zoom med to fingre eller musehjulet. Du kan også bruge knapperne. Kun det kvadratiske udsnit bliver uploadet." :dismissible="!saving" @after:leave="clearImage">
        <template #body>
            <Cropper
                v-if="imageUrl"
                ref="cropper"
                :src="imageUrl"
                class="mb-4 h-72 w-full overflow-hidden rounded-md bg-elevated"
                :stencil-props="{ aspectRatio: 1, movable: false, resizable: false, handlers: {} }"
                :stencil-size="stencilSize"
                :resize-image="{ adjustStencil: false }"
                :canvas="{ width: 512, height: 512 }"
                image-restriction="stencil"
                :transitions="false"
                @ready="ready = true"
                @error="imageFailed"
            />
            <fieldset :disabled="!ready || saving" class="flex flex-col gap-2">
                <legend class="sr-only">Justér profilbillede</legend>
                <div class="flex flex-wrap gap-2" role="group" aria-label="Zoom">
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-zoom-in" label="Zoom ind" @click="cropper?.zoom(1.2)" />
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-zoom-out" label="Zoom ud" @click="cropper?.zoom(1 / 1.2)" />
                </div>
                <div class="flex flex-wrap gap-2" role="group" aria-label="Flyt billedet">
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-arrow-left" label="Venstre" @click="cropper?.move(-20, 0)" />
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-arrow-right" label="Højre" @click="cropper?.move(20, 0)" />
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-arrow-up" label="Op" @click="cropper?.move(0, -20)" />
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-arrow-down" label="Ned" @click="cropper?.move(0, 20)" />
                </div>
                <div class="flex flex-wrap gap-2" role="group" aria-label="Rotation og spejling">
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-rotate-cw" label="Drej 90°" @click="cropper?.rotate(90)" />
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-flip-horizontal-2" label="Spejlvend" @click="cropper?.flip(true, false)" />
                    <UButton color="neutral" variant="outline" size="sm" icon="i-lucide-undo-2" label="Nulstil" @click="cropper?.reset()" />
                </div>
            </fieldset>
            <UAlert v-if="error" class="mt-4" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" :disabled="saving" @click="active = false" />
                <UButton label="Beskær og upload" :disabled="!ready" :loading="saving" @click="confirm" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref } from "vue";
import { Cropper, type Boundaries } from "vue-advanced-cropper";
import "vue-advanced-cropper/dist/style.css";

withDefaults(defineProps<{ hasPicture: boolean; disabled: boolean; size?: "xs" | "sm" | "md" }>(), { size: "md" });
const emit = defineEmits<{ confirm: [file: File] }>();

const fileInput = ref<HTMLInputElement | null>(null);
const active = ref(false);
const saving = ref(false);
const cropper = ref<InstanceType<typeof Cropper> | null>(null);
const ready = ref(false);
const error = ref("");
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
    error.value = "Billedet kunne ikke åbnes. Vælg et andet billede.";
}

function onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = "";
    if (!file) return;

    clearImage();
    imageUrl.value = URL.createObjectURL(file);
    error.value = "";
    active.value = true;
}

async function confirm() {
    if (!ready.value || saving.value) return;

    saving.value = true;
    try {
        const canvas = cropper.value?.getResult().canvas;
        if (!canvas) throw new Error("No cropped image");

        const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/png"));
        if (!blob) throw new Error("Image conversion failed");

        emit("confirm", new File([blob], "profilbillede.png", { type: "image/png" }));
        active.value = false;
    } catch {
        error.value = "Billedet kunne ikke beskæres. Prøv igen.";
    } finally {
        saving.value = false;
    }
}

onBeforeUnmount(clearImage);
</script>
