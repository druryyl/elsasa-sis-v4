import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { defineComponent } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import type { RecordAttendanceRequest } from '../../api/attendance.api';
import AttendanceWorkbenchView from '../AttendanceWorkbenchView.vue';

const server = setupServer();

const OPERATIONAL_PERIOD_ID = '01900000-0000-7000-8000-000000000101';
const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000100';
const SUBJECT_ID = '01900000-0000-7000-8000-000000000201';
const STUDY_GROUP_ID = '01900000-0000-7000-8000-000000000301';
const ATTENDANCE_DATE = '2026-11-20';

const STUDENT_1_ID = '01900000-0000-7000-8000-000000000401';
const STUDENT_2_ID = '01900000-0000-7000-8000-000000000402';

function setupDefaultMsw(
  customSheetItems = [
    {
      studentId: STUDENT_1_ID,
      studentNis: '2026001',
      studentName: 'Ahmad Dahlan',
      attendanceRecordId: null,
      status: null,
      notes: null,
      recordedByTeacherId: null
    },
    {
      studentId: STUDENT_2_ID,
      studentNis: '2026002',
      studentName: 'Siti Badriah',
      attendanceRecordId: null,
      status: null,
      notes: null,
      recordedByTeacherId: null
    }
  ]
) {
  server.use(
    http.get('/api/acd/subjects', () =>
      HttpResponse.json([
        {
          id: SUBJECT_ID,
          name: 'Mathematics',
          code: 'MATH'
        }
      ])
    ),
    http.get('/api/std/study-groups', () =>
      HttpResponse.json({
        items: [
          {
            id: STUDY_GROUP_ID,
            name: '7A',
            academicYearId: ACADEMIC_YEAR_ID
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/acd/attendance', () =>
      HttpResponse.json({
        subjectId: SUBJECT_ID,
        studyGroupId: STUDY_GROUP_ID,
        attendanceDate: ATTENDANCE_DATE,
        operationalPeriodId: OPERATIONAL_PERIOD_ID,
        items: customSheetItems
      })
    ),
    http.post('/api/acd/attendance', async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      const items = (body?.items as unknown[]) ?? [];
      return HttpResponse.json({
        recordedCount: items.length || 2
      });
    })
  );
}

const AppWrapper = defineComponent({
  template: '<router-view />'
});

function createTestRouter(initialPath = '/academic/attendance') {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/academic/attendance',
        name: 'academic.attendance',
        component: AttendanceWorkbenchView
      },
      {
        path: '/other',
        name: 'other',
        component: { template: '<div>Other Page</div>' }
      }
    ]
  });
  void router.push(initialPath);
  return router;
}

