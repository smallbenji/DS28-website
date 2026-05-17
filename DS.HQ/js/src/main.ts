import { createApp } from "vue";
import App from "./App.vue";
import { createPinia } from "pinia";
import router from "@/router";
import 'buefy/dist/css/buefy.css';
import Buefy from "buefy";

const app = createApp(App);

import { library } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";

import { faArrowLeft, faUser } from "@fortawesome/free-solid-svg-icons";

library.add(faArrowLeft, faUser);

app.component("font-awesome-icon", FontAwesomeIcon);

const pinia = createPinia();

app.use(router);
app.use(pinia);
app.use(Buefy);

app.mount("#app");