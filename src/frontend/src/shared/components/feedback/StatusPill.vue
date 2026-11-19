<script setup lang="ts">
import { computed } from 'vue';

export type StatusIntent = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

const STATUS_INTENT_MAP: Record<string, StatusIntent> = {
  // Neutral / Draft / Inactive
  DRAFT: 'neutral',
  INACTIVE: 'neutral',
  NOT_RECORDED: 'neutral',
  ARCHIVED: 'neutral',
  CLOSED: 'neutral',

  // Informational / Queued
  REGISTERED: 'info',
  SUBMITTED: 'info',
  SCHEDULED: 'info',
  GENERAL: 'info',
  VOCATIONAL: 'info',
  REGULAR: 'info',

  // Success / Active / Terminal Positive
  ACTIVE: 'success',
  ACCEPTED: 'success',
  ENROLLED: 'success',
  PRESENT: 'success',
  FINALIZED: 'success',
  COMPLETED: 'success',
  PAID: 'success',
  PUBLISHED: 'success',
  POSITIVE: 'success',
  SCHOOL_DAY: 'success',
  ASSIGNED: 'success',

  // Warning / Partial / Attention
  PARTIALLY_PAID: 'warning',
  SICK: 'warning',
  EXCUSED: 'warning',
  LATE: 'warning',
  UNDER_REVIEW: 'warning',
  READY_TO_FINALIZE: 'warning',
  VERBAL_WARNING: 'warning',
  WRITTEN_WARNING: 'warning',
  PENDING: 'warning',
  HOLIDAY: 'warning',

  // Danger / Blocked / Terminal Negative
  REJECTED: 'danger',
  ABSENT: 'danger',
  OVERDUE: 'danger',
  WITHDRAWN: 'danger',
  REMOVED: 'danger',
  PARENT_MEETING: 'danger',
  SUSPENSION: 'danger',
  NEGATIVE: 'danger',
  CLOSURE: 'danger'
};

const INTENT_CLASSES: Record<StatusIntent, string> = {
  neutral: 'bg-slate-100 text-slate-700 border-slate-200',
  info: 'bg-sky-50 text-sky-700 border-sky-200',
  success: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  warning: 'bg-amber-50 text-amber-700 border-amber-200',
  danger: 'bg-rose-50 text-rose-700 border-rose-200'
};

const DOT_CLASSES: Record<StatusIntent, string> = {
  neutral: 'bg-slate-500',
  info: 'bg-sky-600',
  success: 'bg-emerald-600',
  warning: 'bg-amber-600',
  danger: 'bg-rose-600'
};

const props = withDefaults(
  defineProps<{
    status: string;
    label?: string;
    intent?: StatusIntent;
    showDot?: boolean;
  }>(),
  {
    label: undefined,
    intent: undefined,
    showDot: true
  }
);

const normalizedKey = computed(() => props.status.trim().toUpperCase().replace(/\s+/g, '_'));

const resolvedIntent = computed<StatusIntent>(
  () => props.intent ?? STATUS_INTENT_MAP[normalizedKey.value] ?? 'neutral'
);

const formattedLabel = computed(() => {
  if (props.label) {
    return props.label;
  }
  return props.status
    .replace(/_/g, ' ')
    .toLowerCase()
    .replace(/\b\w/g, (char) => char.toUpperCase());
});
</script>

<template>
  <span
    data-testid="status-pill"
    class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium whitespace-nowrap"
    :class="INTENT_CLASSES[resolvedIntent]"
  >
    <span
      v-if="showDot"
      class="h-1.5 w-1.5 rounded-full"
      :class="DOT_CLASSES[resolvedIntent]"
      aria-hidden="true"
    />
    <span>{{ formattedLabel }}</span>
  </span>
</template>
