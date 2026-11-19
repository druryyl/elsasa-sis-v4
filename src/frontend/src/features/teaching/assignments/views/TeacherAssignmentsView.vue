<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import type { DataTablePageEvent } from 'primevue/datatable';
import { useQuery } from '@tanstack/vue-query';
import {
  BookOpen,
  BookOpenCheck,
  Calendar,
  GraduationCap,
  Layers,
  Pencil,
  Plus,
  Power,
  RotateCcw,
  Search,
  Trash2,
  UserCheck,
  Users
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useCurrentSession } from '@/shared/services/authSession';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import {
  academicYearsLookupQuery,
  activeTeachersLookupQuery,
  studyGroupsLookupQuery,
  subjectsLookupQuery
} from '@/shared/services/lookups.api';
import type { TeacherAssignmentDto } from '../api/assignments.api';
import type { TeacherDto } from '../api/teachers.api';
import {
  useAssignmentsQuery,
  useWorkloadQuery
} from '../api/useAssignmentQueries';
import { useRemoveAssignmentMutation } from '../api/useAssignmentMutations';
import {
  useActivateTeacherMutation,
  useDeactivateTeacherMutation
} from '../api/useTeacherMutations';
import { useTeachersQuery } from '../api/useTeacherQueries';
import AssignTeacherDrawer from '../components/AssignTeacherDrawer.vue';
import TeacherCapabilitiesDrawer from '../components/TeacherCapabilitiesDrawer.vue';
import TeacherFormDrawer from '../components/TeacherFormDrawer.vue';

const session = useCurrentSession();
const { activePeriodId, selectedPeriodId, activeAcademicYearId } = useAppContextBridge();

// Roles & Permissions
const canManageDirectory = computed(() =>
  session.hasPermission('Permissions.Teaching.Directory.Manage')
);
const canManageCapabilities = computed(() =>
  session.hasPermission('Permissions.Teaching.Capabilities.Manage')
);
const canManageAssignments = computed(() =>
  session.hasPermission('Permissions.Teaching.Assignments.Manage')
);
const canViewOversight = computed(() =>
  session.hasPermission('Permissions.Teaching.Assignments.Oversight')
);

// Active Tab: 'directory' | 'allocations'
const activeTab = ref<'directory' | 'allocations'>('directory');

// ==========================================
// TAB 1: FACULTY DIRECTORY STATE & QUERIES
// ==========================================
const directoryPage = ref(1);
const directoryPageSize = ref(10);
const directorySearch = ref('');
const directoryStatus = ref('');
const directorySubjectId = ref('');

const STATUS_OPTIONS = [
  { label: 'All Statuses', value: '' },
  { label: 'Active', value: 'ACTIVE' },
  { label: 'Inactive', value: 'INACTIVE' }
];

const directoryQueryParams = computed(() => ({
  page: directoryPage.value,
  pageSize: directoryPageSize.value,
  search: directorySearch.value.trim() || undefined,
  status: directoryStatus.value || undefined,
  subjectId: directorySubjectId.value || undefined
}));

const {
  data: teachersData,
  isLoading: isTeachersLoading,
  refetch: refetchTeachers
} = useTeachersQuery(directoryQueryParams);

const teachers = computed(() => teachersData.value?.items ?? []);
const totalTeachers = computed(() => teachersData.value?.totalCount ?? 0);
const firstTeacherIndex = computed(
  () => (directoryPage.value - 1) * directoryPageSize.value
);

function handleDirectoryPageChange(event: DataTablePageEvent): void {
  directoryPage.value = event.page + 1;
  directoryPageSize.value = event.rows;
}

function handleDirectorySearch(value: string | undefined): void {
  directorySearch.value = value ?? '';
  directoryPage.value = 1;
}

// Drawer: Teacher Form
const isTeacherFormVisible = ref(false);
const editingTeacher = ref<TeacherDto | null>(null);

function openCreateTeacher(): void {
  editingTeacher.value = null;
  isTeacherFormVisible.value = true;
}

