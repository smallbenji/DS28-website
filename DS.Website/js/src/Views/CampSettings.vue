<template>
    <section class="section">
        <div class="container camp-settings">
            <p class="mb-5">Administrer, hvilke tilmeldinger der er åbne for grupperne.</p>
            <PreSignupToggle />
            <BButton @click="router.push('/')">Tilbage til HQ</BButton>
        </div>
    </section>
</template>

<script setup lang="ts">
import { BButton } from 'buefy';
import { useRouter } from 'vue-router';
import PreSignupToggle from '@/Components/PreSignupToggle.vue';

const router = useRouter();
</script>

<style scoped>
.camp-settings {
    max-width: 52rem;
}
</style>
