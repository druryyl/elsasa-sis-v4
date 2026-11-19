import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAppContextStore } from '../appContext.store';

describe('useAppContextStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('defaults selected context when active context is first set and is not historical', () => {
    const store = useAppContextStore();

    store.setActiveContext('year-2026', 'period-2026-s1', {
      academicYearName: '2026/2027',
      periodName: 'Semester 1'
    });

    expect(store.activeAcademicYearId).toBe('year-2026');
    expect(store.activePeriodId).toBe('period-2026-s1');
    expect(store.selectedAcademicYearId).toBe('year-2026');
    expect(store.selectedPeriodId).toBe('period-2026-s1');
    expect(store.isHistoricalPeriod).toBe(false);
    expect(store.currentContextLabel).toBe('2026/2027 • Semester 1');
  });

  it('marks isHistoricalPeriod true when selecting a different period or academic year', () => {
    const store = useAppContextStore();

    store.setActiveContext('year-2026', 'period-2026-s1', {
      academicYearName: '2026/2027',
      periodName: 'Semester 1'
    });

    store.selectContext('year-2025', 'period-2025-s2', {
      academicYearName: '2025/2026',
      periodName: 'Semester 2'
    });

    expect(store.isHistoricalPeriod).toBe(true);
    expect(store.currentContextLabel).toBe('2025/2026 • Semester 2');

    store.resetToActiveContext();
    expect(store.isHistoricalPeriod).toBe(false);
    expect(store.selectedPeriodId).toBe('period-2026-s1');
  });

  it('clears all context state on clearContext', () => {
    const store = useAppContextStore();
    store.setActiveContext('year-2026', 'period-2026-s1');
    store.clearContext();

    expect(store.activeAcademicYearId).toBeNull();
    expect(store.activePeriodId).toBeNull();
    expect(store.selectedAcademicYearId).toBeNull();
    expect(store.selectedPeriodId).toBeNull();
    expect(store.isHistoricalPeriod).toBe(false);
  });
});