function openEditTeacher(teacher: TeacherDto): void {
  editingTeacher.value = teacher;
  isTeacherFormVisible.value = true;
}

// Drawer: Capabilities
const isCapabilitiesVisible = ref(false);
const capabilitiesTeacher = ref<TeacherDto | null>(null);

function openCapabilitiesDrawer(teacher: TeacherDto): void {
  capabilitiesTeacher.value = teacher;
  isCapabilitiesVisible.value = true;
}

// Dialog: Deactivate / Activate Teacher (BR-TCH-007)
const activateTeacherMutation = useActivateTeacherMutation();
const deactivateTeacherMutation = useDeactivateTeacherMutation();

const isTeacherStatusDialogVisible = ref(false);
const teacherForStatusChange = ref<TeacherDto | null>(null);

function openStatusDialog(teacher: TeacherDto): void {
  teacherForStatusChange.value = teacher;
  isTeacherStatusDialogVisible.value = true;
}

const isTeacherStatusLoading = computed(
  () =>
    activateTeacherMutation.isPending.value ||
    deactivateTeacherMutation.isPending.value
);

const isCurrentTeacherActive = computed(
  () => teacherForStatusChange.value?.status === 'ACTIVE'
);

const teacherStatusTitle = computed(() =>
  isCurrentTeacherActive.value
    ? 'Deactivate Teacher Profile'
    : 'Activate Teacher Profile'
);

const teacherStatusMessage = computed(() => {
  const name = teacherForStatusChange.value?.fullName ?? 'this teacher';
  if (isCurrentTeacherActive.value) {
    return `Deactivating ${name} will mark their profile as inactive. Note that any existing teaching assignments will remain active and must be manually reassigned or removed if needed (per BR-TCH-007). Continue?`;
  }
  return `Activating ${name} will allow them to be assigned to classes again. Continue?`;
});

async function handleTeacherStatusConfirm(): Promise<void> {
  const teacher = teacherForStatusChange.value;
  if (!teacher?.id) return;

  if (teacher.status === 'ACTIVE') {
    await deactivateTeacherMutation.mutateAsync(teacher.id);
  } else {
    await activateTeacherMutation.mutateAsync(teacher.id);
  }
  isTeacherStatusDialogVisible.value = false;
  teacherForStatusChange.value = null;
}

// ==========================================
// TAB 2: ALLOCATIONS & WORKLOAD STATE & QUERIES
// ==========================================
// Operational Periods from active academic year
const { data: academicYears } = useQuery({
  ...academicYearsLookupQuery()
});

const operationalPeriods = computed(() => {
  const currentYear = (academicYears.value ?? []).find(
    (y) => y.id === activeAcademicYearId.value || y.status === 'ACTIVE'
  );
  if (currentYear?.periods && currentYear.periods.length > 0) {
    return currentYear.periods;
  }
  return (academicYears.value ?? []).flatMap((y) => y.periods ?? []);
});

const selectedPeriodIdFilter = ref<string>('');

watch(
  [selectedPeriodId, activePeriodId, operationalPeriods],
  ([sel, act, periods]) => {
    if (!selectedPeriodIdFilter.value) {
      selectedPeriodIdFilter.value = sel || act || periods[0]?.id || '';
    }
  },
  { immediate: true }
);

// Lookups for Allocation Filters
const { data: allSubjects } = useQuery(subjectsLookupQuery());
const { data: allStudyGroups } = useQuery(
  studyGroupsLookupQuery({ academicYearId: activeAcademicYearId.value ?? undefined })
);
const { data: activeTeachersList } = useQuery(activeTeachersLookupQuery());

const filterStudyGroupId = ref('');
const filterTeacherId = ref('');
const filterSubjectId = ref('');
const assignmentSearch = ref('');

const allocationsPage = ref(1);
const allocationsPageSize = ref(10);

const assignmentsQueryParams = computed(() => ({
  operationalPeriodId: selectedPeriodIdFilter.value || undefined,
  studyGroupId: filterStudyGroupId.value || undefined,
  teacherId: filterTeacherId.value || undefined,
  subjectId: filterSubjectId.value || undefined,
  search: assignmentSearch.value.trim() || undefined,
  page: allocationsPage.value,
  pageSize: allocationsPageSize.value
}));

