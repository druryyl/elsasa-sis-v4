import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { defineComponent } from 'vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import AppLayout from '../AppLayout.vue';

const server = setupServer();

const DummyView = defineComponent({
  template: '<div>Workspace</div>'
});

async function mountAppLayout() {
  const pinia = createPinia();
  setActivePinia(pinia);

  const authStore = useAuthStore(pinia);
  authStore.setSession({
    accessToken: 'jwt-token',
    user: {
      id: '01900000-0000-7000-8000-000000000001',
      username: 'admin',
      displayName: 'School Administrator',
      email: 'admin@elsasa.sch.id',
      roles: ['SCHOOL_ADMIN'],
      permissions: [
        'Permissions.Operations.Calendar.View',
        'Permissions.Operations.Calendar.Manage'
      ],
      teacherId: null,
      studentId: null,
      guardianId: null
    }
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        redirect: '/operations/calendar'
      },
      {
        path: '/operations/calendar',
        name: 'operations.calendar',
        component: DummyView
      },
      {
        path: '/auth/login',
        name: 'auth.login',
        component: DummyView
      }
    ]
  });

  await router.push('/operations/calendar');
  await router.isReady();

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  const wrapper = mount(AppLayout, {
    global: {
      plugins: [
        pinia,
        router,
        [VueQueryPlugin, { queryClient }],
        ToastService,
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

  return { wrapper, appContextStore: useAppContextStore(pinia) };
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});

describe('AppLayout.vue Top Bar Context Switcher', () => {
  it('syncs useAppContextStore with ACTIVE year/period and shows historical banner when a non-active period is selected', async () => {
    const yearActiveId = '01900000-0000-7000-8000-000000000101';
    const periodActiveId = '01900000-0000-7000-8000-000000000201';
    const periodInactiveId = '01900000-0000-7000-8000-000000000202';

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [
            {
              id: yearActiveId,
              name: '2025/2026',
              startDate: '2025-07-01',
              endDate: '2026-06-30',
              status: 'ACTIVE',
              periods: [
                {
                  id: periodActiveId,
                  academicYearId: yearActiveId,
                  name: 'Semester 1',
                  startDate: '2025-07-01',
                  endDate: '2025-12-20',
                  status: 'ACTIVE'
                },
                {
                  id: periodInactiveId,
                  academicYearId: yearActiveId,
                  name: 'Semester 2',
                  startDate: '2026-01-05',
                  endDate: '2026-06-30',
                  status: 'INACTIVE'
                }
              ]
            }
          ],
          page: 1,
          pageSize: 100,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const { wrapper, appContextStore } = await mountAppLayout();

    await vi.waitFor(() => {
      expect(appContextStore.activeAcademicYearId).toBe(yearActiveId);
      expect(appContextStore.activePeriodId).toBe(periodActiveId);
      expect(appContextStore.isHistoricalPeriod).toBe(false);
    });

    expect(wrapper.find('[data-testid="historical-period-banner"]').exists()).toBe(false);

    const select = wrapper.find('[data-testid="context-switcher-select"]');
    expect(select.exists()).toBe(true);

    await select.setValue(`${yearActiveId}:${periodInactiveId}`);

    await vi.waitFor(() => {
      expect(appContextStore.selectedPeriodId).toBe(periodInactiveId);
      expect(appContextStore.isHistoricalPeriod).toBe(true);
      expect(wrapper.find('[data-testid="historical-period-banner"]').exists()).toBe(true);
    });
  });
});
