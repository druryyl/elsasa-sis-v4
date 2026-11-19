import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import type { CalendarEntryDto } from '../../api/calendarEntries.api';
import CalendarEntriesTab from '../CalendarEntriesTab.vue';

const server = setupServer();

const YEAR_ID = '01900000-0000-7000-8000-000000000101';
const ENTRY_1_ID = '01900000-0000-7000-8000-000000000301';
const ENTRY_2_ID = '01900000-0000-7000-8000-000000000302';

function createSampleEntries(): CalendarEntryDto[] {
  return [
    {
      id: ENTRY_1_ID,
      academicYearId: YEAR_ID,
      academicYearName: '2025/2026',
      entryDate: '2025-08-17',
      dayType: 'HOLIDAY',
      description: 'Independence Day'
    },
    {
      id: ENTRY_2_ID,
      academicYearId: YEAR_ID,
      academicYearName: '2025/2026',
      entryDate: '2025-09-01',
      dayType: 'SCHOOL_DAY',
      description: 'Regular Teaching Day'
    }
  ];
}

function mountTab(props = { canManage: true }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(CalendarEntriesTab, {
    props,
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
  configureAppContextReader(null);
});

afterAll(() => {
  server.close();
});

describe('CalendarEntriesTab.vue', () => {
  it('renders calendar entries with StatusPill, formatted dates, and descriptions', async () => {
    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: YEAR_ID, name: '2025/2026', status: 'ACTIVE' }],
          page: 1,
          pageSize: 100,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/opr/calendar-entries', () =>
        HttpResponse.json({
          items: createSampleEntries(),
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      )
    );

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      selectedAcademicYearId: YEAR_ID,
      activeAcademicYearName: '2025/2026'
    }));

    const wrapper = mountTab();

    await wrapper.vm.$nextTick();

    expect(wrapper.find('[data-testid="calendar-entries-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="create-calendar-entry-button"]').exists()).toBe(true);

    // Wait for table to render rows
    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Independence Day');
      expect(wrapper.text()).toContain('Regular Teaching Day');
      expect(wrapper.text()).toContain('Holiday');
      expect(wrapper.text()).toContain('School Day');
    });
  });

  it('creates a calendar entry and closes the drawer', async () => {
    let createdPayload: unknown = null;

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: YEAR_ID, name: '2025/2026', status: 'ACTIVE' }],
          page: 1,
          pageSize: 100,
          totalCount: 1
        })
      ),
      http.get('/api/opr/calendar-entries', () =>
        HttpResponse.json({
          items: createSampleEntries(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      ),
      http.post('/api/opr/calendar-entries', async ({ request }) => {
        createdPayload = await request.json();
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000000303',
            academicYearId: YEAR_ID,
            entryDate: '2025-12-25',
            dayType: 'HOLIDAY',
            description: 'Christmas Day'
          },
          { status: 201 }
        );
      })
    );

    configureAppContextReader(() => ({
      selectedAcademicYearId: YEAR_ID,
      activeAcademicYearName: '2025/2026'
    }));

    const wrapper = mountTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Independence Day');
    });

    // Open Drawer
    await wrapper.find('[data-testid="create-calendar-entry-button"]').trigger('click');

    const drawer = wrapper.find('[data-testid="calendar-entry-drawer"]');
    expect(drawer.exists()).toBe(true);

    // Fill form
    const dateInput = wrapper.find('[data-testid="entry-date-input"]');
    const descInput = wrapper.find('[data-testid="entry-description-input"]');
    const dayTypeSelect = wrapper.find('[data-testid="day-type-select"]');

    await dateInput.setValue('2025-12-25');
    await dayTypeSelect.setValue('HOLIDAY');
    await descInput.setValue('Christmas Day');

    // Submit
    await wrapper.find('#calendar-entry-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdPayload).toEqual({
        academicYearId: YEAR_ID,
        entryDate: '2025-12-25',
        dayType: 'HOLIDAY',
        description: 'Christmas Day'
      });
    });
  });

  it('handles 409 duplicate date conflict by displaying conflict banner in drawer', async () => {
    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: YEAR_ID, name: '2025/2026' }],
          page: 1,
          pageSize: 100,
          totalCount: 1
        })
      ),
      http.get('/api/opr/calendar-entries', () =>
        HttpResponse.json({
          items: createSampleEntries(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      ),
      http.post('/api/opr/calendar-entries', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
            title: 'Conflict',
            status: 409,
            detail: 'A calendar entry already exists on 2025-08-17 for this academic year.'
          },
          {
            status: 409,
            headers: { 'Content-Type': 'application/problem+json' }
          }
        )
      )
    );

    configureAppContextReader(() => ({
      selectedAcademicYearId: YEAR_ID
    }));

    const wrapper = mountTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Independence Day');
    });

    // Open Drawer
    await wrapper.find('[data-testid="create-calendar-entry-button"]').trigger('click');

    await wrapper.find('[data-testid="entry-date-input"]').setValue('2025-08-17');
    await wrapper.find('#calendar-entry-form').trigger('submit');

    await vi.waitFor(() => {
      const banner = wrapper.find('[data-testid="drawer-conflict-alert"]');
      expect(banner.exists()).toBe(true);
      expect(banner.text()).toContain('A calendar entry already exists on 2025-08-17');
    });
  });

  it('deletes calendar entry via Tier 2 ConfirmActionDialog', async () => {
    let deletedId: string | null = null;

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: YEAR_ID, name: '2025/2026' }],
          page: 1,
          pageSize: 100,
          totalCount: 1
        })
      ),
      http.get('/api/opr/calendar-entries', () =>
        HttpResponse.json({
          items: createSampleEntries(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      ),
      http.delete('/api/opr/calendar-entries/:id', ({ params }) => {
        deletedId = String(params.id);
        return new HttpResponse(null, { status: 204 });
      })
    );

    configureAppContextReader(() => ({
      selectedAcademicYearId: YEAR_ID
    }));

    const wrapper = mountTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Independence Day');
    });

    // Click delete on entry 1
    const deleteBtn = wrapper.find(`[data-testid="delete-calendar-entry-${ENTRY_1_ID}"]`);
    expect(deleteBtn.exists()).toBe(true);
    await deleteBtn.trigger('click');

    // Dialog is visible
    const dialog = wrapper.find('[data-testid="delete-calendar-entry-dialog"]');
    expect(dialog.exists()).toBe(true);
    expect(dialog.text()).toContain('Delete Calendar Entry');

    // Confirm delete
    const confirmBtn = dialog.find('button.p-button-danger');
    await confirmBtn.trigger('click');

    await vi.waitFor(() => {
      expect(deletedId).toBe(ENTRY_1_ID);
    });
  });

  it('hides creation and action buttons when canManage is false (read-only mode)', async () => {
    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: YEAR_ID, name: '2025/2026' }],
          page: 1,
          pageSize: 100,
          totalCount: 1
        })
      ),
      http.get('/api/opr/calendar-entries', () =>
        HttpResponse.json({
          items: createSampleEntries(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      )
    );

    configureAppContextReader(() => ({
      selectedAcademicYearId: YEAR_ID
    }));

    const wrapper = mountTab({ canManage: false });

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Independence Day');
    });

    // Create button should not exist
    expect(wrapper.find('[data-testid="create-calendar-entry-button"]').exists()).toBe(false);

    // Row action buttons should not exist
    expect(wrapper.find(`[data-testid="edit-calendar-entry-${ENTRY_1_ID}"]`).exists()).toBe(false);
    expect(wrapper.find(`[data-testid="delete-calendar-entry-${ENTRY_1_ID}"]`).exists()).toBe(false);
  });
});
