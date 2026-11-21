import { District, DistrictLabels, type ScoutDto } from "@/types";

export const districtItems = Object.values(District).map((district) => ({ label: DistrictLabels[district], value: district }));

export const genderItems = [
    { label: "Mand", value: "Male" },
    { label: "Kvinde", value: "Female" }
];

export function genderLabel(gender: ScoutDto["gender"] | string): string {
    return gender === "Male" || gender === 0 ? "Mand" : "Kvinde";
}

const birthdayFormat = new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "short", year: "numeric" });

export function formatBirthday(value: string): string {
    if (!value) return "";

    return birthdayFormat.format(new Date(value));
}
