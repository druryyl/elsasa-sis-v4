import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAuthStore, type AuthUser } from '../auth.store';

const sampleUser: AuthUser = {
  id: '01900000-0000-7000-8000-000000000001',
  username: 'admin',
  displayName: 'School Administrator',
  email: 'admin@elsasa.sch.id',
  roles: ['SCHOOL_ADMIN'],
  permissions: ['Permissions.Operations.Calendar.View', 'Permissions.Operations.Calendar.Manage']
};

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
    sessionStorage.clear();
  });

  it('starts unauthenticated with empty roles and permissions', () => {
    const store = useAuthStore();

    expect(store.accessToken).toBeNull();
    expect(store.user).toBeNull();
    expect(store.isAuthenticated).toBe(false);
    expect(store.roles).toEqual([]);
    expect(store.permissions).toEqual([]);
    expect(store.hasPermission('Permissions.Operations.Calendar.View')).toBe(false);
    expect(store.hasAnyRole(['SCHOOL_ADMIN'])).toBe(false);
  });

  it('sets and clears in-memory session without touching browser storage', () => {
    const store = useAuthStore();

    store.setSession({
      accessToken: 'jwt-in-memory-token',
      user: sampleUser
    });

    expect(store.isAuthenticated).toBe(true);
    expect(store.accessToken).toBe('jwt-in-memory-token');
    expect(store.roles).toEqual(['SCHOOL_ADMIN']);
    expect(store.hasPermission('Permissions.Operations.Calendar.Manage')).toBe(true);
    expect(store.hasPermission('Permissions.Billing.Payments.Receive')).toBe(false);
    expect(store.hasAnyRole(['TEACHER', 'SCHOOL_ADMIN'])).toBe(true);
    expect(store.hasAnyRole(['TEACHER'])).toBe(false);

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);

    store.clearSession();
    expect(store.isAuthenticated).toBe(false);
    expect(store.accessToken).toBeNull();
    expect(store.user).toBeNull();
  });
});
