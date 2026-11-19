import { computed, shallowRef, type ComputedRef } from 'vue';

export interface AppContextSnapshot {
  activeAcademicYearId: string | null;
  activePeriodId: string | null;
  selectedAcademicYearId: string | null;
  selectedPeriodId: string | null;
  activeAcademicYearName: string | null;
  activePeriodName: string | null;
  isHistoricalPeriod: boolean;
  currentContextLabel: string;
}

export type AppContextReader = () => Partial<AppContextSnapshot>;

export interface AppContextBridge {
  activeAcademicYearId: ComputedRef<string | null>;
  activePeriodId: ComputedRef<string | null>;
  selectedAcademicYearId: ComputedRef<string | null>;
  selectedPeriodId: ComputedRef<string | null>;
  activeAcademicYearName: ComputedRef<string | null>;
  activePeriodName: ComputedRef<string | null>;
  isHistoricalPeriod: ComputedRef<boolean>;
  currentContextLabel: ComputedRef<string>;
}

const contextReaderRef = shallowRef<AppContextReader | null>(null);

/**
 * Registers the reactive read-only app context reader wired during app bootstrap
 * so feature slices never import app Pinia stores directly.
 */
export function configureAppContextReader(reader: AppContextReader | null): void {
  contextReaderRef.value = reader;
}

/**
 * Returns reactive read-only academic year and operational period context values.
 */
export function useAppContextBridge(): AppContextBridge {
  const snapshot = computed<Partial<AppContextSnapshot>>(() => contextReaderRef.value?.() ?? {});

  const activeAcademicYearId = computed<string | null>(
    () => snapshot.value.activeAcademicYearId ?? null
  );
  const activePeriodId = computed<string | null>(() => snapshot.value.activePeriodId ?? null);
  const selectedAcademicYearId = computed<string | null>(() =>
    snapshot.value.selectedAcademicYearId !== undefined
      ? snapshot.value.selectedAcademicYearId
      : activeAcademicYearId.value
  );
  const selectedPeriodId = computed<string | null>(() =>
    snapshot.value.selectedPeriodId !== undefined
      ? snapshot.value.selectedPeriodId
      : activePeriodId.value
  );
  const activeAcademicYearName = computed<string | null>(
    () => snapshot.value.activeAcademicYearName ?? null
  );
  const activePeriodName = computed<string | null>(() => snapshot.value.activePeriodName ?? null);
  const isHistoricalPeriod = computed<boolean>(() => snapshot.value.isHistoricalPeriod ?? false);
  const currentContextLabel = computed<string>(
    () => snapshot.value.currentContextLabel ?? 'No Active Academic Period'
  );

  return {
    activeAcademicYearId,
    activePeriodId,
    selectedAcademicYearId,
    selectedPeriodId,
    activeAcademicYearName,
    activePeriodName,
    isHistoricalPeriod,
    currentContextLabel
  };
}
