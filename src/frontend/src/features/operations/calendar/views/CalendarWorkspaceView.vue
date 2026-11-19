<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import type { DataTablePageEvent } from 'primevue/datatable';
import { Building2, Calendar, CalendarDays, CheckCircle2, Pencil, Plus, Search } from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import { useCurrentSession } from '@/shared/services/authSession';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { AcademicYearDto, OperationalPeriodDto } from '../api/academicYears.api';
import {
  useActivateAcademicYearMutation,
  useActivateOperationalPeriodMutation
} from '../api/useAcademicYearMutations';
import { useAcademicYearsQuery } from '../api/useAcademicYearQueries';
import AcademicYearFormDrawer from '../components/AcademicYearFormDrawer.vue';
import CalendarEntriesTab from '../components/CalendarEntriesTab.vue';
import RoomsTab from '../components/RoomsTab.vue';

const session = useCurrentSession();
const canManageCalendar = computed(() =>
  !session.isAuthenticated.value || session.hasPermission('Permissions.Operations.Calendar.Manage')
);
const canManageRooms = computed(() =>
  !session.isAuthenticated.value || session.hasPermission('Permissions.Operations.Rooms.Manage')
);

const activeTab = ref('academic-cycle');

// Tab 1 (Academic Cycle) State & Queries
const page = ref(1);
const pageSize = ref(10);
const search = ref('');

const queryParams = computed(() => ({
  page: page.value,
  pageSize: pageSize.value,
  search: search.value
}));

const { data, isLoading, isError, refetch } = useAcademicYearsQuery(queryParams);

const academicYears = computed(() => data.value?.items ?? []);
const totalRecords = computed(() => data.value?.totalCount ?? 0);
const firstRecordIndex = computed(() => (page.value - 1) * pageSize.value);

const isDrawerVisible = ref(false);
const editingAcademicYear = ref<AcademicYearDto | null>(null);

const activateYearMutation = useActivateAcademicYearMutation();
const activatePeriodMutation = useActivateOperationalPeriodMutation();

interface ConfirmState {
  visible: boolean;
  type: 'year' | 'period';
  academicYear: AcademicYearDto | null;
  period: OperationalPeriodDto | null;
}

const confirmState = ref<ConfirmState>({
  visible: false,
  type: 'year',
  academicYear: null,
  period: null
});

const isConfirmLoading = computed(
  () => activateYearMutation.isPending.value || activatePeriodMutation.isPending.value
);

const confirmTitle = computed(() =>
  confirmState.value.type === 'year'
    ? 'Activate Academic Year'
    : 'Activate Operational Period'
);

const confirmMessage = computed(() => {
  if (confirmState.value.type === 'year') {
    const yearName = confirmState.value.academicYear?.name ?? 'this academic year';
    return `Activating ${yearName} will deactivate any currently active academic year and set its earliest operational period as active. Continue?`;
  }
  const periodName = confirmState.value.period?.name ?? 'this operational period';
  const yearName = confirmState.value.academicYear?.name ?? 'the academic year';
  return `Activating ${periodName} in ${yearName} will deactivate the currently active period. Continue?`;
});

function handlePageChange(event: DataTablePageEvent): void {
  page.value = event.page + 1;
  pageSize.value = event.rows;
}

function handleSearchInput(value: string | undefined): void {
  search.value = value ?? '';
  page.value = 1;
}

function openCreateDrawer(): void {
  editingAcademicYear.value = null;
  isDrawerVisible.value = true;
}

function openEditDrawer(year: AcademicYearDto): void {
  editingAcademicYear.value = year;
  isDrawerVisible.value = true;
}

function promptActivateYear(year: AcademicYearDto): void {
  confirmState.value = {
    visible: true,
    type: 'year',
    academicYear: year,
    period: null
  };
}

function promptActivatePeriod(year: AcademicYearDto, period: OperationalPeriodDto): void {
  confirmState.value = {
    visible: true,
    type: 'period',
    academicYear: year,
    period
  };
}

async function handleConfirmAction(): Promise<void> {
  const { type, academicYear, period } = confirmState.value;
  if (!academicYear?.id) {
    return;
  }

  try {
    if (type === 'year') {
      await activateYearMutation.mutateAsync(academicYear.id);
    } else if (period?.id) {
      await activatePeriodMutation.mutateAsync({
        academicYearId: academicYear.id,
        periodId: period.id
      });
    }
    confirmState.value.visible = false;
  } catch {
    // Error toast is handled by mutation onError
  }
}

function getActivateYearTooltip(year: AcademicYearDto): string {
  return year.status === 'ACTIVE'
    ? 'This academic year is already active'
    : 'Set as active academic year';
}

