<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import {
  Award,
  BookOpen,
  Calendar,
  FileCheck,
  FileText,
  Printer,
  UserCheck
} from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import ReportCardPrintView from '@/shared/components/print/ReportCardPrintView.vue';
import {
  useChildReportCardsQuery,
  useReportCardDetailQuery
} from '../api/useParentPortalQueries';
import type { ReportCardDetailDto } from '../api/parentPortal.api';

const props = defineProps<{
  studentId: string;
}>();

const {
  data: reportCardsList,
  isLoading: isLoadingList,
  isError: isListError,
  refetch: refetchList
} = useChildReportCardsQuery(() => props.studentId);

const selectedReportCardId = ref<string | null>(null);
const showPrintModal = ref(false);

// Auto-select latest finalized report card when list updates
watch(
  () => reportCardsList.value,
  (list) => {
    if (list && list.length > 0) {
      if (!selectedReportCardId.value || !list.some((r) => r.id === selectedReportCardId.value)) {
        selectedReportCardId.value = list[0]?.id ?? null;
      }
    } else {
      selectedReportCardId.value = null;
    }
  },
  { immediate: true }
);

// Detail query for active report card
const {
  data: reportCardDetail,
  isLoading: isLoadingDetail,
  refetch: refetchDetail
} = useReportCardDetailQuery(selectedReportCardId, {
  enabled: computed(() => Boolean(selectedReportCardId.value))
});

const currentReportCard = computed<ReportCardDetailDto | null>(() => {
  if (reportCardDetail.value) {
    return reportCardDetail.value;
  }
  // Fallback to list item stub if detail not loaded yet
  const summary = reportCardsList.value?.find((r) => r.id === selectedReportCardId.value);
  if (summary) {
    return {
      id: summary.id,
      studentId: summary.studentId,
      studentName: summary.studentName,
      academicYearName: summary.academicYearName,
      operationalPeriodName: summary.operationalPeriodName,
      studyGroupName: summary.studyGroupName,
      status: summary.status,
      finalizedAt: summary.finalizedAt,
      entries: [],
      attendance: undefined
    };
  }
  return null;
});

const entries = computed(() => currentReportCard.value?.entries ?? []);
const attendance = computed(() => currentReportCard.value?.attendance ?? null);

const averageScore = computed(() => {
  if (typeof currentReportCard.value?.averageScore === 'number') {
    return Number(currentReportCard.value.averageScore.toFixed(1));
  }
  const validEntries = entries.value.filter(
    (e) => typeof e.numericScore === 'number' && !Number.isNaN(e.numericScore)
  );
  if (validEntries.length === 0) return 0;
  const sum = validEntries.reduce((acc, curr) => acc + (curr.numericScore ?? 0), 0);
  return Number((sum / validEntries.length).toFixed(1));
});

function handlePrint(): void {
  showPrintModal.value = true;
  window.print();
}

function selectReportCard(id: string | undefined): void {
  if (id) {
    selectedReportCardId.value = id;
  }
}

defineExpose({
  handlePrint,
  refetchList,
  refetchDetail
});
</script>

