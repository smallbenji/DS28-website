<template>
    <li class="org-node">
        <div class="org-node-body">
            <div
                class="org-card"
                :class="{ 'is-system': node.position.isSystem }"
                :style="cardStyle"
                role="button"
                tabindex="0"
                @click="emit('select', node.position.id)"
                @keydown.enter.prevent="emit('select', node.position.id)"
                @keydown.space.prevent="emit('select', node.position.id)"
            >
                <div class="org-card-head">
                    <span class="org-title">{{ node.position.title }}</span>
                </div>

                <b-taglist v-if="node.position.groups.length" class="org-groups">
                    <b-tag v-for="group in node.position.groups" :key="group" size="is-small">
                        {{ groupTitle(group) }}
                    </b-tag>
                </b-taglist>

                <ul v-if="node.position.holders.length" class="org-holders">
                    <li v-for="holder in node.position.holders" :key="holder.id" class="org-holder">
                        <UserDisplay :user="holder" />
                    </li>
                </ul>
                <p v-else class="org-empty">Ingen tilknyttet</p>
            </div>

            <div
                v-if="node.pa"
                class="org-pa"
                :style="paStyle"
                role="button"
                tabindex="0"
                @click="emit('select', node.pa.id)"
                @keydown.enter.prevent="emit('select', node.pa.id)"
                @keydown.space.prevent="emit('select', node.pa.id)"
            >
                <span class="org-pa-title">Personlig assistent</span>

                <ul v-if="node.pa.holders.length" class="org-holders">
                    <li v-for="holder in node.pa.holders" :key="holder.id" class="org-holder">
                        <UserDisplay :user="holder" />
                    </li>
                </ul>
                <p v-else class="org-empty">Ingen tilknyttet</p>
            </div>
        </div>

        <ul v-if="node.children.length" class="org-children">
            <OrgNode
                v-for="child in node.children"
                :key="child.position.id"
                :node="child"
                @select="emit('select', $event)"
            />
        </ul>
    </li>
</template>

<script lang="ts" setup>
import { computed } from 'vue';
import { BTaglist, BTag } from 'buefy';
import type { OrgNodeModel } from '@/types';
import { groupTitle } from '@/types';
import { tintHex } from './orgPalette';
import UserDisplay from '../User/UserDisplay.vue';

defineOptions({ name: 'OrgNode' });

const props = defineProps<{ node: OrgNodeModel }>();

const emit = defineEmits<{ (event: 'select', positionId: number): void }>();

const cardStyle = computed(() => {
    const color = tintHex(props.node.position.color);
    return color ? { backgroundColor: color } : undefined;
});

const paStyle = computed(() => {
    const color = tintHex(props.node.position.color);
    return color ? { backgroundColor: color } : undefined;
});

function initials(name: string): string {
    return name
        .split(' ')
        .filter((part) => part.length > 0)
        .slice(0, 2)
        .map((part) => part[0])
        .join('')
        .toUpperCase();
}
</script>

<style lang="scss" scoped>
.org-node {
    list-style: none;
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

.org-node-body > .org-card {
    grid-column: 2;
    grid-row: 1;
}

.org-node-body > .org-pa {
    position: relative;
    grid-column: 3;
    grid-row: 1;
}

.org-node-body > .org-pa::before {
    content: '';
    position: absolute;
    top: 50%;
    right: 100%;
    width: 0.75rem;
    border-top: 1px solid hsl(0, 0%, 64%);
}

.org-pa {
    display: flex;
    flex-direction: column;
    background: white;
    border: 1px dashed hsl(0, 0%, 64%);
    border-radius: 6px;
    padding: 0.75rem;
    min-width: 12rem;
    max-width: 16rem;
    text-align: center;
    cursor: pointer;
    transition: box-shadow 0.15s ease, border-color 0.15s ease;
}

.org-pa:hover,
.org-pa:focus-visible {
    border-color: hsl(0, 0%, 29%);
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
    outline: none;
}

.org-pa-title {
    font-weight: 600;
    font-size: 0.85rem;
}

.org-card {
    background: white;
    border: 1px solid hsl(0, 0%, 86%);
    border-radius: 6px;
    padding: 0.75rem;
    min-width: 14rem;
    max-width: 18rem;
    text-align: center;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
    cursor: pointer;
    transition: box-shadow 0.15s ease, border-color 0.15s ease;
}

.org-card:hover,
.org-card:focus-visible {
    border-color: hsl(0, 0%, 64%);
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
    outline: none;
}

.org-card.is-system {
    border-color: hsl(0, 0%, 29%);
}

.org-card-head {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.35rem;
    font-weight: 600;
}

.org-groups {
    margin-top: 0.4rem;
    justify-content: center;
}

.org-holders {
    margin-top: 0.5rem;
}

.org-holder {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    justify-content: center;
}

.org-holder + .org-holder {
    margin-top: 0.25rem;
}

.org-empty {
    margin-top: 0.5rem;
    font-size: 0.8rem;
    color: hsl(0, 0%, 48%);
}

.org-children {
    display: flex;
    justify-content: center;
    position: relative;
    margin-top: 1.25rem;
}

.org-children::before {
    content: '';
    position: absolute;
    top: -1.25rem;
    left: 50%;
    height: 1.25rem;
    border-left: 1px solid hsl(0, 0%, 64%);
}

.org-children > .org-node {
    position: relative;
    padding: 1.25rem 0.75rem 0;
}

.org-children > .org-node::before,
.org-children > .org-node::after {
    content: '';
    position: absolute;
    top: 0;
    height: 1.25rem;
    border-top: 1px solid hsl(0, 0%, 64%);
}

.org-children > .org-node::before {
    right: 50%;
    width: 50%;
}

.org-children > .org-node::after {
    left: 50%;
    width: 50%;
    border-left: 1px solid hsl(0, 0%, 64%);
}

.org-children > .org-node:first-child::before {
    border: 0;
}

.org-children > .org-node:last-child::after {
    border: 0;
}

.org-children > .org-node:last-child::before {
    border-right: 1px solid hsl(0, 0%, 64%);
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
