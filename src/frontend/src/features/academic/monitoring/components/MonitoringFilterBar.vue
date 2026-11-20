<script setup lang="ts">
import { computed } from 'vue';
import Select from 'primevue/select';
import Button from 'primevue/button';
import { Calendar, Filter, Layers, RotateCw } from 'lucide-vue-next';
import type { AcademicYearDto } from '../api/academicMonitoring.api';

export interface MonitoringFilterValues {
  academicYearId?: string;
  operationalPeriodId?: string;
  date: string;
}

const props = withDefaults(
  defineProps<{
    modelValue: MonitoringFilterValues;
    academicYears?: AcademicYearDto[];
    loading?: boolean;
  }>(),
  {
    academicYears: () => [],
    loading: false
  }
);

const emit = defineEmits<{
  'update:modelValue': [values: MonitoringFilterValues];
  refresh: [];
}>();

const academicYearOptions = computed(() => {
  return props.academicYears.map((year) => ({
    label: `${year.name ?? 'Academic Year'}${year.status === 'ACTIVE' ? ' (Active)' : ''}`,
    value: year.id ?? ''
  }));
});

const currentYear = computed(() => {
  return props.academicYears.find((y) => y.id === props.modelValue.academicYearId);
});

const periodOptions = computed(() => {
  if (currentYear.value && currentYear.value.periods) {
    return currentYear.value.periods.map((period) => ({
      label: `${period.name ?? 'Period'}${period.status === 'ACTIVE' ? ' (Active)' : ''}`,
      value: period.id ?? ''
    }));
  }

  // Fallback: collect all periods from all academic years
  const allPeriods: Array<{ label: string; value: string }> = [];
  for (const year of props.academicYears) {
    for (const period of year.periods ?? []) {
      if (period.id) {
        allPeriods.push({
          label: `${year.name ?? ''} - ${period.name ?? 'Period'}${period.status === 'ACTIVE' ? ' (Active)' : ''}`,
          value: period.id
        });
      }
    }
  }
  return allPeriods;
});

function onYearChange(newYearId: string | null) {
  const yearId = newYearId ?? undefined;
  const targetYear = props.academicYears.find((y) => y.id === yearId);
  const activePeriod = targetYear?.periods?.find((p) => p.status === 'ACTIVE') ?? targetYear?.periods?.[0];

  emit('update:modelValue', {
    ...props.modelValue,
    academicYearId: yearId,
    operationalPeriodId: activePeriod?.id ?? undefined
  });
}

function onPeriodChange(newPeriodId: string | null) {
  emit('update:modelValue', {
    ...props.modelValue,
    operationalPeriodId: newPeriodId ?? undefined
  });
}

function onDateChange(event: Event) {
  const target = event.target as HTMLInputElement;
  emit('update:modelValue', {
    ...props.modelValue,
    date: target.value
  });
}
</script>

<template>
  <div
    data-testid="monitoring-filter-bar"
    class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs dark:border-slate-800 dark:bg-slate-900"
  >
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4 items-end">
      <!-- Academic Year Selector -->
      <div class="space-y-1.5">
        <label
          for="filter-academic-year"
          class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-400"
        >
          <Layers class="h-3.5 w-3.5 text-slate-400" />
          <span>Tahun Ajaran</span>
        </label>
        <Select
          id="filter-academic-year"
          :model-value="modelValue.academicYearId"
          :options="academicYearOptions"
          option-label="label"
          option-value="value"
          placeholder="Pilih Tahun Ajaran"
          class="w-full text-sm"
          data-testid="select-academic-year"
          @update:model-value="onYearChange"
        />
      </div>

      <!-- Operational Period Selector -->
      <div class="space-y-1.5">
        <label
          for="filter-operational-period"
          class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-400"
        >
          <Filter class="h-3.5 w-3.5 text-slate-400" />
          <span>Periode Operasional</span>
        </label>
        <Select
          id="filter-operational-period"
          :model-value="modelValue.operationalPeriodId"
          :options="periodOptions"
          option-label="label"
          option-value="value"
          placeholder="Pilih Periode"
          class="w-full text-sm"
          data-testid="select-operational-period"
          @update:model-value="onPeriodChange"
        />
      </div>

      <!-- Date Picker -->
      <div class="space-y-1.5">
        <label
          for="monitoring-date-picker"
          class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-400"
        >
          <Calendar class="h-3.5 w-3.5 text-slate-400" />
          <span>Tanggal Presensi</span>
        </label>
        <input
          id="monitoring-date-picker"
          type="date"
          :value="modelValue.date"
          data-testid="date-picker"
          class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-xs transition-colors focus:border-brand-accent-500 focus:outline-hidden focus:ring-1 focus:ring-brand-accent-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
          @input="onDateChange"
        />
      </div>

      <!-- Action Button -->
      <div class="flex items-center gap-2">
        <Button
          type="button"
          icon="pi pi-refresh"
          severity="secondary"
          outlined
          class="w-full text-sm font-medium"
          :loading="loading"
          data-testid="btn-refresh-monitoring"
          @click="emit('refresh')"
        >
          <template #icon>
            <RotateCw class="h-4 w-4 mr-1.5" :class="{ 'animate-spin': loading }" />
          </template>
          Segarkan Data
        </Button>
      </div>
    </div>
  </div>
</template>
