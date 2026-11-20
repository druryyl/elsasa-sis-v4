<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router';
import { ClipboardCheck, Loader2 } from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { formatBackendDateOnly } from '@/shared/utils/dateUtils';
import { useAttendanceSheetQuery } from '../api/useAttendanceQueries';
import { useRecordAttendanceMutation } from '../api/useAttendanceMutations';
import AttendanceFilterBar from '../components/AttendanceFilterBar.vue';
import AttendanceActionBar from '../components/AttendanceActionBar.vue';
import AttendanceRosterGrid, {
  type AttendanceRosterItem,
  type AttendanceStatusCode
} from '../components/AttendanceRosterGrid.vue';
import AttendanceStickyBar from '../components/AttendanceStickyBar.vue';

const route = useRoute();
const router = useRouter();
const session = useCurrentSession();
const contextBridge = useAppContextBridge();

// Permissions
const canRecord = computed(() =>
  session.hasPermission('Permissions.Academic.Attendance.Record')
);

// Active / Selected Operational Context
const operationalPeriodId = computed(
  () => contextBridge.selectedPeriodId.value ?? contextBridge.activePeriodId.value ?? ''
);
const academicYearId = computed(
  () => contextBridge.selectedAcademicYearId.value ?? contextBridge.activeAcademicYearId.value ?? null
);
const isHistoricalPeriod = computed(() => contextBridge.isHistoricalPeriod.value);
const canEditAttendance = computed(() => canRecord.value && !isHistoricalPeriod.value);

// Session Filter State
const selectedSubjectId = ref<string>((route.query.subjectId as string) || '');
const selectedStudyGroupId = ref<string>((route.query.studyGroupId as string) || '');
const selectedDate = ref<string>(
  (route.query.date as string) || formatBackendDateOnly(new Date())
);

// Pending filter changes when dirty
const pendingFilterChange = ref<{
  subjectId?: string;
  studyGroupId?: string;
  date?: string;
} | null>(null);
const showDiscardFilterConfirm = ref(false);

// Local Working Roster & Baseline Snapshot
const baselineRoster = ref<AttendanceRosterItem[]>([]);
const workingRoster = ref<AttendanceRosterItem[]>([]);

// Query for Subject Attendance Sheet
const sheetParams = computed(() => ({
  subjectId: selectedSubjectId.value,
  studyGroupId: selectedStudyGroupId.value,
  attendanceDate: selectedDate.value,
  operationalPeriodId: operationalPeriodId.value
}));

const hasRequiredFilters = computed(
  () =>
    Boolean(
      selectedSubjectId.value &&
        selectedStudyGroupId.value &&
        selectedDate.value &&
        operationalPeriodId.value
    )
);

const {
  data: sheetData,
  isLoading: isLoadingSheet,
  isFetching: isFetchingSheet
} = useAttendanceSheetQuery(sheetParams, {
  enabled: hasRequiredFilters
});

// Mutation
const recordMutation = useRecordAttendanceMutation();

// Sync Query Data into Local Roster
watch(
  sheetData,
  (newSheet) => {
    if (!newSheet?.items) {
      baselineRoster.value = [];
      workingRoster.value = [];
      return;
    }

    const items: AttendanceRosterItem[] = newSheet.items.map((item) => ({
      studentId: item.studentId ?? '',
      studentName: item.studentName ?? 'Unnamed Student',
      studentNis: item.studentNis ?? '',
      attendanceRecordId: item.attendanceRecordId ?? null,
      status: (item.status as AttendanceStatusCode) || null,
      notes: item.notes ?? ''
    }));

    baselineRoster.value = items.map((i) => ({ ...i }));
    workingRoster.value = items.map((i) => ({ ...i }));
  },
  { immediate: true }
);

// Sync URL Query
watch([selectedSubjectId, selectedStudyGroupId, selectedDate], ([subId, grpId, date]) => {
  const newQuery: Record<string, string | (string | null)[] | null | undefined> = { ...route.query };

  if (subId) newQuery.subjectId = subId;
  else delete newQuery.subjectId;

  if (grpId) newQuery.studyGroupId = grpId;
  else delete newQuery.studyGroupId;

  if (date) newQuery.date = date;
  else delete newQuery.date;

  if (
    route.query.subjectId !== newQuery.subjectId ||
    route.query.studyGroupId !== newQuery.studyGroupId ||
    route.query.date !== newQuery.date
  ) {
    void router.replace({ query: newQuery });
  }
});

// Row Dirty Checking
function isRowModified(current: AttendanceRosterItem, baseline?: AttendanceRosterItem): boolean {
  if (!baseline) return true;
  const statusChanged = (current.status ?? null) !== (baseline.status ?? null);
  const notesChanged = current.notes.trim() !== (baseline.notes ?? '').trim();
  return statusChanged || notesChanged;
}

