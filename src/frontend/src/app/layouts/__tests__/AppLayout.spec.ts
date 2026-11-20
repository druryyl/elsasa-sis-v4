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
import { useAuthStore, type AuthUser } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import AppLayout from '../AppLayout.vue';

const server = setupServer();

const DummyView = defineComponent({
  template: '<div>Workspace</div>'
});

const ROLE_USERS: Record<string, AuthUser> = {
  SCHOOL_ADMIN: {
    id: '01900000-0000-7000-8000-000000000001',
    username: 'admin',
    displayName: 'School Administrator',
    email: 'admin@elsasa.sch.id',
    roles: ['SCHOOL_ADMIN'],
    permissions: [
      'Permissions.Operations.Calendar.View',
      'Permissions.Operations.Calendar.Manage',
      'Permissions.Operations.Rooms.View',
      'Permissions.Operations.Rooms.Manage',
      'Permissions.Operations.Announcements.View',
      'Permissions.Operations.Announcements.Create',
      'Permissions.Operations.Announcements.Publish',
      'Permissions.Students.Admissions.View',
      'Permissions.Students.Admissions.Register',
      'Permissions.Students.Admissions.Decide',
      'Permissions.Students.Admissions.Enroll',
      'Permissions.Students.Directory.View',
      'Permissions.Students.Directory.Manage',
      'Permissions.Students.Placements.Manage',
      'Permissions.Teaching.Directory.Manage',
      'Permissions.Teaching.Capabilities.Manage',
      'Permissions.Teaching.Assignments.View',
      'Permissions.Teaching.Assignments.Manage',
      'Permissions.Teaching.Assignments.Oversight'
    ],
    teacherId: null,
    studentId: null,
    guardianId: null
  },
  PRINCIPAL: {
    id: '01900000-0000-7000-8000-000000000002',
    username: 'principal',
    displayName: 'School Principal',
    email: 'principal@elsasa.sch.id',
    roles: ['PRINCIPAL'],
    permissions: [
      'Permissions.Operations.Calendar.View',
      'Permissions.Operations.Rooms.View',
      'Permissions.Operations.Announcements.View',
      'Permissions.Students.Admissions.View',
      'Permissions.Students.Admissions.Approve',
      'Permissions.Students.Directory.View',
      'Permissions.Teaching.Assignments.View',
      'Permissions.Teaching.Assignments.Oversight'
    ],
    teacherId: null,
    studentId: null,
    guardianId: null
  },
  TEACHER: {
    id: '01900000-0000-7000-8000-000000000003',
    username: 'teacher.andi',
    displayName: 'Andi Wijaya',
    email: 'andi@elsasa.sch.id',
    roles: ['TEACHER'],
    permissions: [
      'Permissions.Operations.Calendar.View',
      'Permissions.Operations.Announcements.View',
      'Permissions.Teaching.Assignments.View'
    ],
    teacherId: '01900000-0000-7000-8000-000000000031',
    studentId: null,
    guardianId: null
  },
  HOMEROOM_TEACHER: {
    id: '01900000-0000-7000-8000-000000000004',
    username: 'homeroom.siti',
    displayName: 'Siti Aminah',
    email: 'siti@elsasa.sch.id',
    roles: ['HOMEROOM_TEACHER'],
    permissions: [
      'Permissions.Operations.Calendar.View',
      'Permissions.Operations.Announcements.View',
      'Permissions.Students.Roster.ViewOwn'
    ],
    teacherId: '01900000-0000-7000-8000-000000000041',
    studentId: null,
    guardianId: null
  },
  TEACHER_AND_HOMEROOM: {
    id: '01900000-0000-7000-8000-000000000005',
    username: 'teacher.budi',
    displayName: 'Budi Santoso',
    email: 'budi@elsasa.sch.id',
    roles: ['TEACHER', 'HOMEROOM_TEACHER'],
    permissions: [
      'Permissions.Operations.Calendar.View',
      'Permissions.Operations.Announcements.View',
      'Permissions.Teaching.Assignments.View',
      'Permissions.Students.Roster.ViewOwn'
    ],
    teacherId: '01900000-0000-7000-8000-000000000051',
    studentId: null,
    guardianId: null
  }
};

