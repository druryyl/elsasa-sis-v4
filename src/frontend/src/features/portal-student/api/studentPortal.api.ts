import { apiClient } from '@/shared/services/http';
import type { components } from '@/shared/types/api/schema';

export type StudentPersonalScheduleResponse =
  components['schemas']['StudentPersonalScheduleResponse'];
export type StudentTimetableInfoDto = components['schemas']['StudentTimetableInfoDto'];
export type ActiveSmkCycleDto = components['schemas']['ActiveSmkCycleDto'];
export type ScheduleSlotDto = components['schemas']['ScheduleSlotDto'];
export type AnnouncementDto = components['schemas']['AnnouncementDto'];
export type AnnouncementPaginatedResponse =
  components['schemas']['AnnouncementDtoPaginatedResponse'];
export type CalendarEntryDto = components['schemas']['CalendarEntryDto'];
export type CalendarEntryPaginatedResponse =
  components['schemas']['CalendarEntryDtoPaginatedResponse'];

export const studentPortalApi = {
  async getPersonalTimetable(date?: string): Promise<StudentPersonalScheduleResponse> {
    return (await apiClient.get('/opr/timetables/my', {
      params: date ? { date } : undefined
    })) as unknown as StudentPersonalScheduleResponse;
  },

  async getStudentAnnouncements(): Promise<AnnouncementPaginatedResponse> {
    return (await apiClient.get('/opr/announcements', {
      params: {
        TargetAudience: 'STUDENTS',
        Status: 'PUBLISHED',
        PageSize: 20
      }
    })) as unknown as AnnouncementPaginatedResponse;
  },

  async getStudentCalendar(): Promise<CalendarEntryPaginatedResponse> {
    try {
      return (await apiClient.get('/opr/calendar-entries', {
        params: { PageSize: 50 }
      })) as unknown as CalendarEntryPaginatedResponse;
    } catch {
      return (await apiClient.get('/opr/calendar/entries', {
        params: { PageSize: 50 }
      })) as unknown as CalendarEntryPaginatedResponse;
    }
  }
};

export const getPersonalTimetable = studentPortalApi.getPersonalTimetable;
export const getStudentAnnouncements = studentPortalApi.getStudentAnnouncements;
export const getStudentCalendar = studentPortalApi.getStudentCalendar;
