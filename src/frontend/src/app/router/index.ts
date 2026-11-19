import {
  createRouter,
  createWebHistory,
  type NavigationGuardNext,
  type RouteLocationNormalized,
  type Router,
  type RouterHistory
} from 'vue-router';
import { routes } from './routes';
import { useAuthStore } from '@/app/stores/auth.store';

function hasRequiredPermission(
  required: string | string[],
  hasPermission: (permission: string) => boolean
): boolean {
  if (Array.isArray(required)) {
    return required.some((permission) => hasPermission(permission));
  }
  return hasPermission(required);
}

export function authNavigationGuard(
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext
): void {
  const authStore = useAuthStore();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({
      name: 'auth.login',
      query: { redirect: to.fullPath }
    });
    return;
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    next({ name: 'operations.calendar' });
    return;
  }

  if (
    to.meta.permission &&
    !hasRequiredPermission(to.meta.permission, authStore.hasPermission)
  ) {
    next({ name: 'forbidden' });
    return;
  }

  if (to.meta.title && typeof document !== 'undefined') {
    document.title = `${to.meta.title} | Elsasa SIS`;
  }

  next();
}

export function createAppRouter(history?: RouterHistory): Router {
  const instance = createRouter({
    history: history ?? createWebHistory(import.meta.env.BASE_URL),
    routes
  });

  instance.beforeEach(authNavigationGuard);

  return instance;
}

export const router = createAppRouter();
export default router;
