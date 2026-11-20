<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Button from 'primevue/button';
import Select from 'primevue/select';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  AlertTriangle,
  CalendarClock,
  CalendarDays,
  Layers,
  UserCheck
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { useCurrentSession } from '@/shared/services/authSession';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import {
  academicYearsLookupQuery,
  studyGroupsLookupQuery
} from '@/shared/services/lookups.api';
import type { StudyGroupTimetableSlotDto } from '../api/timetables.api';
import { useTimetableGridQuery } from '../api/useTimetableQueries';
import type { ScheduleType } from '../schemas/timetable.schema';
import TimetableGrid from '../components/TimetableGrid.vue';
import SlotAssignmentDrawer from '../components/SlotAssignmentDrawer.vue';
import SmkBlockManagementDrawer from '../components/SmkBlockManagementDrawer.vue';
import TeacherPersonalScheduleTab from '../components/TeacherPersonalScheduleTab.vue';

const session = useCurrentSession();
const {
  activeAcademicYearId,
  selectedAcademicYearId,
  activePeriodId,
  selectedPeriodId,
  isHistoricalPeriod
} = useAppContextBridge();

// Permissions
const canManageTimetables = computed(
  () =>
    !session.isAuthenticated.value ||
    session.hasPermission('Permissions.Operations.Timetables.Manage')
);
const canViewStudyGroups = computed(
  () =>
    !session.isAuthenticated.value ||
    session.hasPermission('Permissions.Operations.Timetables.View') ||
    session.hasPermission('Permissions.Operations.Timetables.Manage')
);
const canViewOwnSchedule = computed(
  () =>
    !session.isAuthenticated.value ||
    session.hasPermission('Permissions.Operations.Timetables.ViewOwn')
);

// Read-only condition: historical period or missing manage permission
const isReadOnly = computed(() => isHistoricalPeriod.value || !canManageTimetables.value);

// Active Tab
const activeTab = ref(
  canViewStudyGroups.value ? 'study-group' : canViewOwnSchedule.value ? 'teacher-schedule' : 'study-group'
);

// Effective Academic Year & Operational Period IDs
const currentYearId = computed(
  () => selectedAcademicYearId.value ?? activeAcademicYearId.value ?? ''
);
const currentPeriodId = computed(
  () => selectedPeriodId.value ?? activePeriodId.value ?? ''
);

// Academic Years Lookup
const academicYearsQuery = useQuery(academicYearsLookupQuery());
const academicYears = computed(() => academicYearsQuery.data.value ?? []);

// Filter Academic Year for Study Groups
const filterYearId = ref<string>('');
watch(
  currentYearId,
  (val) => {
    if (val && !filterYearId.value) {
      filterYearId.value = val;
    }
  },
  { immediate: true }
);

// Study Groups Lookup filtered by Academic Year
const studyGroupQueryParams = computed(() => ({
  academicYearId: filterYearId.value || undefined,
  pageSize: 100
}));

const studyGroupsQuery = useQuery(
  computed(() => studyGroupsLookupQuery(studyGroupQueryParams.value))
);
const studyGroups = computed(() => studyGroupsQuery.data.value ?? []);
const isLoadingGroups = computed(() => studyGroupsQuery.isLoading.value);

// Selected Study Group
const selectedStudyGroupId = ref<string>('');

// Auto-select first study group when list is loaded
watch(
  studyGroups,
  (groups) => {
    if (groups.length > 0 && (!selectedStudyGroupId.value || !groups.some((g) => g.id === selectedStudyGroupId.value))) {
      selectedStudyGroupId.value = groups[0]?.id ?? '';
    }
  },
  { immediate: true }
);

const selectedStudyGroup = computed(() =>
  studyGroups.value.find((g) => g.id === selectedStudyGroupId.value)
);

// Schedule Type: 'REGULAR', 'GENERAL', 'VOCATIONAL'
const scheduleType = ref<ScheduleType>('REGULAR');

// Timetable Grid Query
const gridQueryParams = computed(() => ({
  periodId: currentPeriodId.value,
  scheduleType: scheduleType.value
}));

const gridQuery = useTimetableGridQuery(selectedStudyGroupId, gridQueryParams, {
  enabled: computed(() => Boolean(selectedStudyGroupId.value && currentPeriodId.value))
});
const slots = computed(() => gridQuery.data.value ?? []);
const isLoadingSlots = computed(() => gridQuery.isLoading.value);

