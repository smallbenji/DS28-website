<template>
    <li class="org-node">
        <div class="org-node-body">
            <div class="org-card-wrap">
                <div
                    role="button"
                    tabindex="0"
                    class="org-card relative cursor-pointer rounded-lg border bg-default p-3 pb-5 text-center shadow-xs transition hover:shadow-md focus-visible:outline-2 focus-visible:outline-primary"
                    :class="[
                        position.isSystem ? 'border-inverted/60' : 'border-default',
                        {
                            'opacity-40': chart.isDimmed(position.id) || chart.draggedId.value === position.id,
                            'ring-2 ring-primary ring-offset-2 ring-offset-(--ui-bg)': chart.isMatch(position.id) || chart.highlightedId.value === position.id,
                            'outline-2 outline-dashed outline-offset-4 outline-primary': chart.dropTargetId.value === position.id
                        }
                    ]"
                    :style="tint"
                    :data-position-id="position.id"
                    :draggable="draggable"
                    :aria-label="`Åbn ${position.title}`"
                    @click="chart.select(position.id)"
                    @keydown.enter.prevent="chart.select(position.id)"
                    @keydown.space.prevent="chart.select(position.id)"
                    @dragstart="chart.handleDragStart($event, position)"
                    @dragend="chart.endDrag()"
                    @dragover="chart.handleDragOver($event, position)"
                    @dragleave="chart.handleDragLeave($event, position)"
                    @drop.prevent="chart.drop(position)"
                >
                    <UIcon v-if="draggable" name="i-lucide-grip-vertical" class="absolute top-2 left-1 size-4 text-muted" />
                    <span class="block font-semibold text-highlighted">{{ position.title }}</span>
                    <span v-if="position.groups.length" class="mt-1.5 flex flex-wrap justify-center gap-1">
                        <UBadge v-for="group in position.groups" :key="group" :label="groupTitle(group)" color="neutral" variant="outline" size="sm" />
                    </span>
                    <OrgHolders :holders="position.holders" />
                </div>

                <div v-if="canCreate || hasChildren" class="org-pill">
                    <UPopover
                        v-if="canCreate"
                        :open="chart.creatingUnder.value === position.id"
                        :content="{ side: 'bottom', align: 'center' }"
                        @update:open="onCreateOpen"
                    >
                        <UButton
                            icon="i-lucide-plus"
                            size="xs"
                            color="neutral"
                            variant="outline"
                            class="rounded-full bg-default text-primary"
                            :aria-label="`Ny understilling under ${position.title}`"
                        />
                        <template #content>
                            <div class="w-80 p-4">
                                <OrgCreateForm
                                    :parent-title="position.title"
                                    :parent-color="position.color"
                                    :group-items="chart.groupItems.value"
                                    :loading="chart.creating.value"
                                    @submit="chart.create"
                                    @cancel="chart.cancelCreate()"
                                />
                            </div>
                        </template>
                    </UPopover>
                    <UButton
                        v-if="hasChildren"
                        :icon="collapsed ? 'i-lucide-chevron-right' : 'i-lucide-chevron-down'"
                        :label="collapsed ? String(chart.hiddenCount(position.id)) : undefined"
                        size="xs"
                        color="neutral"
                        variant="outline"
                        class="rounded-full bg-default"
                        :aria-expanded="!collapsed"
                        :aria-label="collapsed ? `Fold ${position.title} ud` : `Fold ${position.title} sammen`"
                        @click="chart.toggle(position.id)"
                    />
                </div>
            </div>

            <button
                v-if="node.pa"
                type="button"
                class="org-pa rounded-lg border border-dashed border-accented bg-default p-3 text-center transition hover:shadow-md focus-visible:outline-2 focus-visible:outline-primary"
                :class="{ 'opacity-40': chart.isDimmed(position.id) }"
                :style="tint"
                @click="chart.select(node.pa.id)"
            >
                <span class="block text-sm font-semibold text-highlighted">Personlig assistent</span>
                <OrgHolders :holders="node.pa.holders" />
            </button>
        </div>

        <ul v-if="hasChildren && !collapsed" class="org-children">
            <OrgNode v-for="child in node.children" :key="child.position.id" :node="child" />
        </ul>
    </li>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { groupTitle, type OrgNodeModel } from "@/types";
import { useOrgChart } from "@/lib/org/orgChartContext";
import OrgHolders from "@/Components/Org/OrgHolders.vue";
import OrgCreateForm from "@/Components/Org/OrgCreateForm.vue";

defineOptions({ name: "OrgNode" });

const props = defineProps<{ node: OrgNodeModel }>();

const chart = useOrgChart();

const position = computed(() => props.node.position);
const hasChildren = computed(() => props.node.children.length > 0);
const collapsed = computed(() => chart.isCollapsed(position.value.id));
const canCreate = computed(() => chart.canCreate(position.value));
const draggable = computed(() => chart.canDrag(position.value));

const tint = computed(() => {
    const color = position.value.color;
    if (!color) return undefined;

    return { backgroundColor: `color-mix(in oklab, ${color} 12%, var(--ui-bg))`, borderTopColor: color, borderTopWidth: "3px" };
});

function onCreateOpen(open: boolean) {
    if (open) chart.startCreate(position.value.id);
    else if (chart.creatingUnder.value === position.value.id) chart.cancelCreate();
}
</script>

<style scoped>
.org-node {
    display: flex;
    flex-direction: column;
    flex-shrink: 0;
    align-items: center;
}

.org-node-body {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    align-items: start;
    gap: 0.75rem;
    width: max-content;
}

.org-card-wrap {
    position: relative;
    grid-column: 2;
    grid-row: 1;
}

.org-card {
    min-width: 14rem;
    max-width: 18rem;
}

.org-pill {
    position: absolute;
    bottom: -0.875rem;
    left: 50%;
    display: flex;
    gap: 0.25rem;
    transform: translateX(-50%);
}

.org-pa {
    position: relative;
    grid-column: 3;
    grid-row: 1;
    min-width: 12rem;
    max-width: 16rem;
}

.org-pa::before {
    content: "";
    position: absolute;
    top: 50%;
    right: 100%;
    width: 0.75rem;
    border-top: 1px solid var(--ui-border-accented);
}

.org-children {
    display: flex;
    justify-content: center;
    position: relative;
    margin-top: 2rem;
}

.org-children::before {
    content: "";
    position: absolute;
    top: -2rem;
    left: 50%;
    height: 2rem;
    border-left: 1px solid var(--ui-border-accented);
}

.org-children > .org-node {
    position: relative;
    padding: 1.25rem 0.75rem 0;
}

.org-children > .org-node::before,
.org-children > .org-node::after {
    content: "";
    position: absolute;
    top: 0;
    height: 1.25rem;
    border-top: 1px solid var(--ui-border-accented);
}

.org-children > .org-node::before {
    right: 50%;
    width: 50%;
}

.org-children > .org-node::after {
    left: 50%;
    width: 50%;
    border-left: 1px solid var(--ui-border-accented);
}

.org-children > .org-node:first-child::before,
.org-children > .org-node:last-child::after {
    border: 0;
}

.org-children > .org-node:last-child::before {
    border-right: 1px solid var(--ui-border-accented);
    border-top-right-radius: 6px;
}

.org-children > .org-node:first-child::after {
    border-top-left-radius: 6px;
}

.org-children > .org-node:only-child {
    padding-top: 0;
}

.org-children > .org-node:only-child::before,
.org-children > .org-node:only-child::after {
    border: 0;
}
</style>
