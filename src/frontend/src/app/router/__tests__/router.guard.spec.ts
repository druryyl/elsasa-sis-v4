import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory } from 'vue-router';
import { createAppRouter } from '../index';
import { useAuthStore } from '@/app/stores/auth.store';

describe('Router Auth & Permission Guards', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('redirects unauthenticated access to /operations/calendar to /auth/login with redirect query', async () => {
    const router = createAppRouter(createMemoryHistory());
    await router.push('/operations/calendar');
    await router.isReady();

    expect(router.currentRoute.value.name).toBe('auth.login');
    expect(router.currentRoute.value.path).toBe('/auth/login');
    expect(router.currentRoute.value.query.redirect).toBe('/operations/calendar');
  });

  it('redirects authenticated user without required permission to /403', async () => {
    const authStore = useAuthStore();
    authStore.setSession({
      accessToken: 'token-123',
      user: {
        id: 'u-1',
        username: 'teacher',
        displayName: 'Teacher User',
        roles: ['TEACHER'],
        permissions: ['Permissions.Academic.Attendance.View']
      }
    });

    const router = createAppRouter(createMemoryHistory());
    await router.push('/operations/calendar');
    await router.isReady();

    expect(router.currentRoute.value.name).toBe('forbidden');
    expect(router.currentRoute.value.path).toBe('/403');
  });

  it('allows authenticated user with Calendar.View or Calendar.Manage permission into /operations/calendar', async () => {
    const authStore = useAuthStore();
    authStore.setSession({
      accessToken: 'token-123',
      user: {
        id: 'u-2',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Calendar.Manage']
      }
    });

    const router = createAppRouter(createMemoryHistory());
    await router.push('/operations/calendar');
    await router.isReady();

    expect(router.currentRoute.value.name).toBe('operations.calendar');
    expect(router.currentRoute.value.path).toBe('/operations/calendar');
  });

  it('redirects /login to /auth/login and unknown routes to 404', async () => {
    const router = createAppRouter(createMemoryHistory());

    await router.push('/login');
    expect(router.currentRoute.value.path).toBe('/auth/login');

    await router.push('/non-existent-route');
    expect(router.currentRoute.value.name).toBe('not-found');
  });
});
