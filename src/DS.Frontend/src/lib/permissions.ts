import { useMeStore } from "@/Stores/MeStore";

export function useHasRole() {
    const meStore = useMeStore();

    return (name: string): boolean => {
        const me = meStore.Me;
        if (!me) return false;

        return me.roles.includes(name) || me.appRoles.includes(name);
    };
}
