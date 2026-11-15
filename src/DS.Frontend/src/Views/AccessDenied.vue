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
                        <p v-if="from" class="is-size-7 has-text-grey mb-4">
                            <code>{{ from }}</code>
                        </p>
                        <div class="buttons is-centered">
                            <BButton type="is-primary" @click="goHome">
                                Gå til forsiden
                            </BButton>
                            <BButton v-if="from" @click="goBack">
                                Gå tilbage
                            </BButton>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>
</template>

<script lang="ts" setup>
import { BButton } from 'buefy';
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();

const from = computed(() => {
    const value = route.query.from;

    return typeof value === 'string' && value.startsWith('/') ? value : '';
});

const goHome = () => {
    router.push('/');
};

const goBack = () => {
    router.back();
};
</script>

<style>
.frame {
    min-width: 300px;
}
</style>
