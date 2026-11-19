import { createApp } from 'vue';
import { createPinia } from 'pinia';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import '@/shared/styles/main.css';
import App from './App.vue';
import { primeVueConfig } from '@/app/config/primevue';
import { router } from '@/app/router';
import { setupHttpProvider } from '@/app/providers/http.provider';
import { useAuthStore } from '@/app/stores/auth.store';

async function bootstrap(): Promise<void> {
  const app = createApp(App);
  const pinia = createPinia();
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: 1,
        staleTime: 30_000,
        refetchOnWindowFocus: false
      }
    }
  });

  app.use(pinia);
  app.use(VueQueryPlugin, { queryClient });
  app.use(PrimeVue, primeVueConfig);
  app.use(ToastService);

  setupHttpProvider(router, queryClient);

  const authStore = useAuthStore(pinia);
  try {
    await authStore.hydrateSession();
  } catch {
    // Proceed unauthenticated when no valid refresh cookie exists
  }

  app.use(router);
  await router.isReady();
  app.mount('#app');
}

void bootstrap();
