import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type {
  ActiveSmkCycleDto,
  StudentPersonalScheduleResponse,
  StudentTimetableInfoDto,
  ScheduleSlotDto,
  AnnouncementDto,
  CalendarEntryDto
} from '../../api/studentPortal.api';
import StudentPortalView from '../StudentPortalView.vue';

const sampleStudentInfo: StudentTimetableInfoDto = {
  studentId: '01A1F11A-7010-7010-8000-000000000010',
  fullName: 'Ahmad Dahlan',
  nis: 'NIS-SMK-2026-001',
  studyGroupId: '01900000-0000-7000-8000-000000000010',
  studyGroupName: 'X-RPL-1',
  schoolLevel: 'SMK',
  grade: '10'
};

const sampleActiveCycleVocational: ActiveSmkCycleDto = {
  scheduleType: 'VOCATIONAL',
  isSmkRotation: true,
  startDate: '2026-11-17',
  endDate: '2026-11-28'
};

const sampleActiveCycleRegular: ActiveSmkCycleDto = {
  scheduleType: 'REGULAR',
  isSmkRotation: false,
  startDate: null,
  endDate: null
};

const sampleTodayAgenda: ScheduleSlotDto[] = [
  {
    id: '01900000-0000-7000-8000-000000000101',
    dayOfWeek: 'MONDAY',
    periodSlot: 1,
    startTime: '07:30',
    endTime: '08:15',
    subjectId: '01900000-0000-7000-8000-000000000201',
    subjectName: 'Pemrograman Web & Perangkat Bergerak',
    subjectCode: 'PWPB',
    teacherId: '01900000-0000-7000-8000-000000000301',
    teacherName: 'Budi Santoso, S.Kom',
    roomId: '01900000-0000-7000-8000-000000000401',
    roomName: 'Lab RPL 1',
    scheduleType: 'VOCATIONAL'
  },
  {
    id: '01900000-0000-7000-8000-000000000102',
    dayOfWeek: 'MONDAY',
    periodSlot: 2,
    startTime: '08:15',
    endTime: '09:00',
    subjectId: '01900000-0000-7000-8000-000000000202',
    subjectName: 'Basis Data',
    subjectCode: 'BD',
    teacherId: '01900000-0000-7000-8000-000000000302',
    teacherName: 'Siti Aminah, M.Kom',
    roomId: '01900000-0000-7000-8000-000000000401',
    roomName: 'Lab RPL 1',
    scheduleType: 'VOCATIONAL'
  }
];

const sampleWeeklySchedule: ScheduleSlotDto[] = [
  ...sampleTodayAgenda,
  {
    id: '01900000-0000-7000-8000-000000000103',
    dayOfWeek: 'TUESDAY',
    periodSlot: 1,
    startTime: '07:30',
    endTime: '08:15',
    subjectId: '01900000-0000-7000-8000-000000000203',
    subjectName: 'Pemodelan Perangkat Lunak',
    subjectCode: 'PPL',
    teacherId: '01900000-0000-7000-8000-000000000301',
    teacherName: 'Budi Santoso, S.Kom',
    roomId: '01900000-0000-7000-8000-000000000401',
    roomName: 'Lab RPL 1',
    scheduleType: 'VOCATIONAL'
  }
];

const sampleAnnouncements: AnnouncementDto[] = [
  {
    id: '01900000-0000-7000-8000-000000000501',
    title: 'Pelaksanaan Uji Kompetensi Keahlian (UKK)',
    content: 'Jadwal UKK untuk kelas X RPL akan diselenggarakan minggu depan.',
    authorName: 'Wakil Kurikulum',
    targetAudience: 'STUDENTS',
    status: 'PUBLISHED',
    publishedAt: '2026-11-20T08:00:00Z'
  }
];

