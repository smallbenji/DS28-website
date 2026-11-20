<template>
    <div class="flex min-h-svh items-center justify-center p-4">
        <UCard class="w-full max-w-md text-center">
            <h1 class="text-lg font-semibold text-highlighted">DS28 HQ (beta)</h1>
            <p class="mt-1 text-sm text-muted">Hej {{ Me?.name }}. Den nye brugerflade er under opbygning.</p>
            <UButton class="mt-4" to="/" external color="neutral" variant="outline" label="Til nuværende HQ" />
        </UCard>
    </div>
</template>

<script setup lang="ts">
import { storeToRefs } from "pinia";
import { useMeStore } from "@/Stores/MeStore";

const { Me } = storeToRefs(useMeStore());
</script>
