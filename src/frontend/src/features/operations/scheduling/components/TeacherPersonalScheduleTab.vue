<script setup lang="ts">
import { computed, ref } from 'vue';
import { CheckCircle2, Clock, Info } from 'lucide-vue-next';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import type { TeacherPersonalScheduleSlotDto } from '../api/timetables.api';
import { useTeacherPersonalScheduleQuery } from '../api/useTimetableQueries';
import type { ScheduleType } from '../schemas/timetable.schema';

const props = defineProps<{
  operationalPeriodId: string;
}>();

const { data: scheduleData, isLoading, isError } = useTeacherPersonalScheduleQuery(
  computed(() => ({ periodId: props.operationalPeriodId })),
  { enabled: computed(() => Boolean(props.operationalPeriodId)) }
);

const activeScheduleType = ref<ScheduleType>('REGULAR');

const DAYS = [
  { id: 1, name: 'Monday', labelId: 'Senin' },
  { id: 2, name: 'Tuesday', labelId: 'Selasa' },
  { id: 3, name: 'Wednesday', labelId: 'Rabu' },
  { id: 4, name: 'Thursday', labelId: 'Kamis' },
  { id: 5, name: 'Friday', labelId: 'Jumat' },
  { id: 6, name: 'Saturday', labelId: 'Sabtu' }
];

const PERIOD_SLOTS = Array.from({ length: 10 }, (_, i) => i + 1);

const groups = computed(() => scheduleData.value?.scheduleGroups ?? []);

const currentGroupSlots = computed<TeacherPersonalScheduleSlotDto[]>(() => {
  const match = groups.value.find((g) => g.scheduleType === activeScheduleType.value);
  return match?.slots ?? [];
});

// Map slots by `${dayOfWeek}-${periodSlot}`
const slotsMap = computed(() => {
  const map = new Map<string, TeacherPersonalScheduleSlotDto>();
  for (const slot of currentGroupSlots.value) {
    if (slot.dayOfWeek && slot.periodSlot) {
      map.set(`${slot.dayOfWeek}-${slot.periodSlot}`, slot);
    }
  }
  return map;
});