// Conflicts count in current grid view
const conflictsCount = computed(() =>
  slots.value.filter((s) => s.teacherConflict || s.roomConflict).length
);

// Slot Assignment Drawer State
const isSlotDrawerOpen = ref(false);
const activeDrawerSlot = ref<StudyGroupTimetableSlotDto | null>(null);
const activeDrawerDay = ref<number>(1);
const activeDrawerPeriod = ref<number>(1);

function handleSelectSlot(payload: {
  dayOfWeek: number;
  periodSlot: number;
  slot: StudyGroupTimetableSlotDto | null;
}): void {
  activeDrawerDay.value = payload.dayOfWeek;
  activeDrawerPeriod.value = payload.periodSlot;
  activeDrawerSlot.value = payload.slot;
  isSlotDrawerOpen.value = true;
}

// SMK Block Drawer State
const isSmkBlocksDrawerOpen = ref(false);
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6" data-testid="timetable-workspace-view">
      <!-- Breadcrumb Navigation -->
      <nav aria-label="Breadcrumb" class="flex items-center gap-1.5 text-xs text-slate-500">
        <span>Operations &amp; Comms</span>
        <span aria-hidden="true">/</span>
        <span class="font-medium text-slate-800">Timetables &amp; Scheduling</span>
      </nav>

      <!-- Page Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <CalendarClock class="h-5 w-5 text-brand-primary-600" />
            <h1 class="text-xl font-bold tracking-tight text-slate-900">
              Timetables &amp; Scheduling
            </h1>
          </div>
          <p class="text-sm text-slate-500">
            Master weekly timetable matrix, SMK block date rotations, conflict detection, and personalized teacher schedules.
          </p>
        </div>

        <!-- Global Action: Manage SMK Blocks (visible when in study group tab) -->
        <div v-if="activeTab === 'study-group' && canManageTimetables" class="flex items-center gap-2">
          <Button
            type="button"
            severity="secondary"
            size="small"
            class="text-xs"
            :disabled="!selectedStudyGroupId || isHistoricalPeriod"
            data-testid="manage-smk-blocks-btn"
            @click="isSmkBlocksDrawerOpen = true"
          >
            <Layers class="mr-1.5 h-3.5 w-3.5" />
            Manage SMK Blocks
          </Button>
        </div>
      </div>

      <!-- Historical Warning Banner -->
      <div
        v-if="isHistoricalPeriod"
        class="flex items-center gap-2.5 rounded-xl border border-amber-300 bg-amber-50 p-4 text-xs text-amber-900 shadow-xs"
        data-testid="historical-period-banner"
      >
        <AlertTriangle class="h-4 w-4 shrink-0 text-amber-600" />
        <p>
          <strong>Historical Mode:</strong> Viewing an archived academic period. Timetable slot assignments and SMK block modifications are disabled.
        </p>
      </div>

      <!-- Tab Navigation (Pattern 4) -->
      <Tabs v-model:value="activeTab">
        <TabList class="border-b border-slate-200">
          <Tab
            v-if="canViewStudyGroups"
            value="study-group"
            class="flex items-center gap-2 py-3 text-sm font-medium"
            data-testid="tab-study-group"
          >
            <CalendarDays class="h-4 w-4" />
            <span>Class Timetable Matrix</span>
          </Tab>
          <Tab
            v-if="canViewOwnSchedule"
            value="teacher-schedule"
            class="flex items-center gap-2 py-3 text-sm font-medium"
            data-testid="tab-teacher-schedule"
          >
            <UserCheck class="h-4 w-4" />
            <span>My Teaching Schedule</span>
          </Tab>
        </TabList>

        <TabPanels class="pt-6">
          <!-- Tab 1: Study Group Timetable Matrix -->
          <TabPanel v-if="canViewStudyGroups" value="study-group" class="space-y-6">
            <!-- Context Filter Bar -->
            <div
              class="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-xs lg:flex-row lg:items-center lg:justify-between"
              data-testid="timetable-filter-bar"
            >
              <!-- Left: Academic Year + Study Group Selector -->
              <div class="flex flex-wrap items-center gap-3">
                <div class="space-y-1">
                  <label class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                    Academic Year
                  </label>
                  <Select
                    v-model="filterYearId"
                    :options="academicYears"
                    option-label="name"
                    option-value="id"
                    placeholder="Select Year..."
                    class="w-44 text-xs"
                    data-testid="filter-academic-year-select"
                  />
                </div>

                <div class="space-y-1">
                  <label class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                    Study Group (Rombel)
                  </label>
                  <Select
                    v-model="selectedStudyGroupId"
                    :options="studyGroups"
                    option-label="name"
                    option-value="id"
                    placeholder="Select Study Group..."
                    class="w-56 text-xs"
                    :loading="isLoadingGroups"
                    data-testid="study-group-select"
                  />
                </div>
              </div>

              <!-- Right: Schedule Type Toggle -->
              <div class="space-y-1">
                <label class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                  Schedule Rotation
                </label>
                <div class="inline-flex rounded-lg border border-slate-200 bg-slate-100 p-0.5">
                  <button
                    type="button"
                    class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
                    :class="
                      scheduleType === 'REGULAR'
                        ? 'bg-white text-slate-900 shadow-xs'
                        : 'text-slate-600 hover:text-slate-900'
                    "
                    data-testid="schedule-type-regular"
                    @click="scheduleType = 'REGULAR'"
                  >
                    Regular
                  </button>
                  <button
                    type="button"
                    class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
                    :class="
                      scheduleType === 'GENERAL'
                        ? 'bg-white text-slate-900 shadow-xs'
                        : 'text-slate-600 hover:text-slate-900'
                    "
                    data-testid="schedule-type-general"
                    @click="scheduleType = 'GENERAL'"
                  >
                    SMK General
                  </button>
                  <button
                    type="button"
                    class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
                    :class="
                      scheduleType === 'VOCATIONAL'
                        ? 'bg-white text-slate-900 shadow-xs'
                        : 'text-slate-600 hover:text-slate-900'
                    "
                    data-testid="schedule-type-vocational"
                    @click="scheduleType = 'VOCATIONAL'"
                  >
                    SMK Vocational
                  </button>
                </div>
              </div>
            </div>

            <!-- Conflict Summary Notice (D-10) -->
            <div
              v-if="conflictsCount > 0"
              class="flex items-center gap-2 rounded-xl border border-rose-300 bg-rose-50 p-3 text-xs text-rose-800 shadow-xs"
              data-testid="grid-conflicts-notice"
            >
              <AlertTriangle class="h-4 w-4 shrink-0 text-rose-600" />
              <span>
                <strong>{{ conflictsCount }} scheduling conflict(s) detected</strong> in this weekly timetable. Cells highlighted with a red badge indicate teacher double-booking or room collisions.
              </span>
            </div>

            <!-- Empty State when no Study Group is Selected -->
            <EmptyState
              v-if="!selectedStudyGroupId"
              title="No Study Group Selected"
              description="Choose a study group from the dropdown selector above to view and configure its weekly schedule matrix."
              icon="calendar"
            />

            <!-- Timetable Weekly Matrix Grid -->
            <TimetableGrid
              v-else
              :slots="slots"
              :is-loading="isLoadingSlots"
              :read-only="isReadOnly"
              @select-slot="handleSelectSlot"
            />
          </TabPanel>

          <!-- Tab 2: Teacher Personal Schedule (UC-TCH-10) -->
          <TabPanel v-if="canViewOwnSchedule" value="teacher-schedule">
            <TeacherPersonalScheduleTab
              :operational-period-id="currentPeriodId"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>

      <!-- Slot Assignment Drawer (Tier 3 Right-Anchored) -->
      <SlotAssignmentDrawer
        v-if="selectedStudyGroupId"
        v-model:visible="isSlotDrawerOpen"
        :study-group-id="selectedStudyGroupId"
        :operational-period-id="currentPeriodId"
        :schedule-type="scheduleType"
        :day-of-week="activeDrawerDay"
        :period-slot="activeDrawerPeriod"
        :timetable-slot="activeDrawerSlot"
        :read-only="isReadOnly"
      />

      <!-- SMK Block Date Management Drawer -->
      <SmkBlockManagementDrawer
        v-if="selectedStudyGroupId"
        v-model:visible="isSmkBlocksDrawerOpen"
        :study-group-id="selectedStudyGroupId"
        :operational-period-id="currentPeriodId"
        :study-group-name="selectedStudyGroup?.name ?? ''"
        :read-only="isReadOnly"
      />
    </div>
  </AppErrorBoundary>
</template>
