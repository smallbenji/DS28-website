import MeService from "@/Services/MeService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useMeStore = defineStore("me", () => {
    const meService = new MeService();
    const Me = ref<MeDTO>({
        name: "",
        roles: [],
        appRoles: []
    });
    const ME = computed(() => Me.value);
    const Hq = ref<HomeViewModel>({shortcuts: []});
    const HQ = computed(() => Hq.value);

    async function GET_ME() {
        var data = await meService.getMe();
        Me.value = data;
        return data;
    }

    async function GET_HQ() {
        var data = await meService.getHQ();
        Hq.value = data;
        return data;
    }

    return {
        Me,
        Hq,
        ME,
        HQ,
        GET_ME,
        GET_HQ,
    }
});