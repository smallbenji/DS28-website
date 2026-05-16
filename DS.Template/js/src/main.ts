import { createApp } from "vue";
import App from "@/App.vue";
import { createPinia } from "pinia";
import router from "./router/main";

const app = createApp(App);

const pinia = createPinia();

app.use(router);
app.use(pinia);

app.mount("#app");