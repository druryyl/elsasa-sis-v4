<script setup lang="ts">
import { computed } from 'vue';
import { Clock, User, MapPin, CalendarX } from 'lucide-vue-next';
import type { ScheduleSlotDto } from '../api/studentPortal.api';

const props = defineProps<{
  agenda?: ScheduleSlotDto[] | null;
  currentTime?: string;
  dateLabel?: string;
}>();

function parseTimeToMinutes(timeStr?: string | null): number | null {
  if (!timeStr) return null;
  const parts = timeStr.split(':');
  if (parts.length < 2) return null;
  const p0 = parts[0] ?? '';
  const p1 = parts[1] ?? '';
  const h = parseInt(p0, 10);
  const m = parseInt(p1, 10);
  if (isNaN(h) || isNaN(m)) return null;
  return h * 60 + m;
}

const currentMinutes = computed(() => {
  if (props.currentTime) {
    return parseTimeToMinutes(props.currentTime);
  }
  const now = new Date();
  return now.getHours() * 60 + now.getMinutes();
});

const sortedAgenda = computed(() => {
  if (!props.agenda || props.agenda.length === 0) return [];
  return [...props.agenda].sort((a, b) => (a.periodSlot ?? 0) - (b.periodSlot ?? 0));
});

function isOngoingSlot(slot: ScheduleSlotDto): boolean {
  if (currentMinutes.value === null) return false;
  const start = parseTimeToMinutes(slot.startTime);
  const end = parseTimeToMinutes(slot.endTime);
  if (start === null || end === null) return false;
  return currentMinutes.value >= start && currentMinutes.value <= end;
}
</script>

<template>
  <div data-testid="agenda-timeline" class="space-y-4">
    <!-- Header with title & date -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <Clock class="h-5 w-5 text-slate-700" />
        <h3 class="text-lg font-bold tracking-tight text-slate-900">Agenda Hari Ini</h3>
      </div>
      <span v-if="dateLabel" class="text-xs font-medium text-slate-500 sm:text-sm">
        {{ dateLabel }}
      </span>
    </div>

    <!-- Empty State -->
    <div
      v-if="sortedAgenda.length === 0"
      data-testid="empty-agenda"
      class="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center shadow-xs"
    >
      <div class="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
        <CalendarX class="h-6 w-6" />
      </div>
      <h4 class="mt-3 text-base font-semibold text-slate-900">Tidak Ada Jadwal Pelajaran</h4>
      <p class="mt-1 max-w-sm text-sm text-slate-500">
        Tidak ada kegiatan belajar mengajar hari ini atau tanggal yang dipilih adalah hari libur.
      </p>
    </div>

    <!-- Timeline Slots List -->
    <div v-else class="space-y-3">
      <div
        v-for="slot in sortedAgenda"
        :key="slot.id || `${slot.periodSlot}-${slot.startTime}`"
        data-testid="agenda-slot"
        :class="[
          'relative rounded-xl border p-4 shadow-xs transition sm:p-5',
          isOngoingSlot(slot)
            ? 'border-brand-primary-500 bg-brand-primary-50/40 ring-2 ring-brand-primary-500/20'
            : 'border-slate-200 bg-white hover:border-slate-300'
        ]"
      >
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <!-- Left: Period Slot & Subject -->
          <div class="flex items-start gap-3.5">
            <div
              :class="[
                'flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-sm font-bold shadow-2xs',
                isOngoingSlot(slot)
                  ? 'bg-brand-primary-900 text-white'
                  : 'bg-slate-100 text-slate-700'
              ]"
            >
              <span data-testid="slot-period">{{ slot.periodSlot }}</span>
            </div>

            <div>
              <div class="flex flex-wrap items-center gap-2">
                <span
                  data-testid="slot-subject"
                  class="text-base font-semibold text-slate-900"
                >
                  {{ slot.subjectName || 'Mata Pelajaran' }}
                </span>
                <span
                  v-if="slot.subjectCode"
                  class="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-mono font-medium text-slate-600"
                >
                  {{ slot.subjectCode }}
                </span>
                <span
                  v-if="isOngoingSlot(slot)"
                  data-testid="ongoing-indicator"
                  class="inline-flex items-center rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800 ring-1 ring-inset ring-emerald-600/20 animate-pulse"
                >
                  Sedang Berlangsung
                </span>
              </div>

              <!-- Teacher and Room details -->
              <div class="mt-1 flex flex-wrap items-center gap-3 text-xs sm:text-sm text-slate-500">
                <span class="inline-flex items-center gap-1">
                  <User class="h-3.5 w-3.5 text-slate-400" />
                  <span data-testid="slot-teacher">{{ slot.teacherName || 'Guru Pengampu' }}</span>
                </span>
                <span class="text-slate-300">•</span>
                <span class="inline-flex items-center gap-1">
                  <MapPin class="h-3.5 w-3.5 text-slate-400" />
                  <span data-testid="slot-room">{{ slot.roomName || 'Ruang Kelas' }}</span>
                </span>
              </div>
            </div>
          </div>

          <!-- Right: Time Slot -->
          <div class="flex items-center gap-1.5 font-mono text-xs font-medium text-slate-600 sm:text-sm">
            <Clock class="h-4 w-4 text-slate-400" />
            <span data-testid="slot-time">
              {{ slot.startTime || '--:--' }} - {{ slot.endTime || '--:--' }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