const {
  data: assignmentsData,
  isLoading: isAssignmentsLoading,
  refetch: refetchAssignments
} = useAssignmentsQuery(assignmentsQueryParams, {
  enabled: computed(() => activeTab.value === 'allocations')
});

const assignments = computed(() => assignmentsData.value?.items ?? []);
const totalAssignments = computed(() => assignmentsData.value?.totalCount ?? 0);
const firstAssignmentIndex = computed(
  () => (allocationsPage.value - 1) * allocationsPageSize.value
);

function handleAllocationsPageChange(event: DataTablePageEvent): void {
  allocationsPage.value = event.page + 1;
  allocationsPageSize.value = event.rows;
}

function handleAssignmentSearch(value: string | undefined): void {
  assignmentSearch.value = value ?? '';
  allocationsPage.value = 1;
}

// Workload Leadership Oversight Query (UC-PRC-05)
const workloadQueryParams = computed(() => ({
  operationalPeriodId: selectedPeriodIdFilter.value || undefined
}));

const { data: workloadData, isLoading: isWorkloadLoading } = useWorkloadQuery(
  workloadQueryParams,
  {
    enabled: computed(() => activeTab.value === 'allocations' && canViewOversight.value)
  }
);

// Drawer: Assign Teacher
const isAssignDrawerVisible = ref(false);

function openAssignDrawer(): void {
  isAssignDrawerVisible.value = true;
}

// Dialog: Remove Assignment (Tier 2 Confirm)
const removeAssignmentMutation = useRemoveAssignmentMutation();
const isRemoveAssignmentDialogVisible = ref(false);
const assignmentToRemove = ref<TeacherAssignmentDto | null>(null);

function openRemoveAssignmentDialog(assignment: TeacherAssignmentDto): void {
  assignmentToRemove.value = assignment;
  isRemoveAssignmentDialogVisible.value = true;
}

const removeAssignmentMessage = computed(() => {
  const teacher = assignmentToRemove.value?.teacherName ?? 'Teacher';
  const group = assignmentToRemove.value?.studyGroupName ?? 'Study Group';
  const subject = assignmentToRemove.value?.subjectName ?? 'Subject';
  return `Are you sure you want to remove the teaching assignment of ${teacher} to ${group} (${subject})? Continue?`;
});

