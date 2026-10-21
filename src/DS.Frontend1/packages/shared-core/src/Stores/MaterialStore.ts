import MaterialService from "../Services/MaterialService";
import type { MaterialDto } from "../types";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useMaterialStore = defineStore("materials", () => {
    const materialService = new MaterialService();
    const materials = ref<MaterialDto[]>([]);

    async function GET_MATERIALS() {
        var data = await materialService.getMaterials();
        materials.value = data;
        return data;
    }

    return {
        materials,
        GET_MATERIALS,
    };
})
