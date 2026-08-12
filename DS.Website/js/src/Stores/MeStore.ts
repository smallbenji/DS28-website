import MeService from "@/Services/MeService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { HomeViewModelDto, MeDto } from "@/types";

export const useMeStore = defineStore("me", () => {
  const meService = new MeService();
  const Me = ref<MeDto>({
    name: "",
    firstName: "",
    lastName: "",
    mustEnableTwoFactor: false,
    roles: [],
    appRoles: [],
    isAuthenticated: false,
    passkeys: [],
  });
  const ME = computed(() => Me.value);
  const Hq = ref<HomeViewModelDto>({ shortcuts: [] });
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