const sampleCalendarEntries: CalendarEntryDto[] = [
  {
    id: '01900000-0000-7000-8000-000000000601',
    entryDate: '2026-11-25',
    dayType: 'EVENT',
    description: 'Pekan Proyek Penguatan Pelajar Pancasila (P5)'
  }
];

let timetableResponse: StudentPersonalScheduleResponse = {
  studentInfo: sampleStudentInfo,
  activeCycle: sampleActiveCycleVocational,
  todayAgenda: sampleTodayAgenda,
  weeklySchedule: sampleWeeklySchedule
};

const server = setupServer(
  http.get('*/api/opr/timetables/my', () => {
    return HttpResponse.json(timetableResponse);
  }),
  http.get('*/api/opr/announcements', () => {
    return HttpResponse.json({
      items: sampleAnnouncements,
      totalCount: sampleAnnouncements.length,
      page: 1,
      pageSize: 20
    });
  }),
  http.get('*/api/opr/calendar-entries', () => {
    return HttpResponse.json({
      items: sampleCalendarEntries,
      totalCount: sampleCalendarEntries.length,
      page: 1,
      pageSize: 50
    });
  }),
  http.get('*/api/opr/calendar/entries', () => {
    return HttpResponse.json({
      items: sampleCalendarEntries,
      totalCount: sampleCalendarEntries.length,
      page: 1,
      pageSize: 50
    });
  })
);

beforeAll(() => server.listen({ onUnhandledRequest: 'bypass' }));
afterEach(() => {
  server.resetHandlers();
  timetableResponse = {
    studentInfo: sampleStudentInfo,
    activeCycle: sampleActiveCycleVocational,
    todayAgenda: sampleTodayAgenda,
    weeklySchedule: sampleWeeklySchedule
  };
});
afterAll(() => server.close());

function createTestWrapper() {
  const pinia = createPinia();
  setActivePinia(pinia);

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: Infinity
      }
    }
  });

  return mount(StudentPortalView, {
    global: {
      plugins: [pinia, [VueQueryPlugin, { queryClient }]]
    }
  });
}