function getSlot(dayOfWeek: number, periodSlot: number): TeacherPersonalScheduleSlotDto | null {
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

const totalSlotsCount = computed(() => currentGroupSlots.value.length);
const distinctGroupsCount = computed(() => {
  const set = new Set<string>();
  for (const s of currentGroupSlots.value) {
    if (s.studyGroupId) set.add(s.studyGroupId);
  }
  return set.size;
});
</script>

<template>
  <div class="space-y-6" data-testid="teacher-personal-schedule-tab">
    <!-- Header Controls: Schedule Type Toggle & Inspection Context -->
    <div class="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center sm:justify-between">
      <!-- Schedule Type Toggle -->
      <div class="space-y-1">
        <label class="text-xs font-semibold uppercase tracking-wider text-slate-500">
          Weekly Rotation Cycle
        </label>
        <div class="inline-flex rounded-lg border border-slate-200 bg-slate-100 p-0.5">
          <button
            type="button"
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="
              activeScheduleType === 'REGULAR'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            "
            data-testid="teacher-sched-regular-toggle"
            @click="activeScheduleType = 'REGULAR'"
          >
            Regular Academic
          </button>
          <button
            type="button"
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="
              activeScheduleType === 'GENERAL'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            "
            data-testid="teacher-sched-general-toggle"
            @click="activeScheduleType = 'GENERAL'"
          >
            SMK General Block
          </button>
          <button
            type="button"
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="
              activeScheduleType === 'VOCATIONAL'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            "
            data-testid="teacher-sched-vocational-toggle"
            @click="activeScheduleType = 'VOCATIONAL'"
          >
            SMK Vocational Block
          </button>
        </div>
      </div>

      <!-- Quick Stats & Date Checker -->
      <div class="flex flex-wrap items-center gap-3 text-xs text-slate-600">
        <div class="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-slate-50 px-3 py-1.5">
          <Clock class="h-4 w-4 text-brand-primary-600" />
          <span>Total Periods: <strong class="text-slate-900 tabular-nums">{{ totalSlotsCount }}</strong></span>
        </div>
        <div class="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-slate-50 px-3 py-1.5">
          <CheckCircle2 class="h-4 w-4 text-emerald-600" />
          <span>Classes: <strong class="text-slate-900 tabular-nums">{{ distinctGroupsCount }}</strong></span>
        </div>
      </div>
    </div>

    <!-- Active SMK Date Resolver Info Banner (D-11) -->
    <div
      v-if="activeScheduleType !== 'REGULAR'"
      class="flex items-center gap-2 rounded-xl border border-sky-200 bg-sky-50 px-4 py-2.5 text-xs text-sky-800"
      data-testid="smk-block-info-banner"
    >
      <Info class="h-4 w-4 shrink-0 text-sky-600" />
      <span>
        Displaying <strong>{{ activeScheduleType }}</strong> block timetable template.
        For SMK study groups alternating between general and vocational cycles, calendar rotation dates apply automatically per configured block schedules.
      </span>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="rounded-xl border border-slate-200 bg-white p-8 text-center">
      <div class="space-y-3">
        <div class="h-6 w-48 mx-auto animate-pulse rounded bg-slate-100" />
        <div class="h-64 w-full animate-pulse rounded-lg bg-slate-100" />
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="isError" class="rounded-xl border border-rose-200 bg-rose-50 p-6 text-center text-xs text-rose-800">
      <p class="font-semibold">Unable to load personal teaching schedule.</p>
      <p class="mt-1">Please ensure your user profile is linked to an active teacher record.</p>
    </div>

    <!-- Empty State -->
    <EmptyState
      v-else-if="currentGroupSlots.length === 0"
      title="No Classes Scheduled"
      description="You have no teaching assignments or timetable slots allocated for this rotation cycle."
      icon="calendar"
    />

    <!-- Matrix Grid for Teacher -->
    <div v-else class="rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden" data-testid="teacher-matrix-grid">
      <div class="overflow-x-auto">
        <table class="w-full min-w-[960px] border-collapse text-left">
          <thead>
            <tr class="border-b border-slate-200 bg-slate-50/80 text-xs text-slate-500 font-semibold uppercase tracking-wider">
              <th class="w-20 px-3 py-3 text-center border-r border-slate-200">
                Period
              </th>
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
              <td class="w-20 px-2 py-3 text-center border-r border-slate-200 bg-slate-50/40 font-mono text-xs font-semibold text-slate-600">
                Slot {{ period }}
              </td>

              <td
                v-for="day in DAYS"
                :key="day.id"
                class="p-2 border-r border-slate-200 last:border-r-0 align-top h-24 w-[150px]"
                :data-testid="`teacher-cell-${day.id}-${period}`"
              >
                <div
                  v-if="getSlot(day.id, period)"
                  class="flex h-full flex-col justify-between rounded-lg border border-brand-primary-200 bg-brand-primary-50/40 p-2 text-xs shadow-xs"
                  :data-testid="`teacher-slot-${day.id}-${period}`"
                >
                  <div class="space-y-1">
                    <div class="flex items-start justify-between gap-1">
                      <span class="font-semibold text-slate-900 line-clamp-1 leading-tight">
                        {{ getSlot(day.id, period)?.subjectName }}
                      </span>
                      <span class="rounded bg-brand-primary-100 px-1.5 py-0.5 text-[10px] font-bold text-brand-primary-800">
                        {{ getSlot(day.id, period)?.studyGroupName }}
                      </span>
                    </div>
                  </div>

                  <div class="mt-2 flex items-center justify-between text-[10px] text-slate-500 font-mono">
                    <span
                      v-if="getSlot(day.id, period)?.roomName"
                      class="rounded bg-white/80 px-1 py-0.5 text-slate-700 border border-slate-200 line-clamp-1"
                    >
                      {{ getSlot(day.id, period)?.roomName }}
                    </span>
                    <span v-else class="text-slate-400 italic">No room</span>

                    <span class="tabular-nums">
                      {{ formatSlotTime(getSlot(day.id, period)?.startTime) }}–{{ formatSlotTime(getSlot(day.id, period)?.endTime) }}
                    </span>
                  </div>
                </div>

                <div
                  v-else
                  class="flex h-full w-full items-center justify-center rounded-lg border border-dashed border-slate-100 bg-slate-50/10 text-slate-300 text-xs"
                >
                  &mdash;
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
