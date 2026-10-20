import type { AxiosInstance, AxiosRequestConfig } from "axios";
import axios from "axios";

const safeMethods = ["get", "head", "options", "trace"];

export const apiClient: AxiosInstance = axios.create({
    withCredentials: true,
    headers: {
        "Content-Type": "application/json"
    }
});

let antiforgeryToken: Promise<string> | null = null;

async function getAntiforgeryToken(): Promise<string> {
    if (antiforgeryToken == null) {
        antiforgeryToken = apiClient
            .get<{ token: string }>("/api/v1/antiforgery")
            .then(response => response.data.token)
            .catch(error => {
                antiforgeryToken = null;
                throw error;
            });
    }

    return await antiforgeryToken;
}

function clearAntiforgeryToken(): void {
    antiforgeryToken = null;
}

apiClient.interceptors.request.use(async config => {
    const method = config.method?.toLowerCase() ?? "get";
    if (safeMethods.includes(method)) return config;

    const url = new URL(config.url ?? "", config.baseURL ?? window.location.origin);
    if (url.origin !== window.location.origin) return config;

    const token = await getAntiforgeryToken();
    config.headers.set("RequestVerificationToken", token);

    return config;
});

apiClient.interceptors.response.use(response => {
    if (!safeMethods.includes(response.config.method?.toLowerCase() ?? "get")) {
        clearAntiforgeryToken();
    }

    return response;
}, async error => {
    if (axios.isAxiosError(error) && error.response?.status === 400) {
        clearAntiforgeryToken();
    }

    return await Promise.reject(error);
});

export async function apiGet<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await apiClient.get<T>(url, config);
    return response.data;
}

export async function apiPost<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await apiClient.post<T>(url, data, config);
    return response.data;
}

export async function apiPut<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await apiClient.put<T>(url, data, config);
    return response.data;
}

export async function apiDelete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await apiClient.delete<T>(url, config);
    return response.data;
}

export async function safeApiCall<T>(call: () => Promise<T>, fallback: T): Promise<T> {
    try {
        return await call();
    } catch {
        return fallback;
    }
}

export async function safeApiResult<T>(call: () => Promise<T>): Promise<T | null> {
    try {
        return await call();
    } catch {
        return null;
    }
}

export async function safeApiOk(call: () => Promise<unknown>): Promise<boolean> {
    try {
        await call();
        return true;
    } catch {
        return false;
    }
}

export function getErrorMessage(error: unknown, fallback = "Der skete en uventet fejl."): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data;
        if (typeof data === "string" && data.length > 0) {
            return data;
        }
    }

    return fallback;
}

export function getErrorStatus(error: unknown): number | undefined {
    return axios.isAxiosError(error) ? error.response?.status : undefined;
}
