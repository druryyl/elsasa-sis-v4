<script setup lang="ts">
export type AttendanceStatusCode = 'PRESENT' | 'ABSENT' | 'EXCUSED' | 'SICK' | 'LATE';

export interface AttendanceRosterItem {
  studentId: string;
  studentName: string;
  studentNis: string;
  attendanceRecordId: string | null;
  status: AttendanceStatusCode | null;
  notes: string;
  isDirty?: boolean;
}

const STATUS_CHIPS: {
  code: AttendanceStatusCode;
  label: string;
  activeClass: string;
  inactiveClass: string;
}[] = [
  {
    code: 'PRESENT',
    label: 'Present',
    activeClass:
      'bg-emerald-600 text-white border-emerald-600 shadow-xs ring-2 ring-emerald-300 ring-offset-1 font-semibold',
    inactiveClass:
      'bg-white text-slate-600 border-slate-200 hover:border-emerald-400 hover:bg-emerald-50/60 hover:text-emerald-700'
  },
  {
    code: 'ABSENT',
    label: 'Absent',
    activeClass:
      'bg-rose-600 text-white border-rose-600 shadow-xs ring-2 ring-rose-300 ring-offset-1 font-semibold',
    inactiveClass:
      'bg-white text-slate-600 border-slate-200 hover:border-rose-400 hover:bg-rose-50/60 hover:text-rose-700'
  },
  {
    code: 'EXCUSED',
    label: 'Excused',
    activeClass:
      'bg-blue-600 text-white border-blue-600 shadow-xs ring-2 ring-blue-300 ring-offset-1 font-semibold',
    inactiveClass:
      'bg-white text-slate-600 border-slate-200 hover:border-blue-400 hover:bg-blue-50/60 hover:text-blue-700'
  },
  {
    code: 'SICK',
    label: 'Sick',
    activeClass:
      'bg-amber-500 text-white border-amber-500 shadow-xs ring-2 ring-amber-300 ring-offset-1 font-semibold',
    inactiveClass:
      'bg-white text-slate-600 border-slate-200 hover:border-amber-400 hover:bg-amber-50/60 hover:text-amber-700'
  },
  {
    code: 'LATE',
    label: 'Late',
    activeClass:
      'bg-orange-500 text-white border-orange-500 shadow-xs ring-2 ring-orange-300 ring-offset-1 font-semibold',
    inactiveClass:
      'bg-white text-slate-600 border-slate-200 hover:border-orange-400 hover:bg-orange-50/60 hover:text-orange-700'
  }
];

withDefaults(
  defineProps<{
    items: AttendanceRosterItem[];
    canRecord?: boolean;
    disabled?: boolean;
  }>(),
  {
    canRecord: true,
    disabled: false
  }
);

const emit = defineEmits<{
  'update-status': [studentId: string, status: AttendanceStatusCode];
  'update-notes': [studentId: string, notes: string];
}>();

function getInitials(name?: string | null): string {
  if (!name) return 'S';
  const parts = name.trim().split(/\s+/).filter(Boolean);
  const first = parts[0];
  if (!first) return 'S';
  if (parts.length === 1) return first.slice(0, 2).toUpperCase();
  const last = parts[parts.length - 1];
  if (!last) return first.slice(0, 2).toUpperCase();
  return (first[0]! + last[0]!).toUpperCase();
}
</script>

<template>
  <div
    class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs"
    data-testid="attendance-roster-grid"
  >
    <div class="overflow-x-auto">
      <table class="w-full text-left border-collapse">
        <thead>
          <tr class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-600">
            <th class="py-3 px-4 w-12 text-center">#</th>
            <th class="py-3 px-4 min-w-[220px]">Student</th>
            <th class="py-3 px-4 min-w-[320px]">Attendance Status</th>
            <th class="py-3 px-4 min-w-[240px]">Notes</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 text-sm">
          <tr
            v-for="(item, index) in items"
            :key="item.studentId"
            :class="[
              'transition-colors duration-150 hover:bg-slate-50/80',
              item.isDirty ? 'border-l-4 border-l-amber-500 bg-amber-50/20' : 'border-l-4 border-l-transparent'
            ]"
            :data-testid="`roster-row-${item.studentId}`"
          >
            <!-- Row Number -->
            <td class="py-3 px-4 text-center text-xs font-medium text-slate-400">
              {{ index + 1 }}
            </td>

            <!-- Student Info (Avatar, Name, NIS) -->
            <td class="py-3 px-4">
              <div class="flex items-center gap-3">
                <div
                  class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-accent-50 text-xs font-bold text-brand-accent-700 ring-1 ring-brand-accent-200/60"
                  :title="item.studentName"
                  data-testid="student-avatar"
                >
                  <span>{{ getInitials(item.studentName) }}</span>
                </div>
                <div class="min-w-0">
                  <div class="flex items-center gap-1.5">
                    <span
                      class="font-medium text-slate-900 truncate text-sm"
                      :data-testid="`student-name-${item.studentId}`"
                    >
                      {{ item.studentName }}
                    </span>
                    <span
                      v-if="item.isDirty"
                      class="inline-flex items-center rounded-full bg-amber-100 px-1.5 py-0.2 text-[10px] font-semibold text-amber-800"
                      title="Modified (Unsaved)"
                      data-testid="dirty-indicator"
                    >
                      Edited
                    </span>
                  </div>
                  <div class="flex items-center gap-2 text-xs text-slate-500">
                    <span>NIS: {{ item.studentNis || '—' }}</span>
                  </div>
                </div>
              </div>
            </td>

            <!-- Attendance Status Chips -->
            <td class="py-3 px-4">
              <div
                class="flex flex-wrap items-center gap-1.5"
                role="radiogroup"
                :aria-label="`Attendance status for ${item.studentName}`"
              >
                <button
                  v-for="chip in STATUS_CHIPS"
                  :key="chip.code"
                  type="button"
                  :disabled="disabled || !canRecord"
                  :aria-checked="item.status === chip.code"
                  role="radio"
                  :class="[
                    'inline-flex items-center justify-center rounded-lg border px-2.5 py-1 text-xs transition-all duration-150',
                    item.status === chip.code ? chip.activeClass : chip.inactiveClass,
                    disabled || !canRecord ? 'cursor-not-allowed opacity-60' : 'cursor-pointer'
                  ]"
                  :data-testid="`chip-${chip.code.toLowerCase()}-${item.studentId}`"
                  @click="emit('update-status', item.studentId, chip.code)"
                >
                  {{ chip.label }}
                </button>
              </div>
            </td>

            <!-- Notes Input -->
            <td class="py-3 px-4">
              <input
                type="text"
                :value="item.notes"
                :disabled="disabled || !canRecord"
                maxlength="500"
                placeholder="Optional notes..."
                class="w-full rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs text-slate-800 placeholder-slate-400 shadow-2xs transition-colors focus:border-brand-accent-500 focus:outline-hidden focus:ring-1 focus:ring-brand-accent-500 disabled:cursor-not-allowed disabled:bg-slate-50"
                :data-testid="`notes-input-${item.studentId}`"
                @input="emit('update-notes', item.studentId, ($event.target as HTMLInputElement).value)"
              />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
