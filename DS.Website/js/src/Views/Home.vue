<template>
    <Back :hide-back="true" icon="house" title="Distriktssommerlejr HQ" />
    <div class="grid">
        <a v-for="link in HQ.shortcuts" :href="link.url" class="link-box">
            <div>
                <font-awesome-icon :icon="['fab', 'wordpress']" />
                <i :class="link.icon"></i>
                <p>{{ link.title }}</p>
            </div>
        </a>
    </div>
</template>
<script lang="ts" setup>
import Back from '@/Components/Back.vue';
import { useMeStore } from '@/Stores/MeStore';
import { storeToRefs } from 'pinia';

const meStore = useMeStore();
const { HQ } = storeToRefs(meStore);

</script>