async function handleRemoveAssignmentConfirm(): Promise<void> {
  const assignment = assignmentToRemove.value;
  if (!assignment?.id) return;

  await removeAssignmentMutation.mutateAsync(assignment.id);
  isRemoveAssignmentDialogVisible.value = false;
  assignmentToRemove.value = null;
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6">
      <!-- Top Screen Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <BookOpenCheck class="h-5 w-5 text-brand-accent-600" />
            <h1
              class="text-xl font-semibold tracking-tight text-slate-900"
              data-testid="teaching-header-title"
            >
              Faculty Directory &amp; Teaching Assignments
            </h1>
          </div>
          <p class="text-sm text-slate-600">
            Maintain faculty profiles, qualified subject portfolios, study group teaching
            allocations, and workload distribution.
          </p>
        </div>

        <div v-if="canManageDirectory" class="flex items-center gap-2">
          <Button
            type="button"
            label="Register Teacher"
            data-testid="create-teacher-button"
            @click="openCreateTeacher"
          >
            <template #icon>
              <Plus class="mr-1.5 h-4 w-4" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Navigation Tabs -->
      <div class="border-b border-slate-200">
        <nav class="-mb-px flex space-x-6">
          <button
            type="button"
            :class="[
              activeTab === 'directory'
                ? 'border-brand-accent-600 text-brand-accent-600 font-semibold'
                : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700',
              'whitespace-nowrap border-b-2 py-3 px-1 text-sm flex items-center gap-2 transition-colors'
            ]"
            data-testid="tab-faculty-directory"
            @click="activeTab = 'directory'"
          >
            <Users class="h-4 w-4" />
            Faculty Directory
          </button>
          <button
            type="button"
            :class="[
              activeTab === 'allocations'
                ? 'border-brand-accent-600 text-brand-accent-600 font-semibold'
                : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700',
              'whitespace-nowrap border-b-2 py-3 px-1 text-sm flex items-center gap-2 transition-colors'
            ]"
            data-testid="tab-teaching-allocations"
            @click="activeTab = 'allocations'"
          >
            <Layers class="h-4 w-4" />
            Teaching Allocations &amp; Workload Oversight
          </button>
        </nav>
      </div>

      <!-- ========================================== -->
      <!-- TAB 1: FACULTY DIRECTORY                   -->
      <!-- ========================================== -->
      <div v-if="activeTab === 'directory'" class="space-y-4" data-testid="tab-content-directory">
        <!-- Toolbar & Filters -->
        <div class="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center sm:justify-between">
          <div class="relative w-full max-w-sm">
            <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <InputText
              :model-value="directorySearch"
              placeholder="Search faculty by name or NIP..."
              class="w-full pl-9 text-sm"
              data-testid="teacher-search-input"
              @update:model-value="handleDirectorySearch"
            />
          </div>

          <div class="flex flex-wrap items-center gap-2">
            <!-- Subject Filter -->
            <Select
              v-model="directorySubjectId"
              :options="allSubjects ?? []"
              option-label="name"
              option-value="id"
              placeholder="All Subjects"
              show-clear
              class="w-48 text-sm"
              data-testid="directory-subject-filter"
              @change="directoryPage = 1"
            />

            <!-- Status Filter -->
            <Select
              v-model="directoryStatus"
              :options="STATUS_OPTIONS"
              option-label="label"
              option-value="value"
              placeholder="All Statuses"
              class="w-36 text-sm"
              data-testid="directory-status-filter"
              @change="directoryPage = 1"
            />
          </div>
        </div>

        <!-- Faculty Data Table -->
        <AppDataTable
          :value="teachers"
          :loading="isTeachersLoading"
          :paginator="true"
          :lazy="true"
          :rows="directoryPageSize"
          :first="firstTeacherIndex"
          :total-records="totalTeachers"
          data-key="id"
          empty-title="No faculty members found"
          empty-description="Register a new teacher or adjust your search filters."
          data-testid="faculty-table"
          @page="handleDirectoryPageChange"
        >
          <!-- Faculty Identity -->
          <Column header="Faculty Member">
            <template #body="{ data }">
              <div class="space-y-0.5">
                <div class="flex items-center gap-2">
                  <span class="font-medium text-slate-900" :data-testid="`teacher-name-${data.id}`">
                    {{ data.fullName }}
                  </span>
                  <span
                    v-if="data.nickname"
                    class="rounded-md bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600"
                  >
                    {{ data.nickname }}
                  </span>
                </div>
                <div class="flex items-center gap-3 text-xs text-slate-500">
                  <span v-if="data.nip">NIP: {{ data.nip }}</span>
                  <span v-if="data.nik">NIK: {{ data.nik }}</span>
                </div>
              </div>
            </template>
          </Column>

          <!-- Contact Details (BR-TCH-002) -->
          <Column header="Contact">
            <template #body="{ data }">
              <div class="space-y-0.5 text-xs text-slate-700">
                <p v-if="data.phone" class="font-mono text-slate-800">{{ data.phone }}</p>
                <p v-if="data.email" class="text-slate-500 truncate max-w-[200px]">
                  {{ data.email }}
                </p>
                <p v-if="!data.phone && !data.email" class="text-slate-400 italic">No contact</p>
              </div>
            </template>
          </Column>

          <!-- Status -->
          <Column header="Status" style="width: 110px">
            <template #body="{ data }">
              <StatusPill :status="data.status ?? 'ACTIVE'" />
            </template>
          </Column>

          <!-- Capabilities -->
          <Column header="Subject Capabilities">
            <template #body="{ data }">
              <div class="flex flex-wrap items-center gap-1">
                <template v-if="data.capabilities && data.capabilities.length > 0">
                  <span
                    v-for="(cap, idx) in data.capabilities.slice(0, 3)"
                    :key="cap.subjectId || idx"
                    class="inline-flex items-center rounded-md bg-brand-accent-50 border border-brand-accent-200 px-2 py-0.5 text-xs font-medium text-brand-accent-700"
                  >
                    {{ cap.subjectName }}
                  </span>
                  <span
                    v-if="data.capabilities.length > 3"
                    class="inline-flex items-center rounded-md bg-slate-100 px-1.5 py-0.5 text-xs text-slate-600"
                  >
                    +{{ data.capabilities.length - 3 }} more
                  </span>
                </template>
                <span v-else class="text-xs text-slate-400 italic">None recorded</span>
              </div>
            </template>
          </Column>

          <!-- Active Load Count -->
          <Column header="Active Classes" style="width: 130px">
            <template #body="{ data }">
              <span
                class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="
                  data.activeAssignmentCount && data.activeAssignmentCount > 0
                    ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                    : 'bg-slate-50 text-slate-500 border border-slate-200'
                "
              >
                {{ data.activeAssignmentCount ?? 0 }} rombel
              </span>
            </template>
          </Column>

          <!-- Actions -->
          <Column header="Actions" style="width: 140px" align-frozen="right">
            <template #body="{ data }">
              <div class="flex items-center gap-1">
                <!-- Manage Capabilities Button -->
                <Button
                  type="button"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  title="Manage Capabilities"
                  :data-testid="`manage-capabilities-${data.id}`"
                  @click="openCapabilitiesDrawer(data)"
                >
                  <template #icon>
                    <BookOpen class="h-4 w-4 text-brand-accent-600" />
                  </template>
                </Button>

                <!-- Edit Profile Button -->
                <Button
                  v-if="canManageDirectory"
                  type="button"
                  severity="secondary"
                  text
                  rounded
                  size="small"
                  title="Edit Profile"
                  :data-testid="`edit-teacher-${data.id}`"
                  @click="openEditTeacher(data)"
                >
                  <template #icon>
                    <Pencil class="h-4 w-4 text-slate-600" />
                  </template>
                </Button>

                <!-- Deactivate / Activate Button -->
                <Button
                  v-if="canManageDirectory"
                  type="button"
                  :severity="data.status === 'ACTIVE' ? 'warn' : 'success'"
                  text
                  rounded
                  size="small"
                  :title="data.status === 'ACTIVE' ? 'Deactivate Teacher' : 'Activate Teacher'"
                  :data-testid="`toggle-status-${data.id}`"
                  @click="openStatusDialog(data)"
                >
                  <template #icon>
                    <Power
                      v-if="data.status === 'ACTIVE'"
                      class="h-4 w-4 text-amber-600"
                    />
                    <RotateCcw v-else class="h-4 w-4 text-emerald-600" />
                  </template>
                </Button>
              </div>
            </template>
          </Column>
        </AppDataTable>
      </div>

      <!-- ======================================================== -->
      <!-- TAB 2: TEACHING ALLOCATIONS & WORKLOAD OVERSIGHT         -->
      <!-- ======================================================== -->
      <div v-else class="space-y-6" data-testid="tab-content-allocations">
        <!-- Top Operational Period Context Bar -->
        <div class="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center sm:justify-between">
          <div class="flex items-center gap-2">
            <Calendar class="h-4 w-4 text-slate-500" />
            <span class="text-sm font-semibold text-slate-800">Operational Period Context:</span>
            <Select
              v-model="selectedPeriodIdFilter"
              :options="operationalPeriods"
              option-label="name"
              option-value="id"
              placeholder="Select Operational Period"
              class="w-56 text-sm"
              data-testid="allocations-period-selector"
            />
          </div>

          <div v-if="canManageAssignments" class="flex items-center gap-2">
            <Button
              type="button"
              label="Assign Teacher"
              data-testid="assign-teacher-button"
              @click="openAssignDrawer"
            >
              <template #icon>
                <Plus class="mr-1.5 h-4 w-4" />
              </template>
            </Button>
          </div>
        </div>

        <!-- Section 1: Leadership Workload Oversight (UC-PRC-05, Read-Only for Principal) -->
        <div
          v-if="canViewOversight"
          class="rounded-xl border border-slate-200 bg-white p-5 space-y-5 shadow-xs"
          data-testid="workload-oversight-section"
        >
          <div class="flex items-center justify-between border-b border-slate-100 pb-3">
            <div>
              <h2 class="text-base font-semibold text-slate-900" data-testid="workload-title">
                Leadership Workload Distribution Oversight
              </h2>
              <p class="text-xs text-slate-500">
                Institutional distribution of teaching assignments across staff for
                {{ workloadData?.operationalPeriodName || 'selected period' }}.
              </p>
            </div>
            <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-700">
              Read-Only Leadership View
            </span>
          </div>

          <!-- Workload KPI Cards -->
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6" data-testid="workload-kpis">
            <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-3 text-center">
              <span class="text-xs font-medium text-slate-500">Total Faculty</span>
              <p class="mt-1 text-xl font-bold text-slate-900" data-testid="kpi-total-teachers">
                {{ workloadData?.totalTeachers ?? 0 }}
              </p>
            </div>

            <div class="rounded-lg border border-emerald-200 bg-emerald-50/50 p-3 text-center">
              <span class="text-xs font-medium text-emerald-700">Active Faculty</span>
              <p class="mt-1 text-xl font-bold text-emerald-800" data-testid="kpi-active-teachers">
                {{ workloadData?.activeTeachersCount ?? 0 }}
              </p>
            </div>

            <div class="rounded-lg border border-blue-200 bg-blue-50/50 p-3 text-center">
              <span class="text-xs font-medium text-blue-700">Assigned Staff</span>
              <p class="mt-1 text-xl font-bold text-blue-800" data-testid="kpi-assigned-teachers">
                {{ workloadData?.assignedTeachersCount ?? 0 }}
              </p>
            </div>

            <div class="rounded-lg border border-amber-200 bg-amber-50/50 p-3 text-center">
              <span class="text-xs font-medium text-amber-700">Unassigned Staff</span>
              <p class="mt-1 text-xl font-bold text-amber-800" data-testid="kpi-unassigned-teachers">
                {{ workloadData?.unassignedTeachersCount ?? 0 }}
              </p>
            </div>

            <div class="rounded-lg border border-purple-200 bg-purple-50/50 p-3 text-center">
              <span class="text-xs font-medium text-purple-700">Total Allocations</span>
              <p class="mt-1 text-xl font-bold text-purple-800" data-testid="kpi-total-assignments">
                {{ workloadData?.totalAssignments ?? 0 }}
              </p>
            </div>

            <div class="rounded-lg border border-teal-200 bg-teal-50/50 p-3 text-center">
              <span class="text-xs font-medium text-teal-700">Study Groups</span>
              <p class="mt-1 text-xl font-bold text-teal-800" data-testid="kpi-distinct-groups">
                {{ workloadData?.distinctStudyGroupsCount ?? 0 }}
              </p>
            </div>
          </div>

          <!-- Per-Teacher Workload Matrix Table -->
          <div class="space-y-2">
            <h3 class="text-xs font-semibold uppercase tracking-wider text-slate-700">
              Staff Load Breakdown
            </h3>
            <AppDataTable
              :value="workloadData?.teachers ?? []"
              :loading="isWorkloadLoading"
              data-key="teacherId"
              empty-title="No workload data available"
              empty-description="Assign teachers to classes to populate leadership oversight metrics."
              data-testid="workload-breakdown-table"
            >
              <Column header="Teacher">
                <template #body="{ data }">
                  <div class="font-medium text-slate-900">{{ data.fullName }}</div>
                  <div class="text-xs text-slate-500 font-mono">{{ data.nip || 'No NIP' }}</div>
                </template>
              </Column>

              <Column header="Status" style="width: 100px">
                <template #body="{ data }">
                  <StatusPill :status="data.status ?? 'ACTIVE'" />
                </template>
              </Column>

              <Column header="Total Allocations" style="width: 140px">
                <template #body="{ data }">
                  <span class="font-semibold text-slate-800">{{ data.totalAssignments ?? 0 }}</span>
                  <span class="text-xs text-slate-500 ml-1">classes</span>
                </template>
              </Column>

              <Column header="Distinct Classes" style="width: 130px">
                <template #body="{ data }">
                  <span>{{ data.distinctStudyGroupCount ?? 0 }} rombel</span>
                </template>
              </Column>

              <Column header="Distinct Subjects" style="width: 130px">
                <template #body="{ data }">
                  <span>{{ data.distinctSubjectCount ?? 0 }} subjects</span>
                </template>
              </Column>

              <Column header="Assigned Classes &amp; Subjects">
                <template #body="{ data }">
                  <div class="flex flex-wrap gap-1">
                    <template v-if="data.assignments && data.assignments.length > 0">
                      <span
                        v-for="item in data.assignments"
                        :key="item.assignmentId"
                        class="inline-flex items-center rounded-md bg-slate-100 border border-slate-200 px-2 py-0.5 text-xs text-slate-700"
                      >
                        {{ item.studyGroupName }}: {{ item.subjectName }}
                      </span>
                    </template>
                    <span v-else class="text-xs text-slate-400 italic">No classes assigned</span>
                  </div>
                </template>
              </Column>
            </AppDataTable>
          </div>
        </div>

        <!-- Section 2: Teaching Assignment Registry / Table -->
        <div class="space-y-4">
          <div class="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center sm:justify-between">
            <div class="relative w-full max-w-sm">
              <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <InputText
                :model-value="assignmentSearch"
                placeholder="Search assignments..."
                class="w-full pl-9 text-sm"
                data-testid="assignment-search-input"
                @update:model-value="handleAssignmentSearch"
              />
            </div>

            <div class="flex flex-wrap items-center gap-2">
              <!-- Study Group Filter -->
              <Select
                v-model="filterStudyGroupId"
                :options="allStudyGroups ?? []"
                option-label="name"
                option-value="id"
                placeholder="All Study Groups"
                show-clear
                class="w-44 text-sm"
                data-testid="allocations-studygroup-filter"
                @change="allocationsPage = 1"
              />

              <!-- Subject Filter -->
              <Select
                v-model="filterSubjectId"
                :options="allSubjects ?? []"
                option-label="name"
                option-value="id"
                placeholder="All Subjects"
                show-clear
                class="w-44 text-sm"
                data-testid="allocations-subject-filter"
                @change="allocationsPage = 1"
              />

              <!-- Teacher Filter -->
              <Select
                v-model="filterTeacherId"
                :options="activeTeachersList ?? []"
                option-label="fullName"
                option-value="id"
                placeholder="All Teachers"
                show-clear
                class="w-48 text-sm"
                data-testid="allocations-teacher-filter"
                @change="allocationsPage = 1"
              />
            </div>
          </div>

          <!-- Assignments Data Table -->
          <AppDataTable
            :value="assignments"
            :loading="isAssignmentsLoading"
            :paginator="true"
            :lazy="true"
            :rows="allocationsPageSize"
            :first="firstAssignmentIndex"
            :total-records="totalAssignments"
            data-key="id"
            empty-title="No teaching assignments found"
            empty-description="Assign an active faculty member to a study group and subject for this period."
            data-testid="assignments-table"
            @page="handleAllocationsPageChange"
          >
            <!-- Study Group -->
            <Column header="Study Group (Rombel)" style="width: 160px">
              <template #body="{ data }">
                <span class="font-semibold text-slate-900" :data-testid="`assignment-group-${data.id}`">
                  {{ data.studyGroupName }}
                </span>
              </template>
            </Column>

            <!-- Subject -->
            <Column header="Subject">
              <template #body="{ data }">
                <div class="flex items-center gap-2">
                  <GraduationCap class="h-4 w-4 text-brand-accent-600" />
                  <span class="font-medium text-slate-800" :data-testid="`assignment-subject-${data.id}`">
                    {{ data.subjectName }}
                  </span>
                </div>
              </template>
            </Column>

            <!-- Teacher -->
            <Column header="Assigned Teacher">
              <template #body="{ data }">
                <div class="flex items-center gap-2">
                  <UserCheck class="h-4 w-4 text-emerald-600" />
                  <span class="font-medium text-slate-900" :data-testid="`assignment-teacher-${data.id}`">
                    {{ data.teacherName }}
                  </span>
                </div>
              </template>
            </Column>

            <!-- Period Name -->
            <Column header="Period" style="width: 160px">
              <template #body="{ data }">
                <span class="text-xs text-slate-600">{{ data.operationalPeriodName }}</span>
              </template>
            </Column>

            <!-- Assigned Date -->
            <Column header="Assigned On" style="width: 140px">
              <template #body="{ data }">
                <span class="text-xs text-slate-500">
                  {{ formatDisplayDate(data.createdAt) }}
                </span>
              </template>
            </Column>

            <!-- Actions (Remove Assignment) -->
            <Column header="Actions" style="width: 90px" align-frozen="right">
              <template #body="{ data }">
                <Button
                  v-if="canManageAssignments"
                  type="button"
                  severity="danger"
                  text
                  rounded
                  size="small"
                  title="Remove Assignment"
                  :data-testid="`remove-assignment-${data.id}`"
                  @click="openRemoveAssignmentDialog(data)"
                >
                  <template #icon>
                    <Trash2 class="h-4 w-4 text-rose-500" />
                  </template>
                </Button>
              </template>
            </Column>
          </AppDataTable>
        </div>
      </div>

      <!-- ========================================== -->
      <!-- DRAWERS & DIALOGS                          -->
      <!-- ========================================== -->

      <!-- Teacher Create / Edit Drawer -->
      <TeacherFormDrawer
        v-model:visible="isTeacherFormVisible"
        :teacher="editingTeacher"
        @saved="() => refetchTeachers()"
      />

      <!-- Teacher Capabilities Drawer -->
      <TeacherCapabilitiesDrawer
        v-model:visible="isCapabilitiesVisible"
        :teacher="capabilitiesTeacher"
        :can-manage="canManageCapabilities"
        @updated="() => refetchTeachers()"
      />

      <!-- Assign Teacher Drawer -->
      <AssignTeacherDrawer
        v-model:visible="isAssignDrawerVisible"
        :default-operational-period-id="selectedPeriodIdFilter"
        @saved="() => { refetchAssignments(); refetchTeachers(); }"
      />

      <!-- Tier 2 Confirm: Deactivate / Activate Teacher (BR-TCH-007) -->
      <ConfirmActionDialog
        v-model:visible="isTeacherStatusDialogVisible"
        :title="teacherStatusTitle"
        :message="teacherStatusMessage"
        :severity="isCurrentTeacherActive ? 'warn' : 'primary'"
        :confirm-label="isCurrentTeacherActive ? 'Deactivate' : 'Activate'"
        :loading="isTeacherStatusLoading"
        data-testid="teacher-status-confirm-dialog"
        @confirm="handleTeacherStatusConfirm"
        @cancel="teacherForStatusChange = null"
      />

      <!-- Tier 2 Confirm: Remove Assignment -->
      <ConfirmActionDialog
        v-model:visible="isRemoveAssignmentDialogVisible"
        title="Remove Teaching Assignment"
        :message="removeAssignmentMessage"
        severity="danger"
        confirm-label="Remove Assignment"
        :loading="removeAssignmentMutation.isPending.value"
        data-testid="remove-assignment-confirm-dialog"
        @confirm="handleRemoveAssignmentConfirm"
        @cancel="assignmentToRemove = null"
      />
    </div>
  </AppErrorBoundary>
</template>