describe('StudentPortalView.vue (SCR-STD-PORTAL)', () => {
  it('renders student banner and active SMK block rotation badge text', async () => {
    const wrapper = createTestWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="student-name"]').text()).toContain('Ahmad Dahlan');
    expect(wrapper.find('[data-testid="student-nis"]').text()).toBe('NIS-SMK-2026-001');
    expect(wrapper.find('[data-testid="student-school-level"]').text()).toContain('SMK');
    expect(wrapper.find('[data-testid="student-study-group"]').text()).toContain('X-RPL-1');

    // SMK cycle badge
    const badge = wrapper.find('[data-testid="active-cycle-badge"]');
    expect(badge.exists()).toBe(true);
    expect(wrapper.find('[data-testid="cycle-text"]').text()).toBe(
      'Siklus Kejuruan (Vocational)'
    );
    expect(wrapper.find('[data-testid="cycle-dates"]').text()).toContain(
      '2026-11-17 s/d 2026-11-28'
    );
  });

  it('toggles between Today Agenda timeline and Weekly Timetable grid', async () => {
    const wrapper = createTestWrapper();
    await flushPromises();

    // Default view: Today Agenda
    expect(wrapper.find('[data-testid="agenda-timeline"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="weekly-timetable-grid"]').exists()).toBe(false);
    expect(wrapper.findAll('[data-testid="agenda-slot"]').length).toBe(2);

    // Switch to Weekly Timetable
    const weeklyToggle = wrapper.find('[data-testid="toggle-weekly"]');
    await weeklyToggle.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="weekly-timetable-grid"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="agenda-timeline"]').exists()).toBe(false);

    // Switch back to Today Agenda
    const agendaToggle = wrapper.find('[data-testid="toggle-agenda"]');
    await agendaToggle.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="agenda-timeline"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="weekly-timetable-grid"]').exists()).toBe(false);
  });

  it('renders "Jadwal Reguler" when schedule is not SMK rotation', async () => {
    timetableResponse = {
      studentInfo: sampleStudentInfo,
      activeCycle: sampleActiveCycleRegular,
      todayAgenda: sampleTodayAgenda,
      weeklySchedule: sampleWeeklySchedule
    };

    const wrapper = createTestWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="cycle-text"]').text()).toBe('Jadwal Reguler');
  });

  it('renders empty schedule states when no classes are scheduled', async () => {
    timetableResponse = {
      studentInfo: sampleStudentInfo,
      activeCycle: sampleActiveCycleVocational,
      todayAgenda: [],
      weeklySchedule: []
    };

    const wrapper = createTestWrapper();
    await flushPromises();

    // In Today Agenda view
    expect(wrapper.find('[data-testid="empty-agenda"]').exists()).toBe(true);
    expect(wrapper.findAll('[data-testid="agenda-slot"]').length).toBe(0);

    // In Weekly Timetable view
    await wrapper.find('[data-testid="toggle-weekly"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="empty-weekly-schedule"]').exists()).toBe(true);
  });

  it('renders student announcements feed and upcoming calendar dates', async () => {
    const wrapper = createTestWrapper();
    await flushPromises();

    const feed = wrapper.find('[data-testid="student-announcements-feed"]');
    expect(feed.exists()).toBe(true);

    // Announcements tab
    expect(wrapper.findAll('[data-testid="announcement-item"]').length).toBe(1);
    expect(wrapper.text()).toContain('Pelaksanaan Uji Kompetensi Keahlian (UKK)');

    // Calendar tab
    await wrapper.find('[data-testid="feed-tab-calendar"]').trigger('click');
    await flushPromises();

    expect(wrapper.findAll('[data-testid="calendar-item"]').length).toBe(1);
    expect(wrapper.text()).toContain('Pekan Proyek Penguatan Pelajar Pancasila (P5)');
  });

  it('renders ongoing slot indicator when current time matches a slot', async () => {
    // Current time is between 07:30 and 08:15
    const mockDate = new Date('2026-11-20T07:45:00');
    vi.setSystemTime(mockDate);

    const wrapper = createTestWrapper();
    await flushPromises();

    const ongoing = wrapper.find('[data-testid="ongoing-indicator"]');
    expect(ongoing.exists()).toBe(true);
    expect(ongoing.text()).toBe('Sedang Berlangsung');

    vi.useRealTimers();
  });

  it('renders error state when timetable query fails', async () => {
    server.use(
      http.get('*/api/opr/timetables/my', () => {
        return new HttpResponse(null, { status: 500 });
      })
    );

    const wrapper = createTestWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="portal-error"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Gagal memuat jadwal pelajaran');
  });

  it('switches days in Weekly Timetable grid via day pills', async () => {
    const wrapper = createTestWrapper();
    await flushPromises();

    await wrapper.find('[data-testid="toggle-weekly"]').trigger('click');
    await flushPromises();

    // Monday (default) has 2 slots
    expect(wrapper.findAll('[data-testid="weekly-slot-card"]').length).toBe(2);

    // Switch to Tuesday
    const tuesdayPill = wrapper.find('[data-testid="day-pill-tuesday"]');
    await tuesdayPill.trigger('click');
    await flushPromises();

    expect(wrapper.findAll('[data-testid="weekly-slot-card"]').length).toBe(1);
    expect(wrapper.text()).toContain('Pemodelan Perangkat Lunak');

    // Switch to Wednesday (no classes)
    const wednesdayPill = wrapper.find('[data-testid="day-pill-wednesday"]');
    await wednesdayPill.trigger('click');
    await flushPromises();

    expect(wrapper.findAll('[data-testid="weekly-slot-card"]').length).toBe(0);
    expect(wrapper.text()).toContain('Tidak ada kegiatan belajar pada hari ini.');
  });
});
