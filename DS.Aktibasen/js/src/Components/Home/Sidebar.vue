<template>
    <div class="sidebar">
        <BSelect size="is-small">
            <option>
                Hello
            </option>
        </BSelect>
        <nav class="menu">
            <p class="menu-label">
                Administration
            </p>
            <ul class="menu-list">
                <li>
                    <RouterLink to="/teams">
                        <font-awesome-icon icon="users" class="mr-1" />
                        Holdstyring
                    </RouterLink>
                </li>
            </ul>
        </nav>
    </div>
</template>
<script lang="ts" setup>
import { BSelect } from 'buefy';

</script>
<style lang="scss">
.sidebar {
    background-color: white;
    border: 1px solid rgba(0, 0, 0, 0.2);
    width: 25%;
    border-radius: 10px;
    box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
    margin-top: 1rem;
    margin-left: 1rem;
    margin-bottom: 1rem;
    padding: 1rem;
}
</style>
