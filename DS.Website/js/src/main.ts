import { createApp } from "vue";
import App from "./App.vue";
import { createPinia } from "pinia";
import router from "@/router";
import 'buefy/dist/css/buefy.css';
import Buefy from "buefy";

const app = createApp(App);

import '@fortawesome/fontawesome-free/css/all.css'

import { library } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";

import { faArrowLeft, faUser, faUsers, faDiceD6, faUserPlus, faSearch, faMagnifyingGlass, faPlus } from "@fortawesome/free-solid-svg-icons";

library.add(faArrowLeft, faUser, faUsers, faDiceD6, faUserPlus, faSearch, faMagnifyingGlass, faPlus);

app.component("font-awesome-icon", FontAwesomeIcon);
app.component('vue-fontawesome', FontAwesomeIcon);

const pinia = createPinia();

app.use(router);
app.use(pinia);
app.use(Buefy, {
    defaultIconPack: "fas"
});

app.mount("#app");