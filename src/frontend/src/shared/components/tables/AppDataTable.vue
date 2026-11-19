<script setup lang="ts" generic="T extends object">
import DataTable, { type DataTablePageEvent, type DataTableSortEvent } from 'primevue/datatable';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';

withDefaults(
  defineProps<{
    value: T[];
    loading?: boolean;
    paginator?: boolean;
    lazy?: boolean;
    rows?: number;
    first?: number;
    totalRecords?: number;
    dataKey?: string;
    sortField?: string;
    sortOrder?: 1 | -1 | 0;
    stripedRows?: boolean;
    emptyTitle?: string;
    emptyDescription?: string;
  }>(),
  {
    loading: false,
    paginator: false,
    lazy: false,
    rows: 10,
    first: 0,
    totalRecords: 0,
    dataKey: 'id',
    sortField: undefined,
    sortOrder: undefined,
    stripedRows: false,
    emptyTitle: 'No records found',
    emptyDescription: 'There are no items to display right now.'
  }
);

const emit = defineEmits<{
  page: [event: DataTablePageEvent];
  sort: [event: DataTableSortEvent];
}>();
</script>

<template>
  <DataTable
    :value="value"
    :loading="loading"
    :paginator="paginator"
    :lazy="lazy"
    :rows="rows"
    :first="first"
    :total-records="totalRecords"
    :data-key="dataKey"
    :sort-field="sortField"
    :sort-order="sortOrder"
    :striped-rows="stripedRows"
    scrollable
    @page="emit('page', $event)"
    @sort="emit('sort', $event)"
  >
    <template v-if="$slots.header" #header>
      <slot name="header" />
    </template>

    <slot />

    <template #empty>
      <slot name="empty">
        <EmptyState :title="emptyTitle" :description="emptyDescription" variant="empty" />
      </slot>
    </template>
  </DataTable>
</template>
