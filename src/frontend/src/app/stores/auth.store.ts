import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { authApi, mapAuthUserDtoToSession } from '@/features/auth/api/auth.api';
import type { LoginRequestDto, UserSession } from '@/features/auth/types/auth.types';

export type AuthUser = UserSession;

export interface AuthSessionPayload {
  accessToken: string;
  user: AuthUser;
}

export const useAuthStore = defineStore('auth', () => {
  // Strictly in-memory session state (never persisted to localStorage/sessionStorage)
  const accessToken = ref<string | null>(null);
  const user = ref<AuthUser | null>(null);
  const isHydrated = ref(false);

  const roles = computed<string[]>(() => user.value?.roles ?? []);
  const permissions = computed<string[]>(() => user.value?.permissions ?? []);
  const isAuthenticated = computed<boolean>(() => Boolean(accessToken.value && user.value));

  function hasPermission(permission: string): boolean {
    return permissions.value.includes(permission);
  }

  function hasAnyRole(requiredRoles: string[]): boolean {
    if (requiredRoles.length === 0) return false;
    return requiredRoles.some((role) => roles.value.includes(role));
  }

  function setSession(payload: AuthSessionPayload): void {
    accessToken.value = payload.accessToken;
    user.value = payload.user;
  }

  function clearSession(): void {
    accessToken.value = null;
    user.value = null;
  }

  async function login(payload: LoginRequestDto): Promise<AuthUser> {
    const response = await authApi.login(payload);
    if (!response?.accessToken) {
      throw new Error('Authentication response did not include an access token.');
    }

    accessToken.value = response.accessToken;
    const sessionUser = response.user
      ? mapAuthUserDtoToSession(response.user)
      : mapAuthUserDtoToSession(await authApi.me());

    setSession({
      accessToken: response.accessToken,
      user: sessionUser
    });

    return sessionUser;
  }

  async function refreshSession(): Promise<string | null> {
    try {
      const response = await authApi.refresh();
      if (!response?.accessToken) {
        clearSession();
        return null;
      }

      accessToken.value = response.accessToken;
      if (response.user) {
        user.value = mapAuthUserDtoToSession(response.user);
      } else if (!user.value) {
        user.value = mapAuthUserDtoToSession(await authApi.me());
      }

      return accessToken.value;
    } catch {
      clearSession();
      return null;
    }
  }

  async function hydrateSession(): Promise<void> {
    if (isHydrated.value) return;
    try {
      await refreshSession();
    } finally {
      isHydrated.value = true;
    }
  }

  async function logout(): Promise<void> {
    try {
      await authApi.logout();
    } catch {
      // Best-effort server revocation; always clear local session state
    } finally {
      clearSession();
    }
  }

  return {
    accessToken,
    user,
    isHydrated,
    roles,
    permissions,
    isAuthenticated,
    hasPermission,
    hasAnyRole,
    setSession,
    clearSession,
    login,
    refreshSession,
    hydrateSession,
    logout
  };
});
