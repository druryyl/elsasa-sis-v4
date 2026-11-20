import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { studentPortalApi } from './studentPortal.api';

export const studentPortalKeys = {
  all: ['portal-student'] as const,
  timetables: () => [...studentPortalKeys.all, 'timetable'] as const,
  timetable: (date?: string) => [...studentPortalKeys.timetables(), date ?? 'today'] as const,
  announcements: () => [...studentPortalKeys.all, 'announcements'] as const,
  calendar: () => [...studentPortalKeys.all, 'calendar'] as const
};

export function useStudentTimetableQuery(
  date?: MaybeRefOrGetter<string | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => studentPortalKeys.timetable(toValue(date))),
    queryFn: () => studentPortalApi.getPersonalTimetable(toValue(date)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true))
  });
}

export function useStudentAnnouncementsQuery(options?: {
  enabled?: MaybeRefOrGetter<boolean>;
}) {
  return useQuery({
    queryKey: computed(() => studentPortalKeys.announcements()),
    queryFn: () => studentPortalApi.getStudentAnnouncements(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true))
  });
}

export function useStudentCalendarQuery(options?: {
  enabled?: MaybeRefOrGetter<boolean>;
}) {
  return useQuery({
    queryKey: computed(() => studentPortalKeys.calendar()),
    queryFn: () => studentPortalApi.getStudentCalendar(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true))
  });
}
