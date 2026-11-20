<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import { Activity, AlertOctagon, RotateCw } from 'lucide-vue-next';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import {
  useAcademicMonitoringProgressQuery,
  useAcademicMonitoringYearsQuery
} from '../api/useAcademicMonitoringQuery';
import MonitoringFilterBar, {
  type MonitoringFilterValues
} from '../components/MonitoringFilterBar.vue';
import KpiMetricsGrid from '../components/KpiMetricsGrid.vue';
import ExceptionAlertBanners from '../components/ExceptionAlertBanners.vue';
import StudyGroupProgressTable from '../components/StudyGroupProgressTable.vue';

const route = useRoute();
const router = useRouter();
const contextBridge = useAppContextBridge();

function getInitialDate(): string {
  const queryDate = route.query.date;
  if (typeof queryDate === 'string' && queryDate.length > 0) {
    return queryDate;
  }
  return new Date().toISOString().split('T')[0] ?? '';
}

// Initial filter values from query or context bridge
const filters = ref<MonitoringFilterValues>({
  academicYearId:
    (route.query.academicYearId as string) ||
    contextBridge.selectedAcademicYearId.value ||
    contextBridge.activeAcademicYearId.value ||
    undefined,
  operationalPeriodId:
    (route.query.operationalPeriodId as string) ||
    contextBridge.selectedPeriodId.value ||
    contextBridge.activePeriodId.value ||
    undefined,
  date: getInitialDate()
});

// Academic Years query for the filter dropdowns
const yearsQuery = useAcademicMonitoringYearsQuery();

// Auto-populate academic year & active period once years are loaded if not already selected
watch(
  () => yearsQuery.data.value,
  (years) => {
    if (!years || years.length === 0) return;

    if (!filters.value.academicYearId) {
      const activeYear = years.find((y) => y.status === 'ACTIVE') ?? years[0];
      const activePeriod =
        activeYear?.periods?.find((p) => p.status === 'ACTIVE') ?? activeYear?.periods?.[0];

      filters.value = {
        ...filters.value,
        academicYearId: activeYear?.id ?? undefined,
        operationalPeriodId: activePeriod?.id ?? undefined
      };
    }
  },
  { immediate: true }
);

// Sync filter changes with URL query parameters
watch(
  filters,
  (newFilters) => {
    const query: Record<string, string> = {};
    if (newFilters.academicYearId) query.academicYearId = newFilters.academicYearId;
    if (newFilters.operationalPeriodId) query.operationalPeriodId = newFilters.operationalPeriodId;
    if (newFilters.date) query.date = newFilters.date;

    router.replace({ query }).catch(() => {
      // Ignore redundant navigation errors
    });
  },
  { deep: true }
);

// Query academic monitoring progress data
const monitoringQuery = useAcademicMonitoringProgressQuery(
  computed(() => ({
    academicYearId: filters.value.academicYearId,
    operationalPeriodId: filters.value.operationalPeriodId,
    date: filters.value.date
  }))
);

const overviewData = computed(() => monitoringQuery.data.value?.overview);
const exceptionsData = computed(() => monitoringQuery.data.value?.exceptions);
const studyGroupsData = computed(() => monitoringQuery.data.value?.studyGroups ?? []);

function handleRefresh() {
  monitoringQuery.refetch();
}

function handleSelectStudyGroup(studyGroupId: string) {
  if (!studyGroupId) return;
  router.push({
    name: 'academic.report-cards',
    query: {
      studyGroupId,
      operationalPeriodId: filters.value.operationalPeriodId
    }
  }).catch(() => {
    // If route doesn't exist, safely ignore
  });
}
</script>

<template>
  <div data-testid="academic-monitoring-view" class="space-y-6 p-6">
    <!-- Header Section -->
    <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-4 dark:border-slate-800">
      <div>
        <div class="flex items-center gap-2">
          <Activity class="h-6 w-6 text-brand-accent-600 dark:text-brand-accent-400" />
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
            Monitoring Progres Akademik
          </h1>
        </div>
        <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Dashboard pengawasan institusional untuk tingkat kehadiran harian, keterisian nilai asesmen, dan finalisasi rapor.
        </p>
      </div>
    </div>

    <!-- Filter Bar -->
    <MonitoringFilterBar
      v-model="filters"
      :academic-years="yearsQuery.data.value ?? []"
      :loading="monitoringQuery.isLoading.value || monitoringQuery.isFetching.value"
      @refresh="handleRefresh"
    />

    <!-- Error State -->
    <div
      v-if="monitoringQuery.isError.value"
      data-testid="monitoring-error-state"
      class="rounded-xl border border-rose-200 bg-rose-50 p-6 text-center dark:border-rose-900/60 dark:bg-rose-950/20"
    >
      <AlertOctagon class="mx-auto h-10 w-10 text-rose-500" />
      <h3 class="mt-2 text-base font-bold text-rose-900 dark:text-rose-200">
        Gagal Memuat Data Monitoring
      </h3>
      <p class="mt-1 text-xs text-rose-700 dark:text-rose-300">
        Terjadi kendala saat mengambil rekap data akademik. Silakan coba kembali.
      </p>
      <div class="mt-4">
        <Button
          severity="danger"
          size="small"
          data-testid="btn-retry-monitoring"
          @click="handleRefresh"
        >
          <RotateCw class="h-3.5 w-3.5 mr-1.5" />
          Coba Lagi
        </Button>
      </div>
    </div>

    <!-- Main Content -->
    <template v-else>
      <!-- Executive KPI Metric Tiles -->
      <KpiMetricsGrid
        :overview="overviewData"
        :loading="monitoringQuery.isLoading.value"
      />

      <!-- Operational Exception Alerts -->
      <ExceptionAlertBanners
        :exceptions="exceptionsData"
      />

      <!-- Study Group Progress Matrix -->
      <StudyGroupProgressTable
        :study-groups="studyGroupsData"
        :loading="monitoringQuery.isLoading.value"
        @select-group="handleSelectStudyGroup"
      />
    </template>
  </div>
</template>
