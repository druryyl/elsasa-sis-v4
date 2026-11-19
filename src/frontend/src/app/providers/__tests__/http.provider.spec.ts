import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { QueryClient } from '@tanstack/vue-query';
import { createMemoryHistory, createRouter } from 'vue-router';
import { setupHttpProvider } from '../http.provider';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import {
  configureAuthSessionHook,
  configureAuthSessionReader,
  useCurrentSession
} from '@/shared/services/authSession';
import { configureAppContextReader, useAppContextBridge } from '@/shared/services/appContextBridge';
import { configureHttpClient } from '@/shared/services/http';

describe('http.provider.ts shared bridge wiring', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  afterEach(() => {
    configureAuthSessionHook(null);
    configureAuthSessionReader(null);
    configureAppContextReader(null);
    configureHttpClient(null);
  });

  it('wires useCurrentSession and useAppContextBridge to Pinia stores reactively', () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/auth/login', name: 'auth.login', component: { template: '<div />' } }]
    });
    const queryClient = new QueryClient();

    setupHttpProvider(router, queryClient);

    const authStore = useAuthStore();
    const appContextStore = useAppContextStore();
    const session = useCurrentSession();
    const context = useAppContextBridge();

    authStore.setSession({
      accessToken: 'jwt-token',
      user: {
        id: 'u-budi',
        username: 'teacher.budi',
        displayName: 'Budi Santoso',
        roles: ['TEACHER', 'HOMEROOM_TEACHER'],
        permissions: ['Permissions.Students.Roster.ViewOwn'],
        teacherId: 'teacher-7a'
      }
    });

    appContextStore.setActiveContext('year-2026', 'period-s1', {
      academicYearName: '2026/2027',
      periodName: 'Semester 1'
    });

    expect(session.user.value?.username).toBe('teacher.budi');
    expect(session.teacherId.value).toBe('teacher-7a');
    expect(session.hasPermission('Permissions.Students.Roster.ViewOwn')).toBe(true);
    expect(session.hasAnyRole(['HOMEROOM_TEACHER'])).toBe(true);

    expect(context.activeAcademicYearId.value).toBe('year-2026');
    expect(context.activePeriodId.value).toBe('period-s1');
    expect(context.activeAcademicYearName.value).toBe('2026/2027');
    expect(context.activePeriodName.value).toBe('Semester 1');
    expect(context.isHistoricalPeriod.value).toBe(false);
    expect(context.currentContextLabel.value).toBe('2026/2027 • Semester 1');

    appContextStore.selectContext('year-2025', 'period-old', {
      academicYearName: '2025/2026',
      periodName: 'Semester 2'
    });

    expect(context.selectedAcademicYearId.value).toBe('year-2025');
    expect(context.selectedPeriodId.value).toBe('period-old');
    expect(context.isHistoricalPeriod.value).toBe(true);
    expect(context.currentContextLabel.value).toBe('2025/2026 • Semester 2');
  });
});