async function mountAttendanceWorkbench(options?: {
  permissions?: string[];
  initialPath?: string;
  isHistorical?: boolean;
}) {
  const permissions = options?.permissions ?? [
    'Permissions.Academic.Attendance.View',
    'Permissions.Academic.Attendance.Record'
  ];

  configureAuthSessionReader({
    getUser: () => ({
      id: 'usr-1',
      username: 'teacher.budi',
      displayName: 'Budi Santoso',
      roles: ['TEACHER'],
      permissions
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    activePeriodId: OPERATIONAL_PERIOD_ID,
    selectedAcademicYearId: ACADEMIC_YEAR_ID,
    selectedPeriodId: OPERATIONAL_PERIOD_ID,
    currentContextLabel: '2025/2026 • Semester 1',
    isHistoricalPeriod: options?.isHistorical ?? false
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  const router = createTestRouter(options?.initialPath);
  await router.isReady();

  const wrapper = mount(AppWrapper, {
    global: {
      plugins: [
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
      ],
      stubs: {
        Teleport: true
      }
    }
  });

  return { wrapper, router, queryClient };
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

beforeEach(() => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn()
  }));
});

afterEach(() => {
  server.resetHandlers();
  vi.restoreAllMocks();
});

afterAll(() => {
  server.close();
});

describe('AttendanceWorkbenchView.vue', () => {
  it('renders missing-filters empty state when no session is selected', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountAttendanceWorkbench({ initialPath: '/academic/attendance' });

    expect(wrapper.find('[data-testid="attendance-workbench-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="missing-filters-empty-state"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(false);
  });

  it('loads attendance roster grid when required session filters are provided', async () => {
    setupDefaultMsw();
    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper } = await mountAttendanceWorkbench({ initialPath });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="attendance-roster-grid"]').exists()).toBe(true);
    });

    expect(wrapper.find(`[data-testid="student-name-${STUDENT_1_ID}"]`).text()).toBe('Ahmad Dahlan');
    expect(wrapper.find(`[data-testid="student-name-${STUDENT_2_ID}"]`).text()).toBe('Siti Badriah');
    expect(wrapper.find('[data-testid="tally-total"]').text()).toContain('2');
    expect(wrapper.find('[data-testid="tally-unmarked"]').text()).toContain('2');
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(false);
  });

  it('marks all students as PRESENT when "Mark All Present" shortcut is clicked', async () => {
    setupDefaultMsw();
    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper } = await mountAttendanceWorkbench({ initialPath });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="mark-all-present-btn"]').exists()).toBe(true);
    });

    const markAllBtn = wrapper.find('[data-testid="mark-all-present-btn"]');
    await markAllBtn.trigger('click');

    // Both student chips should be PRESENT
    const chip1 = wrapper.find(`[data-testid="chip-present-${STUDENT_1_ID}"]`);
    const chip2 = wrapper.find(`[data-testid="chip-present-${STUDENT_2_ID}"]`);
    expect(chip1.attributes('aria-checked')).toBe('true');
    expect(chip2.attributes('aria-checked')).toBe('true');

    // Tallies should update
    expect(wrapper.find('[data-testid="tally-present"]').text()).toContain('2');
    expect(wrapper.find('[data-testid="tally-unmarked"]').exists()).toBe(false);

    // Dirty tracking & sticky save bar should become visible
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="dirty-count-label"]').text()).toContain('2 unsaved student attendance changes');
  });

  it('updates dirty state when individual chip is toggled and notes are typed', async () => {
    setupDefaultMsw();
    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper } = await mountAttendanceWorkbench({ initialPath });

    await vi.waitFor(() => {
      expect(wrapper.find(`[data-testid="chip-sick-${STUDENT_1_ID}"]`).exists()).toBe(true);
    });

    // Toggle SICK chip for student 1
    const sickChip = wrapper.find(`[data-testid="chip-sick-${STUDENT_1_ID}"]`);
    await sickChip.trigger('click');

    expect(sickChip.attributes('aria-checked')).toBe('true');
    expect(wrapper.find('[data-testid="tally-sick"]').text()).toContain('1');

    // Enter notes for student 1
    const notesInput = wrapper.find(`[data-testid="notes-input-${STUDENT_1_ID}"]`);
    await notesInput.setValue('Flu and fever');

    // Student 1 row should be dirty
    const student1Row = wrapper.find(`[data-testid="roster-row-${STUDENT_1_ID}"]`);
    expect(student1Row.find('[data-testid="dirty-indicator"]').exists()).toBe(true);

    // Student 2 row should NOT be dirty
    const student2Row = wrapper.find(`[data-testid="roster-row-${STUDENT_2_ID}"]`);
    expect(student2Row.find('[data-testid="dirty-indicator"]').exists()).toBe(false);

    // Sticky save bar should show 1 modified student
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="dirty-count-label"]').text()).toContain('1 unsaved student attendance change');
  });

  it('reverts all changes and hides sticky bar when Discard button is clicked', async () => {
    setupDefaultMsw();
    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper } = await mountAttendanceWorkbench({ initialPath });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="mark-all-present-btn"]').exists()).toBe(true);
    });

    // Mark all present
    await wrapper.find('[data-testid="mark-all-present-btn"]').trigger('click');
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(true);

    // Click Discard
    const discardBtn = wrapper.find('[data-testid="discard-btn"]');
    await discardBtn.trigger('click');

    // Should revert to unmarked
    expect(wrapper.find(`[data-testid="chip-present-${STUDENT_1_ID}"]`).attributes('aria-checked')).toBe('false');
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(false);
  });

  it('commits batch upsert via POST /api/acd/attendance and resets dirty state on success', async () => {
    setupDefaultMsw();
    let capturedPayload: RecordAttendanceRequest | null = null;
    server.use(
      http.post('/api/acd/attendance', async ({ request }) => {
        capturedPayload = (await request.json()) as RecordAttendanceRequest;
        return HttpResponse.json({ recordedCount: 2 });
      })
    );

    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper } = await mountAttendanceWorkbench({ initialPath });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="mark-all-present-btn"]').exists()).toBe(true);
    });

    // Mark all present, then adjust student 2 to SICK with notes
    await wrapper.find('[data-testid="mark-all-present-btn"]').trigger('click');
    await wrapper.find(`[data-testid="chip-sick-${STUDENT_2_ID}"]`).trigger('click');
    await wrapper.find(`[data-testid="notes-input-${STUDENT_2_ID}"]`).setValue('Demam tinggi');

    // Click Save Changes in sticky bar
    const saveBtn = wrapper.find('[data-testid="save-changes-btn"]');
    await saveBtn.trigger('click');

    await vi.waitFor(() => {
      expect(capturedPayload).not.toBeNull();
    });

    expect(capturedPayload!.subjectId).toBe(SUBJECT_ID);
    expect(capturedPayload!.studyGroupId).toBe(STUDY_GROUP_ID);
    expect(capturedPayload!.attendanceDate).toBe(ATTENDANCE_DATE);
    expect(capturedPayload!.operationalPeriodId).toBe(OPERATIONAL_PERIOD_ID);
    expect(capturedPayload!.items).toHaveLength(2);
    expect(capturedPayload!.items![0]).toEqual({
      studentId: STUDENT_1_ID,
      status: 'PRESENT',
      notes: null
    });
    expect(capturedPayload!.items![1]).toEqual({
      studentId: STUDENT_2_ID,
      status: 'SICK',
      notes: 'Demam tinggi'
    });

    // Sticky bar should disappear after successful commit
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(false);
    });
  });

  it('intercepts route exit via onBeforeRouteLeave when dirty', async () => {
    setupDefaultMsw();
    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper, router } = await mountAttendanceWorkbench({ initialPath });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="mark-all-present-btn"]').exists()).toBe(true);
    });

    // Make dirty
    await wrapper.find('[data-testid="mark-all-present-btn"]').trigger('click');
    expect(wrapper.find('[data-testid="attendance-sticky-bar"]').exists()).toBe(true);

    // Mock confirm dialog rejection
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

    // Attempt navigation
    await router.push('/other');

    expect(confirmSpy).toHaveBeenCalled();
    // Route should still be /academic/attendance
    expect(router.currentRoute.value.path).toBe('/academic/attendance');

    // Now confirm navigation
    confirmSpy.mockReturnValue(true);
    await router.push('/other');

    expect(router.currentRoute.value.path).toBe('/other');
  });

  it('disables editing when user only has View permission', async () => {
    setupDefaultMsw();
    const initialPath = `/academic/attendance?subjectId=${SUBJECT_ID}&studyGroupId=${STUDY_GROUP_ID}&date=${ATTENDANCE_DATE}`;
    const { wrapper } = await mountAttendanceWorkbench({
      initialPath,
      permissions: ['Permissions.Academic.Attendance.View']
    });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="attendance-roster-grid"]').exists()).toBe(true);
    });

    // Quick mark button should not be rendered
    expect(wrapper.find('[data-testid="mark-all-present-btn"]').exists()).toBe(false);

    // Chips and inputs should be disabled
    const chip = wrapper.find(`[data-testid="chip-present-${STUDENT_1_ID}"]`);
    expect(chip.attributes('disabled')).toBeDefined();

    const notesInput = wrapper.find(`[data-testid="notes-input-${STUDENT_1_ID}"]`);
    expect(notesInput.attributes('disabled')).toBeDefined();
  });
});
