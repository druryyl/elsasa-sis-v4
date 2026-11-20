import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import type {
  StudyGroupTimetableSlotDto,
  StudyGroupTimetableBlockDto,
  TeacherPersonalScheduleResponse
} from '../../api/timetables.api';
import TimetableWorkspaceView from '../TimetableWorkspaceView.vue';

const server = setupServer();

const YEAR_ID = '01900000-0000-7000-8000-000000000001';
const PERIOD_ID = '01900000-0000-7000-8000-000000000002';
const GROUP_7A_ID = '01900000-0000-7000-8000-000000000003';
const GROUP_7B_ID = '01900000-0000-7000-8000-000000000004';
const SUBJECT_MTK_ID = '01900000-0000-7000-8000-000000000005';
const TEACHER_BUDI_ID = '01900000-0000-7000-8000-000000000006';
const ROOM_7A_ID = '01900000-0000-7000-8000-000000000007';

function createSampleSlots(): StudyGroupTimetableSlotDto[] {
  return [
    {
      id: '01900000-0000-7000-8000-000000000010',
      operationalPeriodId: PERIOD_ID,
      studyGroupId: GROUP_7A_ID,
      studyGroupName: 'Class 7A',
      scheduleType: 'REGULAR',
      dayOfWeek: 1, // Monday
      periodSlot: 1,
      startTime: '07:30:00',
      endTime: '08:15:00',
      subjectId: SUBJECT_MTK_ID,
      subjectName: 'Matematika',
      teacherId: TEACHER_BUDI_ID,
      teacherName: 'Budi Santoso',
      roomId: ROOM_7A_ID,
      roomName: 'Classroom 7A',
      teacherConflict: false,
      roomConflict: false
    },
    {
      id: '01900000-0000-7000-8000-000000000011',
      operationalPeriodId: PERIOD_ID,
      studyGroupId: GROUP_7A_ID,
      studyGroupName: 'Class 7A',
      scheduleType: 'REGULAR',
      dayOfWeek: 2, // Tuesday
      periodSlot: 2,
      startTime: '08:15:00',
      endTime: '09:00:00',
      subjectId: SUBJECT_MTK_ID,
      subjectName: 'Matematika',
      teacherId: TEACHER_BUDI_ID,
      teacherName: 'Budi Santoso',
      roomId: ROOM_7A_ID,
      roomName: 'Classroom 7A',
      teacherConflict: true, // Conflict simulation
      roomConflict: false
    }
  ];
}

function createSampleBlocks(): StudyGroupTimetableBlockDto[] {
  return [
    {
      id: '01900000-0000-7000-8000-000000000020',
      operationalPeriodId: PERIOD_ID,
      studyGroupId: GROUP_7A_ID,
      studyGroupName: 'Class 7A',
      scheduleType: 'GENERAL',
      startDate: '2026-08-01',
      endDate: '2026-08-14'
    },
    {
      id: '01900000-0000-7000-8000-000000000021',
      operationalPeriodId: PERIOD_ID,
      studyGroupId: GROUP_7A_ID,
      studyGroupName: 'Class 7A',
      scheduleType: 'VOCATIONAL',
      startDate: '2026-08-15',
      endDate: '2026-08-28'
    }
  ];
}

function createSampleTeacherSchedule(): TeacherPersonalScheduleResponse {
  return {
    teacherId: TEACHER_BUDI_ID,
    operationalPeriodId: PERIOD_ID,
    scheduleGroups: [
      {
        scheduleType: 'REGULAR',
        slots: [
          {
            id: '01900000-0000-7000-8000-000000000010',
            operationalPeriodId: PERIOD_ID,
            studyGroupId: GROUP_7A_ID,
            studyGroupName: 'Class 7A',
            scheduleType: 'REGULAR',
            dayOfWeek: 1,
            periodSlot: 1,
            startTime: '07:30:00',
            endTime: '08:15:00',
            subjectId: SUBJECT_MTK_ID,
            subjectName: 'Matematika',
            teacherId: TEACHER_BUDI_ID,
            teacherName: 'Budi Santoso',
            roomId: ROOM_7A_ID,
            roomName: 'Classroom 7A'
          }
        ]
      },
      {
        scheduleType: 'GENERAL',
        slots: []
      },
      {
        scheduleType: 'VOCATIONAL',
        slots: []
      }
    ]
  };
}

