<template>
    <BDropdown
        v-if="editable"
        class="board-card-property-dropdown"
        position="is-bottom-left"
        aria-role="list"
        append-to-body
        expanded
        :close-on-click="closeOnClick"
        :disabled="disabled"
    >
        <template #trigger>
            <button type="button" class="board-card-property is-editable" :aria-label="actionLabel" :disabled="disabled">
                <span class="board-card-property-label">{{ label }}</span>
                <span class="board-card-property-value"><slot name="value" /></span>
            </button>
        </template>
        <BDropdownItem custom aria-role="listitem" class="board-card-property-editor">
            <slot name="editor" />
        </BDropdownItem>
    </BDropdown>
    <div v-else class="board-card-property">
        <span class="board-card-property-label">{{ label }}</span>
        <span class="board-card-property-value"><slot name="value" /></span>
    </div>
</template>

<script setup lang="ts">
import { BDropdown, BDropdownItem } from 'buefy';

withDefaults(defineProps<{
    label: string;
    actionLabel: string;
    editable: boolean;
    closeOnClick?: boolean;
    disabled?: boolean;
}>(), {
    closeOnClick: true,
    disabled: false
});
</script>

<style scoped>
.board-card-property-dropdown {
    display: block;
    width: 100%;
}

.board-card-property-dropdown :deep(.dropdown-trigger) {
    width: 100%;
}

.board-card-property {
    display: grid;
    grid-template-columns: 6rem minmax(0, 1fr);
    align-items: center;
    width: 100%;
    min-height: 2.5rem;
    padding: 0.25rem 0.5rem;
    margin: 0 -0.5rem;
    border: 0;
    border-radius: 6px;
    background: transparent;
    font: inherit;
    color: inherit;
    text-align: left;
    box-sizing: content-box;
}

.board-card-property.is-editable {
    cursor: pointer;
}

.board-card-property.is-editable:hover,
.board-card-property.is-editable:focus-visible {
    background: #f0f2f5;
}

.board-card-property.is-editable:focus-visible {
    outline: 2px solid #3273dc;
    outline-offset: 1px;
}

.board-card-property-label {
    font-size: 0.8rem;
    color: #6b7280;
}

.board-card-property-value {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
    font-size: 0.9rem;
    color: #1f1f1f;
}

.board-card-property-editor {
    padding: 0.5rem 0.75rem;
}
</style>
