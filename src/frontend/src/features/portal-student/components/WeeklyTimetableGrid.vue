<script setup lang="ts">
import { ref, computed } from 'vue';
import { Calendar, Clock, User, MapPin, CalendarX } from 'lucide-vue-next';
import type { ScheduleSlotDto, ActiveSmkCycleDto } from '../api/studentPortal.api';

const props = defineProps<{
  schedule?: ScheduleSlotDto[] | null;
  activeCycle?: ActiveSmkCycleDto | null;
}>();

interface DayConfig {
  key: string;
  aliases: string[];
  label: string;
  shortLabel: string;
}

const DAYS: DayConfig[] = [
  { key: 'MONDAY', aliases: ['MONDAY', 'SENIN', '1'], label: 'Senin', shortLabel: 'Sen' },
  { key: 'TUESDAY', aliases: ['TUESDAY', 'SELASA', '2'], label: 'Selasa', shortLabel: 'Sel' },
  { key: 'WEDNESDAY', aliases: ['WEDNESDAY', 'RABU', '3'], label: 'Rabu', shortLabel: 'Rab' },
  { key: 'THURSDAY', aliases: ['THURSDAY', 'KAMIS', '4'], label: 'Kamis', shortLabel: 'Kam' },
  { key: 'FRIDAY', aliases: ['FRIDAY', 'JUMAT', '4', '5'], label: 'Jumat', shortLabel: 'Jum' },
  { key: 'SATURDAY', aliases: ['SATURDAY', 'SABTU', '6'], label: 'Sabtu', shortLabel: 'Sab' }
];

const selectedDayKey = ref<string>('MONDAY');

const groupedSchedule = computed(() => {
  const map: Record<string, ScheduleSlotDto[]> = {};
  for (const day of DAYS) {
    map[day.key] = [];
  }

  if (!props.schedule || props.schedule.length === 0) {
    return map;
  }

  for (const slot of props.schedule) {
    const rawDay = (slot.dayOfWeek ?? '').trim().toUpperCase();
    const matched = DAYS.find((d) => d.aliases.includes(rawDay) || d.key === rawDay);
    const key = matched ? matched.key : 'MONDAY';
    const target = map[key];
    if (target) {
      target.push(slot);
    }
  }

  for (const key of Object.keys(map)) {
    const slots = map[key];
    if (slots) {
      slots.sort((a, b) => (a.periodSlot ?? 0) - (b.periodSlot ?? 0));
    }
  }

  return map;
});

const activeScheduleList = computed(() => props.schedule ?? []);

const hasAnySlots = computed(() => activeScheduleList.value.length > 0);

const selectedDaySlots = computed(() => groupedSchedule.value[selectedDayKey.value] || []);
</script>

<template>
  <div data-testid="weekly-timetable-grid" class="space-y-4">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <Calendar class="h-5 w-5 text-slate-700" />
        <h3 class="text-lg font-bold tracking-tight text-slate-900">Jadwal Mingguan</h3>
      </div>
      <div v-if="activeCycle?.scheduleType" class="text-xs text-slate-500">
        Tipe: <span class="font-semibold text-slate-700">{{ activeCycle.scheduleType }}</span>
      </div>
    </div>

    <!-- Empty State -->
    <div
      v-if="!hasAnySlots"
      data-testid="empty-weekly-schedule"
      class="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center shadow-xs"
    >
      <div class="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
        <CalendarX class="h-6 w-6" />
      </div>
      <h4 class="mt-3 text-base font-semibold text-slate-900">Belum Ada Jadwal Mingguan</h4>
      <p class="mt-1 max-w-sm text-sm text-slate-500">
        Jadwal pelajaran mingguan untuk siklus ini belum dikonfigurasi atau belum diterbitkan.
      </p>
    </div>

    <!-- Filled Content -->
    <div v-else class="space-y-4">
      <!-- Day Selector Pills (Mobile & Tablet touch-friendly >= 44px) -->
      <div
        data-testid="day-pills"
        class="flex items-center gap-1.5 overflow-x-auto pb-1 sm:gap-2"
      >
        <button
          v-for="day in DAYS"
          :key="day.key"
          type="button"
          :data-testid="`day-pill-${day.key.toLowerCase()}`"
          :class="[
            'flex min-h-[44px] min-w-[56px] shrink-0 items-center justify-center rounded-xl px-3.5 py-2 text-sm font-semibold transition sm:min-w-[70px]',
            selectedDayKey === day.key
              ? 'bg-brand-primary-900 text-white shadow-xs'
              : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'
          ]"
          @click="selectedDayKey = day.key"
        >
          <span class="sm:hidden">{{ day.shortLabel }}</span>
          <span class="hidden sm:inline">{{ day.label }}</span>
          <span
            v-if="groupedSchedule[day.key]?.length"
            class="ml-1.5 rounded-full px-1.5 py-0.2 text-[10px]"
            :class="selectedDayKey === day.key ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-600'"
          >
            {{ groupedSchedule[day.key]?.length }}
          </span>
        </button>
      </div>

      <!-- Selected Day Schedule View (Mobile & Responsive Card List) -->
      <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:p-5">
        <div class="mb-4 flex items-center justify-between border-b border-slate-100 pb-3">
          <h4 class="font-bold text-slate-900">
            Jadwal Hari {{ DAYS.find((d) => d.key === selectedDayKey)?.label }}
          </h4>
          <span class="text-xs text-slate-500">
            {{ selectedDaySlots.length }} mata pelajaran
          </span>
        </div>

        <div v-if="selectedDaySlots.length === 0" class="py-8 text-center text-sm text-slate-400">
          Tidak ada kegiatan belajar pada hari ini.
        </div>

        <div v-else class="space-y-2.5">
          <div
            v-for="slot in selectedDaySlots"
            :key="slot.id || `${slot.periodSlot}-${slot.startTime}`"
            data-testid="weekly-slot-card"
            class="flex flex-col gap-2 rounded-lg border border-slate-100 bg-slate-50/60 p-3.5 transition hover:bg-slate-100/70 sm:flex-row sm:items-center sm:justify-between"
          >
            <div class="flex items-start gap-3">
              <span
                class="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-white text-xs font-bold text-slate-700 shadow-2xs border border-slate-200"
              >
                {{ slot.periodSlot }}
              </span>
              <div>
                <div class="flex items-center gap-2">
                  <span class="text-sm font-semibold text-slate-900">{{ slot.subjectName }}</span>
                  <span
                    v-if="slot.subjectCode"
                    class="rounded bg-white px-1.5 py-0.5 text-[11px] font-mono font-medium text-slate-600 border border-slate-200"
                  >
                    {{ slot.subjectCode }}
                  </span>
                </div>
                <div class="mt-0.5 flex flex-wrap items-center gap-2 text-xs text-slate-500">
                  <span class="inline-flex items-center gap-1">
                    <User class="h-3 w-3 text-slate-400" />
                    <span>{{ slot.teacherName || '-' }}</span>
                  </span>
                  <span class="text-slate-300">•</span>
                  <span class="inline-flex items-center gap-1">
                    <MapPin class="h-3 w-3 text-slate-400" />
                    <span>{{ slot.roomName || 'Kelas' }}</span>
                  </span>
                </div>
              </div>
            </div>

            <div class="flex items-center gap-1 font-mono text-xs text-slate-600">
              <Clock class="h-3.5 w-3.5 text-slate-400" />
              <span>{{ slot.startTime || '--:--' }} - {{ slot.endTime || '--:--' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
