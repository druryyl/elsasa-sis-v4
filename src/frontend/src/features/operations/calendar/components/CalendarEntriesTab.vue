<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import type { DataTablePageEvent } from 'primevue/datatable';
import { useQuery } from '@tanstack/vue-query';
import { Calendar, Pencil, Plus, Search, Trash2 } from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { CalendarEntryDto } from '../api/calendarEntries.api';
import { useDeleteCalendarEntryMutation } from '../api/useCalendarEntryMutations';
import { useCalendarEntriesQuery } from '../api/useCalendarEntryQueries';
import CalendarEntryFormDrawer from './CalendarEntryFormDrawer.vue';

withDefaults(
  defineProps<{
    canManage?: boolean;
  }>(),
  {
    canManage: true
  }
);

const appContext = useAppContextBridge();

// Available Academic Years for filter
const { data: academicYearsLookup } = useQuery(academicYearsLookupQuery());

// Filter state
const page = ref(1);
const pageSize = ref(10);
const search = ref('');
const dayTypeFilter = ref('');
const selectedYearId = ref<string>('');

// Initialize and sync selected academic year
watch(
  () => appContext.selectedAcademicYearId.value,
  (newYearId) => {
    if (!selectedYearId.value && newYearId) {
      selectedYearId.value = newYearId;
    }
  },
  { immediate: true }
);

// Fallback to first available academic year if none selected
watch(
  () => academicYearsLookup.value,
  (years) => {
    if (!selectedYearId.value && years && years.length > 0 && years[0]?.id) {
      selectedYearId.value = years[0].id;
    }
  },
  { immediate: true }
);

const selectedAcademicYearName = computed(() => {
  const match = academicYearsLookup.value?.find((y) => y.id === selectedYearId.value);
  return match?.name ?? appContext.activeAcademicYearName.value ?? 'Current Academic Year';
});

const queryParams = computed(() => ({
  academicYearId: selectedYearId.value || undefined,
  dayType: dayTypeFilter.value || undefined,
  search: search.value || undefined,
  page: page.value,
  pageSize: pageSize.value,
  sortField: 'EntryDate',
  sortOrder: 'asc'
}));

const { data, isLoading, isError, refetch } = useCalendarEntriesQuery(queryParams, {
  enabled: computed(() => Boolean(selectedYearId.value))
});

const calendarEntries = computed(() => data.value?.items ?? []);
const totalRecords = computed(() => data.value?.totalCount ?? 0);
const firstRecordIndex = computed(() => (page.value - 1) * pageSize.value);

// Drawer state
const isDrawerVisible = ref(false);
const editingEntry = ref<CalendarEntryDto | null>(null);

// Delete Confirmation state
const isDeleteDialogOpen = ref(false);
const entryToDelete = ref<CalendarEntryDto | null>(null);
const deleteMutation = useDeleteCalendarEntryMutation();

function handlePageChange(event: DataTablePageEvent): void {
  page.value = event.page + 1;
  pageSize.value = event.rows;
}

function handleSearchInput(value: string | undefined): void {
  search.value = value ?? '';
  page.value = 1;
}

function handleDayTypeChange(event: Event): void {
  const target = event.target as HTMLSelectElement | null;
  dayTypeFilter.value = target?.value ?? '';
  page.value = 1;
}

function handleYearChange(event: Event): void {
  const target = event.target as HTMLSelectElement | null;
  selectedYearId.value = target?.value ?? '';
  page.value = 1;
}

function openCreateDrawer(): void {
  editingEntry.value = null;
  isDrawerVisible.value = true;
}

function openEditDrawer(entry: CalendarEntryDto): void {
  editingEntry.value = entry;
  isDrawerVisible.value = true;
}

function promptDelete(entry: CalendarEntryDto): void {
  entryToDelete.value = entry;
  isDeleteDialogOpen.value = true;
}

