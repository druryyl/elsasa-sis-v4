import { computed, ref } from 'vue';
import { defineStore } from 'pinia';

export interface ContextLabels {
  academicYearName?: string | null;
  periodName?: string | null;
}

export const useAppContextStore = defineStore('appContext', () => {
  const activeAcademicYearId = ref<string | null>(null);
  const activePeriodId = ref<string | null>(null);
  const selectedAcademicYearId = ref<string | null>(null);
  const selectedPeriodId = ref<string | null>(null);

  const activeAcademicYearName = ref<string | null>(null);
  const activePeriodName = ref<string | null>(null);
  const selectedAcademicYearName = ref<string | null>(null);
  const selectedPeriodName = ref<string | null>(null);

  const isHistoricalPeriod = computed<boolean>(() => {
    if (!selectedPeriodId.value || !activePeriodId.value) {
      return false;
    }
    return (
      selectedPeriodId.value !== activePeriodId.value ||
      (activeAcademicYearId.value !== null &&
        selectedAcademicYearId.value !== null &&
        selectedAcademicYearId.value !== activeAcademicYearId.value)
    );
  });

  const currentContextLabel = computed<string>(() => {
    const yearLabel = selectedAcademicYearName.value ?? activeAcademicYearName.value;
    const periodLabel = selectedPeriodName.value ?? activePeriodName.value;
    if (yearLabel && periodLabel) {
      return `${yearLabel} • ${periodLabel}`;
    }
    if (yearLabel) return yearLabel;
    if (periodLabel) return periodLabel;
    return 'No Active Academic Period';
  });

  function setActiveContext(
    academicYearId: string | null,
    periodId: string | null,
    labels?: ContextLabels
  ): void {
    activeAcademicYearId.value = academicYearId;
    activePeriodId.value = periodId;
    activeAcademicYearName.value = labels?.academicYearName ?? null;
    activePeriodName.value = labels?.periodName ?? null;

    if (!selectedAcademicYearId.value && !selectedPeriodId.value) {
      selectedAcademicYearId.value = academicYearId;
      selectedPeriodId.value = periodId;
      selectedAcademicYearName.value = labels?.academicYearName ?? null;
      selectedPeriodName.value = labels?.periodName ?? null;
    }
  }

  function selectContext(
    academicYearId: string | null,
    periodId: string | null,
    labels?: ContextLabels
  ): void {
    selectedAcademicYearId.value = academicYearId;
    selectedPeriodId.value = periodId;
    selectedAcademicYearName.value = labels?.academicYearName ?? null;
    selectedPeriodName.value = labels?.periodName ?? null;
  }

  function resetToActiveContext(): void {
    selectedAcademicYearId.value = activeAcademicYearId.value;
    selectedPeriodId.value = activePeriodId.value;
    selectedAcademicYearName.value = activeAcademicYearName.value;
    selectedPeriodName.value = activePeriodName.value;
  }

  function clearContext(): void {
    activeAcademicYearId.value = null;
    activePeriodId.value = null;
    selectedAcademicYearId.value = null;
    selectedPeriodId.value = null;
    activeAcademicYearName.value = null;
    activePeriodName.value = null;
    selectedAcademicYearName.value = null;
    selectedPeriodName.value = null;
  }

  return {
    activeAcademicYearId,
    activePeriodId,
    selectedAcademicYearId,
    selectedPeriodId,
    activeAcademicYearName,
    activePeriodName,
    selectedAcademicYearName,
    selectedPeriodName,
    isHistoricalPeriod,
    currentContextLabel,
    setActiveContext,
    selectContext,
    resetToActiveContext,
    clearContext
  };
});
