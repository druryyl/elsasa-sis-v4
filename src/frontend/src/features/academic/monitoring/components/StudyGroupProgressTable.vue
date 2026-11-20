<script setup lang="ts">
import { computed, ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Button from 'primevue/button';
import { Search, Eye } from 'lucide-vue-next';
import type { StudyGroupMonitoringRowDto } from '../api/academicMonitoring.api';

const props = withDefaults(
  defineProps<{
    studyGroups?: StudyGroupMonitoringRowDto[] | null;
    loading?: boolean;
  }>(),
  {
    studyGroups: () => [],
    loading: false
  }
);

const emit = defineEmits<{
  'select-group': [studyGroupId: string];
}>();

const searchQuery = ref('');
const selectedLevel = ref('ALL');
const selectedGrade = ref('ALL');

const levelOptions = [
  { label: 'Semua Jenjang', value: 'ALL' },
  { label: 'SD', value: 'SD' },
  { label: 'SMP', value: 'SMP' },
  { label: 'SMA', value: 'SMA' },
  { label: 'SMK', value: 'SMK' }
];

const gradeOptions = computed(() => {
  const grades = new Set<string>();
  for (const row of props.studyGroups ?? []) {
    if (row.grade) grades.add(row.grade);
  }
  const sorted = Array.from(grades).sort((a, b) => {
    const numA = parseInt(a, 10);
    const numB = parseInt(b, 10);
    return isNaN(numA) || isNaN(numB) ? a.localeCompare(b) : numA - numB;
  });

  return [
    { label: 'Semua Kelas', value: 'ALL' },
    ...sorted.map((g) => ({ label: `Kelas ${g}`, value: g }))
  ];
});

const filteredRows = computed(() => {
  let rows = props.studyGroups ?? [];

  if (selectedLevel.value && selectedLevel.value !== 'ALL') {
    rows = rows.filter((r) => r.schoolLevel?.toUpperCase() === selectedLevel.value);
  }

  if (selectedGrade.value && selectedGrade.value !== 'ALL') {
    rows = rows.filter((r) => r.grade === selectedGrade.value);
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    rows = rows.filter(
      (r) =>
        r.studyGroupName?.toLowerCase().includes(q) ||
        r.homeroomTeacherName?.toLowerCase().includes(q)
    );
  }

  return rows;
});

function rowClass(data: StudyGroupMonitoringRowDto) {
  if (data.isAttendanceMissingToday || data.hasOverdueGrading || data.hasPendingReportCards) {
    return '!bg-amber-50/60 dark:!bg-amber-950/20';
  }
  return '';
}

function attendanceBarColor(rate?: number) {
  const val = rate ?? 0;
  if (val >= 90) return 'bg-emerald-500';
  if (val >= 75) return 'bg-amber-500';
  return 'bg-rose-500';
}
</script>

<template>
  <div
    data-testid="study-group-progress-container"
    class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs dark:border-slate-800 dark:bg-slate-900"
  >
    <!-- Table Header Toolbar -->
    <div class="border-b border-slate-200 p-4 dark:border-slate-800">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 class="text-base font-bold text-slate-900 dark:text-white">
            Matriks Progres Rombongan Belajar
          </h2>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Daftar rombel, kehadiran harian, keterisian nilai, dan rasio penyelesaian rapor.
          </p>
        </div>

        <!-- Filter Controls -->
        <div class="flex flex-wrap items-center gap-2">
          <!-- School Level Filter -->
          <Select
            v-model="selectedLevel"
            :options="levelOptions"
            option-label="label"
            option-value="value"
            placeholder="Jenjang"
            class="text-xs"
            data-testid="filter-school-level"
          />

          <!-- Grade Filter -->
          <Select
            v-model="selectedGrade"
            :options="gradeOptions"
            option-label="label"
            option-value="value"
            placeholder="Kelas"
            class="text-xs"
            data-testid="filter-grade"
          />

          <!-- Global Search -->
          <div class="relative min-w-44">
            <Search class="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400" />
            <InputText
              v-model="searchQuery"
              placeholder="Cari rombel/wali..."
              class="w-full pl-8 text-xs"
              data-testid="input-search"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- PrimeVue DataTable -->
    <DataTable
      :value="filteredRows"
      :loading="loading"
      data-key="studyGroupId"
      striped-rows
      responsive-layout="scroll"
      :row-class="rowClass"
      data-testid="study-group-progress-table"
      class="text-sm"
    >
      <template #empty>
        <div class="p-8 text-center text-sm text-slate-500 dark:text-slate-400">
          Tidak ada data rombongan belajar yang sesuai dengan kriteria filter.
        </div>
      </template>

      <!-- Study Group -->
      <Column header="Rombel" class="min-w-36 font-medium">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <div class="flex flex-col">
            <span class="font-bold text-slate-900 dark:text-white">{{ data.studyGroupName }}</span>
            <div
              v-if="data.isAttendanceMissingToday || data.hasOverdueGrading || data.hasPendingReportCards"
              class="mt-1 flex flex-wrap gap-1"
            >
              <span
                v-if="data.isAttendanceMissingToday"
                class="rounded bg-rose-100 px-1.5 py-0.2 text-[10px] font-bold text-rose-700 dark:bg-rose-950 dark:text-rose-400"
              >
                Presensi Kosong
              </span>
              <span
                v-if="data.hasOverdueGrading"
                class="rounded bg-amber-100 px-1.5 py-0.2 text-[10px] font-bold text-amber-700 dark:bg-amber-950 dark:text-amber-400"
              >
                Nilai Overdue
              </span>
              <span
                v-if="data.hasPendingReportCards"
                class="rounded bg-sky-100 px-1.5 py-0.2 text-[10px] font-bold text-sky-700 dark:bg-sky-950 dark:text-sky-400"
              >
                Rapor Draf
              </span>
            </div>
          </div>
        </template>
      </Column>

      <!-- Level & Grade -->
      <Column header="Jenjang & Kelas" class="w-36">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <span class="rounded bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700 dark:bg-slate-800 dark:text-slate-300">
            {{ data.schoolLevel }} &bull; Kls {{ data.grade }}
          </span>
        </template>
      </Column>

      <!-- Homeroom Teacher -->
      <Column header="Wali Kelas" class="min-w-44">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <span class="text-sm text-slate-700 dark:text-slate-300">
            {{ data.homeroomTeacherName || 'Belum Ditugaskan' }}
          </span>
        </template>
      </Column>

      <!-- Attendance Today % -->
      <Column header="Presensi Hari Ini" class="w-44">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <div class="flex items-center gap-2">
            <div class="h-2 w-16 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
              <div
                class="h-full rounded-full transition-all duration-300"
                :class="attendanceBarColor(data.attendanceTodayPercent)"
                :style="{ width: `${Math.min(data.attendanceTodayPercent ?? 0, 100)}%` }"
              />
            </div>
            <span class="font-mono text-xs font-bold text-slate-900 dark:text-white">
              {{ (data.attendanceTodayPercent ?? 0).toFixed(1) }}%
            </span>
          </div>
        </template>
      </Column>

      <!-- Grading Completeness % -->
      <Column header="Ketuntasan Nilai" class="w-48">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <div class="flex items-center gap-2">
            <div class="h-2 w-16 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
              <div
                class="h-full rounded-full bg-indigo-500 transition-all duration-300"
                :style="{ width: `${Math.min(data.gradingCompletenessPercent ?? 0, 100)}%` }"
              />
            </div>
            <span class="font-mono text-xs font-bold text-slate-900 dark:text-white">
              {{ (data.gradingCompletenessPercent ?? 0).toFixed(1) }}%
            </span>
            <span class="text-[11px] text-slate-400">
              ({{ data.assignmentsCount ?? 0 }} tugas)
            </span>
          </div>
        </template>
      </Column>

      <!-- Report Cards Finalized Ratio -->
      <Column header="Rapor Final" class="w-32">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <span class="rounded bg-slate-100 px-2 py-0.5 font-mono text-xs font-semibold text-slate-800 dark:bg-slate-800 dark:text-slate-200">
            {{ data.reportCardsFinalizedRatio || '0/0' }}
          </span>
        </template>
      </Column>

      <!-- Actions -->
      <Column header="Aksi" class="w-24 text-right">
        <template #body="{ data }: { data: StudyGroupMonitoringRowDto }">
          <Button
            type="button"
            icon="pi pi-eye"
            severity="secondary"
            text
            size="small"
            class="h-8 w-8 !p-0"
            title="Lihat Detail Rombel"
            data-testid="btn-view-study-group"
            @click="emit('select-group', data.studyGroupId ?? '')"
          >
            <template #icon>
              <Eye class="h-4 w-4 text-slate-600 dark:text-slate-400" />
            </template>
          </Button>
        </template>
      </Column>
    </DataTable>
  </div>
</template>