<template>
  <div
    class="space-y-6"
    data-testid="report-cards-tab"
  >
    <!-- Loading skeleton -->
    <div
      v-if="isLoadingList"
      class="space-y-4"
      data-testid="report-cards-skeleton"
    >
      <div class="h-10 w-64 animate-pulse rounded-lg bg-slate-200" />
      <div class="h-64 w-full animate-pulse rounded-xl bg-slate-200" />
    </div>

    <!-- Error state -->
    <div
      v-else-if="isListError"
      class="rounded-xl border border-red-200 bg-red-50 p-6 text-center text-red-700"
      data-testid="report-cards-error"
    >
      <p class="font-medium">Gagal memuat data rapor akademik.</p>
      <Button
        label="Coba Lagi"
        severity="danger"
        text
        size="small"
        class="mt-2"
        @click="() => refetchList()"
      />
    </div>

    <!-- Empty state -->
    <div
      v-else-if="!reportCardsList || reportCardsList.length === 0"
      class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 bg-slate-50/50 px-6 py-12 text-center"
      data-testid="empty-report-cards"
    >
      <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-amber-100 text-amber-700 shadow-sm">
        <FileText class="h-7 w-7" />
      </div>
      <h3 class="mt-4 text-base font-semibold text-slate-900">
        Belum Ada Rapor Final
      </h3>
      <p class="mt-1 max-w-md text-sm text-slate-500">
        Rapor akademik semester belum difinalisasi oleh pihak sekolah. Rapor resmi akan tampil di sini setelah masa penilaian selesai.
      </p>
    </div>

    <!-- Active Report Cards View -->
    <div
      v-else
      class="space-y-6"
    >
      <!-- Semester Switcher & Action Bar -->
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <!-- Semester selector tabs -->
        <div class="flex flex-wrap items-center gap-2">
          <button
            v-for="rc in reportCardsList"
            :key="rc.id"
            type="button"
            :data-testid="`report-card-semester-btn-${rc.id}`"
            class="inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs font-semibold transition-all focus:outline-none"
            :class="[
              rc.id === selectedReportCardId
                ? 'border-brand-primary-600 bg-brand-primary-600 text-white shadow-sm'
                : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
            ]"
            @click="selectReportCard(rc.id)"
          >
            <Calendar class="h-3.5 w-3.5" />
            {{ rc.operationalPeriodName || 'Semester' }}
            <span class="opacity-75">({{ rc.academicYearName }})</span>
          </button>
        </div>

        <!-- Print Trigger Button -->
        <Button
          type="button"
          label="Unduh Rapor PDF"
          data-testid="print-report-card-btn"
          class="shrink-0"
          :disabled="isLoadingDetail || !currentReportCard"
          @click="handlePrint"
        >
          <template #icon>
            <Printer class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
      </div>

      <!-- Report Card Content Container -->
      <div class="space-y-6">
        <!-- Performance Overview Cards -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-primary-50 text-brand-primary-700">
                <Award class="h-5 w-5" />
              </div>
              <div>
                <p class="text-xs font-medium text-slate-500">Rata-rata Nilai</p>
                <p class="text-xl font-bold text-slate-900" data-testid="average-score-value">
                  {{ averageScore > 0 ? averageScore : '—' }}
                </p>
              </div>
            </div>
          </div>

          <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
                <BookOpen class="h-5 w-5" />
              </div>
              <div>
                <p class="text-xs font-medium text-slate-500">Mata Pelajaran</p>
                <p class="text-xl font-bold text-slate-900" data-testid="total-subjects-value">
                  {{ entries.length }} Mapel
                </p>
              </div>
            </div>
          </div>

          <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 text-blue-700">
                <FileCheck class="h-5 w-5" />
              </div>
              <div>
                <p class="text-xs font-medium text-slate-500">Status Rapor</p>
                <div class="mt-0.5 inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-800">
                  Resmi / Final
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Grades Table -->
        <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
          <div class="border-b border-slate-200 bg-slate-50/75 px-4 py-3 sm:px-6">
            <h4 class="text-sm font-semibold text-slate-900">
              Daftar Nilai Hasil Belajar
            </h4>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm" data-testid="grades-table">
              <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase text-slate-500">
                <tr>
                  <th scope="col" class="px-4 py-3 sm:px-6">Mata Pelajaran</th>
                  <th scope="col" class="px-4 py-3 text-center sm:px-6">Nilai Akhir</th>
                  <th scope="col" class="px-4 py-3 text-center sm:px-6">Predikat</th>
                  <th scope="col" class="px-4 py-3 sm:px-6">Catatan Guru</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr
                  v-for="entry in entries"
                  :key="entry.id || entry.subjectId"
                  class="hover:bg-slate-50/50"
                  :data-testid="`grade-row-${entry.subjectId}`"
                >
                  <td class="px-4 py-3.5 font-medium text-slate-900 sm:px-6">
                    <div>
                      <span class="block">{{ entry.subjectName }}</span>
                      <span v-if="entry.subjectCode" class="text-xs text-slate-400">
                        Kode: {{ entry.subjectCode }}
                      </span>
                    </div>
                  </td>
                  <td class="px-4 py-3.5 text-center font-bold text-slate-900 sm:px-6">
                    {{ entry.numericScore !== undefined ? entry.numericScore.toFixed(1) : '-' }}
                  </td>
                  <td class="px-4 py-3.5 text-center sm:px-6">
                    <span
                      class="inline-block min-w-7 rounded-md px-2 py-0.5 text-xs font-bold"
                      :class="[
                        entry.letterGrade === 'A'
                          ? 'bg-emerald-100 text-emerald-800'
                          : entry.letterGrade === 'B'
                            ? 'bg-blue-100 text-blue-800'
                            : entry.letterGrade === 'C'
                              ? 'bg-amber-100 text-amber-800'
                              : 'bg-red-100 text-red-800'
                      ]"
                    >
                      {{ entry.letterGrade ?? '-' }}
                    </span>
                  </td>
                  <td class="max-w-xs px-4 py-3.5 text-xs text-slate-600 sm:px-6">
                    {{ entry.teacherNotes || '—' }}
                  </td>
                </tr>

                <tr v-if="entries.length === 0">
                  <td colspan="4" class="py-6 text-center text-xs text-slate-400">
                    Tidak ada rincian mata pelajaran.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Homeroom Remarks & Attendance Grid -->
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <!-- Homeroom Remarks -->
          <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
            <div class="flex items-center gap-2 border-b border-slate-100 pb-3">
              <UserCheck class="h-4 w-4 text-brand-primary-600" />
              <h4 class="text-sm font-semibold text-slate-900">
                Catatan Wali Kelas
              </h4>
            </div>
            <p
              class="mt-3 text-sm italic text-slate-700"
              data-testid="homeroom-remarks-text"
            >
              "{{ currentReportCard?.generalRemarks || 'Tetap pertahankan prestasi belajar dan tingkatkan kedisiplinan.' }}"
            </p>
            <div class="mt-4 flex items-center justify-between text-xs text-slate-500">
              <span>Wali Kelas: <strong>{{ currentReportCard?.homeroomTeacherName || 'Wali Kelas' }}</strong></span>
              <span v-if="currentReportCard?.finalizedAt">
                Finalisasi: {{ formatDisplayDate(currentReportCard.finalizedAt) }}
              </span>
            </div>
          </div>

          <!-- Attendance Summary -->
          <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs">
            <div class="flex items-center gap-2 border-b border-slate-100 pb-3">
              <Calendar class="h-4 w-4 text-brand-primary-600" />
              <h4 class="text-sm font-semibold text-slate-900">
                Rekapitulasi Kehadiran
              </h4>
            </div>
            <div class="mt-3 grid grid-cols-3 gap-3 text-center sm:grid-cols-5">
              <div class="rounded-lg bg-slate-50 p-2.5">
                <p class="text-[11px] font-medium text-slate-500">Hadir</p>
                <p class="text-base font-bold text-emerald-700">
                  {{ attendance?.presentCount ?? '-' }}
                </p>
              </div>
              <div class="rounded-lg bg-slate-50 p-2.5">
                <p class="text-[11px] font-medium text-slate-500">Sakit</p>
                <p class="text-base font-bold text-amber-700">
                  {{ attendance?.sickCount ?? 0 }}
                </p>
              </div>
              <div class="rounded-lg bg-slate-50 p-2.5">
                <p class="text-[11px] font-medium text-slate-500">Izin</p>
                <p class="text-base font-bold text-blue-700">
                  {{ attendance?.excusedCount ?? 0 }}
                </p>
              </div>
              <div class="rounded-lg bg-slate-50 p-2.5">
                <p class="text-[11px] font-medium text-slate-500">Alpa</p>
                <p class="text-base font-bold text-red-700">
                  {{ attendance?.absentCount ?? 0 }}
                </p>
              </div>
              <div class="rounded-lg bg-slate-50 p-2.5">
                <p class="text-[11px] font-medium text-slate-500">Terlambat</p>
                <p class="text-base font-bold text-slate-700">
                  {{ attendance?.lateCount ?? 0 }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Official Report Card Print Modal / Overlay -->
    <ReportCardPrintView
      v-if="showPrintModal && currentReportCard"
      :report-card="currentReportCard"
      @close="showPrintModal = false"
    />
  </div>
</template>
