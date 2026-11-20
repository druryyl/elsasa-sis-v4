<script setup lang="ts">
import { computed } from 'vue';
import { UserCheck, Award, GraduationCap } from 'lucide-vue-next';
import type { AcademicMonitoringOverviewDto } from '../api/academicMonitoring.api';

const props = withDefaults(
  defineProps<{
    overview?: AcademicMonitoringOverviewDto | null;
    loading?: boolean;
  }>(),
  {
    overview: null,
    loading: false
  }
);

const attendancePercent = computed(() => {
  return (props.overview?.attendanceRateTodayPercent ?? 0).toFixed(1);
});

const gradingPercent = computed(() => {
  return (props.overview?.gradingCompletenessPercent ?? 0).toFixed(1);
});

const reportCardsPercent = computed(() => {
  return (props.overview?.reportCardsFinalizedPercent ?? 0).toFixed(1);
});
</script>

<template>
  <div data-testid="kpi-metrics-grid" class="grid grid-cols-1 gap-5 md:grid-cols-3">
    <!-- Tile 1: School Attendance Today -->
    <div
      data-testid="kpi-attendance"
      class="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 shadow-xs transition-shadow hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
    >
      <div>
        <div class="flex items-center justify-between">
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            Presensi Hari Ini
          </span>
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-400">
            <UserCheck class="h-4 w-4" />
          </div>
        </div>

        <div class="mt-3 flex items-baseline gap-2">
          <span class="text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            {{ attendancePercent }}%
          </span>
          <span class="text-xs text-slate-500 dark:text-slate-400">
            dari {{ overview?.totalEnrolledStudentsCount ?? 0 }} siswa
          </span>
        </div>

        <!-- Attendance Progress Bar -->
        <div class="mt-3 h-2 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
          <div
            class="h-full rounded-full bg-emerald-500 transition-all duration-500"
            :style="{ width: `${Math.min(overview?.attendanceRateTodayPercent ?? 0, 100)}%` }"
          />
        </div>
      </div>

      <!-- Attendance Breakdown Chips -->
      <div class="mt-4 grid grid-cols-5 gap-1.5 border-t border-slate-100 pt-3 text-center dark:border-slate-800">
        <div class="rounded bg-emerald-50/70 p-1 dark:bg-emerald-950/30">
          <div class="text-[10px] font-medium text-emerald-700 dark:text-emerald-400">Hadir</div>
          <div class="text-xs font-bold text-emerald-800 dark:text-emerald-300">
            {{ overview?.presentCount ?? 0 }}
          </div>
        </div>
        <div class="rounded bg-amber-50/70 p-1 dark:bg-amber-950/30">
          <div class="text-[10px] font-medium text-amber-700 dark:text-amber-400">Sakit</div>
          <div class="text-xs font-bold text-amber-800 dark:text-amber-300">
            {{ overview?.sickCount ?? 0 }}
          </div>
        </div>
        <div class="rounded bg-sky-50/70 p-1 dark:bg-sky-950/30">
          <div class="text-[10px] font-medium text-sky-700 dark:text-sky-400">Izin</div>
          <div class="text-xs font-bold text-sky-800 dark:text-sky-300">
            {{ overview?.excusedCount ?? 0 }}
          </div>
        </div>
        <div class="rounded bg-orange-50/70 p-1 dark:bg-orange-950/30">
          <div class="text-[10px] font-medium text-orange-700 dark:text-orange-400">Late</div>
          <div class="text-xs font-bold text-orange-800 dark:text-orange-300">
            {{ overview?.lateCount ?? 0 }}
          </div>
        </div>
        <div class="rounded bg-rose-50/70 p-1 dark:bg-rose-950/30">
          <div class="text-[10px] font-medium text-rose-700 dark:text-rose-400">Alpa</div>
          <div class="text-xs font-bold text-rose-800 dark:text-rose-300">
            {{ overview?.absentCount ?? 0 }}
          </div>
        </div>
      </div>
    </div>

    <!-- Tile 2: Assessment Grading Completeness -->
    <div
      data-testid="kpi-grading"
      class="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 shadow-xs transition-shadow hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
    >
      <div>
        <div class="flex items-center justify-between">
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            Ketuntasan Penilaian
          </span>
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600 dark:bg-indigo-950/40 dark:text-indigo-400">
            <Award class="h-4 w-4" />
          </div>
        </div>

        <div class="mt-3 flex items-baseline gap-2">
          <span class="text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            {{ gradingPercent }}%
          </span>
          <span class="text-xs text-slate-500 dark:text-slate-400">
            dari {{ overview?.totalAssignmentsCount ?? 0 }} penugasan
          </span>
        </div>

        <!-- Grading Progress Bar -->
        <div class="mt-3 h-2 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
          <div
            class="h-full rounded-full bg-indigo-500 transition-all duration-500"
            :style="{ width: `${Math.min(overview?.gradingCompletenessPercent ?? 0, 100)}%` }"
          />
        </div>
      </div>

      <!-- Grading Breakdown -->
      <div class="mt-4 flex items-center justify-between border-t border-slate-100 pt-3 text-xs dark:border-slate-800">
        <div class="text-slate-500 dark:text-slate-400">
          Nilai Terekam:
          <span class="font-bold text-slate-800 dark:text-slate-200">
            {{ overview?.actualScoresCount ?? 0 }}
          </span>
        </div>
        <div class="text-slate-500 dark:text-slate-400">
          Target Skor:
          <span class="font-bold text-slate-800 dark:text-slate-200">
            {{ overview?.expectedScoresCount ?? 0 }}
          </span>
        </div>
      </div>
    </div>

    <!-- Tile 3: Report Cards Finalized -->
    <div
      data-testid="kpi-report-cards"
      class="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 shadow-xs transition-shadow hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
    >
      <div>
        <div class="flex items-center justify-between">
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            Penyelesaian Rapor
          </span>
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-sky-50 text-sky-600 dark:bg-sky-950/40 dark:text-sky-400">
            <GraduationCap class="h-4 w-4" />
          </div>
        </div>

        <div class="mt-3 flex items-baseline gap-2">
          <span class="text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white">
            {{ reportCardsPercent }}%
          </span>
          <span class="text-xs text-slate-500 dark:text-slate-400">
            dari {{ overview?.totalPlacedStudentsCount ?? 0 }} siswa
          </span>
        </div>

        <!-- Report Card Progress Bar -->
        <div class="mt-3 h-2 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
          <div
            class="h-full rounded-full bg-sky-500 transition-all duration-500"
            :style="{ width: `${Math.min(overview?.reportCardsFinalizedPercent ?? 0, 100)}%` }"
          />
        </div>
      </div>

      <!-- Report Card Breakdown -->
      <div class="mt-4 flex items-center justify-between border-t border-slate-100 pt-3 text-xs dark:border-slate-800">
        <div class="text-slate-500 dark:text-slate-400">
          Final:
          <span class="font-bold text-emerald-600 dark:text-emerald-400">
            {{ overview?.finalizedReportCardsCount ?? 0 }}
          </span>
        </div>
        <div class="text-slate-500 dark:text-slate-400">
          Draft:
          <span class="font-bold text-amber-600 dark:text-amber-400">
            {{ overview?.draftReportCardsCount ?? 0 }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
