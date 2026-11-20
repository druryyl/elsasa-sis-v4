<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import { Loader2, Printer, X } from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { ReportCardDetailDto } from '../api/reportCards.api';

const props = withDefaults(
  defineProps<{
    reportCard: ReportCardDetailDto | null;
    loading?: boolean;
  }>(),
  {
    loading: false
  }
);

const emit = defineEmits<{
  close: [];
}>();

const formattedFinalizedDate = computed(() =>
  props.reportCard?.finalizedAt ? formatDisplayDate(props.reportCard.finalizedAt) : '-'
);

const entries = computed(() => props.reportCard?.entries ?? []);

const attendance = computed(
  () =>
    props.reportCard?.attendance ?? {
      presentCount: 0,
      absentCount: 0,
      sickCount: 0,
      lateCount: 0,
      excusedCount: 0,
      totalRecordedDays: 0
    }
);

function formatScore(score: number | undefined): string {
  if (score === undefined || score === null) return '0.00';
  return score.toFixed(2);
}

function handlePrint(): void {
  window.print();
}
</script>

<template>
  <div
    class="report-card-print-overlay fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 p-4 sm:p-6 print:static print:inset-auto print:overflow-visible print:bg-white print:p-0"
    data-testid="report-card-print-view"
  >
    <!-- Non-printable Print Preview Toolbar -->
    <div
      class="no-print mx-auto mb-4 flex max-w-[210mm] items-center justify-between rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-md"
    >
      <div>
        <h2 class="text-sm font-semibold text-slate-900">Report Card Print Preview (A4 Portrait)</h2>
        <p class="text-xs text-slate-500">
          Review the official semester report card layout before printing.
        </p>
      </div>
      <div class="flex items-center gap-2">
        <Button
          type="button"
          label="Print Report Card"
          size="small"
          :disabled="loading || !reportCard"
          data-testid="print-trigger-btn"
          @click="handlePrint"
        >
          <template #icon>
            <Printer class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
        <Button
          type="button"
          label="Close"
          severity="secondary"
          outlined
          size="small"
          data-testid="print-close-btn"
          @click="emit('close')"
        >
          <template #icon>
            <X class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="loading"
      class="no-print mx-auto flex max-w-[210mm] items-center justify-center rounded-xl bg-white py-20 shadow-lg"
    >
      <Loader2 class="h-6 w-6 animate-spin text-brand-accent-600" />
      <span class="ml-2 text-sm text-slate-600">Loading printable report card...</span>
    </div>

    <!-- A4 Portrait Printable Sheet -->
    <article
      v-else-if="reportCard"
      class="report-card-print-sheet mx-auto max-w-[210mm] rounded-lg bg-white p-8 text-slate-900 shadow-lg print:max-w-none print:rounded-none print:p-0 print:shadow-none"
    >
      <!-- Official School Header -->
      <header class="border-b-2 border-slate-900 pb-4 text-center">
        <div class="flex items-center justify-between">
          <div class="text-left">
            <p class="text-xs font-bold uppercase tracking-widest text-slate-500">
              Elsasa School Information System
            </p>
            <h1 class="mt-0.5 text-xl font-bold uppercase tracking-wide text-slate-900">
              Official Semester Report Card
            </h1>
            <p class="text-xs text-slate-600">Laporan Hasil Belajar Siswa (Rapor Semester)</p>
          </div>
          <div class="text-right">
            <span
              class="inline-block rounded border px-2.5 py-1 text-xs font-bold uppercase tracking-wider"
              :class="
                reportCard.status === 'FINALIZED'
                  ? 'border-emerald-700 bg-emerald-50 text-emerald-800'
                  : 'border-amber-600 bg-amber-50 text-amber-800'
              "
            >
              {{ reportCard.status === 'FINALIZED' ? 'OFFICIAL / FINALIZED' : 'DRAFT PREVIEW' }}
            </span>
          </div>
        </div>
      </header>

      <!-- Student & Academic Cycle Metadata -->
      <section class="mt-4 grid grid-cols-2 gap-x-8 gap-y-2 border-b border-slate-300 pb-4 text-xs">
        <div class="space-y-1.5">
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Student Name</span>
            <span class="font-bold text-slate-900">: {{ reportCard.studentName || '-' }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">NIS / NISN</span>
            <span class="text-slate-900">
              : {{ reportCard.studentNis || '-' }} / {{ reportCard.studentNisn || '-' }}
            </span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Study Group</span>
            <span class="font-semibold text-slate-900">: {{ reportCard.studyGroupName || '-' }}</span>
          </div>
        </div>

        <div class="space-y-1.5">
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Academic Year</span>
            <span class="text-slate-900">: {{ reportCard.academicYearName || '-' }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Semester / Period</span>
            <span class="text-slate-900">: {{ reportCard.operationalPeriodName || '-' }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Homeroom Teacher</span>
            <span class="text-slate-900">: {{ reportCard.homeroomTeacherName || '-' }}</span>
          </div>
        </div>
      </section>

      <!-- Academic Achievement / Subject Grade Table -->
      <section class="mt-5">
        <h2 class="mb-2 text-xs font-bold uppercase tracking-wider text-slate-800">
          A. Academic Subject Grades & Teacher Feedback
        </h2>
        <table class="w-full border-collapse border border-slate-400 text-xs">
          <thead>
            <tr class="bg-slate-100 text-left">
              <th class="w-8 border border-slate-400 px-2 py-1.5 text-center font-semibold">No</th>
              <th class="border border-slate-400 px-2.5 py-1.5 font-semibold">Subject</th>
              <th class="w-20 border border-slate-400 px-2 py-1.5 text-center font-semibold">Score</th>
              <th class="w-16 border border-slate-400 px-2 py-1.5 text-center font-semibold">Grade</th>
              <th class="border border-slate-400 px-2.5 py-1.5 font-semibold">
                Subject Teacher Qualitative Feedback
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="entries.length === 0">
              <td
                colspan="5"
                class="border border-slate-400 px-3 py-4 text-center italic text-slate-500"
              >
                No subject grades recorded for this period.
              </td>
            </tr>
            <tr v-for="(entry, index) in entries" :key="entry.id ?? index" class="break-inside-avoid">
              <td class="border border-slate-400 px-2 py-1.5 text-center">{{ index + 1 }}</td>
              <td class="border border-slate-400 px-2.5 py-1.5">
                <div class="font-semibold text-slate-900">{{ entry.subjectName || '-' }}</div>
                <div class="text-[11px] text-slate-500">
                  {{ entry.subjectCode ? `${entry.subjectCode} • ` : ''
                  }}{{ entry.subjectStageName || '' }}
                </div>
              </td>
              <td class="border border-slate-400 px-2 py-1.5 text-center font-mono font-semibold">
                {{ formatScore(entry.numericScore) }}
              </td>
              <td class="border border-slate-400 px-2 py-1.5 text-center font-semibold">
                {{ entry.letterGrade || '-' }}
              </td>
              <td class="border border-slate-400 px-2.5 py-1.5 leading-relaxed">
                <p>{{ entry.teacherNotes || '-' }}</p>
                <p v-if="entry.teacherName" class="mt-0.5 text-[10px] text-slate-500">
                  — {{ entry.teacherName }}
                </p>
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="bg-slate-50 font-semibold">
              <td colspan="2" class="border border-slate-400 px-2.5 py-1.5 text-right">
                Overall Semester Average ({{ reportCard.totalSubjects ?? entries.length }} subjects)
              </td>
              <td class="border border-slate-400 px-2 py-1.5 text-center font-mono">
                {{ formatScore(reportCard.averageScore) }}
              </td>
              <td colspan="2" class="border border-slate-400 px-2.5 py-1.5 text-slate-600" />
            </tr>
          </tfoot>
        </table>
      </section>

      <!-- Attendance Summary -->
      <section class="mt-5 break-inside-avoid">
        <h2 class="mb-2 text-xs font-bold uppercase tracking-wider text-slate-800">
          B. Semester Attendance Summary
        </h2>
        <table class="w-full border-collapse border border-slate-400 text-xs">
          <thead>
            <tr class="bg-slate-100 text-center">
              <th class="border border-slate-400 px-2 py-1.5 font-semibold">Present (Hadir)</th>
              <th class="border border-slate-400 px-2 py-1.5 font-semibold">Sick (Sakit)</th>
              <th class="border border-slate-400 px-2 py-1.5 font-semibold">Excused (Izin)</th>
              <th class="border border-slate-400 px-2 py-1.5 font-semibold">Absent (Alpa)</th>
              <th class="border border-slate-400 px-2 py-1.5 font-semibold">Late (Terlambat)</th>
              <th class="border border-slate-400 px-2 py-1.5 font-semibold">Total Recorded</th>
            </tr>
          </thead>
          <tbody>
            <tr class="text-center font-semibold">
              <td class="border border-slate-400 px-2 py-1.5">
                {{ attendance.presentCount ?? 0 }} day(s)
              </td>
              <td class="border border-slate-400 px-2 py-1.5">
                {{ attendance.sickCount ?? 0 }} day(s)
              </td>
              <td class="border border-slate-400 px-2 py-1.5">
                {{ attendance.excusedCount ?? 0 }} day(s)
              </td>
              <td class="border border-slate-400 px-2 py-1.5">
                {{ attendance.absentCount ?? 0 }} day(s)
              </td>
              <td class="border border-slate-400 px-2 py-1.5">
                {{ attendance.lateCount ?? 0 }} day(s)
              </td>
              <td class="border border-slate-400 px-2 py-1.5">
                {{ attendance.totalRecordedDays ?? 0 }} day(s)
              </td>
            </tr>
          </tbody>
        </table>
      </section>

      <!-- Homeroom Teacher Holistic Remarks -->
      <section class="mt-5 break-inside-avoid">
        <h2 class="mb-2 text-xs font-bold uppercase tracking-wider text-slate-800">
          C. Homeroom Teacher Holistic Remarks
        </h2>
        <div
          class="min-h-[68px] rounded border border-slate-400 bg-slate-50/50 p-3 text-xs leading-relaxed text-slate-800"
        >
          {{ reportCard.generalRemarks || 'No general remarks recorded.' }}
        </div>
      </section>

      <!-- Signature Blocks -->
      <footer class="mt-8 break-inside-avoid">
        <p class="mb-6 text-right text-xs text-slate-600">
          Finalized Date: <span class="font-semibold text-slate-900">{{ formattedFinalizedDate }}</span>
        </p>
        <div class="grid grid-cols-3 gap-6 text-center text-xs">
          <div>
            <p class="font-semibold text-slate-700">Parent / Guardian</p>
            <div class="mt-14 border-t border-slate-500 pt-1 text-slate-600">
              ( ........................................ )
            </div>
          </div>
          <div>
            <p class="font-semibold text-slate-700">Homeroom Teacher</p>
            <div class="mt-14 border-t border-slate-500 pt-1 font-semibold text-slate-900">
              {{ reportCard.homeroomTeacherName || '........................................' }}
            </div>
          </div>
          <div>
            <p class="font-semibold text-slate-700">School Principal</p>
            <div class="mt-14 border-t border-slate-500 pt-1 text-slate-600">
              ( ........................................ )
            </div>
          </div>
        </div>
      </footer>
    </article>
  </div>
</template>

<style>
@media print {
  @page {
    size: A4 portrait;
    margin: 12mm;
  }

  body {
    background: #ffffff !important;
  }

  /* Hide app shell navigation, top header, breadcrumbs, and preview toolbar when printing */
  #app > div > header,
  #app > div > div > aside,
  nav[aria-label='Breadcrumb'],
  [data-testid='historical-period-banner'],
  .no-print {
    display: none !important;
  }

  main {
    padding: 0 !important;
    margin: 0 !important;
  }

  .report-card-print-overlay {
    position: static !important;
    background: transparent !important;
    padding: 0 !important;
    overflow: visible !important;
  }

  .report-card-print-sheet {
    box-shadow: none !important;
    padding: 0 !important;
    max-width: 100% !important;
  }
}
</style>
