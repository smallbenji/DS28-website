<template>
    <div class="home">
        <div class="grid">
            <component
                v-for="(link, index) in Hq.shortcuts"
                :key="index"
                :is="isExternal(link.url) ? 'a' : 'router-link'"
                :[isExternal(link.url)?'href':'to']="link.url"
                :target="isExternal(link.url) ? '_blank' : undefined"
                :rel="isExternal(link.url) ? 'noopener noreferrer' : undefined"
                class="link-box"
                :aria-label="isExternal(link.url) ? link.title + ' (åbner i nyt vindue)' : link.title"
            >
                <div>
                    <font-awesome-icon :icon="link.icon.length == 1 ? link.icon[0] : link.icon" aria-hidden="true" />
                    <p>{{ link.title }}</p>
                    <font-awesome-icon v-if="isExternal(link.url)" icon="up-right-from-square" class="external-icon" aria-hidden="true" />
                </div>
            </component>
        </div>
        <p class="home-version">{{ Version.version }}</p>
    </div>
</template>

<script lang="ts" setup>
import { useMeStore } from '@/Stores/MeStore';
import { useVersionStore } from '@/Stores/VersionStore';
import { storeToRefs } from 'pinia';

const versionStore = useVersionStore();
const { Version } = storeToRefs(versionStore);

const meStore = useMeStore();
const { Hq } = storeToRefs(meStore);

// Helper function to check if a URL is external
const isExternal = (url: string) => {
    return /^https?:\/\//i.test(url);
};
</script>

<style scoped>
.home {
    width: 100%;
    padding: 1rem;
}

.home > .grid {
    width: 100%;
    margin: 0 auto;
}

.home-version {
    margin: 1.5rem auto 0;
    max-width: 52rem;
    color: #595959;
    font-size: 0.85rem;
    text-align: center;
}

@media (max-width: 1024px) {
    .grid {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
    }

    .link-box {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex: 0 0 auto;
        gap: 1rem;
        height: auto;
        min-height: 4.5rem;
        padding: 1rem;
        border-radius: 10px;
    }

    .link-box > div {
        flex: 1;
        min-width: 0;
        height: auto;
        flex-direction: row;
        justify-content: flex-start;
        gap: 0.75rem;
    }

    .link-box svg {
        flex: 0 0 2rem;
        width: 2rem;
        font-size: 28px;
    }

    .link-box .external-icon {
        flex: 0 0 auto;
        width: auto;
        font-size: 12px;
        margin-left: auto;
    }

    .link-box p {
        text-align: left;
        overflow-wrap: anywhere;
    }
}
</style>
