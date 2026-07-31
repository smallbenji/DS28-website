import ActivityService from "@/Services/ActivityService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useActivityStore = defineStore("activity", () => {
    const activityService = new ActivityService();
    const Activities = ref<ActivityDTO[] | null>(null);
    const ACTIVITIES = computed(() => Activities.value);

    async function GET_ACTIVITIES() {
        var data = await activityService.getActivities();
        Activities.value = data;
        return data;
    }

    return {
        Activities,
        ACTIVITIES,
        GET_ACTIVITIES
    };
});
