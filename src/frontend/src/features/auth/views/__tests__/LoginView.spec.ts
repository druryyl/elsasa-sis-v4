import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { defineComponent } from 'vue';
import { configureHttpClient } from '@/shared/services/http';
import {
  configureAuthSessionHook,
  type AuthSessionPayload
} from '@/shared/services/authSession';
import LoginView from '../LoginView.vue';

const server = setupServer();

const DummyView = defineComponent({
  template: '<div data-testid="dummy-target">Target</div>'
});

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/auth/login',
        name: 'auth.login',
        component: LoginView
      },
      {
        path: '/operations/calendar',
        name: 'operations.calendar',
        component: DummyView
      },
      {
        path: '/custom-redirect',
        name: 'custom.redirect',
        component: DummyView
      },
      {
        path: '/portal/parent',
        name: 'portal.parent',
        component: DummyView
      },
      {
        path: '/portal/student',
        name: 'portal.student',
        component: DummyView
      }
    ]
  });
}

async function mountLoginView(initialPath = '/auth/login') {
  let capturedSession: AuthSessionPayload | null = null;
  configureAuthSessionHook((payload) => {
    capturedSession = payload;
  });

  const router = createTestRouter();
  await router.push(initialPath);
  await router.isReady();

  const wrapper = mount(LoginView, {
    global: {
      plugins: [
        router,
        [
          PrimeVue,
          {
            theme: {
              preset: Aura
            }
          }
        ]
      ]
    }
  });

  return {
    wrapper,
    router,
    getCapturedSession: () => capturedSession
  };
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
  configureAuthSessionHook(null);
});

afterAll(() => {
  server.close();
});

describe('LoginView.vue (SCR-AUTH-LOGIN)', () => {
  beforeEach(() => {
    configureAuthSessionHook(null);
  });

  it('displays validation errors when submitting empty credentials without calling API', async () => {
    let apiCalled = false;
    server.use(
      http.post('/api/auth/login', () => {
        apiCalled = true;
        return HttpResponse.json({});
      })
    );

    const { wrapper } = await mountLoginView();

    await wrapper.find('form').trigger('submit');

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Username is required');
      expect(wrapper.text()).toContain('Password is required');
    });
    expect(apiCalled).toBe(false);
  });

  it('signs in successfully via /api/auth/login, publishes session, and redirects to /operations/calendar', async () => {
    server.use(
      http.post('/api/auth/login', async ({ request }) => {
        const body = (await request.json()) as { username: string; password: string };
        expect(body).toEqual({ username: 'admin', password: 'Password123!' });

        return HttpResponse.json({
          accessToken: 'jwt-token-123',
          expiresAt: '2026-11-18T17:00:00Z',
          user: {
            id: '01900000-0000-7000-8000-000000000001',
            username: 'admin',
            displayName: 'School Administrator',
            email: 'admin@elsasa.sch.id',
            roles: ['SCHOOL_ADMIN'],
            permissions: [
              'Permissions.Operations.Calendar.View',
              'Permissions.Operations.Calendar.Manage'
            ]
          }
        });
      })
    );

    const { wrapper, router, getCapturedSession } = await mountLoginView();

    await wrapper.find('#username').setValue('admin');
    await wrapper.find('#password').setValue('Password123!');
    await wrapper.find('form').trigger('submit');

    await vi.waitFor(() => {
      const session = getCapturedSession();
      expect(session).not.toBeNull();
      expect(session?.accessToken).toBe('jwt-token-123');
      expect(session?.user.username).toBe('admin');
      expect(router.currentRoute.value.path).toBe('/operations/calendar');
    });
  });

  it('redirects to route.query.redirect after successful login when provided', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({
          accessToken: 'jwt-token-456',
          expiresAt: '2026-11-18T17:00:00Z',
          user: {
            id: '01900000-0000-7000-8000-000000000001',
            username: 'admin',
            displayName: 'School Administrator',
            roles: ['SCHOOL_ADMIN'],
            permissions: []
          }
        })
      )
    );

    const { wrapper, router } = await mountLoginView('/auth/login?redirect=/custom-redirect');

    await wrapper.find('#username').setValue('admin');
    await wrapper.find('#password').setValue('Password123!');
    await wrapper.find('form').trigger('submit');

    await vi.waitFor(() => {
      expect(router.currentRoute.value.path).toBe('/custom-redirect');
    });
  });

  it('signs in successfully as PARENT and redirects to /portal/parent', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({
          accessToken: 'jwt-token-parent',
          expiresAt: '2026-11-18T17:00:00Z',
          user: {
            id: '01900000-0000-7000-8000-000000000002',
            username: 'parent.hendra',
            displayName: 'Hendra Dahlan',
            roles: ['PARENT'],
            permissions: ['Permissions.Students.Children.View']
          }
        })
      )
    );

    const { wrapper, router, getCapturedSession } = await mountLoginView();

    await wrapper.find('#username').setValue('parent.hendra');
    await wrapper.find('#password').setValue('Password123!');
    await wrapper.find('form').trigger('submit');

    await vi.waitFor(() => {
      const session = getCapturedSession();
      expect(session).not.toBeNull();
      expect(session?.user.roles).toContain('PARENT');
      expect(router.currentRoute.value.path).toBe('/portal/parent');
    });
  });

  it('signs in successfully as STUDENT and redirects to /portal/student', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({
          accessToken: 'jwt-token-student',
          expiresAt: '2026-11-18T17:00:00Z',
          user: {
            id: '01900000-0000-7000-8000-000000000003',
            username: 'student.ahmad',
            displayName: 'Ahmad Dahlan',
            roles: ['STUDENT'],
            permissions: ['Permissions.Operations.Timetables.ViewOwn']
          }
        })
      )
    );

    const { wrapper, router, getCapturedSession } = await mountLoginView();

    await wrapper.find('#username').setValue('student.ahmad');
    await wrapper.find('#password').setValue('Password123!');
    await wrapper.find('form').trigger('submit');

    await vi.waitFor(() => {
      const session = getCapturedSession();
      expect(session).not.toBeNull();
      expect(session?.user.roles).toContain('STUDENT');
      expect(router.currentRoute.value.path).toBe('/portal/student');
    });
  });

  it('renders inline 401 error banner on invalid credentials and remains unauthenticated', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json(
          {
            title: 'Unauthorized',
            status: 401,
            detail: 'Invalid username or password.'
          },
          { status: 401 }
        )
      )
    );

    const { wrapper, router, getCapturedSession } = await mountLoginView();

    await wrapper.find('#username').setValue('admin');
    await wrapper.find('#password').setValue('wrong-password');
    await wrapper.find('form').trigger('submit');

    await vi.waitFor(() => {
      const banner = wrapper.find('[data-testid="login-error-banner"]');
      expect(banner.exists()).toBe(true);
      expect(banner.text()).toContain('Invalid username or password');
    });
    expect(getCapturedSession()).toBeNull();
    expect(router.currentRoute.value.path).toBe('/auth/login');
  });
});
