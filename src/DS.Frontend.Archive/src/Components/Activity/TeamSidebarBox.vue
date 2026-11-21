<template>
    <div class="team-sidebar-box" :class="{ active: selected }">
        <div class="team-sidebar-box-name">{{ team.name }}</div>
        <div class="team-sidebar-box-meta">
            <span>
                <i class="fas fa-users"></i>
                {{ team.members.length }} medlemmer
            </span>
            <span>
                <i class="fas fa-newspaper"></i>
                {{ team.activities.length }} aktiviteter
            </span>
        </div>
    </div>
</template>
<script lang="ts" setup>
import type { ActivityTeamDto } from '@/types';

defineProps<{
    team: ActivityTeamDto;
    selected: boolean;
}>();
</script>
<style lang="scss">
.team-sidebar-box {
    border: 1px solid rgba(0, 0, 0, 0.1);
    padding: 1rem 1.2rem;
    border-radius: 10px;
    margin-bottom: 1rem;
    cursor: pointer;
    transition: background-color 0.15s ease, border-color 0.15s ease, box-shadow 0.15s ease;

    &-name {
        font-weight: 600;
        font-size: 1rem;
        margin-bottom: 0.4rem;
    }

    &-meta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        font-size: 0.8rem;
        color: #777;

        i {
            margin-right: 0.3rem;
        }
    }

    &:hover {
        background-color: #f7f8fa;
        border-color: rgba(0, 0, 0, 0.2);
    }

    &.active {
        background: #ececec;
        border: 1px solid #696969;
        box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
    }
}
</style>
