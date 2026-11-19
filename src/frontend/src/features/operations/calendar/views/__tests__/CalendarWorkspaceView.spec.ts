import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import type { AcademicYearDto } from '../../api/academicYears.api';
import CalendarWorkspaceView from '../CalendarWorkspaceView.vue';

const server = setupServer();

const YEAR_ACTIVE_ID = '01900000-0000-7000-8000-000000000101';
const YEAR_INACTIVE_ID = '01900000-0000-7000-8000-000000000102';
const PERIOD_1_ID = '01900000-0000-7000-8000-000000000201';
const PERIOD_2_ID = '01900000-0000-7000-8000-000000000202';
const PERIOD_3_ID = '01900000-0000-7000-8000-000000000203';

function createSampleYears(): AcademicYearDto[] {
  return [
    {
      id: YEAR_ACTIVE_ID,
      name: '2025/2026',
      startDate: '2025-07-01',
      endDate: '2026-06-30',
      status: 'ACTIVE',
      periods: [
        {
          id: PERIOD_1_ID,
          academicYearId: YEAR_ACTIVE_ID,
          name: 'Semester 1',
          startDate: '2025-07-01',
          endDate: '2025-12-20',
          status: 'ACTIVE'
        },
        {
          id: PERIOD_2_ID,
          academicYearId: YEAR_ACTIVE_ID,
          name: 'Semester 2',
          startDate: '2026-01-05',
          endDate: '2026-06-30',
          status: 'INACTIVE'
        }
      ]
    },
    {
      id: YEAR_INACTIVE_ID,
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      status: 'INACTIVE',
      periods: [
        {
          id: PERIOD_3_ID,
          academicYearId: YEAR_INACTIVE_ID,
          name: 'Semester 1',
          startDate: '2026-07-01',
          endDate: '2026-12-20',
          status: 'INACTIVE'
        }
      ]
    }
  ];
}

function mountCalendarView() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(CalendarWorkspaceView, {
    global: {
      plugins: [
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
      ],
      stubs: {
        Teleport: true
      }
    }
  });
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
});

afterAll(() => {
  server.close();
});

