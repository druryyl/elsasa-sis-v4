<script setup lang="ts">
import Button from 'primevue/button';
import { CheckCheck, Users } from 'lucide-vue-next';

withDefaults(
  defineProps<{
    rosterLength: number;
    presentCount: number;
    absentCount: number;
    excusedCount: number;
    sickCount: number;
    lateCount: number;
    unmarkedCount: number;
    canRecord?: boolean;
    disabled?: boolean;
  }>(),
  {
    canRecord: true,
    disabled: false
  }
);

const emit = defineEmits<{
  'mark-all-present': [];
}>();
</script>

<template>
  <div
    class="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center sm:justify-between"
    data-testid="attendance-action-bar"
  >
    <!-- Left: Status Summary Badges -->
    <div class="flex flex-wrap items-center gap-2">
      <div
        class="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700"
        data-testid="tally-total"
      >
        <Users class="h-3.5 w-3.5 text-slate-500" />
        <span>Total: {{ rosterLength }}</span>
      </div>

      <div
        class="inline-flex items-center gap-1.5 rounded-lg bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700 border border-emerald-200"
        data-testid="tally-present"
      >
        <span class="h-2 w-2 rounded-full bg-emerald-500" />
        <span>Present: {{ presentCount }}</span>
      </div>

      <div
        class="inline-flex items-center gap-1.5 rounded-lg bg-rose-50 px-2.5 py-1 text-xs font-semibold text-rose-700 border border-rose-200"
        data-testid="tally-absent"
      >
        <span class="h-2 w-2 rounded-full bg-rose-500" />
        <span>Absent: {{ absentCount }}</span>
      </div>

      <div
        class="inline-flex items-center gap-1.5 rounded-lg bg-blue-50 px-2.5 py-1 text-xs font-semibold text-blue-700 border border-blue-200"
        data-testid="tally-excused"
      >
        <span class="h-2 w-2 rounded-full bg-blue-500" />
        <span>Excused: {{ excusedCount }}</span>
      </div>

      <div
        class="inline-flex items-center gap-1.5 rounded-lg bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700 border border-amber-200"
        data-testid="tally-sick"
      >
        <span class="h-2 w-2 rounded-full bg-amber-500" />
        <span>Sick: {{ sickCount }}</span>
      </div>

      <div
        class="inline-flex items-center gap-1.5 rounded-lg bg-orange-50 px-2.5 py-1 text-xs font-semibold text-orange-700 border border-orange-200"
        data-testid="tally-late"
      >
        <span class="h-2 w-2 rounded-full bg-orange-500" />
        <span>Late: {{ lateCount }}</span>
      </div>

      <div
        v-if="unmarkedCount > 0"
        class="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-500 border border-slate-300"
        data-testid="tally-unmarked"
      >
        <span class="h-2 w-2 rounded-full bg-slate-400" />
        <span>Unmarked: {{ unmarkedCount }}</span>
      </div>
    </div>

    <!-- Right: Quick-Mark Action Button -->
    <div v-if="canRecord" class="flex items-center gap-2">
      <Button
        type="button"
        severity="success"
        size="small"
        :disabled="disabled || rosterLength === 0"
        class="!bg-emerald-600 hover:!bg-emerald-700 !border-emerald-600 !text-white flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium"
        data-testid="mark-all-present-btn"
        @click="emit('mark-all-present')"
      >
        <CheckCheck class="h-4 w-4" />
        <span>Mark All Present</span>
      </Button>
    </div>
  </div>
</template>
