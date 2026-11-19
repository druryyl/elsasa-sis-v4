<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import type { DataTablePageEvent, DataTableSortEvent } from 'primevue/datatable';
import {
  Phone,
  School,
  Search,
  ShieldCheck,
  UserCheck,
  Users
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import {
  fetchAcademicYearsLookup,
  fetchStudyGroupsLookup,
  type AcademicYearLookupItem,
  type StudyGroupLookupItem
} from '@/shared/services/lookups.api';
import type { StudentListItemDto } from '../api/students.api';
import type { StudyGroupListItemDto } from '../api/studyGroups.api';
import {
  useStudentsQuery,
  useStudyGroupRosterQuery,
  useStudyGroupsQuery
} from '../api/useStudentQueries';
import StudentDossierDrawer from '../components/StudentDossierDrawer.vue';
import StudyGroupFormDrawer from '../components/StudyGroupFormDrawer.vue';
import StudyGroupRosterDrawer from '../components/StudyGroupRosterDrawer.vue';
import PlaceStudentModal from '../components/PlaceStudentModal.vue';

const route = useRoute();
const { activeAcademicYearId } = useAppContextBridge();
const { hasPermission } = useCurrentSession();

// Permission checks
const PERMISSION_DIRECTORY_VIEW = 'Permissions.Students.Directory.View';
const PERMISSION_DIRECTORY_MANAGE = 'Permissions.Students.Directory.Manage';
const PERMISSION_PLACEMENTS_MANAGE = 'Permissions.Students.Placements.Manage';
const PERMISSION_ROSTER_VIEW_OWN = 'Permissions.Students.Roster.ViewOwn';

const canViewDirectory = computed(() => hasPermission(PERMISSION_DIRECTORY_VIEW));
const canManageDirectory = computed(() => hasPermission(PERMISSION_DIRECTORY_MANAGE));
const canManagePlacements = computed(() => hasPermission(PERMISSION_PLACEMENTS_MANAGE));
const canViewOwnRoster = computed(() => hasPermission(PERMISSION_ROSTER_VIEW_OWN));

// Homeroom Teacher Scoped Mode: has Roster.ViewOwn without Directory.View
const isHomeroomScopedMode = computed(
  () => canViewOwnRoster.value && !canViewDirectory.value
);

// Active Tab in full directory mode
const activeMainTab = ref('roster');

// -------------------------------------------------------------
// HOMEROOM TEACHER SCOPED MODE LOGIC (UC-HRM-05)
// -------------------------------------------------------------
const scopedStudyGroupsQuery = useStudyGroupsQuery(
  {},
  { enabled: () => isHomeroomScopedMode.value }
);

const scopedGroups = computed(() => scopedStudyGroupsQuery.data.value?.items ?? []);
const selectedScopedGroupId = ref<string | null>(null);

watch(
  scopedGroups,
  (groups) => {
    if (groups.length > 0 && !selectedScopedGroupId.value) {
      selectedScopedGroupId.value = groups[0]?.id ?? null;
    }
  },
  { immediate: true }
);

const activeScopedGroup = computed(() =>
  scopedGroups.value.find((g) => g.id === selectedScopedGroupId.value) ?? scopedGroups.value[0] ?? null
);

const scopedRosterQuery = useStudyGroupRosterQuery(
  () => selectedScopedGroupId.value,
  { enabled: () => isHomeroomScopedMode.value && Boolean(selectedScopedGroupId.value) }
);

const scopedRosterStudents = computed(() => scopedRosterQuery.data.value?.students ?? []);

// -------------------------------------------------------------
// TAB 1: STUDENT ROSTER DIRECTORY LOGIC
// -------------------------------------------------------------
const studentPage = ref(1);
const studentPageSize = ref(10);
const studentSearch = ref('');
const selectedGrade = ref<string | null>(null);
const selectedStudyGroupId = ref<string | null>(null);
const selectedStatus = ref<string | null>(null);
const studentSortField = ref<string>('full_name');
const studentSortOrder = ref<string>('asc');

const studentQueryParams = computed(() => ({
  page: studentPage.value,
  pageSize: studentPageSize.value,
  search: studentSearch.value || undefined,
  grade: selectedGrade.value || undefined,
  studyGroupId: selectedStudyGroupId.value || undefined,
  status: selectedStatus.value || undefined,
  sortField: studentSortField.value,
  sortOrder: studentSortOrder.value
}));

const studentsQuery = useStudentsQuery(
  studentQueryParams,
  { enabled: () => !isHomeroomScopedMode.value }
);

const students = computed(() => studentsQuery.data.value?.items ?? []);
const totalStudents = computed(() => studentsQuery.data.value?.totalCount ?? 0);
const studentFirstIndex = computed(() => (studentPage.value - 1) * studentPageSize.value);

function handleStudentPage(event: DataTablePageEvent): void {
  studentPage.value = event.page + 1;
  studentPageSize.value = event.rows;
}

function handleStudentSort(event: DataTableSortEvent): void {
  if (event.sortField) {
    studentSortField.value = String(event.sortField);
    studentSortOrder.value = event.sortOrder === 1 ? 'asc' : 'desc';
  }
}

// -------------------------------------------------------------
// TAB 2: STUDY GROUPS & PLACEMENT REGISTRY LOGIC
// -------------------------------------------------------------
const groupPage = ref(1);
const groupPageSize = ref(10);
const groupSearch = ref('');
const groupSchoolLevel = ref<string | null>(null);
const groupGrade = ref<string | null>(null);
const groupAcademicYearId = ref<string | null>(null);

const studyGroupQueryParams = computed(() => ({
  page: groupPage.value,
  pageSize: groupPageSize.value,
  search: groupSearch.value || undefined,
  schoolLevel: groupSchoolLevel.value || undefined,
  grade: groupGrade.value || undefined,
  academicYearId: groupAcademicYearId.value || undefined
}));

const studyGroupsQuery = useStudyGroupsQuery(
  studyGroupQueryParams,
  { enabled: () => !isHomeroomScopedMode.value }
);

const studyGroups = computed(() => studyGroupsQuery.data.value?.items ?? []);
const totalStudyGroups = computed(() => studyGroupsQuery.data.value?.totalCount ?? 0);
const groupFirstIndex = computed(() => (groupPage.value - 1) * groupPageSize.value);

function handleGroupPage(event: DataTablePageEvent): void {
  groupPage.value = event.page + 1;
  groupPageSize.value = event.rows;
}

// Lookups for filters
const academicYearLookups = ref<AcademicYearLookupItem[]>([]);
const filterStudyGroups = ref<StudyGroupLookupItem[]>([]);

onMounted(async () => {
  try {
    const years = await fetchAcademicYearsLookup();
    academicYearLookups.value = years;
    if (activeAcademicYearId.value) {
      groupAcademicYearId.value = activeAcademicYearId.value;
    } else if (years.length > 0) {
      groupAcademicYearId.value = years[0]?.id ?? null;
    }
  } catch {
    // Non-fatal
  }

  try {
    const groups = await fetchStudyGroupsLookup();
    filterStudyGroups.value = groups;
  } catch {
    // Non-fatal
  }
});

const gradeFilterOptions = [
  { label: 'All Grades', value: null },
  { label: 'Grade 1', value: '1' },
  { label: 'Grade 2', value: '2' },
  { label: 'Grade 3', value: '3' },
  { label: 'Grade 4', value: '4' },
  { label: 'Grade 5', value: '5' },
  { label: 'Grade 6', value: '6' },
  { label: 'Grade 7', value: '7' },
  { label: 'Grade 8', value: '8' },
  { label: 'Grade 9', value: '9' },
  { label: 'Grade 10', value: '10' },
  { label: 'Grade 11', value: '11' },
  { label: 'Grade 12', value: '12' }
];

const statusFilterOptions = [
  { label: 'All Statuses', value: null },
  { label: 'Active', value: 'ACTIVE' },
  { label: 'Inactive', value: 'INACTIVE' },
  { label: 'Graduated', value: 'GRADUATED' }
];

const schoolLevelFilterOptions = [
  { label: 'All Levels', value: null },
  { label: 'SD', value: 'SD' },
  { label: 'SMP', value: 'SMP' },
  { label: 'SMA', value: 'SMA' },
  { label: 'SMK', value: 'SMK' }
];

const studyGroupDropdownOptions = computed(() => [
  { label: 'All Study Groups', value: null },
  ...filterStudyGroups.value.map((g) => ({
    label: `${g.name} (${g.schoolLevel} Gr.${g.grade})`,
    value: g.id
  }))
]);

// -------------------------------------------------------------
// DRAWERS & MODALS STATE
// -------------------------------------------------------------
const isDossierOpen = ref(false);
const selectedDossierStudentId = ref<string | null>(null);

const isGroupFormOpen = ref(false);
const editingGroup = ref<StudyGroupListItemDto | null>(null);

const isRosterDrawerOpen = ref(false);
const selectedRosterGroupId = ref<string | null>(null);

const isPlaceModalOpen = ref(false);
const placeStudentTargetId = ref<string>('');
const placeStudentTargetName = ref<string>('');

watch(
  () => route.query.studentId,
  (queryStudentId) => {
    if (typeof queryStudentId === 'string' && queryStudentId) {
      selectedDossierStudentId.value = queryStudentId;
      isDossierOpen.value = true;
    }
  },
  { immediate: true }
);

function openStudentDossier(studentId: string): void {
  selectedDossierStudentId.value = studentId;
  isDossierOpen.value = true;
}

function openCreateGroupDrawer(): void {
  editingGroup.value = null;
  isGroupFormOpen.value = true;
}

function openEditGroupDrawer(group: StudyGroupListItemDto): void {
  editingGroup.value = group;
  isGroupFormOpen.value = true;
}

function openRosterDrawer(groupId: string): void {
  selectedRosterGroupId.value = groupId;
  isRosterDrawerOpen.value = true;
}

function openPlaceModal(student: StudentListItemDto): void {
  placeStudentTargetId.value = student.id ?? '';
  placeStudentTargetName.value = student.fullName ?? 'Student';
  isPlaceModalOpen.value = true;
}

function handlePlacementSuccess(): void {
  studentsQuery.refetch();
  studyGroupsQuery.refetch();
}

function handleGroupSaved(): void {
  studyGroupsQuery.refetch();
  fetchStudyGroupsLookup().then((groups) => {
    filterStudyGroups.value = groups;
  });
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6">
      <!-- ======================================================== -->
      <!-- A) HOMEROOM TEACHER SCOPED MODE (UC-HRM-05)              -->
      <!-- ======================================================== -->
      <section v-if="isHomeroomScopedMode" class="space-y-6">
        <!-- Scoped Header with Badge -->
        <div class="flex flex-col justify-between gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-center">
          <div class="space-y-1">
            <div class="flex flex-wrap items-center gap-2.5">
              <Users class="h-6 w-6 text-brand-accent-600" />
              <h1 class="text-xl font-bold tracking-tight text-slate-900">
                Homeroom Student Directory
              </h1>
              <!-- Dedicated Scoped Badge -->
              <span
                class="inline-flex items-center gap-1.5 rounded-full bg-brand-accent-100 px-3 py-1 text-xs font-semibold text-brand-accent-800"
              >
                <ShieldCheck class="h-3.5 w-3.5" />
                {{ activeScopedGroup?.name ? `My Homeroom Class (${activeScopedGroup.name})` : 'My Homeroom Class' }}
              </span>
            </div>
            <p class="text-sm text-slate-600">
              Assigned pastoral study group roster, student profiles, and parent emergency contacts (UC-HRM-05).
            </p>
          </div>

          <!-- Class selector if teacher has multiple homeroom classes -->
          <div v-if="scopedGroups.length > 1" class="flex items-center gap-2">
            <label class="text-xs font-medium text-slate-700">Class:</label>
            <Select
              v-model="selectedScopedGroupId"
              :options="scopedGroups"
              option-label="name"
              option-value="id"
              class="w-40 text-xs"
            />
          </div>
        </div>

        <!-- Scoped Homeroom Summary Card -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
            <div class="text-xs font-medium text-slate-500">Study Group (Rombel)</div>
            <div class="mt-1 text-lg font-bold text-slate-900">
              {{ activeScopedGroup?.name ?? '—' }}
            </div>
            <div class="text-xs text-slate-500">
              {{ activeScopedGroup?.schoolLevel }} • Grade {{ activeScopedGroup?.grade }}
            </div>
          </div>

          <div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
            <div class="text-xs font-medium text-slate-500">Academic Year</div>
            <div class="mt-1 text-lg font-bold text-slate-900">
              {{ activeScopedGroup?.academicYearName ?? 'Active Cycle' }}
            </div>
            <div class="text-xs text-slate-500">Current enrolled academic cycle</div>
          </div>

          <div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
            <div class="text-xs font-medium text-slate-500">Total Enrolled Students</div>
            <div class="mt-1 text-lg font-bold text-brand-accent-700">
              {{ scopedRosterStudents.length }} Students
            </div>
            <div class="text-xs text-slate-500">Active class cohort size</div>
          </div>
        </div>

        <!-- Homeroom Roster Table with Guardian Emergency Contacts -->
        <div class="rounded-lg border border-slate-200 bg-white shadow-xs">
          <div class="border-b border-slate-200 px-4 py-3">
            <h2 class="text-sm font-semibold text-slate-800">Class Roster &amp; Emergency Directory</h2>
          </div>

          <AppDataTable
            :value="scopedRosterStudents"
            :loading="scopedRosterQuery.isLoading.value"
            data-key="id"
            striped-rows
            empty-title="No students enrolled"
            empty-description="There are currently no students placed in your homeroom study group."
          >
            <!-- NIS & NISN -->
            <Column header="NIS / NISN" class="w-36">
              <template #body="{ data }">
                <div class="font-mono text-sm font-medium text-slate-900">{{ data.nis }}</div>
                <div v-if="data.nisn" class="font-mono text-xs text-slate-500">
                  NISN: {{ data.nisn }}
                </div>
              </template>
            </Column>

            <!-- Student Name & Gender -->
            <Column header="Student Name" class="min-w-48">
              <template #body="{ data }">
                <div class="font-semibold text-slate-900">{{ data.fullName }}</div>
                <div class="text-xs text-slate-500">
                  {{ data.gender === 'MALE' ? 'Laki-laki (L)' : 'Perempuan (P)' }}
                  <span v-if="data.nickname"> • "{{ data.nickname }}"</span>
                </div>
              </template>
            </Column>

            <!-- Status -->
            <Column header="Status" class="w-24">
              <template #body="{ data }">
                <StatusPill :status="data.status ?? 'ACTIVE'" />
              </template>
            </Column>

            <!-- Prominent Guardian Emergency Phone Numbers (UC-HRM-05) -->
            <Column header="Parent / Guardian Emergency Contacts" class="min-w-72">
              <template #body="{ data }">
                <div v-if="data.guardians && data.guardians.length > 0" class="space-y-1.5 py-1">
                  <div
                    v-for="(guardian, idx) in data.guardians"
                    :key="guardian.id ?? idx"
                    class="flex items-center justify-between gap-2 rounded border border-slate-200 bg-slate-50 px-2.5 py-1.5"
                  >
                    <div class="min-w-0">
                      <div class="flex items-center gap-1.5">
                        <span class="text-xs font-semibold text-slate-800">{{ guardian.fullName }}</span>
                        <span class="rounded bg-slate-200 px-1 py-0.2 text-[10px] font-medium text-slate-700">
                          {{ guardian.relationshipType }}
                        </span>
                      </div>
                      <div v-if="guardian.address" class="truncate text-[11px] text-slate-500">
                        {{ guardian.address }}
                      </div>
                    </div>

                    <!-- Prominent Emergency Phone Badge -->
                    <div class="flex shrink-0 items-center gap-1.5 rounded-md bg-emerald-50 px-2 py-1 text-xs font-bold text-emerald-800">
                      <Phone class="h-3.5 w-3.5 text-emerald-600" />
                      <span class="font-mono">{{ guardian.phone || 'No Phone' }}</span>
                    </div>
                  </div>
                </div>
                <div v-else class="italic text-slate-400">
                  No guardian recorded
                </div>
              </template>
            </Column>
          </AppDataTable>
        </div>
      </section>

      <!-- ======================================================== -->
      <!-- B) FULL ADMIN / READ DIRECTORY MODE                      -->
      <!-- ======================================================== -->
      <section v-else class="space-y-6">
        <!-- Main Header -->
        <div class="flex flex-col justify-between gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-center">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <Users class="h-6 w-6 text-brand-accent-600" />
              <h1 class="text-xl font-bold tracking-tight text-slate-900">
                Student Directory &amp; Placements
              </h1>
            </div>
            <p class="text-sm text-slate-600">
              Manage student master records, official guardians, annual study group rombels, and class placements.
            </p>
          </div>

          <!-- Top-Right Actions -->
          <div v-if="canManageDirectory" class="flex items-center gap-2">
            <Button
              icon="pi pi-plus"
              label="New Study Group"
              size="small"
              @click="openCreateGroupDrawer"
            />
          </div>
        </div>

        <!-- 2 Cohesive Tabs -->
        <Tabs v-model:value="activeMainTab">
          <TabList>
            <Tab value="roster">
              <span class="flex items-center gap-2">
                <Users class="h-4 w-4" />
                Student Roster Directory
              </span>
            </Tab>
            <Tab value="studyGroups">
              <span class="flex items-center gap-2">
                <School class="h-4 w-4" />
                Study Groups &amp; Placement Registry
              </span>
            </Tab>
          </TabList>

          <TabPanels class="pt-4">
            <!-- ---------------------------------------------------- -->
            <!-- TAB 1: STUDENT ROSTER DIRECTORY                      -->
            <!-- ---------------------------------------------------- -->
            <TabPanel value="roster" class="space-y-4">
              <!-- Filter & Search Bar -->
              <div class="flex flex-wrap items-center gap-3 rounded-lg border border-slate-200 bg-white p-3 shadow-xs">
                <!-- Search input -->
                <div class="relative min-w-56 flex-1">
                  <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <InputText
                    v-model="studentSearch"
                    placeholder="Search by NIS, NISN, Name..."
                    class="w-full pl-9 text-xs"
                    @input="studentPage = 1"
                  />
                </div>

                <!-- Grade Filter -->
                <Select
                  v-model="selectedGrade"
                  :options="gradeFilterOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="Grade"
                  class="w-36 text-xs"
                  @change="studentPage = 1"
                />

                <!-- Study Group Filter -->
                <Select
                  v-model="selectedStudyGroupId"
                  :options="studyGroupDropdownOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="Study Group"
                  class="w-48 text-xs"
                  @change="studentPage = 1"
                />

                <!-- Status Filter -->
                <Select
                  v-model="selectedStatus"
                  :options="statusFilterOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="Status"
                  class="w-32 text-xs"
                  @change="studentPage = 1"
                />
              </div>

              <!-- Student Roster Table -->
              <AppDataTable
                :value="students"
                :loading="studentsQuery.isLoading.value"
                paginator
                lazy
                :rows="studentPageSize"
                :first="studentFirstIndex"
                :total-records="totalStudents"
                data-key="id"
                striped-rows
                empty-title="No students found"
                empty-description="Try adjusting your search query or filters."
                @page="handleStudentPage"
                @sort="handleStudentSort"
              >
                <!-- NIS / NISN -->
                <Column header="NIS / NISN" field="nis" sortable class="w-36">
                  <template #body="{ data }">
                    <div class="font-mono text-xs font-semibold text-slate-900">{{ data.nis }}</div>
                    <div v-if="data.nisn" class="font-mono text-[11px] text-slate-500">
                      NISN: {{ data.nisn }}
                    </div>
                  </template>
                </Column>

                <!-- Student Full Name & Demographics -->
                <Column header="Full Name" field="fullName" sortable class="min-w-48">
                  <template #body="{ data }">
                    <button
                      type="button"
                      class="text-left font-semibold text-brand-accent-700 hover:underline"
                      @click="openStudentDossier(data.id)"
                    >
                      {{ data.fullName }}
                    </button>
                    <div class="text-[11px] text-slate-500">
                      {{ data.gender === 'MALE' ? 'Male' : 'Female' }}
                      <span v-if="data.nickname"> • "{{ data.nickname }}"</span>
                    </div>
                  </template>
                </Column>

                <!-- Grade & Study Group -->
                <Column header="Grade &amp; Class" class="w-40">
                  <template #body="{ data }">
                    <div v-if="data.studyGroupName" class="flex items-center gap-1.5">
                      <span class="rounded bg-brand-neutral-100 px-2 py-0.5 text-xs font-medium text-slate-800">
                        {{ data.studyGroupName }}
                      </span>
                      <span class="text-xs text-slate-500">({{ data.schoolLevel }} Gr.{{ data.grade }})</span>
                    </div>
                    <div v-else class="text-xs italic text-slate-400">
                      Unplaced
                    </div>
                  </template>
                </Column>

                <!-- Primary Guardian Contact -->
                <Column header="Primary Contact" class="min-w-48">
                  <template #body="{ data }">
                    <div v-if="data.primaryGuardianName">
                      <div class="text-xs font-medium text-slate-800">{{ data.primaryGuardianName }}</div>
                      <div v-if="data.primaryGuardianPhone" class="flex items-center gap-1 text-[11px] font-mono text-slate-600">
                        <Phone class="h-3 w-3 text-slate-400" />
                        {{ data.primaryGuardianPhone }}
                      </div>
                    </div>
                    <div v-else class="text-xs italic text-slate-400">No guardian</div>
                  </template>
                </Column>

                <!-- Status -->
                <Column header="Status" field="status" class="w-24">
                  <template #body="{ data }">
                    <StatusPill :status="data.status ?? 'ACTIVE'" />
                  </template>
                </Column>

                <!-- Row Actions -->
                <Column header="Actions" class="w-32 text-right">
                  <template #body="{ data }">
                    <div class="flex items-center justify-end gap-1">
                      <Button
                        size="small"
                        severity="secondary"
                        text
                        icon="pi pi-id-card"
                        aria-label="View dossier"
                        title="View Student Dossier"
                        @click="openStudentDossier(data.id)"
                      />

                      <Button
                        v-if="canManagePlacements"
                        size="small"
                        severity="secondary"
                        text
                        icon="pi pi-arrows-alt"
                        aria-label="Place in study group"
                        title="Place in Study Group"
                        @click="openPlaceModal(data)"
                      />
                    </div>
                  </template>
                </Column>
              </AppDataTable>
            </TabPanel>

            <!-- ---------------------------------------------------- -->
            <!-- TAB 2: STUDY GROUPS & PLACEMENT REGISTRY             -->
            <!-- ---------------------------------------------------- -->
            <TabPanel value="studyGroups" class="space-y-4">
              <!-- Filter Bar -->
              <div class="flex flex-wrap items-center gap-3 rounded-lg border border-slate-200 bg-white p-3 shadow-xs">
                <!-- Search input -->
                <div class="relative min-w-56 flex-1">
                  <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <InputText
                    v-model="groupSearch"
                    placeholder="Search class or teacher name..."
                    class="w-full pl-9 text-xs"
                    @input="groupPage = 1"
                  />
                </div>

                <!-- Academic Year Filter -->
                <Select
                  v-model="groupAcademicYearId"
                  :options="academicYearLookups"
                  option-label="name"
                  option-value="id"
                  placeholder="Academic Year"
                  class="w-44 text-xs"
                  @change="groupPage = 1"
                />

                <!-- School Level Filter -->
                <Select
                  v-model="groupSchoolLevel"
                  :options="schoolLevelFilterOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="Level"
                  class="w-28 text-xs"
                  @change="groupPage = 1"
                />

                <!-- Grade Filter -->
                <Select
                  v-model="groupGrade"
                  :options="gradeFilterOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="Grade"
                  class="w-32 text-xs"
                  @change="groupPage = 1"
                />
              </div>

              <!-- Study Groups Table -->
              <AppDataTable
                :value="studyGroups"
                :loading="studyGroupsQuery.isLoading.value"
                paginator
                lazy
                :rows="groupPageSize"
                :first="groupFirstIndex"
                :total-records="totalStudyGroups"
                data-key="id"
                striped-rows
                empty-title="No study groups found"
                empty-description="Configure new study groups for this academic year using 'New Study Group'."
                @page="handleGroupPage"
              >
                <!-- Level & Grade -->
                <Column header="Level / Grade" class="w-32">
                  <template #body="{ data }">
                    <span class="rounded bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700">
                      {{ data.schoolLevel }} Gr.{{ data.grade }}
                    </span>
                  </template>
                </Column>

                <!-- Class / Study Group Name -->
                <Column header="Class (Rombel)" class="w-40 font-bold text-slate-900">
                  <template #body="{ data }">
                    <button
                      type="button"
                      class="text-left font-bold text-brand-accent-700 hover:underline"
                      @click="openRosterDrawer(data.id)"
                    >
                      Class {{ data.name }}
                    </button>
                  </template>
                </Column>

                <!-- Academic Year -->
                <Column header="Academic Cycle" class="w-40">
                  <template #body="{ data }">
                    <div class="text-xs text-slate-600">{{ data.academicYearName }}</div>
                  </template>
                </Column>

                <!-- Homeroom Teacher (Wali Kelas) -->
                <Column header="Homeroom Teacher (Wali Kelas)" class="min-w-48">
                  <template #body="{ data }">
                    <div v-if="data.homeroomTeacherName" class="flex items-center gap-1.5 text-xs font-medium text-slate-900">
                      <UserCheck class="h-3.5 w-3.5 text-brand-accent-600" />
                      {{ data.homeroomTeacherName }}
                    </div>
                    <div v-else class="text-xs italic text-slate-400">
                      Unassigned
                    </div>
                  </template>
                </Column>

                <!-- Student Count -->
                <Column header="Enrolled" class="w-28 text-center">
                  <template #body="{ data }">
                    <span class="inline-flex items-center rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-semibold text-slate-800">
                      {{ data.studentCount ?? 0 }} students
                    </span>
                  </template>
                </Column>

                <!-- Actions -->
                <Column header="Actions" class="w-44 text-right">
                  <template #body="{ data }">
                    <div class="flex items-center justify-end gap-1">
                      <Button
                        size="small"
                        severity="secondary"
                        text
                        icon="pi pi-users"
                        label="Roster"
                        aria-label="View roster"
                        title="View Class Roster"
                        @click="openRosterDrawer(data.id)"
                      />

                      <Button
                        v-if="canManageDirectory"
                        size="small"
                        severity="secondary"
                        text
                        icon="pi pi-pencil"
                        aria-label="Edit study group"
                        title="Edit Study Group"
                        @click="openEditGroupDrawer(data)"
                      />
                    </div>
                  </template>
                </Column>
              </AppDataTable>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </section>

      <!-- ======================================================== -->
      <!-- SHARED DRAWERS & MODALS                                  -->
      <!-- ======================================================== -->

      <!-- Student Detailed Dossier Drawer -->
      <StudentDossierDrawer
        v-model:visible="isDossierOpen"
        :student-id="selectedDossierStudentId"
        :can-manage="canManageDirectory"
        :can-place="canManagePlacements"
        @changed="handlePlacementSuccess"
      />

      <!-- Create / Edit Study Group Drawer -->
      <StudyGroupFormDrawer
        v-model:visible="isGroupFormOpen"
        :study-group="editingGroup"
        @saved="handleGroupSaved"
      />

      <!-- Study Group Roster Slide-Over Drawer -->
      <StudyGroupRosterDrawer
        v-model:visible="isRosterDrawerOpen"
        :study-group-id="selectedRosterGroupId"
        :can-manage-placements="canManagePlacements"
        @place-student="() => { isRosterDrawerOpen = false; activeMainTab = 'roster'; }"
      />

      <!-- Place Student Modal -->
      <PlaceStudentModal
        v-if="placeStudentTargetId"
        v-model:visible="isPlaceModalOpen"
        :student-id="placeStudentTargetId"
        :student-name="placeStudentTargetName"
        @placed="handlePlacementSuccess"
      />
    </div>
  </AppErrorBoundary>
</template>