function setupDefaultMsw() {
  server.use(
    http.get('/api/opr/academic-years', () =>
      HttpResponse.json({
        items: [{ id: YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
        totalCount: 1
      })
    ),
    http.get('/api/std/study-groups', () =>
      HttpResponse.json({
        items: [
          { id: GROUP_7A_ID, name: 'Class 7A', academicYearId: YEAR_ID },
          { id: GROUP_7B_ID, name: 'Class 7B', academicYearId: YEAR_ID }
        ],
        totalCount: 2
      })
    ),
    http.get('/api/acd/subjects', () =>
      HttpResponse.json([
        { id: SUBJECT_MTK_ID, name: 'Matematika', code: 'MTK' }
      ])
    ),
    http.get('/api/tch/teachers', () =>
      HttpResponse.json({
        items: [
          { id: TEACHER_BUDI_ID, fullName: 'Budi Santoso', nip: '19800101' }
        ],
        totalCount: 1
      })
    ),
    http.get('/api/opr/rooms', () =>
      HttpResponse.json({
        items: [
          { id: ROOM_7A_ID, name: 'Classroom 7A', roomType: 'CLASSROOM' }
        ],
        totalCount: 1
      })
    ),
    http.get('/api/opr/study-groups/:id/timetables', () =>
      HttpResponse.json(createSampleSlots())
    ),
    http.get('/api/opr/study-groups/:id/blocks', () =>
      HttpResponse.json(createSampleBlocks())
    ),
    http.get('/api/opr/teacher-schedule', () =>
      HttpResponse.json(createSampleTeacherSchedule())
    )
  );
}

function mountTimetableView() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(TimetableWorkspaceView, {
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

class MockResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

beforeAll(() => {
  if (typeof globalThis.ResizeObserver === 'undefined') {
    globalThis.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;
  }
  if (typeof window !== 'undefined' && !window.matchMedia) {
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
  }
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
  configureAuthSessionReader(null);
  configureAppContextReader(null);
});

afterAll(() => {
  server.close();
});

describe('TimetableWorkspaceView.vue (SCR-OPR-TIMETABLE)', () => {
  it('renders breadcrumb, title, context filters, and weekly timetable matrix grid', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      selectedAcademicYearId: YEAR_ID,
      selectedPeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: [
          'Permissions.Operations.Timetables.View',
          'Permissions.Operations.Timetables.Manage',
          'Permissions.Operations.Timetables.ViewOwn'
        ]
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Verify header and breadcrumbs
    expect(wrapper.find('[data-testid="timetable-workspace-view"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Timetables & Scheduling');
    expect(wrapper.text()).toContain('Operations & Comms');

    // Verify context filter bar and rotation buttons
    expect(wrapper.find('[data-testid="timetable-filter-bar"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="schedule-type-regular"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="schedule-type-general"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="schedule-type-vocational"]').exists()).toBe(true);

    // Verify weekly matrix grid renders days Monday–Saturday
    expect(wrapper.find('[data-testid="timetable-grid"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Monday');
    expect(wrapper.text()).toContain('Saturday');
    expect(wrapper.text()).toContain('Slot 1');
    expect(wrapper.text()).toContain('Slot 10');

    // Verify populated slot rendering
    const slot11 = wrapper.find('[data-testid="slot-1-1"]');
    expect(slot11.exists()).toBe(true);
    expect(slot11.text()).toContain('Matematika');
    expect(slot11.text()).toContain('Budi Santoso');
    expect(slot11.text()).toContain('Classroom 7A');
    expect(slot11.text()).toContain('07:30–08:15');
  });

  it('renders inline rose-600 conflict badge and warning notice when conflict exists (D-10)', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Verify top conflict notice banner
    const notice = wrapper.find('[data-testid="grid-conflicts-notice"]');
    expect(notice.exists()).toBe(true);
    expect(notice.text()).toContain('scheduling conflict(s) detected');

    // Slot (2, 2) has teacherConflict: true
    const conflictingSlot = wrapper.find('[data-testid="slot-2-2"]');
    expect(conflictingSlot.exists()).toBe(true);

    const badge = conflictingSlot.find('[data-testid="conflict-badge"]');
    expect(badge.exists()).toBe(true);
    expect(badge.classes()).toContain('bg-rose-600');
    expect(badge.text()).toContain('Conflict');
    expect(badge.attributes('title')).toContain('Teacher conflict');
  });

  it('opens Slot Assignment Drawer when clicking an empty slot', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Click on empty slot (Day 3, Period 1)
    const emptySlot = wrapper.find('[data-testid="empty-slot-3-1"]');
    expect(emptySlot.exists()).toBe(true);
    await emptySlot.trigger('click');
    await flushPromises();

    // Drawer opens with title Assign Timetable Slot
    const drawerTitle = wrapper.find('[data-testid="slot-drawer-title"]');
    expect(drawerTitle.exists()).toBe(true);
    expect(drawerTitle.text()).toBe('Assign Timetable Slot');
    expect(wrapper.text()).toContain('Wednesday (Rabu) • Period Slot 1');
  });

  it('opens Slot Assignment Drawer with populated values when clicking an existing slot', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Click populated slot (Day 1, Period 1)
    const slot = wrapper.find('[data-testid="slot-1-1"]');
    expect(slot.exists()).toBe(true);
    await slot.trigger('click');
    await flushPromises();

    const drawerTitle = wrapper.find('[data-testid="slot-drawer-title"]');
    expect(drawerTitle.exists()).toBe(true);
    expect(drawerTitle.text()).toBe('Edit Timetable Slot');

    // Delete button exists in edit mode
    expect(wrapper.find('[data-testid="slot-delete-btn"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="slot-save-btn"]').exists()).toBe(true);
  });

  it('opens SMK Block Management Drawer and lists active date ranges', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Click "Manage SMK Blocks" action button
    const manageBtn = wrapper.find('[data-testid="manage-smk-blocks-btn"]');
    expect(manageBtn.exists()).toBe(true);
    await manageBtn.trigger('click');
    await flushPromises();

    // Verify SMK blocks drawer title and items
    const title = wrapper.find('[data-testid="smk-blocks-drawer-title"]');
    expect(title.exists()).toBe(true);
    expect(title.text()).toContain('SMK Block Date Rotations');

    const blockItems = wrapper.findAll('[data-testid="smk-block-item"]');
    expect(blockItems.length).toBe(2);
    expect(wrapper.text()).toContain('GENERAL');
    expect(wrapper.text()).toContain('VOCATIONAL');
  });

  it('switches to Teacher Personal Schedule Tab (UC-TCH-10) and displays personal weekly matrix', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-teacher',
        username: 'teacher.budi',
        displayName: 'Budi Santoso',
        roles: ['TEACHER'],
        teacherId: TEACHER_BUDI_ID,
        permissions: [
          'Permissions.Operations.Timetables.ViewOwn'
        ]
      }),
      hasPermission: (perm: string) => perm === 'Permissions.Operations.Timetables.ViewOwn'
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Teacher tab is active
    const teacherTab = wrapper.find('[data-testid="tab-teacher-schedule"]');
    expect(teacherTab.exists()).toBe(true);
    await teacherTab.trigger('click');
    await flushPromises();

    // Verify teacher weekly matrix grid renders
    expect(wrapper.find('[data-testid="teacher-matrix-grid"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="teacher-slot-1-1"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="teacher-slot-1-1"]').text()).toContain('Matematika');
    expect(wrapper.find('[data-testid="teacher-slot-1-1"]').text()).toContain('Class 7A');

    // Verify schedule rotation toggle (SMK General / Vocational)
    const genToggle = wrapper.find('[data-testid="teacher-sched-general-toggle"]');
    expect(genToggle.exists()).toBe(true);
    await genToggle.trigger('click');
    await flushPromises();

    // Verify active SMK block info banner renders (D-11)
    expect(wrapper.find('[data-testid="smk-block-info-banner"]').exists()).toBe(true);
  });

  it('renders historical warning banner and disables edit controls in historical period', async () => {
    setupDefaultMsw();

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      selectedPeriodId: '01900000-0000-7000-8000-000000000999', // Historical period
      isHistoricalPeriod: true
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Banner is visible
    expect(wrapper.find('[data-testid="historical-period-banner"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Historical Mode');

    // "Manage SMK Blocks" button is disabled
    const manageBtn = wrapper.find('[data-testid="manage-smk-blocks-btn"]');
    expect(manageBtn.attributes('disabled')).toBeDefined();
  });

  it('saves slot configuration when submitting Slot Assignment Drawer', async () => {
    setupDefaultMsw();

    let capturedPayload: Record<string, unknown> | null = null;
    let capturedGroupId: string | null = null;

    server.use(
      http.post('/api/opr/study-groups/:id/timetables', async ({ params, request }) => {
        capturedGroupId = params.id as string;
        capturedPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          id: '01900000-0000-7000-8000-000000000099',
          operationalPeriodId: capturedPayload.operationalPeriodId,
          studyGroupId: capturedGroupId,
          studyGroupName: 'Class 7A',
          scheduleType: capturedPayload.scheduleType,
          dayOfWeek: capturedPayload.dayOfWeek,
          periodSlot: capturedPayload.periodSlot,
          startTime: capturedPayload.startTime,
          endTime: capturedPayload.endTime,
          subjectId: capturedPayload.subjectId,
          subjectName: 'Matematika',
          teacherId: capturedPayload.teacherId,
          teacherName: 'Budi Santoso',
          roomId: capturedPayload.roomId,
          roomName: 'Classroom 7A'
        });
      })
    );

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Open populated slot (Day 1, Slot 1)
    await wrapper.find('[data-testid="slot-1-1"]').trigger('click');
    await flushPromises();

    // Submit save slot
    await wrapper.find('#slot-form').trigger('submit');

    await vi.waitFor(() => {
      expect(capturedPayload).not.toBeNull();
    });

    expect(capturedGroupId).toBe(GROUP_7A_ID);
    expect(capturedPayload?.['subjectId']).toBe(SUBJECT_MTK_ID);
    expect(capturedPayload?.['teacherId']).toBe(TEACHER_BUDI_ID);
  });

  it('deletes slot via Tier 2 confirmation dialog', async () => {
    setupDefaultMsw();

    let deletedScheduleId: string | null = null;

    server.use(
      http.delete('/api/opr/study-groups/:id/timetables/:scheduleId', ({ params }) => {
        deletedScheduleId = params.scheduleId as string;
        return new HttpResponse(null, { status: 204 });
      })
    );

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Click populated slot
    await wrapper.find('[data-testid="slot-1-1"]').trigger('click');
    await flushPromises();

    // Click Delete button in drawer
    const deleteBtn = wrapper.find('[data-testid="slot-delete-btn"]');
    expect(deleteBtn.exists()).toBe(true);
    await deleteBtn.trigger('click');
    await flushPromises();

    // Confirmation dialog appears; click confirm
    const buttons = wrapper.findAll('button');
    const confirmBtn = buttons.find((b) => b.text().includes('Confirm Delete'));
    expect(confirmBtn).toBeDefined();
    await confirmBtn?.trigger('click');

    await vi.waitFor(() => {
      expect(deletedScheduleId).toBe('01900000-0000-7000-8000-000000000010');
    });
  });

  it('adds and removes SMK rotation blocks via SMK Block Drawer', async () => {
    setupDefaultMsw();

    let createdBlockPayload: Record<string, unknown> | null = null;
    let deletedBlockId: string | null = null;

    server.use(
      http.post('/api/opr/study-groups/:id/blocks', async ({ request }) => {
        createdBlockPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          id: 'new-block-id',
          operationalPeriodId: createdBlockPayload.operationalPeriodId,
          studyGroupId: GROUP_7A_ID,
          studyGroupName: 'Class 7A',
          scheduleType: createdBlockPayload.scheduleType,
          startDate: createdBlockPayload.startDate,
          endDate: createdBlockPayload.endDate
        });
      }),
      http.delete('/api/opr/study-groups/:id/blocks/:blockId', ({ params }) => {
        deletedBlockId = params.blockId as string;
        return new HttpResponse(null, { status: 204 });
      })
    );

    configureAppContextReader(() => ({
      activeAcademicYearId: YEAR_ID,
      activePeriodId: PERIOD_ID,
      isHistoricalPeriod: false
    }));

    configureAuthSessionReader({
      getUser: () => ({
        id: 'user-admin',
        username: 'admin',
        displayName: 'School Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: ['Permissions.Operations.Timetables.View', 'Permissions.Operations.Timetables.Manage']
      }),
      hasPermission: () => true
    });

    const wrapper = mountTimetableView();
    await flushPromises();

    // Open SMK Block drawer
    await wrapper.find('[data-testid="manage-smk-blocks-btn"]').trigger('click');
    await flushPromises();

    // Fill new block form and submit
    const startDateInput = wrapper.find('[data-testid="block-start-date"]');
    const endDateInput = wrapper.find('[data-testid="block-end-date"]');
    await startDateInput.setValue('2026-09-01');
    await endDateInput.setValue('2026-09-14');
    await wrapper.find('#smk-block-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdBlockPayload).not.toBeNull();
    });

    expect(createdBlockPayload?.['startDate']).toBe('2026-09-01');
    expect(createdBlockPayload?.['endDate']).toBe('2026-09-14');

    // Click delete on an existing block item
    const deleteBlockBtn = wrapper.find('[data-testid="delete-block-btn"]');
    expect(deleteBlockBtn.exists()).toBe(true);
    await deleteBlockBtn.trigger('click');
    await flushPromises();

    // Confirm dialog
    const buttons = wrapper.findAll('button');
    const confirmRemoveBtn = buttons.find((b) => b.text().includes('Remove Block'));
    expect(confirmRemoveBtn).toBeDefined();
    await confirmRemoveBtn?.trigger('click');

    await vi.waitFor(() => {
      expect(deletedBlockId).toBe('01900000-0000-7000-8000-000000000020');
    });
  });
});
