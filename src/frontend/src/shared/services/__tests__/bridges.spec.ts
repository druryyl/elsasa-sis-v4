import { afterEach, describe, expect, it } from 'vitest';
import { ref } from 'vue';
import { configureAuthSessionReader, useCurrentSession, type UserSession } from '../authSession';
import {
  configureAppContextReader,
  useAppContextBridge,
  type AppContextSnapshot
} from '../appContextBridge';

afterEach(() => {
  configureAuthSessionReader(null);
  configureAppContextReader(null);
});

describe('authSession reactive read-only bridge', () => {
  it('returns empty defaults when no session reader is configured', () => {
    const session = useCurrentSession();

    expect(session.user.value).toBeNull();
    expect(session.roles.value).toEqual([]);
    expect(session.permissions.value).toEqual([]);
    expect(session.teacherId.value).toBeNull();
    expect(session.isAuthenticated.value).toBe(false);
    expect(session.hasPermission('Permissions.Operations.Calendar.View')).toBe(false);
    expect(session.hasAnyRole(['SCHOOL_ADMIN'])).toBe(false);
  });

  it('reactively updates when underlying session state changes', () => {
    const backingUser = ref<UserSession | null>({
      id: 'u-1',
      username: 'teacher.budi',
      displayName: 'Budi Santoso',
      roles: ['TEACHER', 'HOMEROOM_TEACHER'],
      permissions: ['Permissions.Students.Roster.ViewOwn', 'Permissions.Teaching.Assignments.View'],
      teacherId: 'teacher-budi-id'
    });

    configureAuthSessionReader({
      getUser: () => backingUser.value
    });

    const session = useCurrentSession();

    expect(session.isAuthenticated.value).toBe(true);
    expect(session.teacherId.value).toBe('teacher-budi-id');
    expect(session.roles.value).toEqual(['TEACHER', 'HOMEROOM_TEACHER']);
    expect(session.hasPermission('Permissions.Students.Roster.ViewOwn')).toBe(true);
    expect(session.hasPermission('Permissions.Students.Admissions.Enroll')).toBe(false);
    expect(session.hasAnyRole(['PRINCIPAL', 'HOMEROOM_TEACHER'])).toBe(true);
    expect(session.hasAnyRole([])).toBe(false);

    backingUser.value = null;

    expect(session.isAuthenticated.value).toBe(false);
    expect(session.teacherId.value).toBeNull();
    expect(session.roles.value).toEqual([]);
    expect(session.permissions.value).toEqual([]);
    expect(session.hasPermission('Permissions.Students.Roster.ViewOwn')).toBe(false);
  });
});

describe('appContextBridge reactive read-only bridge', () => {
  it('returns null/default context values when no reader is configured', () => {
    const ctx = useAppContextBridge();

    expect(ctx.activeAcademicYearId.value).toBeNull();
    expect(ctx.activePeriodId.value).toBeNull();
    expect(ctx.selectedAcademicYearId.value).toBeNull();
    expect(ctx.selectedPeriodId.value).toBeNull();
    expect(ctx.activeAcademicYearName.value).toBeNull();
    expect(ctx.activePeriodName.value).toBeNull();
    expect(ctx.isHistoricalPeriod.value).toBe(false);
    expect(ctx.currentContextLabel.value).toBe('No Active Academic Period');
  });

  it('reactively mirrors academic year and period context updates', () => {
    const snapshot = ref<AppContextSnapshot>({
      activeAcademicYearId: 'year-2026',
      activePeriodId: 'period-2026-s1',
      selectedAcademicYearId: 'year-2026',
      selectedPeriodId: 'period-2026-s1',
      activeAcademicYearName: '2026/2027',
      activePeriodName: 'Semester 1',
      isHistoricalPeriod: false,
      currentContextLabel: '2026/2027 • Semester 1'
    });

    configureAppContextReader(() => snapshot.value);

    const ctx = useAppContextBridge();
    expect(ctx.activeAcademicYearId.value).toBe('year-2026');
    expect(ctx.selectedPeriodId.value).toBe('period-2026-s1');
    expect(ctx.isHistoricalPeriod.value).toBe(false);
    expect(ctx.currentContextLabel.value).toBe('2026/2027 • Semester 1');

    snapshot.value = {
      ...snapshot.value,
      selectedAcademicYearId: 'year-2025',
      selectedPeriodId: 'period-2025-s2',
      isHistoricalPeriod: true,
      currentContextLabel: '2025/2026 • Semester 2'
    };

    expect(ctx.selectedAcademicYearId.value).toBe('year-2025');
    expect(ctx.selectedPeriodId.value).toBe('period-2025-s2');
    expect(ctx.isHistoricalPeriod.value).toBe(true);
    expect(ctx.currentContextLabel.value).toBe('2025/2026 • Semester 2');
  });
});
