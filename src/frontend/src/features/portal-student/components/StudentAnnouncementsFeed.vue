<script setup lang="ts">
import { ref, computed } from 'vue';
import { Calendar, Megaphone, CalendarDays, Inbox } from 'lucide-vue-next';
import type { AnnouncementDto, CalendarEntryDto } from '../api/studentPortal.api';

const props = defineProps<{
  announcements?: AnnouncementDto[] | null;
  calendarEntries?: CalendarEntryDto[] | null;
  isLoading?: boolean;
}>();

const activeTab = ref<'announcements' | 'calendar'>('announcements');

const announcementList = computed(() => props.announcements ?? []);
const calendarList = computed(() => {
  if (!props.calendarEntries) return [];
  // Sort by entryDate ascending
  return [...props.calendarEntries].sort((a, b) =>
    (a.entryDate ?? '').localeCompare(b.entryDate ?? '')
  );
});

function formatDisplayDate(dateStr?: string | null): string {
  if (!dateStr) return '-';
  try {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    return d.toLocaleDateString('id-ID', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  } catch {
    return dateStr;
  }
}
</script>

<template>
  <div
    data-testid="student-announcements-feed"
    class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:p-5"
  >
    <!-- Tab Selector -->
    <div class="flex items-center gap-2 border-b border-slate-200 pb-3">
      <button
        type="button"
        data-testid="feed-tab-announcements"
        :class="[
          'flex min-h-[44px] items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold transition',
          activeTab === 'announcements'
            ? 'bg-brand-primary-50 text-brand-primary-900 ring-1 ring-brand-primary-600/20'
            : 'text-slate-600 hover:bg-slate-100'
        ]"
        @click="activeTab = 'announcements'"
      >
        <Megaphone class="h-4 w-4" />
        <span>Pengumuman</span>
        <span
          v-if="announcementList.length > 0"
          class="rounded-full bg-slate-200 px-2 py-0.5 text-xs text-slate-700"
        >
          {{ announcementList.length }}
        </span>
      </button>

      <button
        type="button"
        data-testid="feed-tab-calendar"
        :class="[
          'flex min-h-[44px] items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold transition',
          activeTab === 'calendar'
            ? 'bg-brand-primary-50 text-brand-primary-900 ring-1 ring-brand-primary-600/20'
            : 'text-slate-600 hover:bg-slate-100'
        ]"
        @click="activeTab = 'calendar'"
      >
        <CalendarDays class="h-4 w-4" />
        <span>Kalender Sekolah</span>
        <span
          v-if="calendarList.length > 0"
          class="rounded-full bg-slate-200 px-2 py-0.5 text-xs text-slate-700"
        >
          {{ calendarList.length }}
        </span>
      </button>
    </div>

    <!-- Tab 1: Announcements Content -->
    <div v-if="activeTab === 'announcements'" class="mt-4 space-y-3">
      <div
        v-if="announcementList.length === 0"
        data-testid="empty-announcements"
        class="flex flex-col items-center justify-center py-8 text-center"
      >
        <div class="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-400">
          <Inbox class="h-5 w-5" />
        </div>
        <p class="mt-2 text-sm font-medium text-slate-900">Belum Ada Pengumuman</p>
        <p class="text-xs text-slate-500">Tidak ada pengumuman terbaru untuk siswa saat ini.</p>
      </div>

      <div
        v-for="announcement in announcementList"
        :key="announcement.id"
        data-testid="announcement-item"
        class="rounded-lg border border-slate-100 bg-slate-50/50 p-3.5 transition hover:bg-slate-100/60"
      >
        <div class="flex items-start justify-between gap-2">
          <h5 class="text-sm font-bold text-slate-900">
            {{ announcement.title }}
          </h5>
          <span class="shrink-0 text-[11px] font-medium text-slate-400">
            {{ formatDisplayDate(announcement.publishedAt) }}
          </span>
        </div>
        <p class="mt-1 text-xs text-slate-600 line-clamp-3">
          {{ announcement.content }}
        </p>
        <div v-if="announcement.authorName" class="mt-2 text-[11px] text-slate-400">
          Diposting oleh: <span class="font-medium text-slate-600">{{ announcement.authorName }}</span>
        </div>
      </div>
    </div>

    <!-- Tab 2: Calendar Entries Content -->
    <div v-if="activeTab === 'calendar'" class="mt-4 space-y-3">
      <div
        v-if="calendarList.length === 0"
        data-testid="empty-calendar"
        class="flex flex-col items-center justify-center py-8 text-center"
      >
        <div class="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-400">
          <Calendar class="h-5 w-5" />
        </div>
        <p class="mt-2 text-sm font-medium text-slate-900">Belum Ada Entri Kalender</p>
        <p class="text-xs text-slate-500">Jadwal kegiatan atau libur sekolah belum ditetapkan.</p>
      </div>

      <div
        v-for="entry in calendarList"
        :key="entry.id"
        data-testid="calendar-item"
        class="flex items-start gap-3 rounded-lg border border-slate-100 bg-slate-50/50 p-3.5 transition hover:bg-slate-100/60"
      >
        <div
          class="flex h-10 w-10 shrink-0 flex-col items-center justify-center rounded-lg bg-white text-slate-800 shadow-2xs border border-slate-200"
        >
          <span class="text-[10px] font-bold uppercase text-brand-primary-900">
            {{ entry.dayType || 'TGL' }}
          </span>
        </div>
        <div class="min-w-0 flex-1">
          <div class="flex items-center justify-between gap-2">
            <span class="text-xs font-semibold text-slate-900">
              {{ formatDisplayDate(entry.entryDate) }}
            </span>
            <span
              v-if="entry.dayType"
              class="rounded bg-slate-200 px-1.5 py-0.2 text-[10px] font-bold text-slate-700"
            >
              {{ entry.dayType }}
            </span>
          </div>
          <p class="mt-0.5 text-xs text-slate-600">
            {{ entry.description || '-' }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
