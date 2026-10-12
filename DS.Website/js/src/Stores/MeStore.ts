import MeService from "@/Services/MeService";
import ImpersonationService from "@/Services/ImpersonationService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { HomeViewModelDto, MeDto } from "@/types";

export const useMeStore = defineStore("me", () => {
  const meService = new MeService();
  const impersonationService = new ImpersonationService();
  const Me = ref<MeDto>({
    name: "",
    id: "",
    firstName: "",
    lastName: "",
    title: "",
    phone: "",
    mustEnableTwoFactor: false,
    isImpersonating: false,
    roles: [],
    appRoles: [],
    isAuthenticated: false,
    passkeys: [],
    profilePicture: null,
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

  async function UPDATE_PROFILE_PICTURE(publicId: string | null) {
    const ok = await meService.updateProfilePicture(publicId);
    if (ok) await GET_ME();
    return ok;
  }

  async function UPDATE_PHONE(phone: string) {
    const ok = await meService.updatePhone(phone);
    if (ok) await GET_ME();
    return ok;
  }

  async function START_IMPERSONATION(group: string) {
    const ok = await impersonationService.start(group);
    if (ok) {
      await GET_ME();
      await GET_HQ();
    }
    return ok;
  }

  async function STOP_IMPERSONATION() {
    const ok = await impersonationService.stop();
    if (ok) {
      await GET_ME();
      await GET_HQ();
    }
    return ok;
  }

  return {
    Me,
    Hq,
    ME,
    HQ,
    GET_ME,
    GET_HQ,
    UPDATE_PROFILE_PICTURE,
    UPDATE_PHONE,
    START_IMPERSONATION,
    STOP_IMPERSONATION,
  }
});
