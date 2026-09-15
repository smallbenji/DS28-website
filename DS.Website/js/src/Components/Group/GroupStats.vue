<template>
    <div class="group-stats">
        <div class="group-stat">
            <span class="stat-title">Medlemmer</span>
            <span>0</span>
        </div>
        <!-- <div class="group-stat"></div> -->
    </div>
</template>
<script lang="ts" setup>
</script>
<style lang="scss">
.group-stats {
    display: flex;
    gap: 1rem;

    .stat-title {
        font-weight: 600;
        font-size: large;
    }
}

.group-stat {
    background-color: white;
    padding: 1rem;
    border-radius: 10px;
    border: 1px solid rgba(0, 0, 0, 0.1);
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center ;
}
</style>