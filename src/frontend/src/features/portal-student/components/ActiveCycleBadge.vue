<script setup lang="ts">
import { computed } from 'vue';
import { RefreshCw, Wrench, BookOpen, Calendar } from 'lucide-vue-next';
import type { ActiveSmkCycleDto } from '../api/studentPortal.api';

const props = defineProps<{
  activeCycle?: ActiveSmkCycleDto | null;
}>();

const isSmkRotation = computed(() => Boolean(props.activeCycle?.isSmkRotation));

const scheduleType = computed(() => (props.activeCycle?.scheduleType ?? 'REGULAR').toUpperCase());

const cycleTitle = computed(() => {
  if (isSmkRotation.value) {
    if (scheduleType.value === 'VOCATIONAL') {
      return 'Siklus Kejuruan (Vocational)';
    }
    if (scheduleType.value === 'GENERAL') {
      return 'Siklus Umum (General)';
    }
    return `Siklus ${props.activeCycle?.scheduleType}`;
  }
  return 'Jadwal Reguler';
});

const dateRange = computed(() => {
  if (!props.activeCycle?.startDate && !props.activeCycle?.endDate) {
    return null;
  }
  if (props.activeCycle?.startDate && props.activeCycle?.endDate) {
    return `${props.activeCycle.startDate} s/d ${props.activeCycle.endDate}`;
  }
  return props.activeCycle?.startDate ?? props.activeCycle?.endDate ?? null;
});

const badgeVariant = computed(() => {
  if (isSmkRotation.value) {
    if (scheduleType.value === 'VOCATIONAL') {
      return {
        container: 'bg-emerald-50 text-emerald-800 border-emerald-200 ring-emerald-500/20',
        badge: 'bg-emerald-600 text-white',
        icon: Wrench
      };
    }
    return {
      container: 'bg-indigo-50 text-indigo-800 border-indigo-200 ring-indigo-500/20',
      badge: 'bg-indigo-600 text-white',
      icon: BookOpen
    };
  }
  return {
    container: 'bg-slate-50 text-slate-700 border-slate-200 ring-slate-500/10',
    badge: 'bg-slate-500 text-white',
    icon: Calendar
  };
});
</script>

<template>
  <div
    data-testid="active-cycle-badge"
    :class="[
      'inline-flex flex-wrap items-center gap-2.5 rounded-xl border px-3.5 py-2 text-sm font-medium shadow-2xs transition sm:px-4 sm:py-2.5',
      badgeVariant.container
    ]"
  >
    <div class="flex items-center gap-2">
      <span
        class="flex h-6 w-6 items-center justify-center rounded-lg shadow-2xs"
        :class="badgeVariant.badge"
      >
        <component :is="badgeVariant.icon" class="h-3.5 w-3.5" />
      </span>
      <span data-testid="cycle-text" class="font-semibold tracking-tight">
        {{ cycleTitle }}
      </span>
    </div>

    <div v-if="dateRange" class="flex items-center gap-1.5 text-xs opacity-90 sm:text-sm">
      <span class="hidden text-slate-300 sm:inline">•</span>
      <span class="inline-flex items-center gap-1 font-mono">
        <RefreshCw v-if="isSmkRotation" class="h-3 w-3 animate-spin text-slate-400" />
        <span data-testid="cycle-dates">{{ dateRange }}</span>
      </span>
    </div>
  </div>
</template>
