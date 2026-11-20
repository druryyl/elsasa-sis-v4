import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import type {
  ExtracurricularActivityDto,
  ExtracurricularParticipationDto
} from '../../api/extracurricular.api';
import ExtracurricularWorkbenchView from '../ExtracurricularWorkbenchView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000100';
const TEACHER_BUDI_ID = '01900000-0000-7000-8000-000000000201';
const TEACHER_SITI_ID = '01900000-0000-7000-8000-000000000202';
const ACTIVITY_PRAMUKA_ID = '01900000-0000-7000-8000-000000000301';
const ACTIVITY_FUTSAL_ID = '01900000-0000-7000-8000-000000000302';
const ACTIVITY_CHESS_ID = '01900000-0000-7000-8000-000000000303';
const STUDENT_AHMAD_ID = '01900000-0000-7000-8000-000000000401';
const STUDENT_RINA_ID = '01900000-0000-7000-8000-000000000402';
const PARTICIPATION_1_ID = '01900000-0000-7000-8000-000000000501';

function createInitialActivities(): ExtracurricularActivityDto[] {
  return [
    {
      id: ACTIVITY_PRAMUKA_ID,
      name: 'Pramuka',
      description: 'Leadership and outdoor scouting activities.',
      supervisorId: TEACHER_BUDI_ID,
      supervisorName: 'Budi Santoso',
      status: 'ACTIVE',
      activeParticipantCount: 1,
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    },
    {
      id: ACTIVITY_FUTSAL_ID,
      name: 'Futsal Club',
      description: 'After-school futsal training and friendly matches.',
      supervisorId: TEACHER_SITI_ID,
      supervisorName: 'Siti Aminah',
      status: 'ACTIVE',
      activeParticipantCount: 0,
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    },
    {
      id: ACTIVITY_CHESS_ID,
      name: 'Chess Club',
      description: 'Strategic board game sessions.',
      supervisorId: TEACHER_SITI_ID,
      supervisorName: 'Siti Aminah',
      status: 'INACTIVE',
      activeParticipantCount: 0,
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    }
  ];
}

function createInitialParticipants(): ExtracurricularParticipationDto[] {
  return [
    {
      id: PARTICIPATION_1_ID,
      activityId: ACTIVITY_PRAMUKA_ID,
      activityName: 'Pramuka',
      studentId: STUDENT_AHMAD_ID,
      studentName: 'Ahmad Dahlan',
      studentNis: '20260001',
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      status: 'ACTIVE',
      joinedDate: '2026-07-15',
      completedAt: null,
      withdrawnAt: null,
      lastSessionLog: 'Practiced pioneering knots and teamwork.',
      lastSessionDate: '2026-11-10',
      createdAt: '2026-07-15T08:00:00Z',
      updatedAt: '2026-11-10T08:00:00Z'
    }
  ];
}

