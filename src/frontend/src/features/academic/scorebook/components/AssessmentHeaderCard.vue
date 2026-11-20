<script setup lang="ts">
import { computed } from 'vue';
import type { GradebookAssignmentDto } from '../api/scorebook.api';

const props = defineProps<{
  assignment: GradebookAssignmentDto;
}>();

const badgeClasses = computed(() => {
  const type = (props.assignment.assignmentType ?? '').toUpperCase();
  switch (type) {
    case 'FORMATIVE':
      return 'bg-sky-50 text-sky-700 border-sky-200';
    case 'SUMMATIVE':
      return 'bg-amber-50 text-amber-700 border-amber-200';
    case 'EXAM':
      return 'bg-purple-50 text-purple-700 border-purple-200';
    default:
      return 'bg-slate-50 text-slate-700 border-slate-200';
  }
});
</script>

<template>
  <div
    class="flex flex-col gap-1.5 p-2 text-left"
    :data-testid="`assessment-header-${assignment.id}`"
  >
    <div class="flex items-center justify-between gap-1.5">
      <span
        class="inline-flex items-center rounded-md border px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider"
        :class="badgeClasses"
        data-testid="assessment-type-badge"
      >
        {{ assignment.assignmentType ?? 'OTHER' }}
      </span>
      <span
        v-if="assignment.weight !== null && assignment.weight !== undefined"
        class="text-[11px] font-medium text-slate-500"
        data-testid="assessment-weight"
      >
        {{ assignment.weight }}%
      </span>
      <span v-else class="text-[11px] text-slate-400" data-testid="assessment-weight">
        -
      </span>
    </div>

    <div
      class="truncate font-semibold text-xs text-slate-900"
      :title="assignment.title ?? ''"
      data-testid="assessment-title"
    >
      {{ assignment.title }}
    </div>

    <div class="flex items-center justify-between text-[11px] text-slate-500">
      <span>Max: {{ assignment.maxScore }}</span>
      <span v-if="assignment.dueDate" class="text-[10px] text-slate-400">
        Due {{ assignment.dueDate }}
      </span>
    </div>
  </div>
</template>
