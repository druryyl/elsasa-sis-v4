import { computed, shallowRef, type ComputedRef } from 'vue';

export interface UserSession {
  id: string;
  username: string;
  displayName: string;
  email?: string | null;
  roles: string[];
  permissions: string[];
  teacherId?: string | null;
  studentId?: string | null;
  guardianId?: string | null;
}

export interface AuthSessionPayload {
  accessToken: string;
  user: UserSession;
}

export interface AuthSessionReader {
  getUser: () => UserSession | null;
  getRoles?: () => string[];
  getPermissions?: () => string[];
  hasPermission?: (permission: string) => boolean;
  hasAnyRole?: (requiredRoles: string[]) => boolean;
}

export interface CurrentSession {
  user: ComputedRef<UserSession | null>;
  roles: ComputedRef<string[]>;
  permissions: ComputedRef<string[]>;
  teacherId: ComputedRef<string | null>;
  isAuthenticated: ComputedRef<boolean>;
  hasPermission: (permission: string) => boolean;
  hasAnyRole: (requiredRoles: string[]) => boolean;
}

type SetSessionHook = (payload: AuthSessionPayload) => void;

let setSessionHook: SetSessionHook | null = null;
const sessionReaderRef = shallowRef<AuthSessionReader | null>(null);

export function configureAuthSessionHook(hook: SetSessionHook | null): void {
  setSessionHook = hook;
}

export function publishAuthSession(payload: AuthSessionPayload): void {
  setSessionHook?.(payload);
}

/**
 * Registers the reactive read-only session reader wired during app bootstrap so
 * feature slices can inspect session state without importing app stores.
 */
export function configureAuthSessionReader(reader: AuthSessionReader | null): void {
  sessionReaderRef.value = reader;
}

/**
 * Returns reactive read-only session projections and permission helpers.
 */
export function useCurrentSession(): CurrentSession {
  const user = computed<UserSession | null>(() => sessionReaderRef.value?.getUser() ?? null);

  const roles = computed<string[]>(() => [
    ...(sessionReaderRef.value?.getRoles?.() ?? user.value?.roles ?? [])
  ]);

  const permissions = computed<string[]>(() => [
    ...(sessionReaderRef.value?.getPermissions?.() ?? user.value?.permissions ?? [])
  ]);

  const teacherId = computed<string | null>(() => user.value?.teacherId ?? null);

  const isAuthenticated = computed<boolean>(() => Boolean(user.value));

  function hasPermission(permission: string): boolean {
    if (sessionReaderRef.value?.hasPermission) {
      return sessionReaderRef.value.hasPermission(permission);
    }
    return permissions.value.includes(permission);
  }

  function hasAnyRole(requiredRoles: string[]): boolean {
    if (requiredRoles.length === 0) {
      return false;
    }
    if (sessionReaderRef.value?.hasAnyRole) {
      return sessionReaderRef.value.hasAnyRole(requiredRoles);
    }
    return requiredRoles.some((role) => roles.value.includes(role));
  }

  return {
    user,
    roles,
    permissions,
    teacherId,
    isAuthenticated,
    hasPermission,
    hasAnyRole
  };
}
