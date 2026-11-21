import FormService from "@/Services/FormService";
import { getErrorMessage, getErrorStatus } from "@/lib/api";
import type { FormTemplate } from "@/types";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useFormStore = defineStore("forms", () => {
    const formService = new FormService();
    const template = ref<FormTemplate | null>(null);
    const isLoading = ref(false);
    const error = ref<string | null>(null);
    let requestId = 0;

    async function GET_TEMPLATE(id: string): Promise<FormTemplate | null> {
        const currentRequestId = ++requestId;
        template.value = null;
        error.value = null;
        isLoading.value = true;

        try {
            const data = await formService.getTemplate(id);
            if (currentRequestId !== requestId) return null;

            template.value = data;
            return data;
        } catch (cause) {
            if (currentRequestId !== requestId) return null;

            error.value = getErrorStatus(cause) === 404
                ? "Formularen findes ikke."
                : getErrorMessage(cause, "Formularen kunne ikke hentes.");
            return null;
        } finally {
            if (currentRequestId === requestId) {
                isLoading.value = false;
            }
        }
    }

    function RESET() {
        requestId++;
        template.value = null;
        error.value = null;
        isLoading.value = false;
    }

    return {
        template,
        isLoading,
        error,
        GET_TEMPLATE,
        RESET,
    };
});
