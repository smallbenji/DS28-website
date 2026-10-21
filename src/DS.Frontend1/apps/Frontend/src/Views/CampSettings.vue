<template>
    <section class="section">
        <div class="camp-settings">
            <PreSignupToggle />
            <SignupToggle />
        </div>
    </section>
</template>

<script setup lang="ts">
import PreSignupToggle from '@/Components/PreSignupToggle.vue';
import SignupToggle from '@/Components/SignupToggle.vue';
</script>

<style scoped>

.camp-settings {
    background-color: white;
    padding: 1rem;
    border-radius: 10px;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
    border: 1px solid rgba(0, 0, 0, 0.1);
    width: fit-content;
}
</style>
