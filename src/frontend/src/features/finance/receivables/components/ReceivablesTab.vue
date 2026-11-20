<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Skeleton from 'primevue/skeleton';
import {
  AlertCircle,
  AlertTriangle,
  Banknote,
  FileText,
  RotateCcw,
  Search,
  TrendingUp,
  Wallet
} from 'lucide-vue-next';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import {
  studyGroupsLookupQuery,
  type AcademicYearLookupItem
} from '@/shared/services/lookups.api';
import { formatBackendDateOnly } from '@/shared/utils/dateUtils';
import { ApiError } from '@/shared/types/common';
import type { ReceivablesAgingStudentItemDto } from '../api/receivables.api';
import {
  useReceivablesAgingQuery,
  useReceivablesSummaryQuery,
  useStudentStatementQuery
} from '../api/useReceivableQueries';
import {
  ALL_GRADES,
  GRADES_BY_SCHOOL_LEVEL,
  SCHOOL_LEVEL_OPTIONS,
  computeCollectionRate,
  formatIdr
} from '../utils/formatters';
import StudentStatementDrawer from './StudentStatementDrawer.vue';

const props = withDefaults(
  defineProps<{
    academicYears: AcademicYearLookupItem[];
    defaultAcademicYearId?: string | null;
    canViewStatements?: boolean;
  }>(),
  {
    defaultAcademicYearId: null,
    canViewStatements: false
  }
);

const filterAcademicYearId = ref<string>(props.defaultAcademicYearId ?? '');
const filterSchoolLevel = ref<string | null>(null);
const filterGrade = ref<string | null>(null);
const filterStudyGroupId = ref<string | null>(null);
const filterAsOfDate = ref<string>(formatBackendDateOnly(new Date()));
const searchText = ref<string>('');

const selectedStudent = ref<ReceivablesAgingStudentItemDto | null>(null);
const isStatementDrawerOpen = ref(false);

watch(
  () => props.defaultAcademicYearId,
  (nextDefault) => {
    if (!filterAcademicYearId.value && nextDefault) {
      filterAcademicYearId.value = nextDefault;
    }
  },
  { immediate: true }
);

watch(filterSchoolLevel, (nextLevel) => {
  if (nextLevel && filterGrade.value) {
    const allowed = GRADES_BY_SCHOOL_LEVEL[nextLevel] ?? [];
    if (!allowed.includes(filterGrade.value)) {
      filterGrade.value = null;
    }
  }
  filterStudyGroupId.value = null;
});

watch([filterAcademicYearId, filterGrade], () => {
  filterStudyGroupId.value = null;
});

const academicYearOptions = computed(() => [
  { label: 'All Academic Years', value: '' },
  ...props.academicYears
    .filter((y) => Boolean(y.id))
    .map((y) => ({
      label: y.status === 'ACTIVE' ? `${y.name} (Active)` : (y.name ?? 'Unnamed Year'),
      value: y.id!
    }))
]);

const gradeOptions = computed(() => {
  const list = filterSchoolLevel.value
    ? (GRADES_BY_SCHOOL_LEVEL[filterSchoolLevel.value] ?? ALL_GRADES)
    : ALL_GRADES;
  return list.map((g) => ({ label: `Grade ${g}`, value: g }));
});

const studyGroupsQuery = useQuery(
  computed(() =>
    studyGroupsLookupQuery({
      academicYearId: filterAcademicYearId.value || undefined,
      schoolLevel: filterSchoolLevel.value || undefined,
      grade: filterGrade.value || undefined
    })
  )
);

const studyGroupOptions = computed(() =>
  (studyGroupsQuery.data.value ?? [])
    .filter((sg) => Boolean(sg.id))
    .map((sg) => ({
      label: sg.name ?? 'Unnamed Group',
      value: sg.id!
    }))
);

const summaryParams = computed(() => ({
  academicYearId: filterAcademicYearId.value || undefined
}));

const agingParams = computed(() => ({
  academicYearId: filterAcademicYearId.value || undefined,
  schoolLevel: filterSchoolLevel.value || undefined,
  grade: filterGrade.value || undefined,
  studyGroupId: filterStudyGroupId.value || undefined,
  asOfDate: filterAsOfDate.value || undefined
}));

