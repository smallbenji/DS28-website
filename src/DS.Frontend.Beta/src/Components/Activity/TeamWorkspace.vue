<template>
    <div class="flex flex-col gap-6">
        <div>
            <h2 class="text-xl font-semibold text-highlighted">{{ team.name }}</h2>
            <p class="text-sm text-muted">{{ team.members.length }} medlemmer · {{ team.activities.length }} aktiviteter</p>
        </div>
        <div class="grid min-w-0 gap-6 xl:grid-cols-2">
            <TeamMembers class="min-w-0 self-start" :team="team" />
            <TeamActivities class="min-w-0 self-start" :team="team" />
        </div>
    </div>
</template>

<script setup lang="ts">
import TeamMembers from "@/Components/Activity/TeamMembers.vue";
import TeamActivities from "@/Components/Activity/TeamActivities.vue";
import type { ActivityTeamDto } from "@/types";

defineProps<{ team: ActivityTeamDto }>();
</script>
