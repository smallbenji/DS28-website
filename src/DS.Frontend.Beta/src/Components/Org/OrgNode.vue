<template>
    <li class="org-node">
        <div class="org-node-body">
            <button
                type="button"
                class="org-card rounded-lg border bg-default p-3 text-center shadow-xs transition hover:shadow-md focus-visible:outline-2 focus-visible:outline-primary"
                :class="node.position.isSystem ? 'border-inverted/60' : 'border-default'"
                :style="tint"
                @click="emit('select', node.position.id)"
            >
                <span class="block font-semibold text-highlighted">{{ node.position.title }}</span>
                <span v-if="node.position.groups.length" class="mt-1.5 flex flex-wrap justify-center gap-1">
                    <UBadge v-for="group in node.position.groups" :key="group" :label="groupTitle(group)" color="neutral" variant="outline" size="sm" />
                </span>
                <OrgHolders :holders="node.position.holders" />
            </button>

            <button
                v-if="node.pa"
                type="button"
                class="org-pa rounded-lg border border-dashed border-accented bg-default p-3 text-center transition hover:shadow-md focus-visible:outline-2 focus-visible:outline-primary"
                :style="tint"
                @click="emit('select', node.pa.id)"
            >
                <span class="block text-sm font-semibold text-highlighted">Personlig assistent</span>
                <OrgHolders :holders="node.pa.holders" />
            </button>
        </div>

        <ul v-if="node.children.length" class="org-children">
            <OrgNode v-for="child in node.children" :key="child.position.id" :node="child" @select="emit('select', $event)" />
        </ul>
    </li>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { groupTitle, type OrgNodeModel } from "@/types";
import OrgHolders from "@/Components/Org/OrgHolders.vue";

defineOptions({ name: "OrgNode" });

const props = defineProps<{ node: OrgNodeModel }>();
const emit = defineEmits<{ (e: "select", positionId: number): void }>();

const tint = computed(() => {
    const color = props.node.position.color;
    if (!color) return undefined;

    return { backgroundColor: `color-mix(in oklab, ${color} 12%, var(--ui-bg))`, borderTopColor: color, borderTopWidth: "3px" };
});
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

.org-card {
    grid-column: 2;
    grid-row: 1;
    min-width: 14rem;
    max-width: 18rem;
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
    margin-top: 1.25rem;
}

.org-children::before {
    content: "";
    position: absolute;
    top: -1.25rem;
    left: 50%;
    height: 1.25rem;
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