const rosterItemsWithDirty = computed<AttendanceRosterItem[]>(() => {
  const baselineMap = new Map<string, AttendanceRosterItem>(
    baselineRoster.value.map((i) => [i.studentId, i])
  );

  return workingRoster.value.map((item) => ({
    ...item,
    isDirty: isRowModified(item, baselineMap.get(item.studentId))
  }));
});

// Dirty State & Modified Count
const modifiedCount = computed(
  () => rosterItemsWithDirty.value.filter((i) => i.isDirty).length
);
const isDirty = computed(() => modifiedCount.value > 0);

// Status Tallies for Action Bar
const presentCount = computed(
  () => workingRoster.value.filter((i) => i.status === 'PRESENT').length
);
const absentCount = computed(
  () => workingRoster.value.filter((i) => i.status === 'ABSENT').length
);
const excusedCount = computed(
  () => workingRoster.value.filter((i) => i.status === 'EXCUSED').length
);
const sickCount = computed(
  () => workingRoster.value.filter((i) => i.status === 'SICK').length
);
const lateCount = computed(
  () => workingRoster.value.filter((i) => i.status === 'LATE').length
);
const unmarkedCount = computed(
  () => workingRoster.value.filter((i) => !i.status).length
);

// Actions: Roster Edits
function handleUpdateStatus(studentId: string, status: AttendanceStatusCode) {
  if (!canEditAttendance.value) return;
  const target = workingRoster.value.find((i) => i.studentId === studentId);
  if (target) {
    target.status = status;
  }
}

function handleUpdateNotes(studentId: string, notes: string) {
  if (!canEditAttendance.value) return;
  const target = workingRoster.value.find((i) => i.studentId === studentId);
  if (target) {
    target.notes = notes;
  }
}

function handleMarkAllPresent() {
  if (!canEditAttendance.value || workingRoster.value.length === 0) return;
  workingRoster.value.forEach((item) => {
    item.status = 'PRESENT';
  });
}

// Discard / Reset
function handleDiscardChanges() {
  workingRoster.value = baselineRoster.value.map((i) => ({ ...i }));
}

// Save Changes Batch Dispatch
async function handleSaveChanges() {
  if (!hasRequiredFilters.value || !isDirty.value) return;

  const itemsToCommit = workingRoster.value
    .filter((i) => i.status !== null)
    .map((i) => ({
      studentId: i.studentId,
      status: i.status!,
      notes: i.notes.trim() || null
    }));

  if (itemsToCommit.length === 0) {
    return;
  }

  await recordMutation.mutateAsync({
    subjectId: selectedSubjectId.value,
    studyGroupId: selectedStudyGroupId.value,
    attendanceDate: selectedDate.value,
    operationalPeriodId: operationalPeriodId.value,
    items: itemsToCommit
  });

  // Update baseline snapshot to working state to clear dirty state
  baselineRoster.value = workingRoster.value.map((i) => ({ ...i }));
}

// Filter change interception when dirty
function tryUpdateSubject(newSubId: string) {
  if (isDirty.value) {
    pendingFilterChange.value = { subjectId: newSubId };
    showDiscardFilterConfirm.value = true;
  } else {
    selectedSubjectId.value = newSubId;
  }
}

function tryUpdateStudyGroup(newGrpId: string) {
  if (isDirty.value) {
    pendingFilterChange.value = { studyGroupId: newGrpId };
    showDiscardFilterConfirm.value = true;
  } else {
    selectedStudyGroupId.value = newGrpId;
  }
}

function tryUpdateDate(newDate: string) {
  if (isDirty.value) {
    pendingFilterChange.value = { date: newDate };
    showDiscardFilterConfirm.value = true;
  } else {
    selectedDate.value = newDate;
  }
}

function handleConfirmFilterDiscard() {
  handleDiscardChanges();
  if (pendingFilterChange.value?.subjectId !== undefined) {
    selectedSubjectId.value = pendingFilterChange.value.subjectId;
  }
  if (pendingFilterChange.value?.studyGroupId !== undefined) {
    selectedStudyGroupId.value = pendingFilterChange.value.studyGroupId;
  }
  if (pendingFilterChange.value?.date !== undefined) {
    selectedDate.value = pendingFilterChange.value.date;
  }
  pendingFilterChange.value = null;
  showDiscardFilterConfirm.value = false;
}

function handleCancelFilterDiscard() {
  pendingFilterChange.value = null;
  showDiscardFilterConfirm.value = false;
}

// Navigation Exit Guard (Vue Router)
onBeforeRouteLeave((_to, _from) => {
  if (isDirty.value) {
    const answer = window.confirm(
      'You have unsaved attendance changes. Are you sure you want to leave this page?'
    );
    if (!answer) return false;
  }
});

// Browser unload guard (window beforeunload)
function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (isDirty.value) {
    event.preventDefault();
    event.returnValue = '';
  }
}

