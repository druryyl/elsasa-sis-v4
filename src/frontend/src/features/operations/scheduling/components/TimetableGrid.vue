<script setup lang="ts">
import { computed } from 'vue';
import { AlertCircle, Plus } from 'lucide-vue-next';
import type { StudyGroupTimetableSlotDto } from '../api/timetables.api';

const props = withDefaults(
  defineProps<{
    slots: StudyGroupTimetableSlotDto[];
    isLoading?: boolean;
    readOnly?: boolean;
  }>(),
  {
    isLoading: false,
    readOnly: false
  }
);

const emit = defineEmits<{
  selectSlot: [
    payload: {
      dayOfWeek: number;
      periodSlot: number;
      slot: StudyGroupTimetableSlotDto | null;
    }
  ];
}>();

const DAYS = [
  { id: 1, name: 'Monday', labelId: 'Senin' },
  { id: 2, name: 'Tuesday', labelId: 'Selasa' },
  { id: 3, name: 'Wednesday', labelId: 'Rabu' },
  { id: 4, name: 'Thursday', labelId: 'Kamis' },
  { id: 5, name: 'Friday', labelId: 'Jumat' },
  { id: 6, name: 'Saturday', labelId: 'Sabtu' }
];

const PERIOD_SLOTS = Array.from({ length: 10 }, (_, i) => i + 1);

// Map slots by key `${dayOfWeek}-${periodSlot}`
const slotsMap = computed(() => {
  const map = new Map<string, StudyGroupTimetableSlotDto>();
  for (const slot of props.slots) {
    if (slot.dayOfWeek && slot.periodSlot) {
      map.set(`${slot.dayOfWeek}-${slot.periodSlot}`, slot);
    }
  }
  return map;
});

function getSlot(dayOfWeek: number, periodSlot: number): StudyGroupTimetableSlotDto | null {
  return slotsMap.value.get(`${dayOfWeek}-${periodSlot}`) ?? null;
}

function formatSlotTime(timeStr?: string | null): string {
  if (!timeStr) return '';
  const parts = timeStr.split(':');
  if (parts.length >= 2) {
    return `${parts[0]}:${parts[1]}`;
  }
  return timeStr;
}

function handleSlotClick(dayOfWeek: number, periodSlot: number): void {
  const slot = getSlot(dayOfWeek, periodSlot);
  if (props.readOnly && !slot) return;
  emit('selectSlot', { dayOfWeek, periodSlot, slot });
}

function getConflictTooltip(slot: StudyGroupTimetableSlotDto): string {
  const reasons: string[] = [];
  if (slot.teacherConflict) {
    reasons.push(
      `Teacher conflict: ${slot.teacherName ?? 'Teacher'} is scheduled in another class at this time`
    );
  }
  if (slot.roomConflict) {
    reasons.push(
      `Room conflict: ${slot.roomName ?? 'Room'} is double-booked by another class at this time`
    );
  }
  return reasons.join(' • ');
}
</script>

