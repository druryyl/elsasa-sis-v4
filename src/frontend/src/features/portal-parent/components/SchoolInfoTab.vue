<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import {
  Bell,
  Calendar,
  CalendarDays,
  Clock,
  Megaphone
} from 'lucide-vue-next';
import { formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import {
  useParentAnnouncementsQuery,
  useParentCalendarQuery
} from '../api/useParentPortalQueries';

const {
  data: announcements,
  isLoading: isLoadingAnnouncements,
  isError: isErrorAnnouncements,
  refetch: refetchAnnouncements
} = useParentAnnouncementsQuery();

const {
  data: calendarEntries,
  isLoading: isLoadingCalendar,
  isError: isErrorCalendar,
  refetch: refetchCalendar
} = useParentCalendarQuery();

const announcementsList = computed(() => announcements.value ?? []);
const calendarList = computed(() => {
  const list = calendarEntries.value ?? [];
  return [...list].sort((a, b) => {
    if (!a.entryDate || !b.entryDate) return 0;
    return a.entryDate.localeCompare(b.entryDate);
  });
});

function getDayTypeBadge(dayType?: string | null): { label: string; class: string } {
  switch (dayType?.toUpperCase()) {
    case 'HOLIDAY':
      return {
        label: 'Libur',
        class: 'bg-rose-100 text-rose-800 border-rose-200'
      };
    case 'CLOSURE':
      return {
        label: 'Tutup',
        class: 'bg-amber-100 text-amber-800 border-amber-200'
      };
    case 'SCHOOL_DAY':
    default:
      return {
        label: 'Hari Efektif',
        class: 'bg-blue-100 text-blue-800 border-blue-200'
      };
  }
}

function refetchAll(): void {
  refetchAnnouncements();
  refetchCalendar();
}

defineExpose({
  refetchAll
});
</script>

<template>
  <div
    class="space-y-6"
    data-testid="school-info-tab"
  >
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
      <!-- Left Column: Parent Bulletins & Announcements Feed -->
      <section class="space-y-4 lg:col-span-7">
        <div class="flex items-center justify-between border-b border-slate-200 pb-3">
          <div class="flex items-center gap-2">
            <Megaphone class="h-5 w-5 text-brand-primary-600" />
            <h3 class="text-base font-semibold text-slate-900">
              Pengumuman & Buletin Sekolah
            </h3>
          </div>
          <span class="text-xs text-slate-500">
            {{ announcementsList.length }} pengumuman
          </span>
        </div>

        <!-- Loading skeleton -->
        <div
          v-if="isLoadingAnnouncements"
          class="space-y-3"
          data-testid="announcements-skeleton"
        >
          <div v-for="i in 3" :key="i" class="h-28 animate-pulse rounded-xl bg-slate-200" />
        </div>

        <!-- Error state -->
        <div
          v-else-if="isErrorAnnouncements"
          class="rounded-xl border border-red-200 bg-red-50 p-4 text-center text-sm text-red-700"
          data-testid="announcements-error"
        >
          <p>Gagal memuat pengumuman sekolah.</p>
          <Button
            label="Coba Lagi"
            severity="danger"
            text
            size="small"
            class="mt-1"
            @click="() => refetchAnnouncements()"
          />
        </div>

        <!-- Empty state -->
        <div
          v-else-if="announcementsList.length === 0"
          class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 bg-slate-50/50 p-8 text-center"
          data-testid="empty-announcements"
        >
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-slate-100 text-slate-400">
            <Bell class="h-6 w-6" />
          </div>
          <p class="mt-3 text-sm font-semibold text-slate-900">Belum Ada Pengumuman</p>
          <p class="text-xs text-slate-500">Saat ini belum ada pengumuman baru untuk orang tua.</p>
        </div>

        <!-- Announcements Feed -->
        <div
          v-else
          class="space-y-4"
          data-testid="announcements-feed"
        >
          <article
            v-for="item in announcementsList"
            :key="item.id"
            class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs transition-shadow hover:shadow-sm"
            :data-testid="`announcement-item-${item.id}`"
          >
            <div class="flex flex-wrap items-center justify-between gap-2">
              <span class="rounded bg-brand-primary-50 px-2 py-0.5 text-[11px] font-medium text-brand-primary-700">
                {{ item.targetAudience === 'PARENTS' ? 'Orang Tua' : 'Umum' }}
              </span>
              <div class="flex items-center gap-1 text-xs text-slate-400">
                <Clock class="h-3.5 w-3.5" />
                <span>{{ item.publishedAt ? formatDisplayDateTime(item.publishedAt) : (item.createdAt ? formatDisplayDateTime(item.createdAt) : '-') }}</span>
              </div>
            </div>

            <h4 class="mt-2.5 text-base font-semibold text-slate-900">
              {{ item.title }}
            </h4>

            <p class="mt-1.5 whitespace-pre-line text-sm leading-relaxed text-slate-600">
              {{ item.content }}
            </p>

            <div v-if="item.authorName" class="mt-3 border-t border-slate-100 pt-2 text-xs text-slate-500">
              Dipublikasikan oleh: <strong class="text-slate-700">{{ item.authorName }}</strong>
            </div>
          </article>
        </div>
      </section>

      <!-- Right Column: Academic Calendar & Holidays -->
      <section class="space-y-4 lg:col-span-5">
        <div class="flex items-center justify-between border-b border-slate-200 pb-3">
          <div class="flex items-center gap-2">
            <CalendarDays class="h-5 w-5 text-brand-primary-600" />
            <h3 class="text-base font-semibold text-slate-900">
              Kalender & Agenda Sekolah
            </h3>
          </div>
          <span class="text-xs text-slate-500">
            {{ calendarList.length }} agenda
          </span>
        </div>

        <!-- Loading skeleton -->
        <div
          v-if="isLoadingCalendar"
          class="space-y-3"
          data-testid="calendar-skeleton"
        >
          <div v-for="i in 3" :key="i" class="h-16 animate-pulse rounded-xl bg-slate-200" />
        </div>

        <!-- Error state -->
        <div
          v-else-if="isErrorCalendar"
          class="rounded-xl border border-red-200 bg-red-50 p-4 text-center text-sm text-red-700"
          data-testid="calendar-error"
        >
          <p>Gagal memuat kalender akademik.</p>
          <Button
            label="Coba Lagi"
            severity="danger"
            text
            size="small"
            class="mt-1"
            @click="() => refetchCalendar()"
          />
        </div>

        <!-- Empty state -->
        <div
          v-else-if="calendarList.length === 0"
          class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 bg-slate-50/50 p-8 text-center"
          data-testid="empty-calendar"
        >
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-slate-100 text-slate-400">
            <Calendar class="h-6 w-6" />
          </div>
          <p class="mt-3 text-sm font-semibold text-slate-900">Belum Ada Agenda</p>
          <p class="text-xs text-slate-500">Tidak ada agenda operasional dalam kalender saat ini.</p>
        </div>

        <!-- Calendar Events List -->
        <div
          v-else
          class="space-y-3"
          data-testid="calendar-list"
        >
          <div
            v-for="entry in calendarList"
            :key="entry.id"
            class="flex items-start gap-3 rounded-xl border border-slate-200 bg-white p-3.5 shadow-xs"
            :data-testid="`calendar-entry-${entry.id}`"
          >
            <!-- Date Box -->
            <div class="flex h-12 w-12 shrink-0 flex-col items-center justify-center rounded-lg bg-slate-100 text-slate-800">
              <span class="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                {{ entry.entryDate ? new Date(entry.entryDate).toLocaleDateString('id-ID', { month: 'short' }) : 'TGL' }}
              </span>
              <span class="text-sm font-black leading-none text-slate-900">
                {{ entry.entryDate ? new Date(entry.entryDate).getDate() : '-' }}
              </span>
            </div>

            <!-- Content -->
            <div class="min-w-0 flex-1">
              <div class="flex items-center justify-between gap-1.5">
                <h5 class="truncate text-sm font-semibold text-slate-900">
                  {{ entry.description || getDayTypeBadge(entry.dayType).label }}
                </h5>
                <span
                  class="shrink-0 rounded-md border px-1.5 py-0.2 text-[10px] font-semibold"
                  :class="getDayTypeBadge(entry.dayType).class"
                >
                  {{ getDayTypeBadge(entry.dayType).label }}
                </span>
              </div>

              <div class="mt-1 text-[11px] text-slate-400">
                {{ formatDisplayDate(entry.entryDate) }}
                <span v-if="entry.academicYearName"> • {{ entry.academicYearName }}</span>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>