onMounted(() => {
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onUnmounted(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
</script>

<template>
  <AppErrorBoundary>
    <div
      class="space-y-6 pb-24"
      data-testid="attendance-workbench-view"
    >
      <!-- Breadcrumb Navigation -->
      <nav aria-label="Breadcrumb" class="flex items-center gap-1.5 text-xs text-slate-500">
        <span>Faculty &amp; Teaching</span>
        <span aria-hidden="true">/</span>
        <span class="font-medium text-slate-800">Classroom Attendance</span>
      </nav>

      <!-- Page Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <ClipboardCheck class="h-6 w-6 text-brand-accent-600" />
            <h1 class="text-xl font-bold tracking-tight text-slate-900">
              Classroom Attendance
            </h1>
          </div>
          <p class="text-sm text-slate-500">
            Daily subject-level attendance roster, quick-mark shortcuts, and status entry.
          </p>
        </div>

        <!-- Academic Context Tag -->
        <div
          v-if="contextBridge.currentContextLabel.value"
          class="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-medium text-slate-700"
          data-testid="context-tag"
        >
          <span>{{ contextBridge.currentContextLabel.value }}</span>
        </div>
      </div>

      <!-- Historical Period Warning Banner -->
      <div
        v-if="isHistoricalPeriod"
        class="rounded-lg border border-amber-200 bg-amber-50 p-4 text-xs sm:text-sm text-amber-900"
        role="alert"
        data-testid="historical-period-banner"
      >
        <span class="font-semibold">Notice:</span> You are viewing historical attendance records.
        Modifications and saves are disabled for closed academic periods.
      </div>

      <!-- 1. Session Filter Bar -->
      <AttendanceFilterBar
        :selected-subject-id="selectedSubjectId"
        :selected-study-group-id="selectedStudyGroupId"
        :selected-date="selectedDate"
        :academic-year-id="academicYearId"
        @update:selected-subject-id="tryUpdateSubject"
        @update:selected-study-group-id="tryUpdateStudyGroup"
        @update:selected-date="tryUpdateDate"
      />

      <!-- Content Area -->
      <div class="space-y-4">
        <!-- Missing Filter State -->
        <EmptyState
          v-if="!hasRequiredFilters"
          title="Select Class Session"
          description="Choose a Subject, Study Group, and Date from the filter bar above to load the attendance roster."
          data-testid="missing-filters-empty-state"
        />

        <!-- Loading State -->
        <div
          v-else-if="isLoadingSheet"
          class="flex flex-col items-center justify-center rounded-xl border border-slate-200 bg-white py-16 text-center"
          data-testid="loading-state"
        >
          <Loader2 class="h-8 w-8 animate-spin text-brand-accent-600 mb-2" />
          <p class="text-sm font-medium text-slate-700">Loading attendance sheet...</p>
          <p class="text-xs text-slate-400">Fetching enrolled student roster</p>
        </div>

        <!-- No Enrolled Students State -->
        <EmptyState
          v-else-if="workingRoster.length === 0"
          title="No Students Enrolled"
          description="There are no active student participations found for this subject and study group."
          data-testid="no-students-empty-state"
        />

        <!-- Loaded Attendance Workbench Sheet -->
        <div v-else class="space-y-4">
          <!-- 2. Quick-Mark Action Bar -->
          <AttendanceActionBar
            :roster-length="workingRoster.length"
            :present-count="presentCount"
            :absent-count="absentCount"
            :excused-count="excusedCount"
            :sick-count="sickCount"
            :late-count="lateCount"
            :unmarked-count="unmarkedCount"
            :can-record="canEditAttendance"
            :disabled="recordMutation.isPending.value || isFetchingSheet"
            @mark-all-present="handleMarkAllPresent"
          />

          <!-- 3. Student Attendance Roster Grid -->
          <AttendanceRosterGrid
            :items="rosterItemsWithDirty"
            :can-record="canEditAttendance"
            :disabled="recordMutation.isPending.value"
            @update-status="handleUpdateStatus"
            @update-notes="handleUpdateNotes"
          />
        </div>
      </div>

      <!-- 4. High-Velocity Sticky Bottom Save Bar -->
      <AttendanceStickyBar
        :visible="isDirty && canEditAttendance"
        :modified-count="modifiedCount"
        :saving="recordMutation.isPending.value"
        @save="handleSaveChanges"
        @discard="handleDiscardChanges"
      />

      <!-- Discard Filter Confirmation Dialog -->
      <ConfirmActionDialog
        :visible="showDiscardFilterConfirm"
        title="Discard Unsaved Changes?"
        message="You have unsaved attendance changes for this session. Switching sessions will discard your edits. Do you want to continue?"
        confirm-label="Discard Changes"
        cancel-label="Keep Editing"
        severity="warn"
        @update:visible="showDiscardFilterConfirm = $event"
        @confirm="handleConfirmFilterDiscard"
        @cancel="handleCancelFilterDiscard"
      />
    </div>
  </AppErrorBoundary>
</template>
