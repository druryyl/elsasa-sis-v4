<script setup lang="ts">
import { computed, ref } from 'vue';
import {
  AlertCircle,
  ChevronDown,
  ChevronUp,
  Clock,
  FileWarning,
  CheckCircle2
} from 'lucide-vue-next';
import type { AcademicMonitoringExceptionsDto } from '../api/academicMonitoring.api';

const props = withDefaults(
  defineProps<{
    exceptions?: AcademicMonitoringExceptionsDto | null;
  }>(),
  {
    exceptions: null
  }
);

const isMissingAttendanceOpen = ref(true);
const isOverdueGradingOpen = ref(true);
const isPendingReportCardsOpen = ref(true);

const missingAttendanceList = computed(
  () => props.exceptions?.missingAttendanceStudyGroups ?? []
);
const overdueGradingList = computed(
  () => props.exceptions?.overdueGradingAssignments ?? []
);
const pendingReportCardsList = computed(
  () => props.exceptions?.pendingReportCards ?? []
);

const hasAnyAlerts = computed(
  () =>
    missingAttendanceList.value.length > 0 ||
    overdueGradingList.value.length > 0 ||
    pendingReportCardsList.value.length > 0
);
</script>

<template>
  <div data-testid="exception-alert-banners" class="space-y-3">
    <!-- All Clear State -->
    <div
      v-if="!hasAnyAlerts"
      data-testid="no-exception-alerts"
      class="flex items-center gap-3 rounded-xl border border-emerald-200 bg-emerald-50/60 p-4 text-emerald-800 dark:border-emerald-900/50 dark:bg-emerald-950/20 dark:text-emerald-300"
    >
      <CheckCircle2 class="h-5 w-5 shrink-0 text-emerald-600 dark:text-emerald-400" />
      <div class="text-sm">
        <span class="font-semibold">Semua Operasional Berjalan Lancar:</span>
        Tidak ditemukan kendala presensi tertunda, keterlambatan penilaian, atau rapor tertahan.
      </div>
    </div>

    <!-- 1. Missing Attendance Alert Banner -->
    <div
      v-if="missingAttendanceList.length > 0"
      data-testid="alert-missing-attendance"
      class="overflow-hidden rounded-xl border border-rose-200 bg-rose-50/70 dark:border-rose-900/50 dark:bg-rose-950/20"
    >
      <button
        type="button"
        class="flex w-full items-center justify-between px-4 py-3 text-left transition-colors hover:bg-rose-100/50 dark:hover:bg-rose-900/30"
        @click="isMissingAttendanceOpen = !isMissingAttendanceOpen"
      >
        <div class="flex items-center gap-2.5">
          <AlertCircle class="h-5 w-5 text-rose-600 dark:text-rose-400" />
          <span class="text-sm font-semibold text-rose-900 dark:text-rose-200">
            Presensi Belum Tercatat ({{ missingAttendanceList.length }} Rombongan Belajar)
          </span>
          <span class="rounded-full bg-rose-200/80 px-2 py-0.5 text-[11px] font-bold text-rose-800 dark:bg-rose-900/60 dark:text-rose-300">
            Perlu Tindakan
          </span>
        </div>
        <ChevronUp v-if="isMissingAttendanceOpen" class="h-4 w-4 text-rose-600 dark:text-rose-400" />
        <ChevronDown v-else class="h-4 w-4 text-rose-600 dark:text-rose-400" />
      </button>

      <div v-show="isMissingAttendanceOpen" class="border-t border-rose-200/60 p-4 dark:border-rose-900/40">
        <p class="text-xs text-rose-700 dark:text-rose-300 mb-3">
          Rombel di bawah ini belum mencatat presensi harian pada tanggal yang dipilih:
        </p>
        <div class="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="group in missingAttendanceList"
            :key="group.studyGroupId"
            class="flex items-center justify-between rounded-lg border border-rose-200 bg-white px-3 py-2 text-xs shadow-2xs dark:border-rose-900/60 dark:bg-slate-900"
          >
            <div>
              <span class="font-bold text-slate-900 dark:text-white">
                {{ group.studyGroupName }}
              </span>
              <span class="ml-1 text-[11px] text-slate-500">
                ({{ group.schoolLevel }} Kls {{ group.grade }})
              </span>
              <div class="text-[11px] text-slate-600 dark:text-slate-400">
                Wali: {{ group.homeroomTeacherName || 'Belum Ditugaskan' }}
              </div>
            </div>
            <span class="rounded bg-rose-100 px-1.5 py-0.5 text-[10px] font-bold text-rose-700 dark:bg-rose-950 dark:text-rose-400">
              0 Tercatat
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- 2. Overdue Grading Alert Banner -->
    <div
      v-if="overdueGradingList.length > 0"
      data-testid="alert-overdue-grading"
      class="overflow-hidden rounded-xl border border-amber-200 bg-amber-50/70 dark:border-amber-900/50 dark:bg-amber-950/20"
    >
      <button
        type="button"
        class="flex w-full items-center justify-between px-4 py-3 text-left transition-colors hover:bg-amber-100/50 dark:hover:bg-amber-900/30"
        @click="isOverdueGradingOpen = !isOverdueGradingOpen"
      >
        <div class="flex items-center gap-2.5">
          <Clock class="h-5 w-5 text-amber-600 dark:text-amber-400" />
          <span class="text-sm font-semibold text-amber-900 dark:text-amber-200">
            Entri Nilai Terlambat > 7 Hari ({{ overdueGradingList.length }} Penugasan)
          </span>
          <span class="rounded-full bg-amber-200/80 px-2 py-0.5 text-[11px] font-bold text-amber-800 dark:bg-amber-900/60 dark:text-amber-300">
            Overdue
          </span>
        </div>
        <ChevronUp v-if="isOverdueGradingOpen" class="h-4 w-4 text-amber-600 dark:text-amber-400" />
        <ChevronDown v-else class="h-4 w-4 text-amber-600 dark:text-amber-400" />
      </button>

      <div v-show="isOverdueGradingOpen" class="border-t border-amber-200/60 p-4 dark:border-amber-900/40">
        <p class="text-xs text-amber-700 dark:text-amber-300 mb-3">
          Tugas dan ujian yang telah melewati batas 7 hari namun skor siswa belum tuntas diinput:
        </p>
        <div class="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="item in overdueGradingList"
            :key="item.assignmentId"
            class="rounded-lg border border-amber-200 bg-white p-2.5 text-xs shadow-2xs dark:border-amber-900/60 dark:bg-slate-900"
          >
            <div class="flex items-start justify-between gap-1">
              <span class="font-bold text-slate-900 dark:text-white line-clamp-1">
                {{ item.title }}
              </span>
              <span class="shrink-0 rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-bold text-amber-800 dark:bg-amber-950 dark:text-amber-300">
                +{{ item.daysOverdue }} hari
              </span>
            </div>
            <div class="mt-1 flex items-center justify-between text-[11px] text-slate-600 dark:text-slate-400">
              <span>{{ item.subjectName }} &bull; {{ item.studyGroupName }}</span>
              <span class="font-medium text-rose-600 dark:text-rose-400">
                {{ item.missingScoresCount }} skor tertunda
              </span>
            </div>
            <div class="mt-0.5 text-[11px] text-slate-500">
              Guru: {{ item.teacherName || '-' }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 3. Pending Report Cards Alert Banner -->
    <div
      v-if="pendingReportCardsList.length > 0"
      data-testid="alert-pending-report-cards"
      class="overflow-hidden rounded-xl border border-sky-200 bg-sky-50/70 dark:border-sky-900/50 dark:bg-sky-950/20"
    >
      <button
        type="button"
        class="flex w-full items-center justify-between px-4 py-3 text-left transition-colors hover:bg-sky-100/50 dark:hover:bg-sky-900/30"
        @click="isPendingReportCardsOpen = !isPendingReportCardsOpen"
      >
        <div class="flex items-center gap-2.5">
          <FileWarning class="h-5 w-5 text-sky-600 dark:text-sky-400" />
          <span class="text-sm font-semibold text-sky-900 dark:text-sky-200">
            Rapor Belum Selesai Disahkan ({{ pendingReportCardsList.length }} Rombongan Belajar)
          </span>
          <span class="rounded-full bg-sky-200/80 px-2 py-0.5 text-[11px] font-bold text-sky-800 dark:bg-sky-900/60 dark:text-sky-300">
            Pending
          </span>
        </div>
        <ChevronUp v-if="isPendingReportCardsOpen" class="h-4 w-4 text-sky-600 dark:text-sky-400" />
        <ChevronDown v-else class="h-4 w-4 text-sky-600 dark:text-sky-400" />
      </button>

      <div v-show="isPendingReportCardsOpen" class="border-t border-sky-200/60 p-4 dark:border-sky-900/40">
        <p class="text-xs text-sky-700 dark:text-sky-300 mb-3">
          Rombel dengan draf rapor yang belum difinalisasi oleh wali kelas menjelang akhir periode:
        </p>
        <div class="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="group in pendingReportCardsList"
            :key="group.studyGroupId"
            class="flex items-center justify-between rounded-lg border border-sky-200 bg-white px-3 py-2 text-xs shadow-2xs dark:border-sky-900/60 dark:bg-slate-900"
          >
            <div>
              <span class="font-bold text-slate-900 dark:text-white">
                {{ group.studyGroupName }}
              </span>
              <span class="ml-1 text-[11px] text-slate-500">
                ({{ group.schoolLevel }} Kls {{ group.grade }})
              </span>
              <div class="text-[11px] text-slate-600 dark:text-slate-400">
                Wali: {{ group.homeroomTeacherName || 'Belum Ditugaskan' }}
              </div>
            </div>
            <div class="text-right">
              <span class="rounded bg-sky-100 px-1.5 py-0.5 text-[10px] font-bold text-sky-800 dark:bg-sky-950 dark:text-sky-300">
                {{ group.draftCount }} / {{ group.totalCount }} Draft
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
