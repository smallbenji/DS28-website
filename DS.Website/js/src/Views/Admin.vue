<template>
    <div class="admin">
        <div class="admin-main">
            <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>

            <p v-if="loading && sections.length === 0" class="center" role="status">Indlæser admin…</p>

            <section v-for="section in sections" :key="section.title" class="admin-section">
                <h2 class="title is-5 admin-heading mb-4">
                    <font-awesome-icon :icon="section.icon" />
                    {{ section.title }}
                </h2>
                <div class="grid">
                    <component
                        v-for="(link, index) in section.entries"
                        :key="index"
                        :is="isExternal(link) ? 'a' : 'router-link'"
                        :[isExternal(link)?'href':'to']="link.url"
                        :target="isExternal(link) ? '_blank' : undefined"
                        :rel="isExternal(link) ? 'noopener noreferrer' : undefined"
                        class="link-box"
                    >
                        <div>
                            <font-awesome-icon :icon="link.icon.length == 1 ? link.icon[0] : link.icon" />
                            <p>{{ link.title }}</p>
                        </div>
                    </component>
                </div>
            </section>

            <BButton v-if="!loading && !error && sections.length === 0" @click="load">Prøv igen</BButton>
        </div>

        <aside class="admin-aside">
            <h2 class="title is-6 admin-heading mb-4">
                <font-awesome-icon :icon="'chart-simple'" />
                Tilmeldingsstatistik
            </h2>

            <p v-if="loading && signupProgress.length === 0" role="status">Indlæser…</p>

            <div v-for="item in signupProgress" :key="item.key" class="box progress-card">
                <p class="progress-label">{{ item.label }}</p>
                <p class="progress-value">
                    {{ item.current }}
                    <span class="progress-target">af {{ item.target }}</span>
                </p>
                <progress
                    class="progress is-small"
                    :class="progressClass(item)"
                    :value="item.current"
                    :max="item.target"
                ></progress>
                <p class="progress-status" :class="statusClass(item)">{{ statusText(item) }}</p>
            </div>

            <p class="progress-note">
                Måltallet er deltagerantallet ved sidste lejr.
            </p>
        </aside>
    </div>
</template>

<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { BButton, BNotification } from 'buefy';
import axios from 'axios';
import type { AdminSectionDto, AdminViewModelDto, HQPanelEntryDto, SignupProgressDto } from '@/types';

const sections = ref<AdminSectionDto[]>([]);
const signupProgress = ref<SignupProgressDto[]>([]);
const loading = ref(true);
const error = ref('');

const isExternal = (link: HQPanelEntryDto) => {
    if (link.mvc) return true;

    return /^https?:\/\//i.test(link.url);
};

function percent(item: SignupProgressDto) {
    if (item.target <= 0) return 0;
    return Math.round((item.current / item.target) * 100);
}

function progressClass(item: SignupProgressDto) {
    if (item.status === 'reached') return 'is-success';
    if (item.status === 'halfway') return 'is-warning';
    return 'is-danger';
}

function statusClass(item: SignupProgressDto) {
    return `has-text-${progressClass(item).replace('is-', '')}`;
}

function statusText(item: SignupProgressDto) {
    if (item.status === 'reached') return 'Målet er nået';
    if (item.status === 'halfway') return `Halvejs – ${percent(item)} %`;
    return `Under halvejs – ${percent(item)} %`;
}

async function load() {
    loading.value = true;
    error.value = '';

    try {
        const { data } = await axios.get<AdminViewModelDto>('/api/v1/admin');
        sections.value = data.sections;

        const progress = await axios.get<SignupProgressDto[]>('/api/v1/admin/signup-progress');
        signupProgress.value = progress.data;
    } catch (e) {
        error.value =
            axios.isAxiosError(e) && typeof e.response?.data === 'string'
                ? e.response.data
                : 'Admin kunne ikke hentes. Kontrollér din adgang, og prøv igen.';
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style lang="scss" scoped>
.admin {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 1.5rem;
    padding: 0 1rem;
    margin-bottom: 2rem;
}

.admin-main {
    flex: 1 1 32rem;
    min-width: 0;
}

.admin-main :deep(.grid) {
    max-width: none;
    margin: 0;
}

.admin-aside {
    flex: 0 1 18rem;
    min-width: 0;
    order: 2;

    @media (max-width: 768px) {
        order: 1;
        flex: 1 1 auto;
    }
}

.admin-section {
    margin-bottom: 2rem;
}

.admin-heading {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 1rem 0 0;
    padding: 0;
}

.admin-heading svg {
    color: rgba(0, 0, 0, 0.4);
}

.progress-card {
    margin-bottom: 1rem;
}

.progress-label {
    font-weight: 600;
    margin-bottom: 0.25rem;
}

.progress-value {
    font-size: 2rem;
    line-height: 1.1;
    margin-bottom: 0.5rem;
}

.progress-target {
    font-size: 1rem;
    color: rgba(0, 0, 0, 0.4);
}

.progress-status {
    font-size: 0.85rem;
    margin-top: 0.35rem;
}

.progress-note {
    font-size: 0.8rem;
    color: rgba(0, 0, 0, 0.4);
}
</style>