<template>
  <div class="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden" data-testid="timetable-grid">
    <!-- Horizontal Scroll Wrapper for dense matrix -->
    <div class="overflow-x-auto">
      <table class="w-full min-w-[960px] border-collapse text-left">
        <thead>
          <tr class="border-b border-slate-200 bg-slate-50/80 text-xs text-slate-500 font-semibold uppercase tracking-wider">
            <!-- Period Slot Column Header -->
            <th class="w-20 px-3 py-3 text-center border-r border-slate-200">
              Period
            </th>
            <!-- Days Columns -->
            <th
              v-for="day in DAYS"
              :key="day.id"
              class="px-3 py-3 text-center border-r border-slate-200 last:border-r-0"
            >
              <div class="font-bold text-slate-800">{{ day.name }}</div>
              <div class="text-[11px] font-normal text-slate-400">({{ day.labelId }})</div>
            </th>
          </tr>
        </thead>

        <tbody class="divide-y divide-slate-200 text-sm">
          <tr v-for="period in PERIOD_SLOTS" :key="period" class="hover:bg-slate-50/30 transition-colors">
            <!-- Slot Index Cell -->
            <td class="w-20 px-2 py-3 text-center border-r border-slate-200 bg-slate-50/40 font-mono text-xs font-semibold text-slate-600">
              Slot {{ period }}
            </td>

            <!-- Day Cells -->
            <td
              v-for="day in DAYS"
              :key="day.id"
              class="p-2 border-r border-slate-200 last:border-r-0 align-top h-24 w-[150px] transition-all"
              :data-testid="`cell-${day.id}-${period}`"
            >
              <div v-if="isLoading" class="h-full w-full animate-pulse rounded-lg bg-slate-100" />

              <!-- Populated Slot -->
              <div
                v-else-if="getSlot(day.id, period)"
                class="group relative flex h-full flex-col justify-between rounded-lg border p-2 text-xs transition-shadow cursor-pointer select-none"
                :class="[
                  getSlot(day.id, period)?.teacherConflict || getSlot(day.id, period)?.roomConflict
                    ? 'border-rose-400 bg-rose-50/60 hover:bg-rose-50 hover:shadow-xs'
                    : 'border-brand-primary-200 bg-brand-primary-50/40 hover:bg-brand-primary-50/80 hover:shadow-xs'
                ]"
                :data-testid="`slot-${day.id}-${period}`"
                @click="handleSlotClick(day.id, period)"
              >
                <!-- Subject & Conflict Indicator -->
                <div class="space-y-1">
                  <div class="flex items-start justify-between gap-1">
                    <span class="font-semibold text-slate-900 line-clamp-2 leading-tight" data-testid="slot-subject-name">
                      {{ getSlot(day.id, period)?.subjectName }}
                    </span>

                    <!-- Rose-600 Conflict Badge & Tooltip (D-10) -->
                    <span
                      v-if="getSlot(day.id, period)?.teacherConflict || getSlot(day.id, period)?.roomConflict"
                      class="inline-flex shrink-0 items-center gap-0.5 rounded-full bg-rose-600 px-1.5 py-0.5 text-[10px] font-bold text-white shadow-xs"
                      :title="getConflictTooltip(getSlot(day.id, period)!)"
                      data-testid="conflict-badge"
                    >
                      <AlertCircle class="h-3 w-3" />
                      <span>Conflict</span>
                    </span>
                  </div>

                  <!-- Teacher Name -->
                  <div class="text-[11px] text-slate-600 line-clamp-1" data-testid="slot-teacher-name">
                    {{ getSlot(day.id, period)?.teacherName }}
                  </div>
                </div>

                <!-- Footer: Room & Time Range -->
                <div class="mt-2 flex items-center justify-between text-[10px] text-slate-500 font-mono">
                  <span
                    v-if="getSlot(day.id, period)?.roomName"
                    class="rounded bg-white/80 px-1 py-0.5 text-slate-700 border border-slate-200 line-clamp-1"
                    data-testid="slot-room-name"
                  >
                    {{ getSlot(day.id, period)?.roomName }}
                  </span>
                  <span v-else class="text-slate-400 italic">No room</span>

                  <span class="tabular-nums" data-testid="slot-time-range">
                    {{ formatSlotTime(getSlot(day.id, period)?.startTime) }}–{{ formatSlotTime(getSlot(day.id, period)?.endTime) }}
                  </span>
                </div>
              </div>

              <!-- Empty Slot Cell -->
              <div
                v-else
                class="flex h-full w-full flex-col items-center justify-center rounded-lg border border-dashed border-slate-200 p-2 text-center text-slate-300 transition-colors"
                :class="[
                  readOnly
                    ? 'cursor-default bg-slate-50/20'
                    : 'cursor-pointer hover:border-slate-400 hover:bg-slate-50/60 hover:text-slate-600'
                ]"
                :data-testid="`empty-slot-${day.id}-${period}`"
                @click="handleSlotClick(day.id, period)"
              >
                <Plus v-if="!readOnly" class="h-4 w-4 opacity-70" />
                <span class="text-[10px]">{{ readOnly ? '—' : 'Assign' }}</span>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
