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
  }, 30_000);

  it('redirects /login to /auth/login and unknown routes to 404', async () => {
    const router = createAppRouter(createMemoryHistory());

    await router.push('/login');
    expect(router.currentRoute.value.path).toBe('/auth/login');

    await router.push('/non-existent-route');
    expect(router.currentRoute.value.name).toBe('not-found');
  }, 15_000);

  it('redirects /teaching/assignments to /teachers/assignments for backward compatibility', async () => {
    const authStore = useAuthStore();
    authStore.setSession({
      accessToken: 'token-123',
      user: {
        id: 'u-teacher',
        username: 'teacher',
        displayName: 'Teacher User',
        roles: ['TEACHER'],
        permissions: ['Permissions.Teaching.Assignments.View']
      }
    });

    const router = createAppRouter(createMemoryHistory());
    await router.push('/teaching/assignments');
    await router.isReady();

    expect(router.currentRoute.value.name).toBe('teachers.assignments');
    expect(router.currentRoute.value.path).toBe('/teachers/assignments');
  }, 15_000);

  it('redirects unauthenticated requests across all 5 Stage 1 routes to /auth/login', async () => {
    const stage1Paths = [
      '/operations/calendar',
      '/operations/announcements',
      '/admissions/queue',
      '/students/directory',
      '/teachers/assignments'
    ];

    for (const path of stage1Paths) {
      const router = createAppRouter(createMemoryHistory());
      await router.push(path);
      await router.isReady();

      expect(router.currentRoute.value.name).toBe('auth.login');
      expect(router.currentRoute.value.query.redirect).toBe(path);
    }
  }, 15_000);

  it('enforces Stage 1 route permission matrix for SCHOOL_ADMIN, PRINCIPAL, TEACHER, and HOMEROOM_TEACHER', async () => {
    const cases: Array<{
      role: string;
      permissions: string[];
      allowed: Array<{ path: string; name: string }>;
      forbidden: string[];
    }> = [
      {
        role: 'SCHOOL_ADMIN',
        permissions: [
          'Permissions.Operations.Calendar.Manage',
          'Permissions.Operations.Announcements.Publish',
          'Permissions.Students.Admissions.Enroll',
          'Permissions.Students.Directory.Manage',
          'Permissions.Teaching.Directory.Manage'
        ],
        allowed: [
          { path: '/operations/calendar', name: 'operations.calendar' },
          { path: '/operations/announcements', name: 'operations.announcements' },
          { path: '/admissions/queue', name: 'admissions.queue' },
          { path: '/students/directory', name: 'students.directory' },
          { path: '/teachers/assignments', name: 'teachers.assignments' }
        ],
        forbidden: []
      },
      {
        role: 'PRINCIPAL',
        permissions: [
          'Permissions.Operations.Calendar.View',
          'Permissions.Operations.Announcements.View',
          'Permissions.Students.Admissions.Approve',
          'Permissions.Students.Directory.View',
          'Permissions.Teaching.Assignments.Oversight'
        ],
        allowed: [
          { path: '/operations/calendar', name: 'operations.calendar' },
          { path: '/operations/announcements', name: 'operations.announcements' },
          { path: '/admissions/queue', name: 'admissions.queue' },
          { path: '/students/directory', name: 'students.directory' },
          { path: '/teachers/assignments', name: 'teachers.assignments' }
        ],
        forbidden: []
      },
      {
        role: 'TEACHER',
        permissions: [
          'Permissions.Operations.Calendar.View',
          'Permissions.Operations.Announcements.View',
          'Permissions.Teaching.Assignments.View'
        ],
        allowed: [
          { path: '/operations/calendar', name: 'operations.calendar' },
          { path: '/operations/announcements', name: 'operations.announcements' },
          { path: '/teachers/assignments', name: 'teachers.assignments' }
        ],
        forbidden: ['/admissions/queue', '/students/directory']
      },
      {
        role: 'HOMEROOM_TEACHER',
        permissions: [
          'Permissions.Operations.Calendar.View',
          'Permissions.Operations.Announcements.View',
          'Permissions.Students.Roster.ViewOwn'
        ],
        allowed: [
          { path: '/operations/calendar', name: 'operations.calendar' },
          { path: '/operations/announcements', name: 'operations.announcements' },
          { path: '/students/directory', name: 'students.directory' }
        ],
        forbidden: ['/admissions/queue', '/teachers/assignments']
      }
    ];

    for (const testCase of cases) {
      const authStore = useAuthStore();
      authStore.setSession({
        accessToken: 'token-123',
        user: {
          id: `u-${testCase.role}`,
          username: testCase.role.toLowerCase(),
          displayName: testCase.role,
          roles: [testCase.role],
          permissions: testCase.permissions
        }
      });

      const router = createAppRouter(createMemoryHistory());

      for (const routeTarget of testCase.allowed) {
        await router.push(routeTarget.path);
        await router.isReady();
        expect(router.currentRoute.value.name).toBe(routeTarget.name);
      }

      for (const blockedPath of testCase.forbidden) {
        await router.push(blockedPath);
        await router.isReady();
        expect(router.currentRoute.value.name).toBe('forbidden');
      }
    }
  }, 30_000);

  it('allows access when holding any alternative Stage 1 route permission', async () => {
    const permissionCases: Array<{ path: string; name: string; permissions: string[] }> = [
      {
        path: '/operations/announcements',
        name: 'operations.announcements',
        permissions: ['Permissions.Operations.Announcements.Create']
      },
      {
        path: '/admissions/queue',
        name: 'admissions.queue',
        permissions: [
          'Permissions.Students.Admissions.View',
          'Permissions.Students.Admissions.Register',
          'Permissions.Students.Admissions.Decide'
        ]
      },
      {
        path: '/teachers/assignments',
        name: 'teachers.assignments',
        permissions: ['Permissions.Teaching.Assignments.Manage']
      }
    ];

    for (const entry of permissionCases) {
      for (const permission of entry.permissions) {
        const authStore = useAuthStore();
        authStore.setSession({
          accessToken: 'token-123',
          user: {
            id: 'u-alt',
            username: 'alt-user',
            displayName: 'Alt User',
            roles: ['SCHOOL_ADMIN'],
            permissions: [permission]
          }
        });

        const router = createAppRouter(createMemoryHistory());
        await router.push(entry.path);
        await router.isReady();
        expect(router.currentRoute.value.name).toBe(entry.name);
      }
    }
  }, 30_000);

  it('redirects unauthenticated requests across all 4 Stage 2 routes to /auth/login', async () => {
    const stage2Paths = [
      '/operations/timetables',
      '/academic/curriculum',
      '/academic/attendance',
      '/academic/scorebook'
    ];

    for (const path of stage2Paths) {
      const router = createAppRouter(createMemoryHistory());
      await router.push(path);
      await router.isReady();

      expect(router.currentRoute.value.name).toBe('auth.login');
      expect(router.currentRoute.value.query.redirect).toBe(path);
    }
  }, 15_000);

  it('enforces Stage 2 route permissions allowing authorized and blocking unauthorized access', async () => {
    const stage2Cases: Array<{
      path: string;
      name: string;
      validPermissions: string[];
      unrelatedPermission: string;
    }> = [
      {
        path: '/operations/timetables',
        name: 'operations.timetables',
        validPermissions: [
          'Permissions.Operations.Timetables.View',
          'Permissions.Operations.Timetables.Manage',
          'Permissions.Operations.Timetables.ViewOwn'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/academic/curriculum',
        name: 'academic.curriculum',
        validPermissions: [
          'Permissions.Academic.Curriculum.View',
          'Permissions.Academic.Curriculum.Manage'
        ],
        unrelatedPermission: 'Permissions.Students.Directory.View'
      },
      {
        path: '/academic/attendance',
        name: 'academic.attendance',
        validPermissions: [
          'Permissions.Academic.Attendance.View',
          'Permissions.Academic.Attendance.Record'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/academic/scorebook',
        name: 'academic.scorebook',
        validPermissions: [
          'Permissions.Academic.Scorebook.View',
          'Permissions.Academic.Scorebook.Manage'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      }
    ];

    for (const testCase of stage2Cases) {
      // 1. Verify access succeeds for each valid permission
      for (const permission of testCase.validPermissions) {
        const authStore = useAuthStore();
        authStore.setSession({
          accessToken: 'token-stage2',
          user: {
            id: 'u-stage2',
            username: 'stage2.user',
            displayName: 'Stage 2 User',
            roles: ['TEACHER'],
            permissions: [permission]
          }
        });

        const router = createAppRouter(createMemoryHistory());
        await router.push(testCase.path);
        await router.isReady();
        expect(router.currentRoute.value.name).toBe(testCase.name);
      }

      // 2. Verify access blocked when lacking valid permissions
      const authStore = useAuthStore();
      authStore.setSession({
        accessToken: 'token-unauthorized',
        user: {
          id: 'u-unauth',
          username: 'unauth.user',
          displayName: 'Unauthorized User',
          roles: ['TEACHER'],
          permissions: [testCase.unrelatedPermission]
        }
      });

      const blockedRouter = createAppRouter(createMemoryHistory());
      await blockedRouter.push(testCase.path);
      await blockedRouter.isReady();
      expect(blockedRouter.currentRoute.value.name).toBe('forbidden');
    }
  }, 30_000);

  it('redirects unauthenticated requests across all 7 Stage 3 routes to /auth/login', async () => {
    const stage3Paths = [
      '/academic/report-cards',
      '/development/pastoral',
      '/development/extracurricular',
      '/finance/fee-policy',
      '/finance/billing',
      '/finance/payments',
      '/finance/receivables'
    ];

    for (const path of stage3Paths) {
      const router = createAppRouter(createMemoryHistory());
      await router.push(path);
      await router.isReady();

      expect(router.currentRoute.value.name).toBe('auth.login');
      expect(router.currentRoute.value.query.redirect).toBe(path);
    }
  }, 15_000);

  it('enforces Stage 3 route permissions allowing authorized and blocking unauthorized access', async () => {
    const stage3Cases: Array<{
      path: string;
      name: string;
      validPermissions: string[];
      unrelatedPermission: string;
    }> = [
      {
        path: '/academic/report-cards',
        name: 'academic.report-cards',
        validPermissions: [
          'Permissions.Academic.ReportCards.View',
          'Permissions.Academic.ReportCards.Generate',
          'Permissions.Academic.ReportCards.ProvideNotes',
          'Permissions.Academic.ReportCards.Remarks',
          'Permissions.Academic.ReportCards.Finalize'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/development/pastoral',
        name: 'development.pastoral',
        validPermissions: [
          'Permissions.Development.Behaviour.Record',
          'Permissions.Development.Discipline.Record',
          'Permissions.Development.Discipline.Escalate',
          'Permissions.Development.Achievements.Record',
          'Permissions.Development.Dossier.View'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/development/extracurricular',
        name: 'development.extracurricular',
        validPermissions: [
          'Permissions.Development.Extracurricular.Manage',
          'Permissions.Development.Extracurricular.Log'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/finance/fee-policy',
        name: 'finance.fee-policy',
        validPermissions: [
          'Permissions.Finance.FeePolicy.Manage',
          'Permissions.Finance.Discounts.Manage'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/finance/billing',
        name: 'finance.billing',
        validPermissions: [
          'Permissions.Finance.Billing.Generate',
          'Permissions.Finance.Billing.ManageAdHoc',
          'Permissions.Finance.Billing.Cancel'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/finance/payments',
        name: 'finance.payments',
        validPermissions: [
          'Permissions.Finance.Payments.Record',
          'Permissions.Finance.Deposits.Allocate',
          'Permissions.Finance.Statements.View'
        ],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      },
      {
        path: '/finance/receivables',
        name: 'finance.receivables',
        validPermissions: ['Permissions.Finance.Reports.View'],
        unrelatedPermission: 'Permissions.Operations.Calendar.View'
      }
    ];

    for (const testCase of stage3Cases) {
      // 1. Verify access succeeds for each valid permission
      for (const permission of testCase.validPermissions) {
        const authStore = useAuthStore();
        authStore.setSession({
          accessToken: 'token-stage3',
          user: {
            id: 'u-stage3',
            username: 'stage3.user',
            displayName: 'Stage 3 User',
            roles: ['SCHOOL_ADMIN'],
            permissions: [permission]
          }
        });

        const router = createAppRouter(createMemoryHistory());
        await router.push(testCase.path);
        await router.isReady();
        expect(router.currentRoute.value.name).toBe(testCase.name);
      }

      // 2. Verify access blocked when lacking valid permissions
      const authStore = useAuthStore();
      authStore.setSession({
        accessToken: 'token-unauthorized',
        user: {
          id: 'u-unauth',
          username: 'unauth.user',
          displayName: 'Unauthorized User',
          roles: ['TEACHER'],
          permissions: [testCase.unrelatedPermission]
        }
      });

      const blockedRouter = createAppRouter(createMemoryHistory());
      await blockedRouter.push(testCase.path);
      await blockedRouter.isReady();
      expect(blockedRouter.currentRoute.value.name).toBe('forbidden');
    }
  }, 45_000);
});