const summaryQuery = useReceivablesSummaryQuery(summaryParams);
const agingQuery = useReceivablesAgingQuery(agingParams);

const selectedStudentId = computed(() => selectedStudent.value?.studentId ?? null);

const statementQuery = useStudentStatementQuery(selectedStudentId, {
  enabled: computed(() => isStatementDrawerOpen.value && props.canViewStatements)
});

const filteredStudents = computed<ReceivablesAgingStudentItemDto[]>(() => {
  const rows = agingQuery.data.value?.students ?? [];
  const term = searchText.value.trim().toLowerCase();
  if (!term) return rows;

  return rows.filter((row) => {
    const haystack = [row.studentName, row.studentNis, row.studyGroupName, row.schoolLevel, row.grade]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();
    return haystack.includes(term);
  });
});

const summaryData = computed(() => summaryQuery.data.value);
const agingSummary = computed(() => agingQuery.data.value?.summary);
const levelBreakdown = computed(() => summaryData.value?.breakdown ?? []);

const collectionRateLabel = computed(() =>
  computeCollectionRate(summaryData.value?.totalCollected, summaryData.value?.totalBilled)
);

const isLoading = computed(() => summaryQuery.isLoading.value || agingQuery.isLoading.value);

const reportErrorMessage = computed<string | null>(() => {
  const err = agingQuery.error.value ?? summaryQuery.error.value;
  if (!err) return null;
  if (err instanceof ApiError) {
    return err.detail || err.title || 'Failed to load receivables report.';
  }
  return err instanceof Error ? err.message : 'Failed to load receivables report.';
});

const statementErrorMessage = computed<string | null>(() => {
  const err = statementQuery.error.value;
  if (!err) return null;
  if (err instanceof ApiError) {
    return err.detail || err.title || 'Failed to load student financial statement.';
  }
  return err instanceof Error ? err.message : 'Failed to load student financial statement.';
});

const hasActiveFilters = computed(() =>
  Boolean(
    filterSchoolLevel.value ||
      filterGrade.value ||
      filterStudyGroupId.value ||
      searchText.value.trim()
  )
);

function resetFilters(): void {
  filterSchoolLevel.value = null;
  filterGrade.value = null;
  filterStudyGroupId.value = null;
  filterAsOfDate.value = formatBackendDateOnly(new Date());
  searchText.value = '';
}

function openStudentStatement(student: ReceivablesAgingStudentItemDto): void {
  if (!props.canViewStatements || !student.studentId) return;
  selectedStudent.value = student;
  isStatementDrawerOpen.value = true;
}

function onDrawerVisibleChange(nextVisible: boolean): void {
  isStatementDrawerOpen.value = nextVisible;
  if (!nextVisible) {
    selectedStudent.value = null;
  }
}
</script>

