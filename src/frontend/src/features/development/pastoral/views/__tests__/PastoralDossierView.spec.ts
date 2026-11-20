import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import { configureHttpClient } from '@/shared/services/http';
import PastoralDossierView from '../PastoralDossierView.vue';

const server = setupServer();

const STUDENT_1_ID = '01900000-0000-7000-8000-000000000001';
const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000901';

function createSampleStudents() {
  return [
    {
      id: STUDENT_1_ID,
      nis: '20260001',
      fullName: 'Ahmad Dahlan',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupName: '7A',
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      status: 'ACTIVE'
    }
  ];
}

function createSampleOverview() {
  return {
    totalBehaviourNotes: 12,
    positiveBehaviourNotes: 9,
    negativeBehaviourNotes: 3,
    totalDisciplineIncidents: 4,
    verbalWarnings: 2,
    writtenWarnings: 1,
    parentMeetings: 1,
    suspensions: 0,
    totalAchievements: 7,
    activeExtracurricularActivities: 5,
    totalExtracurricularParticipants: 84
  };
}

function createSampleDossier() {
  return {
    studentId: STUDENT_1_ID,
    studentName: 'Ahmad Dahlan',
    studentNis: '20260001',
    behaviourNotes: [
      {
        id: '01900000-0000-7000-8000-000000001001',
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        behaviourType: 'POSITIVE',
        observedDate: '2026-10-12',
        description: 'Helped organize classroom library neatly.',
        recorderName: 'Budi Santoso',
        createdAt: '2026-10-12T08:00:00Z'
      },
      {
        id: '01900000-0000-7000-8000-000000001002',
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        behaviourType: 'NEGATIVE',
        observedDate: '2026-10-10',
        description: 'Forgot to bring lab notebook.',
        recorderName: 'Siti Aminah',
        createdAt: '2026-10-10T08:00:00Z'
      }
    ],
    disciplineRecords: [
      {
        id: '01900000-0000-7000-8000-000000002001',
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        incidentDate: '2026-10-08',
        violationType: 'Late Arrival',
        description: 'Arrived 20 minutes after morning assembly.',
        sanction: 'VERBAL_WARNING',
        recorderName: 'Budi Santoso',
        createdAt: '2026-10-08T07:30:00Z'
      }
    ],
    extracurriculars: [
      {
        id: '01900000-0000-7000-8000-000000003001',
        activityId: '01900000-0000-7000-8000-000000003101',
        activityName: 'Robotics Club',
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        status: 'ACTIVE',
        joinedDate: '2026-07-15',
        lastSessionLog: 'Completed line-follower sensor calibration.',
        createdAt: '2026-07-15T09:00:00Z'
      },
      {
        id: '01900000-0000-7000-8000-000000003002',
        activityId: '01900000-0000-7000-8000-000000003102',
        activityName: 'Debate Society',
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        status: 'WITHDRAWN',
        joinedDate: '2026-07-16',
        createdAt: '2026-07-16T09:00:00Z'
      }
    ],
    achievements: [
      {
        id: '01900000-0000-7000-8000-000000004001',
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        achievementType: 'SCIENCE',
        eventName: 'Olimpiade Sains Kota',
        recognitionLevel: 'CITY',
        achievementDate: '2026-10-15',
        description: 'Silver medal in Mathematics.',
        recorderName: 'Admin Sekolah',
        createdAt: '2026-10-15T10:00:00Z'
      }
    ],
    timeline: [
      {
        id: '01900000-0000-7000-8000-000000004001',
        category: 'ACHIEVEMENT',
        date: '2026-10-15',
        title: 'Olimpiade Sains Kota',
        description: 'Silver medal in Mathematics.',
        badge: 'SCIENCE - CITY',
        recordedBy: 'Admin Sekolah',
        createdAt: '2026-10-15T10:00:00Z'
      },
      {
        id: '01900000-0000-7000-8000-000000001001',
        category: 'BEHAVIOUR',
        date: '2026-10-12',
        title: 'Catatan Perilaku (POSITIVE)',
        description: 'Helped organize classroom library neatly.',
        badge: 'POSITIVE',
        recordedBy: 'Budi Santoso',
        createdAt: '2026-10-12T08:00:00Z'
      },
      {
        id: '01900000-0000-7000-8000-000000001002',
        category: 'BEHAVIOUR',
        date: '2026-10-10',
        title: 'Catatan Perilaku (NEGATIVE)',
        description: 'Forgot to bring lab notebook.',
        badge: 'NEGATIVE',
        recordedBy: 'Siti Aminah',
        createdAt: '2026-10-10T08:00:00Z'
      },
      {
        id: '01900000-0000-7000-8000-000000002001',
        category: 'DISCIPLINE',
        date: '2026-10-08',
        title: 'Pelanggaran Disiplin (Late Arrival)',
        description: 'Arrived 20 minutes after morning assembly.',
        badge: 'VERBAL_WARNING',
        recordedBy: 'Budi Santoso',
        createdAt: '2026-10-08T07:30:00Z'
      },
      {
        id: '01900000-0000-7000-8000-000000003001',
        category: 'EXTRACURRICULAR',
        date: '2026-07-15',
        title: 'Ekstrakurikuler: Robotics Club',
        description: 'Completed line-follower sensor calibration.',
        badge: 'ACTIVE',
        recordedBy: '',
        createdAt: '2026-07-15T09:00:00Z'
      }
    ]
  };
}

