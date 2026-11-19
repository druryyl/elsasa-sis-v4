import axios, {
  AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig
} from 'axios';
import { ApiError, type ProblemDetailsPayload } from '@/shared/types/common';

export interface HttpClientHooks {
  getAccessToken: () => string | null;
  getContextHeaders: () => Record<string, string>;
  refreshAccessToken: () => Promise<string | null>;
  onSessionExpired: () => void;
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

let clientHooks: HttpClientHooks | null = null;
let refreshPromise: Promise<string | null> | null = null;

export function configureHttpClient(hooks: HttpClientHooks | null): void {
  clientHooks = hooks;
  refreshPromise = null;
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 15000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json'
  }
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = clientHooks?.getAccessToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  const contextHeaders = clientHooks?.getContextHeaders() ?? {};
  for (const [key, value] of Object.entries(contextHeaders)) {
    if (value && config.headers) {
      config.headers[key] = value;
    }
  }

  if (config.headers && !config.headers['X-Correlation-ID']) {
    config.headers['X-Correlation-ID'] = crypto.randomUUID();
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response.data,
  async (error: AxiosError<ProblemDetailsPayload>) => {
    if (!error.response) {
      return Promise.reject(
        new ApiError(
          0,
          'Network Error',
          'Unable to reach the server. Please check your network connection.'
        )
      );
    }

    const { status, data } = error.response;
    const originalRequest = error.config as RetryableRequestConfig | undefined;
    const isAuthEndpoint =
      originalRequest?.url?.includes('/auth/login') ||
      originalRequest?.url?.includes('/auth/refresh');

    if (
      status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !isAuthEndpoint &&
      clientHooks
    ) {
      originalRequest._retry = true;
      try {
        refreshPromise ??= clientHooks.refreshAccessToken().finally(() => {
          refreshPromise = null;
        });

        const newToken = await refreshPromise;
        if (newToken) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return apiClient(originalRequest as AxiosRequestConfig);
        }
      } catch {
        // Fall through to session expiration handler
      }

      clientHooks.onSessionExpired();
      return Promise.reject(
        new ApiError(401, 'Unauthorized', 'Session has expired. Please log in again.')
      );
    }

    if (status === 401) {
      clientHooks?.onSessionExpired();
      return Promise.reject(
        new ApiError(401, 'Unauthorized', 'Session has expired. Please log in again.')
      );
    }

    return Promise.reject(ApiError.fromProblemDetails(status, data, error.message));
  }
);