<template>
  <div class="space-y-5" data-testid="receivables-tab">
    <!-- Filter Toolbar -->
    <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-6">
        <div class="space-y-1">
          <label class="text-xs font-medium text-slate-600">Academic Year</label>
          <Select
            :model-value="filterAcademicYearId"
            :options="academicYearOptions"
            option-label="label"
            option-value="value"
            placeholder="All Academic Years"
            class="w-full"
            data-testid="filter-academic-year"
            @update:model-value="filterAcademicYearId = $event ?? ''"
          />
        </div>

        <div class="space-y-1">
          <label class="text-xs font-medium text-slate-600">School Level</label>
          <Select
            v-model="filterSchoolLevel"
            :options="[...SCHOOL_LEVEL_OPTIONS]"
            option-label="label"
            option-value="value"
            placeholder="All Levels"
            show-clear
            class="w-full"
            data-testid="filter-school-level"
          />
        </div>

        <div class="space-y-1">
          <label class="text-xs font-medium text-slate-600">Grade</label>
          <Select
            v-model="filterGrade"
            :options="gradeOptions"
            option-label="label"
            option-value="value"
            placeholder="All Grades"
            show-clear
            class="w-full"
            data-testid="filter-grade"
          />
        </div>

        <div class="space-y-1">
          <label class="text-xs font-medium text-slate-600">Study Group</label>
          <Select
            v-model="filterStudyGroupId"
            :options="studyGroupOptions"
            option-label="label"
            option-value="value"
            placeholder="All Groups"
            show-clear
            :loading="studyGroupsQuery.isLoading.value"
            class="w-full"
            data-testid="filter-study-group"
          />
        </div>

        <div class="space-y-1">
          <label for="receivables-as-of-date" class="text-xs font-medium text-slate-600">
            Aging As-Of Date
          </label>
          <input
            id="receivables-as-of-date"
            v-model="filterAsOfDate"
            type="date"
            class="h-[38px] w-full rounded-md border border-slate-300 bg-white px-3 text-sm text-slate-800 shadow-xs focus:border-teal-600 focus:outline-hidden focus:ring-1 focus:ring-teal-600"
            data-testid="filter-as-of-date"
          />
        </div>

        <div class="space-y-1">
          <label class="text-xs font-medium text-slate-600">Search Student</label>
          <div class="relative">
            <Search
              class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              v-model="searchText"
              placeholder="Name, NIS, class..."
              class="w-full pl-9"
              data-testid="filter-student-search"
            />
          </div>
        </div>
      </div>

      <div v-if="hasActiveFilters" class="mt-3 flex justify-end">
        <Button
          type="button"
          label="Reset Filters"
          severity="secondary"
          text
          size="small"
          data-testid="reset-filters-btn"
          @click="resetFilters"
        >
          <template #icon>
            <RotateCcw class="mr-1.5 h-3.5 w-3.5" />
          </template>
        </Button>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="reportErrorMessage"
      class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800"
      data-testid="receivables-error-banner"
    >
      <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
      <div>
        <p class="font-semibold">Unable to load receivables report</p>
        <p class="mt-0.5 text-xs text-rose-700">{{ reportErrorMessage }}</p>
      </div>
    </div>

    <!-- KPI Metric Cards -->
    <div v-if="isLoading" class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <Skeleton v-for="i in 4" :key="i" height="6.5rem" border-radius="0.75rem" />
    </div>

    <div v-else class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div
        class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
        data-testid="kpi-total-billed"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total Billed
          </span>
          <Banknote class="h-4 w-4 text-slate-400" />
        </div>
        <p class="mt-2 text-xl font-bold text-slate-900">
          {{ formatIdr(summaryData?.totalBilled) }}
        </p>
        <p class="mt-1 text-xs text-slate-500">
          {{ summaryData?.totalStudentsCount ?? 0 }} students •
          {{ summaryData?.totalChargesCount ?? 0 }} active charges
        </p>
      </div>

      <div
        class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
        data-testid="kpi-total-collected"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total Collected
          </span>
          <TrendingUp class="h-4 w-4 text-emerald-600" />
        </div>
        <p class="mt-2 text-xl font-bold text-emerald-700">
          {{ formatIdr(summaryData?.totalCollected) }}
        </p>
        <p class="mt-1 text-xs text-slate-500">
          Collection rate: <span class="font-semibold text-slate-700">{{ collectionRateLabel }}</span>
        </p>
      </div>

      <div
        class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
        data-testid="kpi-total-outstanding"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total Outstanding
          </span>
          <Wallet class="h-4 w-4 text-amber-600" />
        </div>
        <p class="mt-2 text-xl font-bold text-amber-700">
          {{ formatIdr(summaryData?.totalOutstanding) }}
        </p>
        <p class="mt-1 text-xs text-slate-500">
          Filtered open balance:
          <span class="font-semibold text-slate-700">
            {{ formatIdr(agingSummary?.totalOutstanding) }}
          </span>
        </p>
      </div>

      <div
        class="rounded-xl border border-rose-200 bg-rose-50/40 p-4 shadow-xs"
        data-testid="kpi-arrears-over-30"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-rose-700">
            Arrears &gt; 30 Days
          </span>
          <AlertTriangle class="h-4 w-4 text-rose-600" />
        </div>
        <p class="mt-2 text-xl font-bold text-rose-700">
          {{ formatIdr(agingSummary?.arrearsOver30Days) }}
        </p>
        <p class="mt-1 text-xs text-rose-700/80">
          31–60d, 61–90d &amp; 90+ days overdue
        </p>
      </div>
    </div>

    <!-- School Level Summary Strip (when breakdown available) -->
    <div
      v-if="!isLoading && levelBreakdown.length > 0"
      class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
      data-testid="level-breakdown-section"
    >
      <h3 class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">
        Collections by School Level
      </h3>
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div
          v-for="item in levelBreakdown"
          :key="`${item.academicYearId}-${item.schoolLevel}`"
          class="rounded-lg border border-slate-200 bg-slate-50/70 p-3 text-xs"
        >
          <div class="flex items-center justify-between">
            <span class="font-bold text-slate-900">
              {{ item.schoolLevel || 'General' }}
            </span>
            <span class="text-[11px] text-slate-500">{{ item.academicYearName }}</span>
          </div>
          <div class="mt-2 space-y-1">
            <div class="flex justify-between text-slate-600">
              <span>Billed</span>
              <span class="font-medium text-slate-800">{{ formatIdr(item.totalBilled) }}</span>
            </div>
            <div class="flex justify-between text-slate-600">
              <span>Collected</span>
              <span class="font-medium text-emerald-700">{{ formatIdr(item.totalCollected) }}</span>
            </div>
            <div class="flex justify-between text-slate-600">
              <span>Outstanding</span>
              <span class="font-semibold text-rose-700">{{ formatIdr(item.totalOutstanding) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Aging Buckets Overview Strip -->
    <div
      v-if="!isLoading && agingSummary"
      class="grid grid-cols-2 gap-2.5 sm:grid-cols-5"
      data-testid="aging-buckets-strip"
    >
      <div class="rounded-lg border border-slate-200 bg-white px-3 py-2.5">
        <p class="text-[11px] font-medium text-slate-500">Current (Not Due)</p>
        <p class="mt-0.5 text-sm font-bold text-slate-900">
          {{ formatIdr(agingSummary.currentAmount) }}
        </p>
      </div>
      <div class="rounded-lg border border-slate-200 bg-white px-3 py-2.5">
        <p class="text-[11px] font-medium text-slate-500">1–30 Days</p>
        <p class="mt-0.5 text-sm font-bold text-amber-700">
          {{ formatIdr(agingSummary.days1To30Amount) }}
        </p>
      </div>
      <div class="rounded-lg border border-slate-200 bg-white px-3 py-2.5">
        <p class="text-[11px] font-medium text-slate-500">31–60 Days</p>
        <p class="mt-0.5 text-sm font-bold text-orange-700">
          {{ formatIdr(agingSummary.days31To60Amount) }}
        </p>
      </div>
      <div class="rounded-lg border border-slate-200 bg-white px-3 py-2.5">
        <p class="text-[11px] font-medium text-slate-500">61–90 Days</p>
        <p class="mt-0.5 text-sm font-bold text-rose-700">
          {{ formatIdr(agingSummary.days61To90Amount) }}
        </p>
      </div>
      <div class="rounded-lg border border-rose-200 bg-rose-50/40 px-3 py-2.5">
        <p class="text-[11px] font-medium text-rose-700">90+ Days</p>
        <p class="mt-0.5 text-sm font-bold text-rose-800">
          {{ formatIdr(agingSummary.over90DaysAmount) }}
        </p>
      </div>
    </div>

    <!-- Aged Arrears Breakdown Table -->
    <div class="rounded-xl border border-slate-200 bg-white shadow-xs">
      <div class="border-b border-slate-200 px-4 py-3">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h2 class="text-sm font-semibold text-slate-900">
              Aged Arrears Breakdown by Student
            </h2>
            <p class="text-xs text-slate-500">
              {{ filteredStudents.length }} student(s) with open receivable balances
            </p>
          </div>
          <span
            v-if="!canViewStatements"
            class="text-xs text-slate-500"
            data-testid="statement-permission-hint"
          >
            Detailed student statement drill-down is available to Finance Officers.
          </span>
        </div>
      </div>

      <AppDataTable
        :value="filteredStudents"
        :loading="agingQuery.isLoading.value"
        :paginator="filteredStudents.length > 10"
        :rows="10"
        data-key="studentId"
        striped-rows
        empty-title="No outstanding receivables"
        empty-description="No unpaid or partially paid student charges match the current filters."
        data-testid="receivables-aging-table"
      >
        <Column field="studentName" header="Student" sortable style="min-width: 12rem">
          <template #body="{ data }">
            <div>
              <div class="font-semibold text-slate-900">{{ data.studentName || '—' }}</div>
              <div class="text-xs text-slate-500">NIS: {{ data.studentNis || '—' }}</div>
            </div>
          </template>
        </Column>

        <Column field="studyGroupName" header="Class" style="min-width: 8rem">
          <template #body="{ data }">
            <div>
              <div class="font-medium text-slate-800">{{ data.studyGroupName || 'Unplaced' }}</div>
              <div class="text-xs text-slate-500">
                {{ [data.schoolLevel, data.grade ? `Grade ${data.grade}` : null].filter(Boolean).join(' • ') || '—' }}
              </div>
            </div>
          </template>
        </Column>

        <Column field="currentAmount" header="Current" sortable style="min-width: 7.5rem">
          <template #body="{ data }">
            <span class="text-sm text-slate-700">{{ formatIdr(data.currentAmount) }}</span>
          </template>
        </Column>

        <Column field="days1To30Amount" header="1–30 Days" sortable style="min-width: 7.5rem">
          <template #body="{ data }">
            <span
              :class="(data.days1To30Amount ?? 0) > 0 ? 'font-medium text-amber-700' : 'text-slate-400'"
            >
              {{ formatIdr(data.days1To30Amount) }}
            </span>
          </template>
        </Column>

        <Column field="days31To60Amount" header="31–60 Days" sortable style="min-width: 7.5rem">
          <template #body="{ data }">
            <span
              :class="(data.days31To60Amount ?? 0) > 0 ? 'font-medium text-orange-700' : 'text-slate-400'"
            >
              {{ formatIdr(data.days31To60Amount) }}
            </span>
          </template>
        </Column>

        <Column field="days61To90Amount" header="61–90 Days" sortable style="min-width: 7.5rem">
          <template #body="{ data }">
            <span
              :class="(data.days61To90Amount ?? 0) > 0 ? 'font-medium text-rose-700' : 'text-slate-400'"
            >
              {{ formatIdr(data.days61To90Amount) }}
            </span>
          </template>
        </Column>

        <Column field="over90DaysAmount" header="90+ Days" sortable style="min-width: 7.5rem">
          <template #body="{ data }">
            <span
              :class="(data.over90DaysAmount ?? 0) > 0 ? 'font-bold text-rose-800' : 'text-slate-400'"
            >
              {{ formatIdr(data.over90DaysAmount) }}
            </span>
          </template>
        </Column>

        <Column field="totalOutstanding" header="Total Outstanding" sortable style="min-width: 9rem">
          <template #body="{ data }">
            <div>
              <div class="font-bold text-slate-900">{{ formatIdr(data.totalOutstanding) }}</div>
              <div class="text-[11px] text-slate-500">
                {{ data.unpaidChargeCount ?? 0 }} open charge(s)
              </div>
            </div>
          </template>
        </Column>

        <Column header="Actions" style="min-width: 8.5rem">
          <template #body="{ data }">
            <Button
              type="button"
              label="Statement"
              size="small"
              severity="secondary"
              outlined
              :disabled="!canViewStatements"
              :title="
                canViewStatements
                  ? 'Open student financial ledger statement'
                  : 'Requires Finance Statements permission'
              "
              :data-testid="`view-statement-btn-${data.studentId}`"
              @click="openStudentStatement(data)"
            >
              <template #icon>
                <FileText class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>
          </template>
        </Column>
      </AppDataTable>
    </div>

    <!-- Student Drill-Down Statement Drawer -->
    <StudentStatementDrawer
      :visible="isStatementDrawerOpen"
      :student-item="selectedStudent"
      :statement="statementQuery.data.value ?? null"
      :loading="statementQuery.isLoading.value"
      :error-message="statementErrorMessage"
      @update:visible="onDrawerVisibleChange"
    />
  </div>
</template>
