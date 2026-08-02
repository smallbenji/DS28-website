<template>
    <div>
        Activity List
        <BButton>
            Opret Aktivitet
        </BButton>
    </div>
    <BModal v-model="isOpen" has-modal-card>
        
    </BModal>
</template>
<script lang="ts" setup>
import { BButton, BModal } from 'buefy';
import { ref } from 'vue';

const isOpen = ref(true);

</script>