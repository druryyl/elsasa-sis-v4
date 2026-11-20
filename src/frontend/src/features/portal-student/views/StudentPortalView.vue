<script setup lang="ts">
import { ref, computed } from 'vue';
import { RefreshCw, AlertCircle } from 'lucide-vue-next';
import {
  useStudentTimetableQuery,
  useStudentAnnouncementsQuery,
  useStudentCalendarQuery
} from '../api/useStudentPortalQueries';
import StudentHeaderBanner from '../components/StudentHeaderBanner.vue';
import ActiveCycleBadge from '../components/ActiveCycleBadge.vue';
import TodayAgendaTimeline from '../components/TodayAgendaTimeline.vue';
import WeeklyTimetableGrid from '../components/WeeklyTimetableGrid.vue';
import StudentAnnouncementsFeed from '../components/StudentAnnouncementsFeed.vue';

function getTodayString(): string {
  const d = new Date();
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const activeView = ref<'agenda' | 'weekly'>('agenda');
const selectedDate = ref<string>(getTodayString());

const {
  data: timetableData,
  isLoading: isTimetableLoading,
  isError: isTimetableError,
  error: timetableError,
  refetch: refetchTimetable,
  isFetching: isTimetableFetching
} = useStudentTimetableQuery(selectedDate);

const {
  data: announcementsData,
  isLoading: isAnnouncementsLoading,
  refetch: refetchAnnouncements
} = useStudentAnnouncementsQuery();

const {
  data: calendarData,
  isLoading: isCalendarLoading,
  refetch: refetchCalendar
} = useStudentCalendarQuery();

const isRefreshing = computed(() => isTimetableFetching.value);

async function handleRefresh(): Promise<void> {
  await Promise.all([refetchTimetable(), refetchAnnouncements(), refetchCalendar()]);
}

const formattedSelectedDate = computed(() => {
  if (!selectedDate.value) return '';
  try {
    const d = new Date(`${selectedDate.value}T00:00:00`);
    if (isNaN(d.getTime())) return selectedDate.value;
    return d.toLocaleDateString('id-ID', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  } catch {
    return selectedDate.value;
  }
});
</script>

<template>
  <div data-testid="student-portal-view" class="space-y-6">
    <!-- Student Header Banner -->
    <StudentHeaderBanner :student-info="timetableData?.studentInfo" />

    <!-- Active SMK Rotation Cycle Badge Bar -->
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <ActiveCycleBadge :active-cycle="timetableData?.activeCycle" />

      <!-- Inspection Date Picker & Refresh Button -->
      <div class="flex items-center gap-2">
        <div class="relative flex items-center">
          <label for="portal-date-input" class="sr-only">Pilih Tanggal Jadwal</label>
          <input
            id="portal-date-input"
            v-model="selectedDate"
            type="date"
            data-testid="date-picker"
            aria-label="Pilih Tanggal Jadwal"
            class="min-h-[44px] rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-medium text-slate-800 shadow-2xs transition focus:border-brand-primary-500 focus:outline-none focus:ring-1 focus:ring-brand-primary-500 sm:text-sm"
          />
        </div>

        <button
          type="button"
          data-testid="refresh-button"
          class="flex min-h-[44px] min-w-[44px] items-center justify-center rounded-lg border border-slate-300 bg-white p-2 text-slate-600 shadow-2xs transition hover:bg-slate-50 hover:text-slate-900 active:scale-95"
          :title="'Muat Ulang Data'"
          @click="handleRefresh"
        >
          <RefreshCw :class="['h-4 w-4', isRefreshing && 'animate-spin text-brand-primary-600']" />
        </button>
      </div>
    </div>

    <!-- Error State Alert -->
    <div
      v-if="isTimetableError"
      data-testid="portal-error"
      class="rounded-xl border border-red-200 bg-red-50 p-4 text-red-800 shadow-xs"
    >
      <div class="flex items-start gap-3">
        <AlertCircle class="h-5 w-5 shrink-0 text-red-500" />
        <div class="text-sm">
          <p class="font-bold">Gagal memuat jadwal pelajaran</p>
          <p class="mt-0.5 text-xs text-red-700">
            {{ timetableError instanceof Error ? timetableError.message : 'Silakan coba beberapa saat lagi.' }}
          </p>
          <button
            type="button"
            class="mt-2 text-xs font-semibold text-red-800 underline hover:text-red-900"
            @click="handleRefresh"
          >
            Coba lagi
          </button>
        </div>
      </div>
    </div>

    <!-- View Toggle: Agenda Hari Ini vs Jadwal Mingguan -->
    <div
      data-testid="view-toggle"
      class="flex rounded-xl bg-slate-200/80 p-1 shadow-2xs sm:w-fit"
    >
      <button
        type="button"
        data-testid="toggle-agenda"
        :class="[
          'flex flex-1 min-h-[44px] items-center justify-center rounded-lg px-4 py-2 text-sm font-semibold transition sm:flex-initial sm:px-6',
          activeView === 'agenda'
            ? 'bg-white text-slate-900 shadow-xs'
            : 'text-slate-600 hover:text-slate-900'
        ]"
        @click="activeView = 'agenda'"
      >
        Agenda Hari Ini
      </button>

      <button
        type="button"
        data-testid="toggle-weekly"
        :class="[
          'flex flex-1 min-h-[44px] items-center justify-center rounded-lg px-4 py-2 text-sm font-semibold transition sm:flex-initial sm:px-6',
          activeView === 'weekly'
            ? 'bg-white text-slate-900 shadow-xs'
            : 'text-slate-600 hover:text-slate-900'
        ]"
        @click="activeView = 'weekly'"
      >
        Jadwal Mingguan
      </button>
    </div>

    <!-- Skeleton Loading State -->
    <div
      v-if="isTimetableLoading"
      data-testid="portal-skeleton"
      class="space-y-4 animate-pulse"
    >
      <div class="h-8 w-48 rounded bg-slate-200" />
      <div class="space-y-3">
        <div class="h-20 rounded-xl bg-slate-200" />
        <div class="h-20 rounded-xl bg-slate-200" />
        <div class="h-20 rounded-xl bg-slate-200" />
      </div>
    </div>

    <!-- Main Schedule Content -->
    <div v-else class="space-y-6">
      <!-- Tab 1: Today's Agenda Timeline -->
      <TodayAgendaTimeline
        v-if="activeView === 'agenda'"
        :agenda="timetableData?.todayAgenda"
        :date-label="formattedSelectedDate"
      />

      <!-- Tab 2: Weekly Timetable Grid -->
      <WeeklyTimetableGrid
        v-if="activeView === 'weekly'"
        :schedule="timetableData?.weeklySchedule"
        :active-cycle="timetableData?.activeCycle"
      />
    </div>

    <!-- Student Announcements & School Calendar Feed -->
    <div class="pt-4 border-t border-slate-200">
      <StudentAnnouncementsFeed
        :announcements="announcementsData?.items"
        :calendar-entries="calendarData?.items"
        :is-loading="isAnnouncementsLoading || isCalendarLoading"
      />
    </div>
  </div>
</template>
