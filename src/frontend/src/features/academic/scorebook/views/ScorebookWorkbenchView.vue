<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router';
import { useQuery, useQueryClient } from '@tanstack/vue-query';
import Button from 'primevue/button';
import {
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  FileSpreadsheet,
  Save,
  Undo2,
  Users
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { studyGroupsLookupQuery } from '@/shared/services/lookups.api';
import { curriculumApi } from '../../curriculum/api/curriculum.api';
import {
  scorebookApi,
  scorebookKeys,
  type GradebookAssignmentDto,
  type GradebookStudentRowDto
} from '../api/scorebook.api';
import AssessmentHeaderCard from '../components/AssessmentHeaderCard.vue';
import CreateAssessmentModal from '../components/CreateAssessmentModal.vue';
import ScorebookFilterBar from '../components/ScorebookFilterBar.vue';
import ScoreCellInput from '../components/ScoreCellInput.vue';

interface DirtyScoreEntry {
  studentId: string;
  assignmentId: string;
  score: number | null;
  feedback: string | null;
  originalScore: number | null;
  originalFeedback: string | null;
}

interface StudentRosterItem extends GradebookStudentRowDto {
  computedRunningScore: number | null;
}

const route = useRoute();
const router = useRouter();
const queryClient = useQueryClient();
const contextBridge = useAppContextBridge();
const session = useCurrentSession();

// Permissions
const canManage = computed(() =>
  session.hasPermission('Permissions.Academic.Scorebook.Manage')
);
const canView = computed(
  () =>
    session.hasPermission('Permissions.Academic.Scorebook.View') || canManage.value
);

// Selection State
const selectedSubjectStageId = ref<string | null>(
  (route.query.subjectStageId as string) || null
);
const selectedStudyGroupId = ref<string | null>(
  (route.query.studyGroupId as string) || null
);
const passingThreshold = ref<number>(
  route.query.threshold ? parseFloat(route.query.threshold as string) || 75 : 75
);

// Operational Context
const activeAcademicYearId = computed(() => contextBridge.selectedAcademicYearId.value);
const activePeriodId = computed(
  () => contextBridge.selectedPeriodId.value ?? contextBridge.activePeriodId.value
);

// Modals and Alerts
const isCreateModalOpen = ref(false);
const saveSuccessMessage = ref<string | null>(null);
const saveErrorMessage = ref<string | null>(null);
const isSaving = ref(false);

// Dirty State Tracking
const dirtyEntries = ref<Record<string, DirtyScoreEntry>>({});
const isDirty = computed(() => Object.keys(dirtyEntries.value).length > 0);
const dirtyCount = computed(() => Object.keys(dirtyEntries.value).length);

// Leave Confirmation Guard
const confirmLeaveVisible = ref(false);
const pendingNavigation = ref<(() => void) | null>(null);

// ----------------------------------------------------
// Lookups: Subjects & Stages
// ----------------------------------------------------
const subjectsQuery = useQuery({
  queryKey: ['acd', 'subjects', 'tree'],
  queryFn: () => curriculumApi.getSubjects(),
  staleTime: 5 * 60 * 1000
});
const subjects = computed(() => subjectsQuery.data.value ?? []);

const stageOptions = computed(() => {
  const options: Array<{
    id: string;
    label: string;
    subjectId: string;
    units?: Array<{ id?: string; name?: string | null }>;
  }> = [];

  for (const subject of subjects.value) {
    if (subject.stages && subject.stages.length > 0) {
      for (const stage of subject.stages) {
        if (stage.id) {
          options.push({
            id: stage.id,
            label: `${subject.name ?? 'Subject'} - ${stage.name ?? 'Stage'}`,
            subjectId: subject.id ?? '',
            units: stage.units ?? []
          });
        }
      }
    }
  }

  return options;
});

const currentSelectedStage = computed(() =>
  stageOptions.value.find((s) => s.id === selectedSubjectStageId.value)
);

const currentSubjectId = computed(() => currentSelectedStage.value?.subjectId ?? '');
const currentLearningUnits = computed(() => currentSelectedStage.value?.units ?? []);

// ----------------------------------------------------
// Lookups: Study Groups
// ----------------------------------------------------
const studyGroupsQuery = useQuery(
  studyGroupsLookupQuery({ academicYearId: activeAcademicYearId.value ?? undefined })
);
const studyGroups = computed(() => studyGroupsQuery.data.value ?? []);

const studyGroupOptions = computed(() =>
  studyGroups.value.map((sg) => ({ id: sg.id ?? '', name: sg.name ?? '' }))
);

// Auto-select first options if none selected
watch(
  [stageOptions, studyGroupOptions],
  ([stages, groups]) => {
    if (!selectedSubjectStageId.value && stages.length > 0 && stages[0]) {
      selectedSubjectStageId.value = stages[0].id;
    }
    if (!selectedStudyGroupId.value && groups.length > 0 && groups[0]) {
      selectedStudyGroupId.value = groups[0].id;
    }
  },
  { immediate: true }
);

// Sync query params
watch([selectedSubjectStageId, selectedStudyGroupId, passingThreshold], ([stageId, groupId, threshold]) => {
  const newQuery: Record<string, string> = {};
  for (const [k, v] of Object.entries(route.query)) {
    if (typeof v === 'string') {
      newQuery[k] = v;
    }
  }
  if (stageId) newQuery.subjectStageId = stageId;
  else delete newQuery.subjectStageId;

  if (groupId) newQuery.studyGroupId = groupId;
  else delete newQuery.studyGroupId;

  if (threshold !== 75) newQuery.threshold = String(threshold);
  else delete newQuery.threshold;

  if (
    route.query.subjectStageId !== newQuery.subjectStageId ||
    route.query.studyGroupId !== newQuery.studyGroupId ||
    route.query.threshold !== newQuery.threshold
  ) {
    void router.replace({ query: newQuery });
  }
});

// ----------------------------------------------------
// Gradebook Query
// ----------------------------------------------------
const isGradebookEnabled = computed(() =>
  Boolean(selectedSubjectStageId.value && selectedStudyGroupId.value && activePeriodId.value)
);

const {
  data: gradebook,
  isLoading: isGradebookLoading,
  isFetching: isGradebookFetching,
  refetch: refetchGradebook
} = useQuery({
  queryKey: computed(() =>
    scorebookKeys.gradebook({
      subjectStageId: selectedSubjectStageId.value ?? '',
      studyGroupId: selectedStudyGroupId.value ?? '',
      operationalPeriodId: activePeriodId.value ?? ''
    })
  ),
  queryFn: () =>
    scorebookApi.getGradebookSummary({
      subjectStageId: selectedSubjectStageId.value ?? '',
      studyGroupId: selectedStudyGroupId.value ?? '',
      operationalPeriodId: activePeriodId.value ?? ''
    }),
  enabled: isGradebookEnabled,
  staleTime: 60 * 1000
});

// Clear dirty entries when context filter changes
watch([selectedSubjectStageId, selectedStudyGroupId, activePeriodId], () => {
  dirtyEntries.value = {};
  saveSuccessMessage.value = null;
  saveErrorMessage.value = null;
});

// ----------------------------------------------------
// Cell Score & Dirty Tracking Helpers
// ----------------------------------------------------
function getCellKey(studentId: string, assignmentId: string): string {
  return `${studentId}:${assignmentId}`;
}

function getOriginalStudentScore(
  student: GradebookStudentRowDto,
  assignmentId: string
): { score: number | null; feedback: string | null } {
  const match = (student.scores ?? []).find((s) => s.assignmentId === assignmentId);
  return {
    score: match && match.score !== undefined ? match.score : null,
    feedback: match?.feedback ?? null
  };
}

function getEffectiveScore(student: GradebookStudentRowDto, assignmentId: string): number | null {
  const key = getCellKey(student.studentId ?? '', assignmentId);
  if (dirtyEntries.value[key] !== undefined) {
    return dirtyEntries.value[key]!.score;
  }
  const original = getOriginalStudentScore(student, assignmentId);
  return original.score;
}

function getEffectiveFeedback(student: GradebookStudentRowDto, assignmentId: string): string | null {
  const key = getCellKey(student.studentId ?? '', assignmentId);
  if (dirtyEntries.value[key] !== undefined) {
    return dirtyEntries.value[key]!.feedback;
  }
  const original = getOriginalStudentScore(student, assignmentId);
  return original.feedback;
}

function isCellDirty(studentId: string, assignmentId: string): boolean {
  return dirtyEntries.value[getCellKey(studentId, assignmentId)] !== undefined;
}

function updateCellScore(
  student: GradebookStudentRowDto,
  assignment: GradebookAssignmentDto,
  newScore: number | null
): void {
  const studentId = student.studentId ?? '';
  const assignmentId = assignment.id ?? '';
  const key = getCellKey(studentId, assignmentId);
  const orig = getOriginalStudentScore(student, assignmentId);
  const currentFeedback = getEffectiveFeedback(student, assignmentId);

  // Check if value is identical to original
  if (newScore === orig.score && currentFeedback === orig.feedback) {
    const updated = { ...dirtyEntries.value };
    delete updated[key];
    dirtyEntries.value = updated;
    return;
  }

  dirtyEntries.value = {
    ...dirtyEntries.value,
    [key]: {
      studentId,
      assignmentId,
      score: newScore,
      feedback: currentFeedback,
      originalScore: orig.score,
      originalFeedback: orig.feedback
    }
  };
}

function updateCellFeedback(
  student: GradebookStudentRowDto,
  assignment: GradebookAssignmentDto,
  newFeedback: string | null
): void {
  const studentId = student.studentId ?? '';
  const assignmentId = assignment.id ?? '';
  const key = getCellKey(studentId, assignmentId);
  const orig = getOriginalStudentScore(student, assignmentId);
  const currentScore = getEffectiveScore(student, assignmentId);

  if (newFeedback === orig.feedback && currentScore === orig.score) {
    const updated = { ...dirtyEntries.value };
    delete updated[key];
    dirtyEntries.value = updated;
    return;
  }

  dirtyEntries.value = {
    ...dirtyEntries.value,
    [key]: {
      studentId,
      assignmentId,
      score: currentScore,
      feedback: newFeedback,
      originalScore: orig.score,
      originalFeedback: orig.feedback
    }
  };
}

// ----------------------------------------------------
// Reactive Running Score Computation (D-05, D-18)
// ----------------------------------------------------
const computedStudentsRoster = computed<StudentRosterItem[]>(() => {
  const baseStudents = gradebook.value?.students ?? [];
  const assignments = gradebook.value?.assignments ?? [];

  if (assignments.length === 0) {
    return baseStudents.map((s) => ({
      ...s,
      computedRunningScore: s.runningScore ?? null
    }));
  }

  const hasNullWeight = assignments.some((a) => a.weight === null || a.weight === undefined);

  return baseStudents.map((student) => {
    let runningScore: number | null = null;
    let scoresEnteredCount = 0;
    let totalScoreSum = 0;
    let totalWeightedScore = 0;
    let totalWeight = 0;

    for (const assignment of assignments) {
      if (!assignment.id) continue;
      const score = getEffectiveScore(student, assignment.id);
      if (score !== null && score !== undefined && !isNaN(score)) {
        scoresEnteredCount++;
        totalScoreSum += score;
        const w = assignment.weight ?? 0;
        totalWeightedScore += score * w;
        totalWeight += w;
      }
    }

    if (scoresEnteredCount > 0) {
      if (hasNullWeight) {
        runningScore = Math.round((totalScoreSum / scoresEnteredCount) * 100) / 100;
      } else {
        runningScore =
          totalWeight > 0 ? Math.round((totalWeightedScore / totalWeight) * 100) / 100 : null;
      }
    }

    return {
      ...student,
      computedRunningScore: runningScore
    };
  });
});

// ----------------------------------------------------
// Spreadsheet Keyboard Navigation
// ----------------------------------------------------
function handleCellNavigate(
  direction: 'up' | 'down' | 'left' | 'right',
  rowIndex: number,
  colIndex: number
): void {
  const totalRows = computedStudentsRoster.value.length;
  const totalCols = (gradebook.value?.assignments ?? []).length;

  let targetRow = rowIndex;
  let targetCol = colIndex;

  if (direction === 'down') {
    targetRow = rowIndex + 1;
  } else if (direction === 'up') {
    targetRow = rowIndex - 1;
  } else if (direction === 'right') {
    targetCol = colIndex + 1;
  } else if (direction === 'left') {
    targetCol = colIndex - 1;
  }

  if (targetRow >= 0 && targetRow < totalRows && targetCol >= 0 && targetCol < totalCols) {
    void nextTick(() => {
      const selector = `input[data-row-index="${targetRow}"][data-col-index="${targetCol}"]`;
      const targetInput = document.querySelector<HTMLInputElement>(selector);
      if (targetInput) {
        targetInput.focus();
        targetInput.select();
      }
    });
  }
}

// ----------------------------------------------------
// Batch Save Scores
// ----------------------------------------------------
async function handleSaveScores(): Promise<void> {
  if (!isDirty.value || isSaving.value) return;

  saveErrorMessage.value = null;
  saveSuccessMessage.value = null;
  isSaving.value = true;

  try {
    // Group dirty entries by assignmentId
    const byAssignment: Record<string, Array<{ studentId: string; score: number; feedback: string | null }>> = {};

    for (const entry of Object.values(dirtyEntries.value)) {
      if (entry.score !== null) {
        if (!byAssignment[entry.assignmentId]) {
          byAssignment[entry.assignmentId] = [];
        }
        byAssignment[entry.assignmentId]!.push({
          studentId: entry.studentId,
          score: entry.score,
          feedback: entry.feedback
        });
      }
    }

    const assignmentIds = Object.keys(byAssignment);
    if (assignmentIds.length === 0) {
      dirtyEntries.value = {};
      return;
    }

    // Dispatch parallel saves
    await Promise.all(
      assignmentIds.map((asgId) =>
        scorebookApi.saveScores(asgId, {
          items: byAssignment[asgId]!
        })
      )
    );

    dirtyEntries.value = {};
    saveSuccessMessage.value = 'All student scores saved successfully.';
    await queryClient.invalidateQueries({
      queryKey: scorebookKeys.gradebook({
        subjectStageId: selectedSubjectStageId.value ?? '',
        studyGroupId: selectedStudyGroupId.value ?? '',
        operationalPeriodId: activePeriodId.value ?? ''
      })
    });
  } catch (err: unknown) {
    saveErrorMessage.value =
      err instanceof Error
        ? err.message
        : 'Failed to save scores. Please review errors and try again.';
  } finally {
    isSaving.value = false;
  }
}

function handleDiscardChanges(): void {
  dirtyEntries.value = {};
  saveSuccessMessage.value = null;
  saveErrorMessage.value = null;
}

// ----------------------------------------------------
// Assessment Creation Callback
// ----------------------------------------------------
function handleAssessmentCreated(): void {
  saveSuccessMessage.value = 'Assessment created successfully.';
  void queryClient.invalidateQueries({
    queryKey: scorebookKeys.gradebook({
      subjectStageId: selectedSubjectStageId.value ?? '',
      studyGroupId: selectedStudyGroupId.value ?? '',
      operationalPeriodId: activePeriodId.value ?? ''
    })
  });
}

// ----------------------------------------------------
// Route Navigation Guard (D-08, D-16)
// ----------------------------------------------------
onBeforeRouteLeave((_to, _from, next) => {
  if (isDirty.value) {
    pendingNavigation.value = () => next();
    confirmLeaveVisible.value = true;
    next(false);
  } else {
    next();
  }
});

function confirmLeave(): void {
  dirtyEntries.value = {};
  confirmLeaveVisible.value = false;
  if (pendingNavigation.value) {
    pendingNavigation.value();
    pendingNavigation.value = null;
  }
}

function cancelLeave(): void {
  confirmLeaveVisible.value = false;
  pendingNavigation.value = null;
}

// Window unload confirmation
function onBeforeUnload(e: BeforeUnloadEvent): void {
  if (isDirty.value) {
    e.preventDefault();
    e.returnValue = '';
  }
}

onMounted(() => {
  window.addEventListener('beforeunload', onBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', onBeforeUnload);
});
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6 pb-20" data-testid="scorebook-workbench-view">
      <!-- Breadcrumb -->
      <nav aria-label="Breadcrumb" class="flex items-center gap-1.5 text-xs text-slate-500">
        <span>Faculty &amp; Teaching</span>
        <span aria-hidden="true">/</span>
        <span class="font-medium text-slate-800">Teacher Scorebook</span>
      </nav>

      <!-- Page Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <FileSpreadsheet class="h-5 w-5 text-brand-accent-600" />
            <h1 class="text-xl font-bold tracking-tight text-slate-900">
              Teacher Scorebook
            </h1>
          </div>
          <p class="text-sm text-slate-500">
            Formative and summative assessments, spreadsheet score entry, and consolidated gradebook summaries.
          </p>
        </div>

        <div class="flex items-center gap-2">
          <span
            v-if="contextBridge.activePeriodName.value"
            class="inline-flex items-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 shadow-2xs"
            data-testid="scorebook-period-badge"
          >
            Period: {{ contextBridge.activePeriodName.value }}
          </span>
        </div>
      </div>

      <!-- Context Filter Bar -->
      <ScorebookFilterBar
        v-model:subject-stage-id="selectedSubjectStageId"
        v-model:study-group-id="selectedStudyGroupId"
        v-model:threshold="passingThreshold"
        :stage-options="stageOptions"
        :study-group-options="studyGroupOptions"
        :can-manage="canManage"
        :is-loading="isGradebookLoading || isGradebookFetching"
        :is-dirty="isDirty"
        @create-assessment="isCreateModalOpen = true"
        @refresh="refetchGradebook"
      />

      <!-- Notifications -->
      <div
        v-if="saveSuccessMessage"
        class="flex items-center justify-between rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-xs text-emerald-900"
        role="status"
        data-testid="scorebook-success-alert"
      >
        <div class="flex items-center gap-2">
          <CheckCircle2 class="h-4 w-4 shrink-0 text-emerald-600" />
          <span>{{ saveSuccessMessage }}</span>
        </div>
        <button
          type="button"
          class="text-emerald-700 hover:text-emerald-900 text-xs font-semibold"
          @click="saveSuccessMessage = null"
        >
          Dismiss
        </button>
      </div>

      <div
        v-if="saveErrorMessage"
        class="flex items-center justify-between rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-xs text-rose-900"
        role="alert"
        data-testid="scorebook-error-alert"
      >
        <div class="flex items-center gap-2">
          <AlertCircle class="h-4 w-4 shrink-0 text-rose-600" />
          <span>{{ saveErrorMessage }}</span>
        </div>
        <button
          type="button"
          class="text-rose-700 hover:text-rose-900 text-xs font-semibold"
          @click="saveErrorMessage = null"
        >
          Dismiss
        </button>
      </div>

      <!-- Permission Warning -->
      <div
        v-if="!canView"
        class="rounded-xl border border-rose-200 bg-rose-50 p-6 text-center text-xs text-rose-800"
        data-testid="scorebook-forbidden-alert"
      >
        You do not have permission to view or manage the academic scorebook.
      </div>

      <!-- Empty Selection State -->
      <div
        v-else-if="!selectedSubjectStageId || !selectedStudyGroupId"
        class="rounded-xl border border-dashed border-slate-300 bg-white p-12 text-center"
        data-testid="scorebook-no-selection"
      >
        <EmptyState
          title="Select Subject Stage &amp; Study Group"
          description="Choose a subject stage and study group from the filters above to load the scorebook grid."
        >
          <template #icon>
            <Users class="h-10 w-10 text-slate-400" />
          </template>
        </EmptyState>
      </div>

      <!-- Loading State -->
      <div
        v-else-if="isGradebookLoading"
        class="flex flex-col items-center justify-center rounded-xl border border-slate-200 bg-white p-16"
        data-testid="scorebook-loading"
      >
        <div class="h-8 w-8 animate-spin rounded-full border-2 border-brand-accent-600 border-t-transparent" />
        <p class="mt-3 text-xs text-slate-500">Loading gradebook summary &amp; assessments...</p>
      </div>

      <!-- Gradebook Content -->
      <div v-else class="space-y-4">
        <!-- Assessment Overview Bar -->
        <div class="flex flex-wrap items-center justify-between gap-3 text-xs text-slate-600 px-1">
          <div class="flex items-center gap-4">
            <span class="font-medium text-slate-900" data-testid="scorebook-roster-count">
              {{ computedStudentsRoster.length }} Students Enrolled
            </span>
            <span class="text-slate-300">|</span>
            <span data-testid="scorebook-assessment-count">
              {{ (gradebook?.assignments ?? []).length }} Assessment Column(s)
            </span>
          </div>

          <div class="flex items-center gap-3 text-[11px]">
            <span class="flex items-center gap-1.5">
              <span class="h-2 w-2 rounded-full bg-rose-500" />
              <span>Below KKM (&lt; {{ passingThreshold }})</span>
            </span>
            <span class="flex items-center gap-1.5">
              <span class="h-2 w-2 rounded-full bg-amber-500" />
              <span>Unsaved Edits</span>
            </span>
          </div>
        </div>

        <!-- Spreadsheet Grid Container (Tier 1 Direct Edit — D-07) -->
        <div
          class="relative overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-2xs"
          data-testid="scorebook-grid-container"
        >
          <table class="w-full border-collapse text-left text-xs" data-testid="scorebook-table">
            <thead>
              <tr class="border-b border-slate-200 bg-slate-50/80 font-medium text-slate-700">
                <!-- Sticky Left Identifier Columns -->
                <th
                  scope="col"
                  class="sticky left-0 z-20 w-12 bg-slate-50 px-3 py-3 text-center border-r border-slate-200"
                >
                  #
                </th>
                <th
                  scope="col"
                  class="sticky left-12 z-20 w-28 bg-slate-50 px-3 py-3 border-r border-slate-200"
                >
                  NIS
                </th>
                <th
                  scope="col"
                  class="sticky left-40 z-20 min-w-44 bg-slate-50 px-4 py-3 border-r border-slate-300 shadow-xs"
                >
                  Student Name
                </th>

                <!-- Dynamic Assessment Columns -->
                <th
                  v-for="asg in (gradebook?.assignments ?? [])"
                  :key="asg.id"
                  scope="col"
                  class="min-w-40 border-r border-slate-200 p-0 font-normal align-top"
                  :data-testid="`asg-col-${asg.id}`"
                >
                  <AssessmentHeaderCard :assignment="asg" />
                </th>

                <!-- Empty Assessment Column Prompt -->
                <th
                  v-if="(gradebook?.assignments ?? []).length === 0"
                  scope="col"
                  class="min-w-64 border-r border-slate-200 px-4 py-3 font-normal text-slate-400 italic text-center"
                >
                  No assessments created yet
                </th>

                <!-- Summary Projection Columns (D-20) -->
                <th
                  scope="col"
                  class="sticky right-52 z-20 w-32 bg-slate-50/90 px-3 py-3 text-center border-l border-slate-300 shadow-xs"
                >
                  <div class="font-semibold text-slate-900">Running Score</div>
                  <div class="text-[10px] text-slate-500 font-normal">
                    {{ (gradebook?.assignments ?? []).some((a) => a.weight === null || a.weight === undefined) ? 'Average' : 'Weighted' }}
                  </div>
                </th>

                <th
                  scope="col"
                  class="sticky right-0 z-20 w-52 bg-slate-50 px-3 py-3 text-center border-l border-slate-200"
                >
                  <div class="font-semibold text-slate-900">Attendance</div>
                  <div class="text-[10px] text-slate-500 font-normal">P / A / L / E / S</div>
                </th>
              </tr>
            </thead>

            <tbody class="divide-y divide-slate-100">
              <tr
                v-for="(student, sIndex) in computedStudentsRoster"
                :key="student.studentId"
                class="transition-colors hover:bg-slate-50/70"
                :data-testid="`student-row-${student.studentId}`"
              >
                <!-- Row # -->
                <td
                  class="sticky left-0 z-10 w-12 bg-white px-3 py-2 text-center text-slate-400 border-r border-slate-200 group-hover:bg-slate-50"
                >
                  {{ sIndex + 1 }}
                </td>

                <!-- NIS -->
                <td
                  class="sticky left-12 z-10 w-28 bg-white px-3 py-2 font-mono text-slate-600 border-r border-slate-200 group-hover:bg-slate-50"
                  data-testid="student-nis"
                >
                  {{ student.studentNis ?? '-' }}
                </td>

                <!-- Student Name -->
                <td
                  class="sticky left-40 z-10 min-w-44 bg-white px-4 py-2 font-medium text-slate-900 border-r border-slate-300 shadow-xs group-hover:bg-slate-50"
                  data-testid="student-name"
                >
                  {{ student.studentName }}
                </td>

                <!-- Assessment Score Cells -->
                <td
                  v-for="(asg, aIndex) in (gradebook?.assignments ?? [])"
                  :key="asg.id"
                  class="border-r border-slate-200 p-1 align-middle"
                >
                  <ScoreCellInput
                    :student-id="student.studentId ?? ''"
                    :student-name="student.studentName ?? ''"
                    :assignment-id="asg.id ?? ''"
                    :assignment-title="asg.title ?? ''"
                    :max-score="asg.maxScore ?? 100"
                    :model-value="getEffectiveScore(student, asg.id ?? '')"
                    :feedback="getEffectiveFeedback(student, asg.id ?? '')"
                    :is-dirty="isCellDirty(student.studentId ?? '', asg.id ?? '')"
                    :threshold="passingThreshold"
                    :row-index="sIndex"
                    :col-index="aIndex"
                    :disabled="!canManage"
                    @update:model-value="updateCellScore(student, asg, $event)"
                    @update:feedback="updateCellFeedback(student, asg, $event)"
                    @navigate="handleCellNavigate"
                  />
                </td>

                <!-- Empty Assessment Column Cell -->
                <td
                  v-if="(gradebook?.assignments ?? []).length === 0"
                  class="border-r border-slate-200 px-4 py-3 text-center text-slate-400"
                >
                  -
                </td>

                <!-- Running Score Column -->
                <td
                  class="sticky right-52 z-10 w-32 bg-white px-3 py-2 text-center border-l border-slate-300 shadow-xs"
                  data-testid="running-score-cell"
                >
                  <span
                    v-if="student.computedRunningScore !== null && student.computedRunningScore !== undefined"
                    class="inline-block rounded px-2 py-0.5 font-mono text-xs font-semibold"
                    :class="[
                      student.computedRunningScore < passingThreshold
                        ? 'bg-rose-50 text-rose-700 font-bold border border-rose-200'
                        : 'text-slate-900'
                    ]"
                    data-testid="running-score-value"
                  >
                    {{ student.computedRunningScore.toFixed(2) }}
                  </span>
                  <span v-else class="text-slate-400 font-mono" data-testid="running-score-empty">
                    -
                  </span>
                </td>

                <!-- Attendance Summary Column -->
                <td
                  class="sticky right-0 z-10 w-52 bg-white px-3 py-2 text-center border-l border-slate-200"
                  data-testid="attendance-metrics-cell"
                >
                  <div class="flex items-center justify-center gap-1.5 font-mono text-[11px]">
                    <span
                      class="rounded bg-emerald-50 px-1.5 py-0.5 text-emerald-700 font-medium"
                      title="Present count"
                      data-testid="attendance-present"
                    >
                      P:{{ student.attendance?.presentCount ?? 0 }}
                    </span>
                    <span
                      class="rounded bg-rose-50 px-1.5 py-0.5 text-rose-700 font-medium"
                      title="Absent count"
                      data-testid="attendance-absent"
                    >
                      A:{{ student.attendance?.absentCount ?? 0 }}
                    </span>
                    <span
                      class="rounded bg-amber-50 px-1.5 py-0.5 text-amber-700 font-medium"
                      title="Late count"
                      data-testid="attendance-late"
                    >
                      L:{{ student.attendance?.lateCount ?? 0 }}
                    </span>
                    <span
                      class="rounded bg-sky-50 px-1.5 py-0.5 text-sky-700 font-medium"
                      title="Excused count"
                      data-testid="attendance-excused"
                    >
                      E:{{ student.attendance?.excusedCount ?? 0 }}
                    </span>
                    <span
                      class="rounded bg-yellow-50 px-1.5 py-0.5 text-yellow-800 font-medium"
                      title="Sick count"
                      data-testid="attendance-sick"
                    >
                      S:{{ student.attendance?.sickCount ?? 0 }}
                    </span>
                  </div>
                </td>
              </tr>

              <!-- Empty Student Roster -->
              <tr v-if="computedStudentsRoster.length === 0">
                <td
                  :colspan="3 + Math.max(1, (gradebook?.assignments ?? []).length) + 2"
                  class="p-8 text-center text-slate-500"
                  data-testid="scorebook-empty-roster"
                >
                  No active students enrolled in this subject stage and study group.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Sticky Bottom Save Bar (Tier 1 Direct Edit — D-07) -->
      <transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="transform translate-y-full opacity-0"
        enter-to-class="transform translate-y-0 opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="transform translate-y-0 opacity-100"
        leave-to-class="transform translate-y-full opacity-0"
      >
        <div
          v-if="isDirty"
          class="fixed bottom-0 left-0 right-0 z-40 border-t border-amber-300 bg-amber-50/95 px-6 py-3.5 shadow-xl backdrop-blur-xs flex items-center justify-between"
          data-testid="scorebook-sticky-save-bar"
        >
          <div class="flex items-center gap-2.5 text-xs text-amber-900">
            <AlertTriangle class="h-4 w-4 text-amber-600 shrink-0" />
            <span class="font-medium">
              You have <strong data-testid="dirty-count">{{ dirtyCount }}</strong> uncommitted score change(s).
            </span>
          </div>

          <div class="flex items-center gap-2">
            <Button
              label="Discard Changes"
              severity="secondary"
              text
              size="small"
              :disabled="isSaving"
              data-testid="scorebook-discard-btn"
              @click="handleDiscardChanges"
            >
              <template #icon>
                <Undo2 class="h-3.5 w-3.5 mr-1" />
              </template>
            </Button>
            <Button
              label="Save Changes"
              severity="primary"
              size="small"
              :loading="isSaving"
              data-testid="scorebook-save-btn"
              @click="handleSaveScores"
            >
              <template #icon>
                <Save class="h-3.5 w-3.5 mr-1" />
              </template>
            </Button>
          </div>
        </div>
      </transition>

      <!-- Tier 2 Assessment Creation Modal -->
      <CreateAssessmentModal
        v-model:visible="isCreateModalOpen"
        :subject-stage-id="selectedSubjectStageId ?? ''"
        :subject-id="currentSubjectId"
        :study-group-id="selectedStudyGroupId ?? ''"
        :operational-period-id="activePeriodId ?? ''"
        :learning-units="currentLearningUnits"
        @created="handleAssessmentCreated"
      />

      <!-- Navigation Guard Confirmation Dialog -->
      <ConfirmActionDialog
        v-model:visible="confirmLeaveVisible"
        title="Unsaved Score Changes"
        message="You have uncommitted score changes in this scorebook. If you leave now, your changes will be discarded. Are you sure you want to leave?"
        confirm-label="Discard &amp; Leave"
        cancel-label="Stay Here"
        severity="danger"
        @confirm="confirmLeave"
        @cancel="cancelLeave"
      />
    </div>
  </AppErrorBoundary>
</template>
