import MeService from "@/Services/MeService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useMeStore = defineStore("me", () => {
    const meService = new MeService();
    const Me = ref<MeDTO | null>(null);
    const ME = computed(() => Me.value);

    async function GET_ME() {
        var data = await meService.getMe();
        Me.value = data;
        return data;
    }

    return {
        Me,
        ME,
        GET_ME
    };
});
