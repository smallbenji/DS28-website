<template>
    <div class="activity">
        <div class="activity-topbar">
            <RouterLink to="/" class="back" title="Tilbage til forsiden">
                <font-awesome-icon icon="arrow-left" />
            </RouterLink>
            <div class="status-row">
                <div class="status">
                    Budget: 10.000 kr.
                </div>
                <div class="status">
                    <span>
                        Bekræftet
                    </span>
                </div>
            </div>
        </div>
    </div>
</template>
<script lang="ts" setup></script>
<style lang="scss">
.activity {
    height: 100%;
    border-radius: 10px;
    background-color: white;
    margin: 1rem;

    &-topbar {
        height: 3rem;
        border-bottom: 1px solid rgba(0, 0, 0, 0.1);
        display: flex;
        align-items: center;
        justify-content: space-between;

        .back {
            border-left: 1px solid rgba(0, 0, 0, 0.1);
            height: 100%;
            display: flex;
            align-items: center;
            padding: 0 1rem;
            color: inherit;

            &:hover {
                background-color: rgba(0, 0, 0, 0.05);
            }
        }

        .status {
            border-left: 1px solid rgba(0, 0, 0, 0.1);
            height: 100%;
            display: flex;
            align-items: center;
            padding: 0 0.5rem;

            &-row {
                height: 100%;
                display: flex;
            }
        }
    }
}
</style>