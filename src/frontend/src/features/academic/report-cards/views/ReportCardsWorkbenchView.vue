<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useQuery } from '@tanstack/vue-query';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import {
  CheckCircle2,
  FilePlus2,
  FileText,
  GraduationCap,
  Loader2,
  Printer,
  Search
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { studyGroupsLookupQuery } from '@/shared/services/lookups.api';
import {
  useHomeroomReportCardsQuery,
  useReportCardAcademicYearsQuery,
  useReportCardDetailQuery,
  useReportCardsOverviewQuery
} from '../api/useReportCardQueries';
import { useGenerateDraftReportCardsMutation } from '../api/useReportCardMutations';
import BatchGenerateModal, {
  type BatchGenerateStudyGroupOption
} from '../components/BatchGenerateModal.vue';
import ReportCardDetailPanel from '../components/ReportCardDetailPanel.vue';
import ReportCardPrintView from '../components/ReportCardPrintView.vue';

interface RosterRow {
  id: string;
  studentId: string;
  studentName: string;
  studentNis: string;
  studyGroupName: string;
  homeroomTeacherName: string;
  status: string;
  averageScore: number;
  totalSubjects: number;
  completedNotesCount: number;
  isHomeroomRemarksCompleted: boolean;
}

interface PeriodOption {
  value: string;
  label: string;
  academicYearId: string;
  status?: string | null;
}

const route = useRoute();
const router = useRouter();
const session = useCurrentSession();
const contextBridge = useAppContextBridge();

// Permissions
const canViewOverview = computed(() =>
  session.hasPermission('Permissions.Academic.ReportCards.View')
);
const canGenerate = computed(() =>
  session.hasPermission('Permissions.Academic.ReportCards.Generate')
);
const canProvideNotes = computed(() =>
  session.hasPermission('Permissions.Academic.ReportCards.ProvideNotes')
);
const canWriteRemarks = computed(() =>
  session.hasPermission('Permissions.Academic.ReportCards.Remarks')
);
const canFinalize = computed(() =>
  session.hasPermission('Permissions.Academic.ReportCards.Finalize')
);
const canOverride = computed(() =>
  session.hasPermission('Permissions.Academic.ReportCards.Override')
);

// Filters & Selection State
const selectedPeriodId = ref<string>(
  (route.query.operationalPeriodId as string) ||
    contextBridge.selectedPeriodId.value ||
    contextBridge.activePeriodId.value ||
    ''
);
const selectedStudyGroupId = ref<string>((route.query.studyGroupId as string) || '');
const searchQuery = ref<string>('');
const statusFilter = ref<'ALL' | 'DRAFT' | 'FINALIZED'>('ALL');
const selectedReportCardId = ref<string | null>(
  (route.query.reportCardId as string) || null
);

// Modals & Print Preview
const isBatchModalOpen = ref(false);
const isPrintPreviewOpen = ref(false);

// Academic Years & Operational Periods Lookup
const academicYearsQuery = useReportCardAcademicYearsQuery();

const periodOptions = computed<PeriodOption[]>(() => {
  const options: PeriodOption[] = [];
  for (const year of academicYearsQuery.data.value ?? []) {
    if (!year.id) continue;
    const yearName = year.name ?? 'Academic Year';
    for (const period of year.periods ?? []) {
      if (!period.id) continue;
      options.push({
        value: period.id,
        label: `${yearName} • ${period.name ?? 'Period'}${
          period.status === 'ACTIVE' ? ' (Active)' : ''
        }`,
        academicYearId: year.id,
        status: period.status
      });
    }
  }

  const fallbackPeriodId =
    contextBridge.selectedPeriodId.value ?? contextBridge.activePeriodId.value;
  if (fallbackPeriodId && !options.some((o) => o.value === fallbackPeriodId)) {
    options.unshift({
      value: fallbackPeriodId,
      label: contextBridge.currentContextLabel.value || 'Current Academic Period',
      academicYearId:
        contextBridge.selectedAcademicYearId.value ??
        contextBridge.activeAcademicYearId.value ??
        '',
      status: contextBridge.isHistoricalPeriod.value ? 'CLOSED' : 'ACTIVE'
    });
  }

  return options;
});

const selectedPeriodOption = computed(
  () => periodOptions.value.find((o) => o.value === selectedPeriodId.value) ?? null
);

const selectedAcademicYearId = computed(
  () =>
    selectedPeriodOption.value?.academicYearId ||
    contextBridge.selectedAcademicYearId.value ||
    contextBridge.activeAcademicYearId.value ||
    ''
);

const isHistoricalPeriod = computed(() => {
  if (contextBridge.isHistoricalPeriod.value) return true;
  if (selectedPeriodOption.value?.status && selectedPeriodOption.value.status !== 'ACTIVE') {
    return true;
  }
  return false;
});

// Keep default period aligned with top-bar context if not explicitly set
watch(
  () => contextBridge.selectedPeriodId.value ?? contextBridge.activePeriodId.value,
  (nextId) => {
    if (nextId && !selectedPeriodId.value) {
      selectedPeriodId.value = nextId;
    }
  },
  { immediate: true }
);

// Study Groups Lookup
const studyGroupsQuery = useQuery(
  computed(() =>
    studyGroupsLookupQuery({
      academicYearId: selectedAcademicYearId.value || undefined
    })
  )
);

const rawStudyGroups = computed(() => studyGroupsQuery.data.value ?? []);

const studyGroupOptions = computed(() => {
  const groups = rawStudyGroups.value.map((g) => ({
    label: `${g.name ?? 'Unnamed Group'}${g.grade ? ` (Grade ${g.grade})` : ''}`,
    value: g.id ?? ''
  }));

  if (canViewOverview.value) {
    return [{ label: 'All Study Groups (School Overview)', value: '' }, ...groups];
  }
  return groups;
});

const batchModalStudyGroups = computed<BatchGenerateStudyGroupOption[]>(() =>
  rawStudyGroups.value
    .filter((g): g is typeof g & { id: string } => Boolean(g.id))
    .map((g) => ({
      id: g.id,
      name: g.name ?? 'Unnamed Group',
      studentCount: g.studentCount ?? 0,
      homeroomTeacherName: g.homeroomTeacherName
    }))
);

// Auto-select a study group for teachers/homeroom teachers who cannot call /overview
watch(
  [rawStudyGroups, canViewOverview],
  ([groups, hasOverview]) => {
    if (hasOverview || selectedStudyGroupId.value || groups.length === 0) return;

    const myHomeroomGroup = session.teacherId.value
      ? groups.find((g) => g.homeroomTeacherId === session.teacherId.value)
      : undefined;

    const target = myHomeroomGroup ?? groups[0];
    if (target?.id) {
      selectedStudyGroupId.value = target.id;
    }
  },
  { immediate: true }
);

// Sync URL query params
watch([selectedPeriodId, selectedStudyGroupId, selectedReportCardId], ([periodId, groupId, cardId]) => {
  const newQuery: Record<string, string | (string | null)[] | null | undefined> = {
    ...route.query
  };

  if (periodId) newQuery.operationalPeriodId = periodId;
  else delete newQuery.operationalPeriodId;

  if (groupId) newQuery.studyGroupId = groupId;
  else delete newQuery.studyGroupId;

  if (cardId) newQuery.reportCardId = cardId;
  else delete newQuery.reportCardId;

  if (
    route.query.operationalPeriodId !== newQuery.operationalPeriodId ||
    route.query.studyGroupId !== newQuery.studyGroupId ||
    route.query.reportCardId !== newQuery.reportCardId
  ) {
    void router.replace({ query: newQuery });
  }
});

// Report Card Queries
const isHomeroomQueryEnabled = computed(() =>
  Boolean(selectedStudyGroupId.value && selectedPeriodId.value)
);

const homeroomQuery = useHomeroomReportCardsQuery(
  computed(() => ({
    studyGroupId: selectedStudyGroupId.value,
    operationalPeriodId: selectedPeriodId.value
  })),
  { enabled: isHomeroomQueryEnabled }
);

const isOverviewQueryEnabled = computed(() =>
  Boolean(canViewOverview.value && !selectedStudyGroupId.value && selectedPeriodId.value)
);

const overviewQuery = useReportCardsOverviewQuery(
  computed(() => ({
    operationalPeriodId: selectedPeriodId.value,
    studyGroupId: selectedStudyGroupId.value || undefined
  })),
  { enabled: isOverviewQueryEnabled }
);

const rosterRows = computed<RosterRow[]>(() => {
  if (selectedStudyGroupId.value) {
    return (homeroomQuery.data.value ?? []).map((item) => ({
      id: item.id ?? '',
      studentId: item.studentId ?? '',
      studentName: item.studentName ?? 'Unnamed Student',
      studentNis: item.studentNis ?? '-',
      studyGroupName: item.studyGroupName ?? '-',
      homeroomTeacherName: item.homeroomTeacherName ?? '-',
      status: item.status ?? 'DRAFT',
      averageScore: item.averageScore ?? 0,
      totalSubjects: item.totalSubjects ?? 0,
      completedNotesCount: item.completedNotesCount ?? 0,
      isHomeroomRemarksCompleted: Boolean(item.isHomeroomRemarksCompleted)
    }));
  }

  if (canViewOverview.value) {
    return (overviewQuery.data.value?.items ?? []).map((item) => ({
      id: item.id ?? '',
      studentId: item.studentId ?? '',
      studentName: item.studentName ?? 'Unnamed Student',
      studentNis: item.studentNis ?? '-',
      studyGroupName: item.studyGroupName ?? '-',
      homeroomTeacherName: item.homeroomTeacherName ?? '-',
      status: item.status ?? 'DRAFT',
      averageScore: item.averageScore ?? 0,
      totalSubjects: item.totalSubjects ?? 0,
      completedNotesCount: item.completedNotesCount ?? 0,
      isHomeroomRemarksCompleted: Boolean(item.isHomeroomRemarksCompleted)
    }));
  }

  return [];
});

const filteredRosterRows = computed(() => {
  const term = searchQuery.value.trim().toLowerCase();
  return rosterRows.value.filter((row) => {
    const matchesStatus =
      statusFilter.value === 'ALL' || row.status.toUpperCase() === statusFilter.value;
    const matchesSearch =
      !term ||
      row.studentName.toLowerCase().includes(term) ||
      row.studentNis.toLowerCase().includes(term) ||
      row.studyGroupName.toLowerCase().includes(term);
    return matchesStatus && matchesSearch;
  });
});

// Auto-select first card in roster if none selected or if selection no longer in list
watch(
  rosterRows,
  (rows) => {
    if (rows.length === 0) {
      selectedReportCardId.value = null;
      return;
    }
    if (!selectedReportCardId.value || !rows.some((r) => r.id === selectedReportCardId.value)) {
      selectedReportCardId.value = rows[0]?.id ?? null;
    }
  },
  { immediate: true }
);

const isLoadingRoster = computed(() =>
  selectedStudyGroupId.value ? homeroomQuery.isLoading.value : overviewQuery.isLoading.value
);

// KPI Summary Metrics
const kpiStats = computed(() => {
  if (!selectedStudyGroupId.value && canViewOverview.value && overviewQuery.data.value) {
    const o = overviewQuery.data.value;
    return {
      total: o.totalReportCards ?? 0,
      draft: o.draftCount ?? 0,
      finalized: o.finalizedCount ?? 0,
      average: o.schoolAverageScore ?? 0
    };
  }

  const rows = rosterRows.value;
  const total = rows.length;
  const draft = rows.filter((r) => r.status === 'DRAFT').length;
  const finalized = rows.filter((r) => r.status === 'FINALIZED').length;
  const average =
    total > 0 ? rows.reduce((sum, r) => sum + r.averageScore, 0) / total : 0;

  return { total, draft, finalized, average };
});

// Selected Report Card Detail Query
const detailQuery = useReportCardDetailQuery(selectedReportCardId);

// Batch Draft Generation Mutation
const generateBatchMutation = useGenerateDraftReportCardsMutation();

async function handleConfirmBatchGenerate(payload: {
  studyGroupId: string;
  operationalPeriodId: string;
}): Promise<void> {
  try {
    await generateBatchMutation.mutateAsync(payload);
    isBatchModalOpen.value = false;
    selectedStudyGroupId.value = payload.studyGroupId;
    selectedPeriodId.value = payload.operationalPeriodId;
  } catch {
    // Error toast handled by mutation hook
  }
}

function handleOpenPrintPreview(reportCardId: string): void {
  selectedReportCardId.value = reportCardId;
  isPrintPreviewOpen.value = true;
}

const statusFilterOptions = [
  { label: 'All Statuses', value: 'ALL' },
  { label: 'Draft Only', value: 'DRAFT' },
  { label: 'Finalized Only', value: 'FINALIZED' }
];
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6" data-testid="report-cards-workbench">
      <!-- Page Header -->
      <div
        class="no-print flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"
      >
        <div>
          <h1 class="flex items-center gap-2.5 text-2xl font-bold tracking-tight text-slate-900">
            <GraduationCap class="h-6 w-6 text-brand-accent-600" />
            <span>Semester Report Cards</span>
          </h1>
          <p class="mt-1 text-sm text-slate-600">
            Collaborative report card review, subject teacher feedback, homeroom remarks,
            finalization, and official printing.
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <Button
            v-if="canGenerate && !isHistoricalPeriod"
            type="button"
            label="Generate Draft Cards"
            size="small"
            :disabled="!selectedPeriodId || batchModalStudyGroups.length === 0"
            data-testid="open-batch-generate-btn"
            @click="isBatchModalOpen = true"
          >
            <template #icon>
              <FilePlus2 class="mr-1.5 h-4 w-4" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Filter Bar -->
      <div
        class="no-print rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:p-5"
        data-testid="report-cards-filter-bar"
      >
        <div class="grid grid-cols-1 gap-4 md:grid-cols-4">
          <div class="space-y-1.5">
            <label
              for="rc-period-select"
              class="block text-xs font-semibold uppercase tracking-wider text-slate-600"
            >
              Academic Period
            </label>
            <Select
              id="rc-period-select"
              v-model="selectedPeriodId"
              :options="periodOptions"
              option-label="label"
              option-value="value"
              placeholder="Select Academic Period..."
              :loading="academicYearsQuery.isLoading.value"
              class="w-full text-sm"
              data-testid="period-selector"
            />
          </div>

          <div class="space-y-1.5">
            <label
              for="rc-study-group-select"
              class="block text-xs font-semibold uppercase tracking-wider text-slate-600"
            >
              Study Group
            </label>
            <Select
              id="rc-study-group-select"
              v-model="selectedStudyGroupId"
              :options="studyGroupOptions"
              option-label="label"
              option-value="value"
              placeholder="Select Study Group..."
              filter
              :loading="studyGroupsQuery.isLoading.value"
              class="w-full text-sm"
              data-testid="study-group-selector"
            />
          </div>

          <div class="space-y-1.5">
            <label
              for="rc-status-filter"
              class="block text-xs font-semibold uppercase tracking-wider text-slate-600"
            >
              Status
            </label>
            <Select
              id="rc-status-filter"
              v-model="statusFilter"
              :options="statusFilterOptions"
              option-label="label"
              option-value="value"
              class="w-full text-sm"
              data-testid="status-filter"
            />
          </div>

          <div class="space-y-1.5">
            <label
              for="rc-search-input"
              class="block text-xs font-semibold uppercase tracking-wider text-slate-600"
            >
              Search Student
            </label>
            <div class="relative">
              <Search
                class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
              />
              <InputText
                id="rc-search-input"
                v-model="searchQuery"
                placeholder="Name or NIS..."
                class="w-full pl-9 text-sm"
                data-testid="student-search-input"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- KPI Summary Strip -->
      <div
        class="no-print grid grid-cols-2 gap-4 sm:grid-cols-4"
        data-testid="report-cards-kpi-strip"
      >
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <p class="text-xs font-medium text-slate-500">Total Report Cards</p>
          <p class="mt-1 text-2xl font-bold text-slate-900">{{ kpiStats.total }}</p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <p class="text-xs font-medium text-slate-500">In Draft</p>
          <p class="mt-1 text-2xl font-bold text-amber-600">{{ kpiStats.draft }}</p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <p class="text-xs font-medium text-slate-500">Finalized</p>
          <p class="mt-1 text-2xl font-bold text-emerald-600">{{ kpiStats.finalized }}</p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <p class="text-xs font-medium text-slate-500">Average Score</p>
          <p class="mt-1 font-mono text-2xl font-bold text-slate-900">
            {{ kpiStats.average.toFixed(2) }}
          </p>
        </div>
      </div>

      <!-- Missing Period Empty State -->
      <EmptyState
        v-if="!selectedPeriodId"
        class="no-print"
        title="Select an Academic Period"
        description="Choose an operational semester period above to review or generate student report cards."
        :icon="FileText"
      />

      <!-- Missing Study Group Empty State (for teachers/homeroom teachers) -->
      <EmptyState
        v-else-if="!canViewOverview && !selectedStudyGroupId"
        class="no-print"
        title="Select a Study Group"
        description="Choose a study group to view student draft and finalized report cards."
        :icon="FileText"
      />

      <!-- Loading Roster -->
      <div
        v-else-if="isLoadingRoster"
        class="no-print flex items-center justify-center rounded-xl border border-slate-200 bg-white py-16"
      >
        <Loader2 class="h-6 w-6 animate-spin text-brand-accent-600" />
        <span class="ml-2 text-sm text-slate-600">Loading report cards...</span>
      </div>

      <!-- Empty Roster -->
      <EmptyState
        v-else-if="rosterRows.length === 0"
        class="no-print"
        title="No Report Cards Found"
        :description="
          canGenerate && !isHistoricalPeriod
            ? 'No semester report cards exist for this selection yet. Click Generate Draft Cards to initialize them from scorebook averages.'
            : 'No semester report cards have been generated for this selection yet.'
        "
        :icon="FileText"
      >
        <template v-if="canGenerate && !isHistoricalPeriod" #actions>
          <Button
            type="button"
            label="Generate Draft Cards"
            size="small"
            :disabled="batchModalStudyGroups.length === 0"
            @click="isBatchModalOpen = true"
          />
        </template>
      </EmptyState>

      <!-- Collaborative Work Queue: Roster + Selected Detail Workbench -->
      <div v-else class="no-print grid grid-cols-1 gap-6 xl:grid-cols-12">
        <!-- Left Worklist: Student Report Card Roster -->
        <div
          class="rounded-xl border border-slate-200 bg-white shadow-xs xl:col-span-5"
          data-testid="report-cards-roster"
        >
          <div class="flex items-center justify-between border-b border-slate-200 px-4 py-3">
            <h2 class="text-sm font-semibold text-slate-900">
              Student Roster ({{ filteredRosterRows.length }})
            </h2>
            <span class="text-xs text-slate-500">Select a student to review</span>
          </div>

          <div
            v-if="filteredRosterRows.length === 0"
            class="px-4 py-10 text-center text-sm text-slate-500"
          >
            No students match your current search or status filter.
          </div>

          <div v-else class="max-h-[680px] divide-y divide-slate-200 overflow-y-auto">
            <div
              v-for="row in filteredRosterRows"
              :key="row.id"
              role="button"
              tabindex="0"
              class="flex cursor-pointer items-center justify-between gap-3 px-4 py-3 transition-colors"
              :class="
                selectedReportCardId === row.id
                  ? 'bg-brand-accent-50/70 ring-1 ring-inset ring-brand-accent-500/40'
                  : 'hover:bg-slate-50'
              "
              :data-testid="`roster-row-${row.id}`"
              @click="selectedReportCardId = row.id"
              @keydown.enter="selectedReportCardId = row.id"
            >
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2">
                  <p class="truncate text-sm font-semibold text-slate-900">
                    {{ row.studentName }}
                  </p>
                  <StatusPill :status="row.status" />
                </div>
                <p class="mt-0.5 truncate text-xs text-slate-500">
                  NIS: {{ row.studentNis }} • {{ row.studyGroupName }}
                </p>
                <div class="mt-1.5 flex flex-wrap items-center gap-3 text-[11px] text-slate-600">
                  <span>
                    Avg:
                    <strong class="font-mono text-slate-900">{{
                      row.averageScore.toFixed(2)
                    }}</strong>
                  </span>
                  <span>
                    Notes:
                    <strong>{{ row.completedNotesCount }}/{{ row.totalSubjects }}</strong>
                  </span>
                  <span
                    class="inline-flex items-center gap-1"
                    :class="row.isHomeroomRemarksCompleted ? 'text-emerald-700' : 'text-slate-400'"
                  >
                    <CheckCircle2 class="h-3 w-3" />
                    <span>Remarks</span>
                  </span>
                </div>
              </div>

              <Button
                type="button"
                severity="secondary"
                text
                rounded
                size="small"
                aria-label="Print report card"
                :data-testid="`row-print-btn-${row.id}`"
                @click.stop="handleOpenPrintPreview(row.id)"
              >
                <template #icon>
                  <Printer class="h-4 w-4 text-slate-600" />
                </template>
              </Button>
            </div>
          </div>
        </div>

        <!-- Right Workbench: Selected Report Card Detail Panel -->
        <div class="xl:col-span-7">
          <ReportCardDetailPanel
            v-if="selectedReportCardId"
            :report-card="detailQuery.data.value ?? null"
            :loading="detailQuery.isLoading.value"
            :can-provide-notes="canProvideNotes"
            :can-write-remarks="canWriteRemarks"
            :can-finalize="canFinalize"
            :can-override="canOverride"
            :is-historical-period="isHistoricalPeriod"
            @print="handleOpenPrintPreview"
          />
          <EmptyState
            v-else
            title="Select a Report Card"
            description="Choose a student from the roster to review grades, edit feedback, or finalize."
            :icon="FileText"
          />
        </div>
      </div>

      <!-- Batch Draft Generation Pre-Flight Modal (UC-ADM-19) -->
      <BatchGenerateModal
        v-model:visible="isBatchModalOpen"
        :study-groups="batchModalStudyGroups"
        :initial-study-group-id="selectedStudyGroupId"
        :operational-period-id="selectedPeriodId"
        :operational-period-label="selectedPeriodOption?.label ?? 'Active Period'"
        :existing-cards-count="selectedStudyGroupId ? rosterRows.length : 0"
        :loading="generateBatchMutation.isPending.value"
        @confirm="handleConfirmBatchGenerate"
      />

      <!-- Official Browser Printable View -->
      <ReportCardPrintView
        v-if="isPrintPreviewOpen"
        :report-card="detailQuery.data.value ?? null"
        :loading="detailQuery.isLoading.value"
        @close="isPrintPreviewOpen = false"
      />
    </div>
  </AppErrorBoundary>
</template>