function setupDefaultMsw(options?: {
  onCreateActivity?: (body: Record<string, unknown>) => void;
  onEnrollParticipant?: (body: Record<string, unknown>) => void;
  onRecordLog?: (body: Record<string, unknown>) => void;
  onWithdrawParticipant?: (participationId: string) => void;
}) {
  const activities = createInitialActivities();
  const participantsByActivity: Record<string, ReturnType<typeof createInitialParticipants>> = {
    [ACTIVITY_PRAMUKA_ID]: createInitialParticipants(),
    [ACTIVITY_FUTSAL_ID]: [],
    [ACTIVITY_CHESS_ID]: []
  };

  server.use(
    http.get('/api/dev/extracurriculars', ({ request }) => {
      const url = new URL(request.url);
      const status = url.searchParams.get('Status');
      const filtered = status ? activities.filter((a) => a.status === status) : activities;
      return HttpResponse.json(filtered);
    }),
    http.post('/api/dev/extracurriculars', async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      options?.onCreateActivity?.(body);
      const created = {
        id: '01900000-0000-7000-8000-000000000399',
        name: String(body.name ?? ''),
        description: (body.description as string | null) ?? null,
        supervisorId: String(body.supervisorId ?? TEACHER_BUDI_ID),
        supervisorName: 'Budi Santoso',
        status: 'ACTIVE',
        activeParticipantCount: 0,
        createdAt: '2026-11-20T08:00:00Z',
        updatedAt: '2026-11-20T08:00:00Z'
      };
      activities.push(created);
      participantsByActivity[created.id] = [];
      return HttpResponse.json(created, { status: 201 });
    }),
    http.get('/api/dev/extracurriculars/:activityId/participants', ({ params }) => {
      const activityId = String(params.activityId);
      return HttpResponse.json(participantsByActivity[activityId] ?? []);
    }),
    http.post('/api/dev/extracurriculars/:activityId/participants', async ({ params, request }) => {
      const activityId = String(params.activityId);
      const body = (await request.json()) as Record<string, unknown>;
      options?.onEnrollParticipant?.(body);
      const enrolled = {
        id: '01900000-0000-7000-8000-000000000599',
        activityId,
        activityName: 'Pramuka',
        studentId: String(body.studentId ?? STUDENT_RINA_ID),
        studentName: String(body.studentName ?? 'Rina Marlina'),
        studentNis: String(body.studentNis ?? '20260002'),
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        status: 'ACTIVE',
        joinedDate: String(body.joinedDate ?? '2026-11-20'),
        completedAt: null,
        withdrawnAt: null,
        lastSessionLog: null,
        lastSessionDate: null,
        createdAt: '2026-11-20T08:00:00Z',
        updatedAt: '2026-11-20T08:00:00Z'
      };
      participantsByActivity[activityId] = [
        ...(participantsByActivity[activityId] ?? []),
        enrolled
      ];
      return HttpResponse.json(enrolled, { status: 201 });
    }),
    http.post(
      '/api/dev/extracurriculars/:activityId/participants/:participationId/withdraw',
      ({ params }) => {
        const activityId = String(params.activityId);
        const participationId = String(params.participationId);
        options?.onWithdrawParticipant?.(participationId);
        const list = participantsByActivity[activityId] ?? [];
        const item = list.find((p) => p.id === participationId);
        if (item) {
          item.status = 'WITHDRAWN';
        }
        return HttpResponse.json(item);
      }
    ),
    http.post(
      '/api/dev/extracurriculars/:activityId/participants/:participationId/log',
      async ({ params, request }) => {
        const activityId = String(params.activityId);
        const participationId = String(params.participationId);
        const body = (await request.json()) as Record<string, unknown>;
        options?.onRecordLog?.(body);
        const list = participantsByActivity[activityId] ?? [];
        const item = list.find((p) => p.id === participationId);
        if (item) {
          item.lastSessionDate = String(body.sessionDate ?? '2026-11-20');
          item.lastSessionLog = String(body.log ?? '');
        }
        return HttpResponse.json(item);
      }
    ),
    http.get('/api/tch/teachers', () =>
      HttpResponse.json({
        items: [
          {
            id: TEACHER_BUDI_ID,
            fullName: 'Budi Santoso',
            nip: '198501012010011001',
            status: 'ACTIVE'
          },
          {
            id: TEACHER_SITI_ID,
            fullName: 'Siti Aminah',
            nip: '198705122012012002',
            status: 'ACTIVE'
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 2,
        totalPages: 1
      })
    ),
    http.get('/api/std/students', () =>
      HttpResponse.json({
        items: [
          {
            id: STUDENT_AHMAD_ID,
            nis: '20260001',
            fullName: 'Ahmad Dahlan',
            studyGroupName: '7A',
            grade: '7',
            status: 'ACTIVE'
          },
          {
            id: STUDENT_RINA_ID,
            nis: '20260002',
            fullName: 'Rina Marlina',
            studyGroupName: '7B',
            grade: '7',
            status: 'ACTIVE'
          }
        ],
        page: 1,
        pageSize: 50,
        totalCount: 2,
        totalPages: 1
      })
    )
  );
}

function mountWorkbench(options?: {
  role?: 'SCHOOL_ADMIN' | 'TEACHER';
  permissions?: string[];
  teacherId?: string | null;
  userId?: string;
}) {
  const role = options?.role ?? 'SCHOOL_ADMIN';
  const permissions =
    options?.permissions ??
    (role === 'SCHOOL_ADMIN'
      ? ['Permissions.Development.Extracurricular.Manage']
      : ['Permissions.Development.Extracurricular.Log']);

  configureAuthSessionReader({
    getUser: () => ({
      id: options?.userId ?? 'usr-1',
      username: role === 'SCHOOL_ADMIN' ? 'admin' : 'teacher.budi',
      displayName: role === 'SCHOOL_ADMIN' ? 'School Admin' : 'Budi Santoso',
      roles: [role],
      permissions,
      teacherId: options?.teacherId ?? null
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    activeAcademicYearName: '2026/2027',
    currentContextLabel: '2026/2027 • Semester 1'
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(ExtracurricularWorkbenchView, {
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

describe('ExtracurricularWorkbenchView.vue', () => {
  it('renders activity catalog grid with clubs, supervisors, and active member counts', async () => {
    setupDefaultMsw();
    const wrapper = mountWorkbench({ role: 'SCHOOL_ADMIN' });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="activity-catalog-grid"]').exists()).toBe(true);
    });

    expect(wrapper.text()).toContain('Pramuka');
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('1 active');
    expect(wrapper.text()).toContain('Futsal Club');
    expect(wrapper.find('[data-testid="new-club-button"]').exists()).toBe(true);
  });

  it('hides admin management buttons when user only has session logging permission', async () => {
    setupDefaultMsw();
    const wrapper = mountWorkbench({
      role: 'TEACHER',
      teacherId: TEACHER_BUDI_ID
    });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="activity-catalog-grid"]').exists()).toBe(true);
    });

    expect(wrapper.find('[data-testid="new-club-button"]').exists()).toBe(false);
    expect(wrapper.find(`[data-testid="edit-club-button-${ACTIVITY_PRAMUKA_ID}"]`).exists()).toBe(
      false
    );
    expect(wrapper.text()).toContain('Supervised by You');
  });

  it('filters catalog by search query and My Supervised Clubs toggle', async () => {
    setupDefaultMsw();
    const wrapper = mountWorkbench({
      role: 'TEACHER',
      teacherId: TEACHER_BUDI_ID
    });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="activity-catalog-grid"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="my-supervised-toggle"]').trigger('click');
    expect(wrapper.find(`[data-testid="activity-card-${ACTIVITY_PRAMUKA_ID}"]`).exists()).toBe(
      true
    );
    expect(wrapper.find(`[data-testid="activity-card-${ACTIVITY_FUTSAL_ID}"]`).exists()).toBe(
      false
    );

    await wrapper.find('[data-testid="my-supervised-toggle"]').trigger('click');
    const searchInput = wrapper.find('[data-testid="club-search-input"]');
    await searchInput.setValue('Futsal');
    expect(wrapper.find(`[data-testid="activity-card-${ACTIVITY_FUTSAL_ID}"]`).exists()).toBe(
      true
    );
    expect(wrapper.find(`[data-testid="activity-card-${ACTIVITY_PRAMUKA_ID}"]`).exists()).toBe(
      false
    );
  });

  it('opens detail drawer showing enrolled roster and recent session activity logs', async () => {
    setupDefaultMsw();
    const wrapper = mountWorkbench({ role: 'SCHOOL_ADMIN' });

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_PRAMUKA_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_PRAMUKA_ID}"]`).trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="participants-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Ahmad Dahlan');
    });

    expect(wrapper.find('[data-testid="recent-session-log-item"]').text()).toContain(
      'Practiced pioneering knots and teamwork.'
    );
  });

  it('enforces supervisor-only access for recording session logs (UC-TCH-09)', async () => {
    let recordedPayload: Record<string, unknown> | null = null;
    setupDefaultMsw({
      onRecordLog: (body) => {
        recordedPayload = body;
      }
    });

    // Budi is supervisor of Pramuka, NOT Futsal
    const wrapper = mountWorkbench({
      role: 'TEACHER',
      teacherId: TEACHER_BUDI_ID
    });

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_FUTSAL_ID}"]`).exists()
      ).toBe(true);
    });

    // Inspect non-supervised club (Futsal) -> non-supervisor banner shown
    await wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_FUTSAL_ID}"]`).trigger('click');
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="non-supervisor-banner"]').exists()).toBe(true);
    });
    expect(wrapper.find(`[data-testid="log-session-button-${PARTICIPATION_1_ID}"]`).exists()).toBe(
      false
    );

    // Inspect supervised club (Pramuka) -> supervisor access banner & Log Session button visible
    await wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_PRAMUKA_ID}"]`).trigger('click');
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="supervisor-access-banner"]').exists()).toBe(true);
      expect(
        wrapper.find(`[data-testid="log-session-button-${PARTICIPATION_1_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper.find(`[data-testid="log-session-button-${PARTICIPATION_1_ID}"]`).trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="session-log-content"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="session-log-date"]').setValue('2026-11-20');
    await wrapper
      .find('[data-testid="session-log-content"]')
      .setValue('Led compass navigation drill.');

    await wrapper.find('[data-testid="session-log-modal"] form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(recordedPayload).toEqual({
        sessionDate: '2026-11-20',
        log: 'Led compass navigation drill.'
      });
    });
  });

  it('allows admin to enroll a new student via EnrollmentModal (UC-ADM-25)', async () => {
    let enrolledPayload: Record<string, unknown> | null = null;
    setupDefaultMsw({
      onEnrollParticipant: (body) => {
        enrolledPayload = body;
      }
    });

    const wrapper = mountWorkbench({ role: 'SCHOOL_ADMIN' });

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_PRAMUKA_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper.find(`[data-testid="view-roster-button-${ACTIVITY_PRAMUKA_ID}"]`).trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="enroll-student-button"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="enroll-student-button"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find(`[data-testid="student-option-${STUDENT_RINA_ID}"]`).exists()).toBe(
        true
      );
    });

    // Ahmad is already actively enrolled -> disabled
    expect(
      wrapper.find(`[data-testid="student-option-${STUDENT_AHMAD_ID}"]`).attributes('disabled')
    ).toBeDefined();

    await wrapper.find(`[data-testid="student-option-${STUDENT_RINA_ID}"]`).trigger('click');
    await wrapper.find('[data-testid="enrollment-joined-date"]').setValue('2026-11-20');
    await wrapper.find('[data-testid="enrollment-modal"] form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(enrolledPayload).toMatchObject({
        studentId: STUDENT_RINA_ID,
        studentName: 'Rina Marlina',
        studentNis: '20260002',
        joinedDate: '2026-11-20'
      });
    });
  });

  it('allows admin to create a new extracurricular club via ActivityEditorModal', async () => {
    let createdPayload: Record<string, unknown> | null = null;
    setupDefaultMsw({
      onCreateActivity: (body) => {
        createdPayload = body;
      }
    });

    const wrapper = mountWorkbench({ role: 'SCHOOL_ADMIN' });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="new-club-button"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="new-club-button"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="club-name-input"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="club-name-input"]').setValue('Robotics Club');
    await wrapper
      .find('[data-testid="club-description-input"]')
      .setValue('STEM robotics engineering workshop');

    const editorModal = wrapper.findComponent({ name: 'ActivityEditorModal' });
    const selectComp = editorModal.findComponent({ name: 'Select' });
    selectComp.vm.$emit('update:modelValue', TEACHER_BUDI_ID);
    await flushPromises();

    await wrapper.find('[data-testid="activity-editor-modal"] form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(createdPayload).toEqual({
        name: 'Robotics Club',
        description: 'STEM robotics engineering workshop',
        supervisorId: TEACHER_BUDI_ID
      });
    });
  });
});
