<template>
    <li class="org-node">
        <div
            class="org-card"
            :class="{ 'is-system': node.position.isSystem }"
            role="button"
            tabindex="0"
            @click="emit('select', node.position.id)"
            @keydown.enter.prevent="emit('select', node.position.id)"
            @keydown.space.prevent="emit('select', node.position.id)"
        >
            <div class="org-card-head">
                <span class="org-title">{{ node.position.title }}</span>
                <b-tag v-if="node.position.isSystem" type="is-dark" size="is-small">System</b-tag>
            </div>

            <b-taglist v-if="node.position.groups.length" class="org-groups">
                <b-tag v-for="group in node.position.groups" :key="group" size="is-small">
                    {{ groupTitle(group) }}
                </b-tag>
            </b-taglist>

            <ul v-if="node.position.holders.length" class="org-holders">
                <li v-for="holder in node.position.holders" :key="holder.userId" class="org-holder">
                    <span class="org-holder-avatar">
                        <img v-if="holder.profilePicture" :src="holder.profilePicture.url" :alt="holder.name" />
                        <span v-else>{{ initials(holder.name) }}</span>
                    </span>
                    <span class="org-holder-name">{{ holder.name }}</span>
                </li>
            </ul>
            <p v-else class="org-empty">Ingen tilknyttet</p>
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
import { BTaglist, BTag } from 'buefy';
import type { OrgNodeModel } from '@/types';
import { groupTitle } from '@/types';

defineOptions({ name: 'OrgNode' });

defineProps<{ node: OrgNodeModel }>();

const emit = defineEmits<{ (event: 'select', positionId: number): void }>();

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

.org-holder-avatar {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1.75rem;
    height: 1.75rem;
    border-radius: 50%;
    overflow: hidden;
    background: hsl(0, 0%, 86%);
    font-size: 0.7rem;
    font-weight: 600;
}

.org-holder-avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
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