function getActivatePeriodTooltip(year: AcademicYearDto, period: OperationalPeriodDto): string {
  if (year.status !== 'ACTIVE') {
    return 'Activate the parent academic year first';
  }
  if (period.status === 'ACTIVE') {
    return 'This operational period is already active';
  }
  return 'Set as active operational period';
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6">
      <!-- Page Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <CalendarDays class="h-5 w-5 text-brand-accent-600" />
            <h1 class="text-xl font-semibold tracking-tight text-slate-900">
              School Operational Setup &amp; Master Data
            </h1>
          </div>
          <p class="text-sm text-slate-600">
            Manage academic years, operational periods, calendar holidays, and physical room inventory.
          </p>
        </div>
      </div>

      <!-- PrimeVue 3-Tab Operational Workspace -->
      <Tabs v-model:value="activeTab" data-testid="calendar-workspace-tabs">
        <TabList class="mb-4">
          <Tab value="academic-cycle" data-testid="tab-academic-cycle">
            <div class="flex items-center gap-2">
              <CalendarDays class="h-4 w-4" />
              <span>Academic Cycle</span>
            </div>
          </Tab>
          <Tab value="calendar-holidays" data-testid="tab-calendar-holidays">
            <div class="flex items-center gap-2">
              <Calendar class="h-4 w-4" />
              <span>Calendar &amp; Holidays</span>
            </div>
          </Tab>
          <Tab value="rooms-facilities" data-testid="tab-rooms-facilities">
            <div class="flex items-center gap-2">
              <Building2 class="h-4 w-4" />
              <span>Rooms &amp; Facilities</span>
            </div>
          </Tab>
        </TabList>

        <TabPanels>
          <!-- Tab 1: Academic Cycle (Preserving Stage 0 Functionality) -->
          <TabPanel value="academic-cycle" data-testid="tab-panel-academic-cycle">
            <div class="space-y-6">
              <!-- Sub-header with Create Button -->
              <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h2 class="text-lg font-semibold tracking-tight text-slate-900">
                    Academic Cycle &amp; Terms
                  </h2>
                  <p class="text-sm text-slate-600">
                    Configure institutional school years and active operational periods (semesters).
                  </p>
                </div>

                <Button
                  v-if="canManageCalendar"
                  type="button"
                  label="Create Academic Year"
                  data-testid="create-academic-year-button"
                  @click="openCreateDrawer"
                >
                  <template #icon>
                    <Plus class="mr-1.5 h-4 w-4" />
                  </template>
                </Button>
              </div>

              <!-- Filter Bar -->
              <div
                class="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between"
              >
                <div class="relative w-full sm:max-w-xs">
                  <Search
                    class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
                  />
                  <InputText
                    :model-value="search"
                    placeholder="Search academic years..."
                    class="w-full pl-9"
                    data-testid="academic-year-search"
                    @update:model-value="handleSearchInput"
                  />
                </div>
                <p class="text-xs text-slate-500">
                  Showing {{ academicYears.length }} of {{ totalRecords }} academic
                  {{ totalRecords === 1 ? 'year' : 'years' }}
                </p>
              </div>

              <!-- Error State -->
              <EmptyState
                v-if="isError"
                variant="error"
                title="Unable to load academic years"
                description="There was a problem retrieving academic cycle records. Please try again."
              >
                <Button type="button" label="Retry" severity="secondary" @click="refetch()" />
              </EmptyState>

              <!-- Academic Years Table -->
              <AppDataTable
                v-else
                :value="academicYears"
                :loading="isLoading"
                :paginator="totalRecords > pageSize"
                lazy
                :rows="pageSize"
                :first="firstRecordIndex"
                :total-records="totalRecords"
                data-key="id"
                empty-title="No academic years configured"
                empty-description="Create your first academic year and operational periods to establish the school calendar."
                @page="handlePageChange"
              >
                <Column field="name" header="Academic Year" class="min-w-[180px] align-top">
                  <template #body="{ data: year }">
                    <div class="space-y-1">
                      <p class="font-semibold text-slate-900">{{ year.name }}</p>
                      <p class="text-xs text-slate-500">
                        {{ formatDisplayDate(year.startDate) }} – {{ formatDisplayDate(year.endDate) }}
                      </p>
                    </div>
                  </template>
                </Column>

                <Column field="status" header="Status" class="w-32 align-top">
                  <template #body="{ data: year }">
                    <StatusPill :status="year.status ?? 'INACTIVE'" />
                  </template>
                </Column>

                <Column header="Operational Periods" class="min-w-[360px] align-top">
                  <template #body="{ data: year }">
                    <div v-if="year.periods && year.periods.length > 0" class="space-y-2">
                      <div
                        v-for="period in year.periods"
                        :key="period.id"
                        class="flex flex-wrap items-center justify-between gap-2 rounded-md border border-slate-200 bg-slate-50/80 px-3 py-2"
                        :data-testid="`period-item-${period.id}`"
                      >
                        <div class="min-w-0 space-y-0.5">
                          <div class="flex items-center gap-2">
                            <span class="text-sm font-medium text-slate-800">{{ period.name }}</span>
                            <StatusPill :status="period.status ?? 'INACTIVE'" />
                          </div>
                          <p class="text-xs text-slate-500">
                            {{ formatDisplayDate(period.startDate) }} –
                            {{ formatDisplayDate(period.endDate) }}
                          </p>
                        </div>

                        <span
                          v-if="canManageCalendar"
                          :title="getActivatePeriodTooltip(year, period)"
                          class="inline-flex"
                        >
                          <Button
                            type="button"
                            label="Activate Period"
                            size="small"
                            severity="secondary"
                            outlined
                            :disabled="year.status !== 'ACTIVE' || period.status === 'ACTIVE'"
                            :data-testid="`activate-period-${period.id}`"
                            @click="promptActivatePeriod(year, period)"
                          >
                            <template #icon>
                              <CheckCircle2 class="mr-1.5 h-3.5 w-3.5" />
                            </template>
                          </Button>
                        </span>
                      </div>
                    </div>
                    <span v-else class="text-xs text-slate-400">No periods defined</span>
                  </template>
                </Column>

                <Column v-if="canManageCalendar" header="Actions" class="w-52 align-top">
                  <template #body="{ data: year }">
                    <div class="flex flex-wrap items-center gap-2">
                      <Button
                        type="button"
                        label="Edit"
                        size="small"
                        severity="secondary"
                        text
                        :data-testid="`edit-year-${year.id}`"
                        @click="openEditDrawer(year)"
                      >
                        <template #icon>
                          <Pencil class="mr-1.5 h-3.5 w-3.5" />
                        </template>
                      </Button>

                      <span :title="getActivateYearTooltip(year)" class="inline-flex">
                        <Button
                          type="button"
                          label="Activate"
                          size="small"
                          severity="primary"
                          outlined
                          :disabled="year.status === 'ACTIVE'"
                          :data-testid="`activate-year-${year.id}`"
                          @click="promptActivateYear(year)"
                        >
                          <template #icon>
                            <CheckCircle2 class="mr-1.5 h-3.5 w-3.5" />
                          </template>
                        </Button>
                      </span>
                    </div>
                  </template>
                </Column>

                <template #empty>
                  <EmptyState
                    :variant="search ? 'no-results' : 'empty'"
                    :title="search ? 'No matching academic years' : 'No academic years configured'"
                    :description="
                      search
                        ? 'Try adjusting your search filter or clear it to view all academic years.'
                        : 'Create your first academic year and operational periods to establish the school calendar.'
                    "
                  >
                    <Button
                      v-if="search"
                      type="button"
                      label="Clear Search"
                      severity="secondary"
                      @click="handleSearchInput('')"
                    />
                    <Button
                      v-else-if="canManageCalendar"
                      type="button"
                      label="Create Academic Year"
                      @click="openCreateDrawer"
                    >
                      <template #icon>
                        <Plus class="mr-1.5 h-4 w-4" />
                      </template>
                    </Button>
                  </EmptyState>
                </template>
              </AppDataTable>

              <!-- Create / Edit Drawer -->
              <AcademicYearFormDrawer
                v-model:visible="isDrawerVisible"
                :academic-year="editingAcademicYear"
              />

              <!-- Activation Confirmation Dialog -->
              <ConfirmActionDialog
                v-model:visible="confirmState.visible"
                :title="confirmTitle"
                :message="confirmMessage"
                confirm-label="Activate"
                severity="primary"
                :loading="isConfirmLoading"
                @confirm="handleConfirmAction"
              />
            </div>
          </TabPanel>

          <!-- Tab 2: Calendar & Holidays -->
          <TabPanel value="calendar-holidays" data-testid="tab-panel-calendar-holidays">
            <CalendarEntriesTab
              v-if="activeTab === 'calendar-holidays'"
              :can-manage="canManageCalendar"
            />
          </TabPanel>

          <!-- Tab 3: Rooms & Facilities -->
          <TabPanel value="rooms-facilities" data-testid="tab-panel-rooms-facilities">
            <RoomsTab
              v-if="activeTab === 'rooms-facilities'"
              :can-manage="canManageRooms"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  </AppErrorBoundary>
</template>
