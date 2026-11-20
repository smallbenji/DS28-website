export interface FormTemplate {
    id: string;
    sections: FormSection[];
}

export type FormValue = string | number | null;
export type FormValues = Record<string, FormValue>;

export interface FormSection {
    title: string;
    fields: FormField[];
}

export interface FormField {
    key: string;
    type: FormFieldType;
    label: string;
    placeholder: string | null;
}

export enum FormFieldType {
    String = "String",
    Number = "Number",
    Date = "Date",
}
