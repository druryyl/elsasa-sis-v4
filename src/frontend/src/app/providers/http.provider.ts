import type { QueryClient } from '@tanstack/vue-query';
import type { Router } from 'vue-router';
import { configureHttpClient } from '@/shared/services/http';
import { configureAuthSessionHook } from '@/shared/services/authSession';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';

export function setupHttpProvider(router: Router, queryClient: QueryClient): void {
  configureAuthSessionHook((payload) => {
    useAuthStore().setSession(payload);
  });

  configureHttpClient({
    getAccessToken: () => useAuthStore().accessToken,
    getContextHeaders: () => {
      const ctx = useAppContextStore();
      const headers: Record<string, string> = {};
      const periodId = ctx.selectedPeriodId ?? ctx.activePeriodId;
      if (periodId) {
        headers['X-Active-Period-ID'] = periodId;
      }
      return headers;
    },
    refreshAccessToken: () => useAuthStore().refreshSession(),
    onSessionExpired: () => {
      const authStore = useAuthStore();
      authStore.clearSession();
      queryClient.clear();

      if (router.currentRoute.value.name !== 'auth.login') {
        void router.push({
          name: 'auth.login',
          query: { redirect: router.currentRoute.value.fullPath }
        });
      }
    }
  });
}
