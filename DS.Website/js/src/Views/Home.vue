<template>
    <div class="grid">
        <component
            v-for="(link, index) in HQ.shortcuts"
            :key="index"
            :is="isExternal(link.url) ? 'a' : 'router-link'"
            :[isExternal(link.url)?'href':'to']="link.url"
            :target="isExternal(link.url) ? '_blank' : undefined"
            :rel="isExternal(link.url) ? 'noopener noreferrer' : undefined"
            class="link-box"
        >
            <div>
                <font-awesome-icon :icon="link.icon.length == 1 ? link.icon[0] : link.icon" />
                <p>{{ link.title }}</p>
            </div>
        </component>
    </div>
    <p class="center">{{ VERSION.version }}</p>
</template>

<script lang="ts" setup>
import { useMeStore } from '@/Stores/MeStore';
import { useVersionStore } from '@/Stores/VersionStore';
import { storeToRefs } from 'pinia';

const versionStore = useVersionStore();
const { VERSION } = storeToRefs(versionStore);

const meStore = useMeStore();
const { HQ } = storeToRefs(meStore);

// Helper function to check if a URL is external
const isExternal = (url: string) => {
    return /^https?:\/\//i.test(url);
};
</script>
