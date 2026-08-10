<template>
    <Back :hide-back="true" icon="house" title="HQ" />
    <div class="grid">
        <component
            v-for="(link, index) in HQ.shortcuts"
            :key="index"
            :is="isExternal(link.url) ? 'a' : 'router-link'"
            :[isExternal(link.url)?'href':'to']="link.url"
            :target="isExternal(link.url) ? '_blank' : undefined"
            :rel="isExternal(link.url) ? 'noopener noreferrer' : undefined"
            class="link-box"
        >
            <div>
                <font-awesome-icon :icon="link.icon.length == 1 ? link.icon[0] : link.icon" />
                <p>{{ link.title }}</p>
            </div>
        </component>
    </div>
</template>

<script lang="ts" setup>
import Back from '@/Components/Back.vue';
import { useMeStore } from '@/Stores/MeStore';
import { storeToRefs } from 'pinia';

const meStore = useMeStore();
const { HQ } = storeToRefs(meStore);

// Helper function to check if a URL is external
const isExternal = (url: string) => {
    return /^https?:\/\//i.test(url);
};
</script>
<style lang="scss">
.grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
  max-width: 52rem;
  margin: auto;
  margin-top: 1rem;

  @media (max-width: 768px) {
    margin-left: 1rem;
    margin-right: 1rem;
    display: flex;
    flex-direction: column;
    // width: 100%;
    height: 100%;
    overflow-y: auto;
  }
}

.link-box {
  height: 8rem;
  width: 100%;
  background-color: #fff;
  border-radius: 10px;
  box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.05);
  transition: 0.2s ease-in-out;
  cursor: pointer;
  text-decoration: none;
  color: black;
  width: 100%;
  
  @media (max-width: 768px) {
    height: auto;
    padding: 1rem;
  }
}

.link-box:hover {
  background-color: #eee;
}
.link-box div {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  height: 100%;


  @media (max-width: 768px) {
    flex-direction: row;
    width: 100%;
    justify-content: space-between;
  }

  svg {
    font-size: 44px;
    color: rgba(0, 0, 0, 0.4);
    transition: ease-in 0.2s;
  }

  svg:hover {
    color: rgb(59,130,246);
  }

  p {
    text-align: center;
    margin: 0;
    font-size: 18px;
    padding: 0;
    color: rgba(0, 0, 0, 0.4);
    transition: ease-in 0.2s;
  }

}

.link-box:hover p {
  color: #000;
}

.link-box:hover svg {
  color: rgb(59,130,246);
}

</style>