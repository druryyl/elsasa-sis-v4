<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import type { DataTablePageEvent } from 'primevue/datatable';
import { Building2, Pencil, Plus, Search } from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import type { RoomDto } from '../api/rooms.api';
import { useRoomsQuery } from '../api/useRoomQueries';
import RoomFormDrawer from './RoomFormDrawer.vue';

withDefaults(
  defineProps<{
    canManage?: boolean;
  }>(),
  {
    canManage: true
  }
);

const page = ref(1);
const pageSize = ref(10);
const search = ref('');
const roomTypeFilter = ref('');

const queryParams = computed(() => ({
  page: page.value,
  pageSize: pageSize.value,
  search: search.value || undefined,
  roomType: roomTypeFilter.value || undefined,
  sortField: 'Name',
  sortOrder: 'asc'
}));

const { data, isLoading, isError, refetch } = useRoomsQuery(queryParams);

const rooms = computed(() => data.value?.items ?? []);
const totalRecords = computed(() => data.value?.totalCount ?? 0);
const firstRecordIndex = computed(() => (page.value - 1) * pageSize.value);

const isDrawerVisible = ref(false);
const editingRoom = ref<RoomDto | null>(null);

function handlePageChange(event: DataTablePageEvent): void {
  page.value = event.page + 1;
  pageSize.value = event.rows;
}

function handleSearchInput(value: string | undefined): void {
  search.value = value ?? '';
  page.value = 1;
}

function handleTypeChange(event: Event): void {
  const target = event.target as HTMLSelectElement | null;
  roomTypeFilter.value = target?.value ?? '';
  page.value = 1;
}

function openCreateDrawer(): void {
  editingRoom.value = null;
  isDrawerVisible.value = true;
}

function openEditDrawer(room: RoomDto): void {
  editingRoom.value = room;
  isDrawerVisible.value = true;
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6" data-testid="rooms-tab">
      <!-- Section Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <Building2 class="h-5 w-5 text-brand-accent-600" />
            <h2 class="text-lg font-semibold tracking-tight text-slate-900">
              Rooms &amp; Facilities
            </h2>
          </div>
          <p class="text-sm text-slate-600">
            Manage instructional classrooms, science labs, halls, and other physical school facilities.
          </p>
        </div>

        <Button
          v-if="canManage"
          type="button"
          label="Add Room"
          data-testid="create-room-button"
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
          <!-- Room Type Filter -->
          <div class="min-w-[180px]">
            <select
              :value="roomTypeFilter"
              aria-label="Filter Facility Type"
              data-testid="room-type-filter"
              class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
              @change="handleTypeChange"
            >
              <option value="">All Facility Types</option>
              <option value="CLASSROOM">Classroom</option>
              <option value="LABORATORY">Laboratory</option>
              <option value="HALL">Hall / Auditorium</option>
              <option value="OTHER">Other Facility</option>
            </select>
          </div>

          <!-- Search Input -->
          <div class="relative w-full sm:w-64">
            <Search
              class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              :model-value="search"
              placeholder="Search room name..."
              class="w-full pl-9"
              data-testid="room-search"
              @update:model-value="handleSearchInput"
            />
          </div>
        </div>

        <p class="text-xs text-slate-500 shrink-0">
          Showing {{ rooms.length }} of {{ totalRecords }}
          {{ totalRecords === 1 ? 'room' : 'rooms' }}
        </p>
      </div>

      <!-- Error State -->
      <EmptyState
        v-if="isError"
        variant="error"
        title="Unable to load facilities"
        description="There was a problem retrieving room and facility inventory. Please try again."
      >
        <Button type="button" label="Retry" severity="secondary" @click="refetch()" />
      </EmptyState>

      <!-- Rooms Data Table -->
      <AppDataTable
        v-else
        :value="rooms"
        :loading="isLoading"
        :paginator="totalRecords > pageSize"
        lazy
        :rows="pageSize"
        :first="firstRecordIndex"
        :total-records="totalRecords"
        data-key="id"
        empty-title="No facilities configured"
        empty-description="Create classrooms, science laboratories, or halls to configure school facilities."
        data-testid="rooms-table"
        @page="handlePageChange"
      >
        <!-- Name Column -->
        <Column field="name" header="Room / Facility Name" class="min-w-[200px] align-top">
          <template #body="{ data: room }">
            <div class="space-y-0.5">
              <span class="font-semibold text-slate-900">{{ room.name }}</span>
            </div>
          </template>
        </Column>

        <!-- Room Type Column -->
        <Column field="roomType" header="Facility Type" class="w-48 align-top">
          <template #body="{ data: room }">
            <StatusPill :status="room.roomType ?? 'CLASSROOM'" />
          </template>
        </Column>

        <!-- Capacity Column -->
        <Column field="capacity" header="Student Capacity" class="w-40 align-top">
          <template #body="{ data: room }">
            <span class="text-sm font-medium text-slate-800">
              {{ room.capacity }} {{ room.capacity === 1 ? 'seat' : 'seats' }}
            </span>
          </template>
        </Column>

        <!-- Actions Column (Gated to users with manage permission) -->
        <Column v-if="canManage" header="Actions" class="w-32 align-top">
          <template #body="{ data: room }">
            <Button
              type="button"
              label="Edit"
              size="small"
              severity="secondary"
              text
              :data-testid="`edit-room-${room.id}`"
              @click="openEditDrawer(room)"
            >
              <template #icon>
                <Pencil class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>
          </template>
        </Column>

        <template #empty>
          <EmptyState
            :variant="search || roomTypeFilter ? 'no-results' : 'empty'"
            :title="search || roomTypeFilter ? 'No matching facilities' : 'No facilities configured'"
            :description="
              search || roomTypeFilter
                ? 'Try adjusting your search or facility type filter.'
                : 'Create classrooms, science laboratories, or halls to configure school facilities.'
            "
          >
            <Button
              v-if="canManage && !search && !roomTypeFilter"
              type="button"
              label="Add Room"
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
      <RoomFormDrawer
        v-model:visible="isDrawerVisible"
        :room="editingRoom"
      />
    </div>
  </AppErrorBoundary>
</template>