describe('CalendarWorkspaceView.vue (SCR-OPR-CALENDAR Part 1)', () => {
  it('renders paginated academic years, nested periods, and disabled-state tooltips', async () => {
    const years = createSampleYears();
    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: years,
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      )
    );

    const wrapper = mountCalendarView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('2025/2026');
      expect(wrapper.text()).toContain('2026/2027');
      expect(wrapper.text()).toContain('Semester 1');
      expect(wrapper.text()).toContain('Semester 2');
    });

    const activeYearBtn = wrapper.find(`[data-testid="activate-year-${YEAR_ACTIVE_ID}"]`);
    expect(activeYearBtn.attributes('disabled')).toBeDefined();

    const activePeriodBtn = wrapper.find(`[data-testid="activate-period-${PERIOD_1_ID}"]`);
    expect(activePeriodBtn.attributes('disabled')).toBeDefined();

    const inactiveParentPeriodBtn = wrapper.find(`[data-testid="activate-period-${PERIOD_3_ID}"]`);
    expect(inactiveParentPeriodBtn.attributes('disabled')).toBeDefined();
    expect(inactiveParentPeriodBtn.element.parentElement?.getAttribute('title')).toBe(
      'Activate the parent academic year first'
    );

    const inactivePeriodInActiveYearBtn = wrapper.find(
      `[data-testid="activate-period-${PERIOD_2_ID}"]`
    );
    expect(inactivePeriodInActiveYearBtn.attributes('disabled')).toBeUndefined();
  });

  it('creates an academic year via the drawer and refreshes the list', async () => {
    let years = createSampleYears();
    let createdPayload: unknown = null;

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: years,
          page: 1,
          pageSize: 10,
          totalCount: years.length,
          totalPages: 1
        })
      ),
      http.post('/api/opr/academic-years', async ({ request }) => {
        createdPayload = await request.json();
        const created: AcademicYearDto = {
          id: '01900000-0000-7000-8000-000000000103',
          name: '2027/2028',
          startDate: '2027-07-01',
          endDate: '2028-06-30',
          status: 'INACTIVE',
          periods: [
            {
              id: '01900000-0000-7000-8000-000000000204',
              name: 'Semester 1',
              startDate: '2027-07-01',
              endDate: '2027-12-20',
              status: 'INACTIVE'
            }
          ]
        };
        years = [...years, created];
        return HttpResponse.json(created, { status: 201 });
      })
    );

    const wrapper = mountCalendarView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('2025/2026');
    });

    await wrapper.find('[data-testid="create-academic-year-button"]').trigger('click');

    await wrapper.find('#ay-name').setValue('2027/2028');
    await wrapper.find('#ay-start-date').setValue('2027-07-01');
    await wrapper.find('#ay-end-date').setValue('2028-06-30');
    await wrapper.find('#period-name-0').setValue('Semester 1');
    await wrapper.find('#period-start-0').setValue('2027-07-01');
    await wrapper.find('#period-end-0').setValue('2027-12-20');

    await wrapper.find('#academic-year-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdPayload).toEqual({
        name: '2027/2028',
        startDate: '2027-07-01',
        endDate: '2028-06-30',
        periods: [
          {
            name: 'Semester 1',
            startDate: '2027-07-01',
            endDate: '2027-12-20'
          }
        ]
      });
      expect(wrapper.text()).toContain('2027/2028');
    });
  });

  it('edits an academic year and maps backend 422 camelCase field errors via setErrors()', async () => {
    const years = createSampleYears();
    let putCalled = false;

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: years,
          page: 1,
          pageSize: 10,
          totalCount: years.length,
          totalPages: 1
        })
      ),
      http.put(`/api/opr/academic-years/${YEAR_INACTIVE_ID}`, () => {
        putCalled = true;
        return HttpResponse.json(
          {
            title: 'Validation Failed',
            status: 422,
            detail: 'One or more validation errors occurred.',
            errorCode: 'Request.ValidationFailed',
            errors: {
              name: ['Academic year name already exists.']
            }
          },
          { status: 422 }
        );
      })
    );

    const wrapper = mountCalendarView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('2026/2027');
    });

    await wrapper.find(`[data-testid="edit-year-${YEAR_INACTIVE_ID}"]`).trigger('click');
    await wrapper.find('#ay-name').setValue('2025/2026');
    await wrapper.find('#academic-year-form').trigger('submit');

    await vi.waitFor(() => {
      expect(putCalled).toBe(true);
      expect(wrapper.text()).toContain('Academic year name already exists.');
    });
  });

  it('activates an inactive academic year and an inactive operational period via ConfirmActionDialog', async () => {
    const years = createSampleYears();
    let yearActivated = false;
    let periodActivated = false;

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: years,
          page: 1,
          pageSize: 10,
          totalCount: years.length,
          totalPages: 1
        })
      ),
      http.post(`/api/opr/academic-years/${YEAR_INACTIVE_ID}/activate`, () => {
        yearActivated = true;
        return HttpResponse.json({
          ...years[1],
          status: 'ACTIVE'
        });
      }),
      http.post(`/api/opr/academic-years/${YEAR_ACTIVE_ID}/periods/${PERIOD_2_ID}/activate`, () => {
        periodActivated = true;
        return HttpResponse.json(years[0]);
      })
    );

    const wrapper = mountCalendarView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('2026/2027');
    });

    // Activate Academic Year
    await wrapper.find(`[data-testid="activate-year-${YEAR_INACTIVE_ID}"]`).trigger('click');
    expect(wrapper.text()).toContain('Activate Academic Year');

    const dialog1 = wrapper.find('[role="dialog"]');
    const confirmBtn1 = dialog1
      .findAll('button')
      .find((btn) => btn.text().trim() === 'Activate');
    expect(confirmBtn1).toBeDefined();
    await confirmBtn1!.trigger('click');

    await vi.waitFor(() => {
      expect(yearActivated).toBe(true);
    });

    // Activate Operational Period
    await wrapper.find(`[data-testid="activate-period-${PERIOD_2_ID}"]`).trigger('click');
    expect(wrapper.text()).toContain('Activate Operational Period');

    const dialog2 = wrapper.find('[role="dialog"]');
    const confirmBtn2 = dialog2
      .findAll('button')
      .find((btn) => btn.text().trim() === 'Activate');
    expect(confirmBtn2).toBeDefined();
    await confirmBtn2!.trigger('click');

    await vi.waitFor(() => {
      expect(periodActivated).toBe(true);
    });
  });
});
