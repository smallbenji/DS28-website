import VersionService from "@/Services/VersionService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { VersionDto } from "@/types";

export const useVersionStore = defineStore("version", () => {
  const versionService = new VersionService();
  const Version = ref<VersionDto>({ version: "", name: "" });
  const VERSION = computed(() => Version.value);

  async function GET_VERSION() {
    var data = await versionService.get();
    Version.value = data;
    return data;
  }

  return {
    Version,
    VERSION,
    GET_VERSION,
  }
});