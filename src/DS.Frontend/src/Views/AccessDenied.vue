<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-5-desktop is-7-tablet">
                <div class="card frame">
                    <header class="card-header">
                        <p class="card-header-title is-centered">Adgang nægtet</p>
                    </header>

                    <div class="card-content has-text-centered">
                        <p class="mb-4">
                            Du har ikke adgang til denne side.
                        </p>
                        <p class="mb-4">
                            Kontakt en administrator, hvis du mener, du skulle have adgang.
                        </p>
                        <BButton type="is-primary" @click="goHome">
                            Gå til forsiden
                        </BButton>
                    </div>
                </div>
            </div>
        </div>
    </section>
</template>

<script lang="ts" setup>
import { BButton } from 'buefy';
import { useRouter } from 'vue-router';

const router = useRouter();

const goHome = () => {
    router.push('/');
};
</script>

<style>
.frame {
    min-width: 300px;
}
</style>