async function mountAppLayout(user: AuthUser = ROLE_USERS.SCHOOL_ADMIN!) {
  const pinia = createPinia();
  setActivePinia(pinia);

  const authStore = useAuthStore(pinia);
  authStore.setSession({
    accessToken: 'jwt-token',
    user
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
        path: '/operations/announcements',
        name: 'operations.announcements',
        component: DummyView
      },
      {
        path: '/admissions/queue',
        name: 'admissions.queue',
        component: DummyView
      },
      {
        path: '/students/directory',
        name: 'students.directory',
        component: DummyView
      },
      {
        path: '/teachers/assignments',
        name: 'teachers.assignments',
        component: DummyView
      },
      {
        path: '/operations/timetables',
        name: 'operations.timetables',
        component: DummyView
      },
      {
        path: '/academic/curriculum',
        name: 'academic.curriculum',
        component: DummyView
      },
      {
        path: '/academic/attendance',
        name: 'academic.attendance',
        component: DummyView
      },
      {
        path: '/academic/scorebook',
        name: 'academic.scorebook',
        component: DummyView
      },
      {
        path: '/academic/monitoring',
        name: 'academic.monitoring',
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

function mockAcademicYearsEndpoint() {
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

  return { yearActiveId, periodActiveId, periodInactiveId };
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

  it('populates Top Bar switcher for read-only Operations.Calendar.View users and skips query when lacking calendar permissions', async () => {
    const { yearActiveId, periodActiveId } = mockAcademicYearsEndpoint();

    const { wrapper: teacherWrapper, appContextStore: teacherContext } = await mountAppLayout(
      ROLE_USERS.TEACHER
    );

    await vi.waitFor(() => {
      expect(teacherContext.activeAcademicYearId).toBe(yearActiveId);
      expect(teacherContext.activePeriodId).toBe(periodActiveId);
      expect(teacherWrapper.find('[data-testid="context-switcher-select"]').exists()).toBe(true);
    });

    server.resetHandlers();
    const { wrapper: noCalendarWrapper, appContextStore: noCalendarContext } = await mountAppLayout(
      {
        ...ROLE_USERS.TEACHER!,
        permissions: ['Permissions.Teaching.Assignments.View']
      }
    );

    expect(noCalendarContext.activeAcademicYearId).toBeNull();
    expect(noCalendarWrapper.find('[data-testid="context-switcher-select"]').exists()).toBe(false);
  });
});

describe('AppLayout.vue Role-Filtered Navigation Taxonomy', () => {
  function getVisibleNavLabels(wrapper: ReturnType<typeof mount>): string[] {
    return wrapper.findAll('aside nav a').map((link) => link.text().trim());
  }

  function getVisibleGroupHeaders(wrapper: ReturnType<typeof mount>): string[] {
    return wrapper.findAll('aside nav p').map((heading) => heading.text().trim());
  }

  it('renders all 4 Stage 1 groups and 5 menu items for SCHOOL_ADMIN', async () => {
    mockAcademicYearsEndpoint();
    const { wrapper } = await mountAppLayout(ROLE_USERS.SCHOOL_ADMIN);

    expect(getVisibleGroupHeaders(wrapper)).toEqual([
      'Admissions',
      'Students & Classes',
      'Faculty & Teaching',
      'Operations & Comms'
    ]);
    expect(getVisibleNavLabels(wrapper)).toEqual([
      'Admissions Queue',
      'Student Directory & Placements',
      'Teachers & Assignments',
      'School Calendar & Academic Cycle',
      'Announcements & Bulletin'
    ]);
  });

  it('renders all 4 Stage 1 groups and 5 menu items for PRINCIPAL', async () => {
    mockAcademicYearsEndpoint();
    const { wrapper } = await mountAppLayout(ROLE_USERS.PRINCIPAL);

    expect(getVisibleGroupHeaders(wrapper)).toEqual([
      'Admissions',
      'Students & Classes',
      'Faculty & Teaching',
      'Operations & Comms'
    ]);
    expect(getVisibleNavLabels(wrapper)).toEqual([
      'Admissions Queue',
      'Student Directory & Placements',
      'Teachers & Assignments',
      'School Calendar & Academic Cycle',
      'Announcements & Bulletin'
    ]);
  });

  it('filters sidebar to Faculty & Teaching and Operations & Comms for TEACHER', async () => {
    mockAcademicYearsEndpoint();
    const { wrapper } = await mountAppLayout(ROLE_USERS.TEACHER);

    expect(getVisibleGroupHeaders(wrapper)).toEqual(['Faculty & Teaching', 'Operations & Comms']);
    expect(getVisibleNavLabels(wrapper)).toEqual([
      'Teachers & Assignments',
      'School Calendar & Academic Cycle',
      'Announcements & Bulletin'
    ]);
    expect(wrapper.find('[data-testid="nav-item-admissions.queue"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="nav-item-students.directory"]').exists()).toBe(false);
  });

  it('filters sidebar to Students & Classes and Operations & Comms for HOMEROOM_TEACHER', async () => {
    mockAcademicYearsEndpoint();
    const { wrapper } = await mountAppLayout(ROLE_USERS.HOMEROOM_TEACHER);

    expect(getVisibleGroupHeaders(wrapper)).toEqual(['Students & Classes', 'Operations & Comms']);
    expect(getVisibleNavLabels(wrapper)).toEqual([
      'Student Directory & Placements',
      'School Calendar & Academic Cycle',
      'Announcements & Bulletin'
    ]);
    expect(wrapper.find('[data-testid="nav-item-admissions.queue"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="nav-item-teachers.assignments"]').exists()).toBe(false);
  });

  it('combines navigation items additively for composable TEACHER + HOMEROOM_TEACHER persona', async () => {
    mockAcademicYearsEndpoint();
    const { wrapper } = await mountAppLayout(ROLE_USERS.TEACHER_AND_HOMEROOM);

    expect(getVisibleGroupHeaders(wrapper)).toEqual([
      'Students & Classes',
      'Faculty & Teaching',
      'Operations & Comms'
    ]);
    expect(getVisibleNavLabels(wrapper)).toEqual([
      'Student Directory & Placements',
      'Teachers & Assignments',
      'School Calendar & Academic Cycle',
      'Announcements & Bulletin'
    ]);
    expect(wrapper.find('[data-testid="nav-item-admissions.queue"]').exists()).toBe(false);
  });

  it('renders Stage 2 navigation items when user holds Stage 2 permissions', async () => {
    mockAcademicYearsEndpoint();
    const stage2User: AuthUser = {
      ...ROLE_USERS.SCHOOL_ADMIN!,
      permissions: [
        ...ROLE_USERS.SCHOOL_ADMIN!.permissions,
        'Permissions.Operations.Timetables.View',
        'Permissions.Academic.Curriculum.View',
        'Permissions.Academic.Attendance.View',
        'Permissions.Academic.Scorebook.View'
      ]
    };
    const { wrapper } = await mountAppLayout(stage2User);

    expect(getVisibleGroupHeaders(wrapper)).toEqual([
      'Admissions',
      'Students & Classes',
      'Faculty & Teaching',
      'Academic & Curriculum',
      'Operations & Comms'
    ]);
    expect(getVisibleNavLabels(wrapper)).toEqual([
      'Admissions Queue',
      'Student Directory & Placements',
      'Teachers & Assignments',
      'Classroom Attendance',
      'Teacher Scorebook',
      'Curriculum & Learning Structure',
      'School Calendar & Academic Cycle',
      'Timetables & Scheduling',
      'Announcements & Bulletin'
    ]);
    expect(wrapper.find('[data-testid="nav-item-operations.timetables"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="nav-item-academic.curriculum"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="nav-item-academic.attendance"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="nav-item-academic.scorebook"]').exists()).toBe(true);
  });

  it('renders Academic Progress Monitoring nav item when user holds Permissions.Academic.Monitoring.View', async () => {
    mockAcademicYearsEndpoint();
    const stage4User: AuthUser = {
      ...ROLE_USERS.PRINCIPAL!,
      permissions: [
        ...ROLE_USERS.PRINCIPAL!.permissions,
        'Permissions.Academic.Monitoring.View'
      ]
    };
    const { wrapper } = await mountAppLayout(stage4User);

    expect(wrapper.find('[data-testid="nav-item-academic.monitoring"]').exists()).toBe(true);
    expect(getVisibleNavLabels(wrapper)).toContain('Academic Progress Monitoring');
  });
});
