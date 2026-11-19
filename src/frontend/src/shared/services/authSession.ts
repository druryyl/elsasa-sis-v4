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

type SetSessionHook = (payload: AuthSessionPayload) => void;

let setSessionHook: SetSessionHook | null = null;

export function configureAuthSessionHook(hook: SetSessionHook | null): void {
  setSessionHook = hook;
}

export function publishAuthSession(payload: AuthSessionPayload): void {
  setSessionHook?.(payload);
}
