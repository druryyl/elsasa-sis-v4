import type { RouteRecordRaw } from 'vue-router';

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean;
    guestOnly?: boolean;
    permission?: string | string[];
    title?: string;
  }
}

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/operations/calendar'
  },
  {
    path: '/login',
    redirect: '/auth/login'
  },
  {
    path: '/teaching/assignments',
    redirect: '/teachers/assignments'
  },
  {
    path: '/auth',
    component: () => import('@/app/layouts/AuthLayout.vue'),
    meta: { requiresAuth: false, guestOnly: true },
    children: [
      {
        path: 'login',
        name: 'auth.login',
        component: () => import('@/features/auth/views/LoginView.vue'),
        meta: {
          requiresAuth: false,
          guestOnly: true,
          title: 'Sign In'
        }
      }
    ]
  },
  {
    path: '/',
    component: () => import('@/app/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'operations',
        children: [
          {
            path: 'calendar',
            name: 'operations.calendar',
            component: () =>
              import('@/features/operations/calendar/views/CalendarWorkspaceView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Operations.Calendar.View',
                'Permissions.Operations.Calendar.Manage'
              ],
              title: 'School Calendar & Academic Cycle'
            }
          },
          {
            path: 'announcements',
            name: 'operations.announcements',
            component: () =>
              import('@/features/operations/announcements/views/AnnouncementsView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Operations.Announcements.View',
                'Permissions.Operations.Announcements.Create',
                'Permissions.Operations.Announcements.Publish'
              ],
              title: 'School Announcements & Bulletin'
            }
          }
        ]
      },
      {
        path: 'admissions',
        children: [
          {
            path: 'queue',
            name: 'admissions.queue',
            component: () => import('@/features/student/admissions/views/AdmissionsQueueView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Students.Admissions.View',
                'Permissions.Students.Admissions.Register',
                'Permissions.Students.Admissions.Decide',
                'Permissions.Students.Admissions.Approve',
                'Permissions.Students.Admissions.Enroll'
              ],
              title: 'Admissions Processing Queue'
            }
          }
        ]
      },
      {
        path: 'students',
        children: [
          {
            path: 'directory',
            name: 'students.directory',
            component: () => import('@/features/student/directory/views/StudentDirectoryView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Students.Directory.View',
                'Permissions.Students.Directory.Manage',
                'Permissions.Students.Roster.ViewOwn'
              ],
              title: 'Student Directory & Placements'
            }
          }
        ]
      },
      {
        path: 'teachers',
        children: [
          {
            path: 'assignments',
            name: 'teachers.assignments',
            component: () =>
              import('@/features/teaching/assignments/views/TeacherAssignmentsView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Teaching.Assignments.View',
                'Permissions.Teaching.Assignments.Manage',
                'Permissions.Teaching.Assignments.Oversight',
                'Permissions.Teaching.Directory.Manage'
              ],
              title: 'Teachers & Assignments'
            }
          }
        ]
      }
    ]
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/shared/views/ForbiddenView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Access Forbidden'
    }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/shared/views/NotFoundView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Page Not Found'
    }
  }
];