async function confirmDelete(): Promise<void> {
  if (!entryToDelete.value?.id) return;
  try {
    await deleteMutation.mutateAsync(entryToDelete.value.id);
    isDeleteDialogOpen.value = false;
    entryToDelete.value = null;
  } catch {
    // Handled by mutation toast
  }
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6" data-testid="calendar-entries-tab">
      <!-- Section Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <Calendar class="h-5 w-5 text-brand-accent-600" />
            <h2 class="text-lg font-semibold tracking-tight text-slate-900">
              Calendar &amp; Holidays
            </h2>
          </div>
          <p class="text-sm text-slate-600">
            Define teaching days, public holidays, and emergency closures for the academic year.
          </p>
        </div>

        <Button
          v-if="canManage"
          type="button"
          label="Add Calendar Entry"
          data-testid="create-calendar-entry-button"
          @click="openCreateDrawer"
        >
          <template #icon>
            <Plus class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
      </div>

      <!-- Filter Controls Bar -->
      <div
        class="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between"
      >
        <div class="flex flex-wrap items-center gap-3 w-full sm:w-auto">
          <!-- Academic Year Filter -->
          <div class="min-w-[180px]">
            <select
              :value="selectedYearId"
              aria-label="Filter Academic Year"
              data-testid="calendar-academic-year-filter"
              class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
              @change="handleYearChange"
            >
              <option
                v-for="year in academicYearsLookup ?? []"
                :key="year.id"
                :value="year.id"
              >
                {{ year.name }}
              </option>
            </select>
          </div>

          <!-- Day Type Filter -->
          <div class="min-w-[160px]">
            <select
              :value="dayTypeFilter"
              aria-label="Filter Day Type"
              data-testid="calendar-day-type-filter"
              class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
              @change="handleDayTypeChange"
            >
              <option value="">All Day Types</option>
              <option value="SCHOOL_DAY">School Day</option>
              <option value="HOLIDAY">Holiday</option>
              <option value="CLOSURE">Closure</option>
            </select>
          </div>

          <!-- Search Input -->
          <div class="relative w-full sm:w-64">
            <Search
              class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              :model-value="search"
              placeholder="Search description..."
              class="w-full pl-9"
              data-testid="calendar-entry-search"
              @update:model-value="handleSearchInput"
            />
          </div>
        </div>

        <p class="text-xs text-slate-500 shrink-0">
          Showing {{ calendarEntries.length }} of {{ totalRecords }}
          {{ totalRecords === 1 ? 'entry' : 'entries' }}
        </p>
      </div>

      <!-- Error State -->
      <EmptyState
        v-if="isError"
        variant="error"
        title="Unable to load calendar entries"
        description="There was a problem retrieving the calendar schedule. Please try again."
      >
        <Button type="button" label="Retry" severity="secondary" @click="refetch()" />
      </EmptyState>

      <!-- Calendar Entries Table -->
      <AppDataTable
        v-else
        :value="calendarEntries"
        :loading="isLoading"
        :paginator="totalRecords > pageSize"
        lazy
        :rows="pageSize"
        :first="firstRecordIndex"
        :total-records="totalRecords"
        data-key="id"
        empty-title="No calendar entries found"
        empty-description="No calendar entries or holidays recorded for the selected academic year."
        data-testid="calendar-entries-table"
        @page="handlePageChange"
      >
        <!-- Date Column -->
        <Column field="entryDate" header="Date" class="w-48 align-top">
          <template #body="{ data: entry }">
            <div class="space-y-0.5">
              <span class="font-semibold text-slate-900">{{ formatDisplayDate(entry.entryDate) }}</span>
              <p class="text-xs text-slate-400">{{ entry.entryDate }}</p>
            </div>
          </template>
        </Column>

        <!-- Day Classification Column -->
        <Column field="dayType" header="Classification" class="w-44 align-top">
          <template #body="{ data: entry }">
            <StatusPill :status="entry.dayType ?? 'SCHOOL_DAY'" />
          </template>
        </Column>

        <!-- Description Column -->
        <Column field="description" header="Description / Notes" class="min-w-[240px] align-top">
          <template #body="{ data: entry }">
            <span v-if="entry.description" class="text-sm text-slate-800">{{ entry.description }}</span>
            <span v-else class="text-xs italic text-slate-400">Standard operational schedule</span>
          </template>
        </Column>

        <!-- Actions Column (Gated to users with manage permission) -->
        <Column v-if="canManage" header="Actions" class="w-40 align-top">
          <template #body="{ data: entry }">
            <div class="flex items-center gap-1">
              <Button
                type="button"
                label="Edit"
                size="small"
                severity="secondary"
                text
                :data-testid="`edit-calendar-entry-${entry.id}`"
                @click="openEditDrawer(entry)"
              >
                <template #icon>
                  <Pencil class="mr-1.5 h-3.5 w-3.5" />
                </template>
              </Button>

              <Button
                type="button"
                label="Delete"
                size="small"
                severity="danger"
                text
                :data-testid="`delete-calendar-entry-${entry.id}`"
                @click="promptDelete(entry)"
              >
                <template #icon>
                  <Trash2 class="mr-1.5 h-3.5 w-3.5 text-rose-600" />
                </template>
              </Button>
            </div>
          </template>
        </Column>

        <template #empty>
          <EmptyState
            :variant="search || dayTypeFilter ? 'no-results' : 'empty'"
            :title="search || dayTypeFilter ? 'No matching entries' : 'No calendar entries configured'"
            :description="
              search || dayTypeFilter
                ? 'Try adjusting your filter criteria or clear the search.'
                : 'Add school days, holidays, or emergency closures for this academic year.'
            "
          >
            <Button
              v-if="canManage && !search && !dayTypeFilter"
              type="button"
              label="Add Calendar Entry"
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
      <CalendarEntryFormDrawer
        v-model:visible="isDrawerVisible"
        :academic-year-id="selectedYearId"
        :academic-year-name="selectedAcademicYearName"
        :entry="editingEntry"
      />

      <!-- Tier 2 Delete Confirmation Dialog -->
      <ConfirmActionDialog
        v-model:visible="isDeleteDialogOpen"
        title="Delete Calendar Entry"
        message="Are you sure you want to delete this calendar entry? This action cannot be undone."
        confirm-label="Delete"
        severity="danger"
        :loading="deleteMutation.isPending.value"
        data-testid="delete-calendar-entry-dialog"
        @confirm="confirmDelete"
      />
    </div>
  </AppErrorBoundary>
</template>
