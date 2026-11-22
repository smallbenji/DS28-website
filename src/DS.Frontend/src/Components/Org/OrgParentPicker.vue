<template>
    <div class="flex flex-col gap-2">
        <UInput v-model="search" icon="i-lucide-search" placeholder="Find stilling" class="w-full" aria-label="Find overordnet stilling" />
        <div class="max-h-64 overflow-y-auto rounded-md border border-accented p-1" role="radiogroup" aria-label="Overordnet stilling">
            <button
                v-for="option in visibleOptions"
                :key="option.value"
                type="button"
                role="radio"
                :aria-checked="model === option.value"
                :disabled="!option.selectable"
                class="flex w-full items-center gap-2 rounded px-2 py-1.5 text-left text-sm transition enabled:hover:bg-elevated/50 disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-primary"
                :class="model === option.value ? 'bg-primary/10 font-semibold text-primary' : 'text-default'"
                :style="{ paddingInlineStart: `${0.5 + (search.trim() ? 0 : option.depth) * 1}rem` }"
                @click="model = option.value"
            >
                <span class="size-2.5 shrink-0 rounded-full border border-accented" :style="option.color ? { backgroundColor: option.color, borderColor: option.color } : undefined" />
                <span class="flex-1 truncate">{{ option.label }}</span>
                <span v-if="option.current" class="text-xs font-normal text-muted">Nuværende</span>
            </button>
            <p v-if="visibleOptions.length === 0" class="px-2 py-1.5 text-sm text-muted">Ingen stillinger matcher.</p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import type { OrgNodeModel, OrgPositionDto } from "@/types";
import { collectDescendantIds, flattenOrgTree } from "@/lib/org/orgTree";

const props = defineProps<{
    position: OrgPositionDto;
    positions: OrgPositionDto[];
    roots: OrgNodeModel[];
    isSysAdmin: boolean;
    canSelect: (target: OrgPositionDto) => boolean;
}>();

const model = defineModel<string>({ required: true });

const rootValue = "root";
const search = ref("");

const options = computed(() => {
    const blocked = collectDescendantIds(props.position.id, props.positions);
    blocked.add(props.position.id);

    const positionOptions = flattenOrgTree(props.roots).map(({ node, depth }) => ({
        value: String(node.position.id),
        label: node.position.title,
        color: node.position.color,
        depth,
        current: node.position.id === props.position.parentPositionId,
        selectable: !blocked.has(node.position.id) && props.canSelect(node.position)
    }));

    if (!props.isSysAdmin) return positionOptions;

    return [
        { value: rootValue, label: "Ingen (rodstilling)", color: null, depth: 0, current: props.position.parentPositionId === null, selectable: true },
        ...positionOptions
    ];
});

const visibleOptions = computed(() => {
    const query = search.value.trim().toLowerCase();
    if (!query) return options.value;

    return options.value.filter((option) => option.label.toLowerCase().includes(query));
});
</script>
