<template>
    <li>
        <div
            class="flex min-h-11 items-center gap-1.5 rounded-md py-1 pe-2 transition hover:bg-elevated/50"
            :class="{
                'opacity-40': chart.isDimmed(position.id) || chart.draggedId.value === position.id,
                'bg-primary/10': chart.highlightedId.value === position.id,
                'bg-primary/10 outline-2 outline-dashed outline-primary': chart.dropTargetId.value === position.id
            }"
            :style="{ paddingInlineStart: `${0.5 + depth * 1.25}rem` }"
            :data-position-id="position.id"
            :draggable="draggable"
            @dragstart="chart.handleDragStart($event, position)"
            @dragend="chart.endDrag()"
            @dragover="chart.handleDragOver($event, position)"
            @dragleave="chart.handleDragLeave($event, position)"
            @drop.prevent="chart.drop(position)"
        >
            <UIcon v-if="draggable" name="i-lucide-grip-vertical" class="size-4 shrink-0 cursor-grab text-muted" />
            <UButton
                v-if="hasChildren"
                :icon="collapsed ? 'i-lucide-chevron-right' : 'i-lucide-chevron-down'"
                size="xs"
                color="neutral"
                variant="ghost"
                :aria-expanded="!collapsed"
                :aria-label="collapsed ? `Fold ${position.title} ud` : `Fold ${position.title} sammen`"
                @click="chart.toggle(position.id)"
            />
            <span v-else class="size-6 shrink-0" />
            <span class="size-2.5 shrink-0 rounded-full border border-accented" :style="position.color ? { backgroundColor: position.color, borderColor: position.color } : undefined" />

            <button
                type="button"
                class="flex min-w-0 flex-1 flex-col rounded px-1 py-0.5 text-left focus-visible:outline-2 focus-visible:outline-primary"
                @click="chart.select(position.id)"
            >
                <span class="flex flex-wrap items-center gap-x-2 font-semibold" :class="chart.isMatch(position.id) ? 'text-primary' : 'text-highlighted'">
                    {{ position.title }}
                    <span v-if="collapsed" class="text-xs font-normal text-muted">{{ chart.hiddenCount(position.id) }} skjult</span>
                </span>
                <span class="truncate text-xs text-muted">{{ holderNames }}</span>
            </button>

            <UButton
                v-if="node.pa"
                label="PA"
                size="xs"
                color="neutral"
                variant="outline"
                class="rounded-full border-dashed"
                :aria-label="`Åbn personlig assistent for ${position.title}`"
                @click="chart.select(node.pa.id)"
            />
            <UButton
                v-if="canCreate"
                icon="i-lucide-plus"
                size="sm"
                color="primary"
                variant="ghost"
                :aria-label="`Ny understilling under ${position.title}`"
                @click="chart.startCreate(position.id)"
            />
        </div>

        <div
            v-if="chart.creatingUnder.value === position.id"
            class="my-1 me-2 rounded-lg border border-accented bg-default p-4 shadow-md"
            :style="{ marginInlineStart: `${0.5 + (depth + 1) * 1.25}rem` }"
        >
            <OrgCreateForm
                :parent-title="position.title"
                :parent-color="position.color"
                :group-items="chart.groupItems.value"
                :loading="chart.creating.value"
                @submit="chart.create"
                @cancel="chart.cancelCreate()"
            />
        </div>

        <ul v-if="hasChildren && !collapsed">
            <OrgListRow v-for="child in node.children" :key="child.position.id" :node="child" :depth="depth + 1" />
        </ul>
    </li>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { OrgNodeModel } from "@/types";
import { useOrgChart } from "@/lib/org/orgChartContext";
import OrgCreateForm from "@/Components/Org/OrgCreateForm.vue";

defineOptions({ name: "OrgListRow" });

const props = defineProps<{ node: OrgNodeModel; depth: number }>();

const chart = useOrgChart();

const position = computed(() => props.node.position);
const hasChildren = computed(() => props.node.children.length > 0);
const collapsed = computed(() => chart.isCollapsed(position.value.id));
const canCreate = computed(() => chart.canCreate(position.value));
const draggable = computed(() => chart.canDrag(position.value));
const holderNames = computed(() => position.value.holders.length
    ? position.value.holders.map((holder) => `${holder.firstName} ${holder.lastName}`).join(", ")
    : "Ingen tilknyttet");
</script>
