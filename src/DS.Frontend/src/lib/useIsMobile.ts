import { onMounted, onUnmounted, ref } from "vue";

export function useIsMobile(query = "(max-width: 1023px)") {
    const isMobile = ref(false);
    let mediaQuery: MediaQueryList | null = null;

    const update = () => {
        isMobile.value = mediaQuery?.matches ?? false;
    };

    onMounted(() => {
        mediaQuery = window.matchMedia(query);
        update();
        mediaQuery.addEventListener("change", update);
    });

    onUnmounted(() => {
        mediaQuery?.removeEventListener("change", update);
    });

    return isMobile;
}