function configurePermissions(perms: string[], roles: string[] = ['TEACHER']): void {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000009999',
      username: 'tester',
      displayName: 'Test Educator',
      roles,
      permissions: perms
    }),
    getRoles: () => roles,
    getPermissions: () => perms,
    hasPermission: (perm: string) => perms.includes(perm),
    hasAnyRole: (required: string[]) => required.some((r) => roles.includes(r))
  });
}

async function mountPastoralView(initialQuery = '') {
  configureHttpClient({
    getAccessToken: () => 'test-token',
    getContextHeaders: () => ({}),
    refreshAccessToken: async () => null,
    onSessionExpired: () => {}
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    activeAcademicYearName: '2026/2027'
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/development/pastoral',
        name: 'development.pastoral',
        component: PastoralDossierView
      }
    ]
  });

  await router.push(`/development/pastoral${initialQuery}`);
  await router.isReady();

  const wrapper = mount(PastoralDossierView, {
    global: {
      plugins: [
        router,
        [VueQueryPlugin, { queryClient }],
        ToastService,
        [PrimeVue, { theme: { preset: Aura } }]
      ],
      stubs: {
        Teleport: true
      }
    }
  });

  await flushPromises();
  return wrapper;
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
  if (typeof window.matchMedia === 'undefined') {
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
  server.listen({ onUnhandledRequest: 'bypass' });
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

describe('PastoralDossierView.vue (SCR-DEV-PASTORAL)', () => {
  it('renders search bar, initial guidance, and school-wide overview when no student is selected', async () => {
    configurePermissions([
      'Permissions.Development.Behaviour.Record',
      'Permissions.Development.Dossier.View'
    ]);

    server.use(
      http.get('/api/dev/overview', () => HttpResponse.json(createSampleOverview()))
    );

    const wrapper = await mountPastoralView();
    await flushPromises();

    expect(wrapper.find('[data-testid="student-search-input"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Select a Student to View 360° Pastoral Dossier');
    const overview = wrapper.find('[data-testid="development-overview-panel"]');
    expect(overview.exists()).toBe(true);
    expect(overview.text()).toContain('12');
    expect(overview.text()).toContain('9 positive · 3 negative');
  });

  it('searches students via autocomplete, loads 360 dossier metric cards, and filters timeline pills', async () => {
    configurePermissions([
      'Permissions.Development.Behaviour.Record',
      'Permissions.Development.Discipline.Record',
      'Permissions.Development.Achievements.Record',
      'Permissions.Development.Dossier.View'
    ]);

    server.use(
      http.get('/api/dev/overview', () => HttpResponse.json(createSampleOverview())),
      http.get('/api/std/students', () =>
        HttpResponse.json({
          items: createSampleStudents(),
          page: 1,
          pageSize: 15,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get(`/api/dev/students/${STUDENT_1_ID}/dossier`, () =>
        HttpResponse.json(createSampleDossier())
      )
    );

    const wrapper = await mountPastoralView();

    const searchInput = wrapper.find('[data-testid="student-search-input"]');
    await searchInput.trigger('focus');
    await flushPromises();

    const options = wrapper.findAll('[data-testid="student-autocomplete-option"]');
    expect(options).toHaveLength(1);
    expect(options[0]!.text()).toContain('Ahmad Dahlan');

    await options[0]!.trigger('click');
    await flushPromises();

    // Verify student header & 5 metric cards
    expect(wrapper.find('[data-testid="dossier-student-name"]').text()).toBe('Ahmad Dahlan');
    expect(wrapper.find('[data-testid="metric-positive"]').text()).toContain('1');
    expect(wrapper.find('[data-testid="metric-negative"]').text()).toContain('1');
    expect(wrapper.find('[data-testid="metric-discipline"]').text()).toContain('1');
    expect(wrapper.find('[data-testid="metric-awards"]').text()).toContain('1');
    expect(wrapper.find('[data-testid="metric-clubs"]').text()).toContain('1');

    // Verify all 5 timeline items initially
    expect(wrapper.findAll('[data-testid="timeline-item"]')).toHaveLength(5);

    // Filter by Behaviour
    await wrapper.find('[data-testid="filter-pill-BEHAVIOUR"]').trigger('click');
    expect(wrapper.findAll('[data-testid="timeline-item"]')).toHaveLength(2);

    // Filter by Discipline
    await wrapper.find('[data-testid="filter-pill-DISCIPLINE"]').trigger('click');
    expect(wrapper.findAll('[data-testid="timeline-item"]')).toHaveLength(1);
    expect(wrapper.find('[data-testid="dossier-timeline"]').text()).toContain('Late Arrival');

    // Filter by Awards
    await wrapper.find('[data-testid="filter-pill-ACHIEVEMENT"]').trigger('click');
    expect(wrapper.findAll('[data-testid="timeline-item"]')).toHaveLength(1);
    expect(wrapper.find('[data-testid="dossier-timeline"]').text()).toContain('Olimpiade Sains Kota');

    // Filter by Clubs
    await wrapper.find('[data-testid="filter-pill-EXTRACURRICULAR"]').trigger('click');
    expect(wrapper.findAll('[data-testid="timeline-item"]')).toHaveLength(1);
    expect(wrapper.find('[data-testid="dossier-timeline"]').text()).toContain('Robotics Club');

    // Back to All
    await wrapper.find('[data-testid="filter-pill-ALL"]').trigger('click');
    expect(wrapper.findAll('[data-testid="timeline-item"]')).toHaveLength(5);

    // Teacher does not see Principal Escalate Sanction button
    expect(wrapper.find('[data-testid="btn-record-behaviour"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-record-discipline"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-record-award"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-escalate-sanction"]').exists()).toBe(false);
  });

  it('submits behaviour note, classroom discipline incident, and achievement award modals', async () => {
    configurePermissions([
      'Permissions.Development.Behaviour.Record',
      'Permissions.Development.Discipline.Record',
      'Permissions.Development.Achievements.Record',
      'Permissions.Development.Dossier.View'
    ]);

    let behaviourPayload: Record<string, unknown> | null = null;
    let disciplinePayload: Record<string, unknown> | null = null;
    let achievementPayload: Record<string, unknown> | null = null;

    server.use(
      http.get(`/api/dev/students/${STUDENT_1_ID}/dossier`, () =>
        HttpResponse.json(createSampleDossier())
      ),
      http.post('/api/dev/behaviour', async ({ request }) => {
        behaviourPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000009001',
            ...behaviourPayload
          },
          { status: 201 }
        );
      }),
      http.post('/api/dev/discipline', async ({ request }) => {
        disciplinePayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000009002',
            ...disciplinePayload
          },
          { status: 201 }
        );
      }),
      http.post('/api/dev/achievements', async ({ request }) => {
        achievementPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000009003',
            ...achievementPayload
          },
          { status: 201 }
        );
      })
    );

    const wrapper = await mountPastoralView(`?studentId=${STUDENT_1_ID}`);
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="dossier-student-name"]').text()).toBe('Ahmad Dahlan');
      expect(wrapper.find('[data-testid="dossier-timeline"]').exists()).toBe(true);
    });

    // 1. Behaviour Note Composer
    await wrapper.find('[data-testid="btn-record-behaviour"]').trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="behaviour-type-NEGATIVE"]').trigger('click');
    await wrapper
      .find('[data-testid="behaviour-description"]')
      .setValue('Distracting peers during group work.');
    await wrapper.find('[data-testid="behaviour-note-modal"] form').trigger('submit');

    await vi.waitFor(() => {
      expect(behaviourPayload).toMatchObject({
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        behaviourType: 'NEGATIVE',
        description: 'Distracting peers during group work.'
      });
    });

    // 2. Classroom Discipline Modal
    await wrapper.find('[data-testid="btn-record-discipline"]').trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="discipline-violation-type"]').setValue('Uniform Violation');
    await wrapper
      .find('[data-testid="discipline-description"]')
      .setValue('Missing school tie during flag ceremony.');
    await wrapper.find('[data-testid="discipline-incident-modal"] form').trigger('submit');

    await vi.waitFor(() => {
      expect(disciplinePayload).toMatchObject({
        studentId: STUDENT_1_ID,
        violationType: 'Uniform Violation',
        sanction: 'VERBAL_WARNING',
        description: 'Missing school tie during flag ceremony.'
      });
    });

    // 3. Achievement Award Modal
    await wrapper.find('[data-testid="btn-record-award"]').trigger('click');
    await flushPromises();

    await wrapper
      .find('[data-testid="achievement-event-name"]')
      .setValue('National Robotics Challenge');
    await wrapper
      .find('[data-testid="achievement-description"]')
      .setValue('First runner up in autonomous navigation.');
    await wrapper.find('[data-testid="achievement-award-modal"] form').trigger('submit');

    await vi.waitFor(() => {
      expect(achievementPayload).toMatchObject({
        studentId: STUDENT_1_ID,
        eventName: 'National Robotics Challenge',
        achievementType: 'SCIENCE',
        recognitionLevel: 'SCHOOL',
        description: 'First runner up in autonomous navigation.'
      });
    });
  });

  it('requires Tier 2 confirmation dialog before submitting Principal escalated sanction', async () => {
    configurePermissions(
      [
        'Permissions.Development.Discipline.Escalate',
        'Permissions.Development.Dossier.View'
      ],
      ['PRINCIPAL']
    );

    let escalatedPayload: Record<string, unknown> | null = null;

    server.use(
      http.get(`/api/dev/students/${STUDENT_1_ID}/dossier`, () =>
        HttpResponse.json(createSampleDossier())
      ),
      http.post('/api/dev/discipline/escalated', async ({ request }) => {
        escalatedPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000009004',
            ...escalatedPayload
          },
          { status: 201 }
        );
      })
    );

    const wrapper = await mountPastoralView(`?studentId=${STUDENT_1_ID}`);
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="dossier-student-name"]').text()).toBe('Ahmad Dahlan');
      expect(wrapper.find('[data-testid="dossier-timeline"]').exists()).toBe(true);
    });

    const escalateBtn = wrapper.find('[data-testid="btn-escalate-sanction"]');
    expect(escalateBtn.exists()).toBe(true);
    await escalateBtn.trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="escalated-violation-type"]').setValue('Repeated Truancy');
    await wrapper
      .find('[data-testid="escalated-description"]')
      .setValue('Missed 5 consecutive instructional days without excuse.');
    await wrapper.find('[data-testid="escalated-sanction-modal"] form').trigger('submit');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="escalated-confirm-dialog"]').exists()).toBe(true);
    });

    // Must NOT have posted yet until Tier 2 confirmation is accepted
    expect(escalatedPayload).toBeNull();
    const confirmDialog = wrapper.find('[data-testid="escalated-confirm-dialog"]');
    expect(confirmDialog.text()).toContain('Confirm Escalated Sanction');
    expect(confirmDialog.text()).toContain('Repeated Truancy');

    // Click Confirm button inside Tier 2 dialog
    const confirmButtons = confirmDialog.findAll('button');
    const confirmActionBtn = confirmButtons.find((btn) =>
      btn.text().includes('Confirm & Issue Sanction')
    );
    expect(confirmActionBtn).toBeDefined();
    await confirmActionBtn!.trigger('click');

    await vi.waitFor(() => {
      expect(escalatedPayload).toMatchObject({
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '20260001',
        violationType: 'Repeated Truancy',
        sanction: 'PARENT_MEETING',
        description: 'Missed 5 consecutive instructional days without excuse.'
      });
    });
  });
});
