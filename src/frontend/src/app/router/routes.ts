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
    path: '/operations',
    component: () => import('@/app/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
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
