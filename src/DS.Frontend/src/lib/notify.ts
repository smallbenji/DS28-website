import { useToast } from "@nuxt/ui/composables";

export function useNotify() {
    const toast = useToast();

    return {
        success(title: string) {
            toast.add({ title, color: "success", icon: "i-lucide-circle-check" });
        },
        error(title: string) {
            toast.add({ title, color: "error", icon: "i-lucide-circle-alert" });
        },
        result(ok: boolean, success: string, failure: string) {
            if (ok) this.success(success);
            else this.error(failure);
        }
    };
}
