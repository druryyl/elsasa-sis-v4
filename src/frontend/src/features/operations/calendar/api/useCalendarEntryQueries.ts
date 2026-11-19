import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { calendarEntriesApi } from './calendarEntries.api';
import { calendarEntryKeys, type CalendarEntriesListParams } from './calendarEntries.keys';

export function useCalendarEntriesQuery(
  params: MaybeRefOrGetter<CalendarEntriesListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => calendarEntryKeys.list(toValue(params))),
    queryFn: () => calendarEntriesApi.list(toValue(params)),
    enabled: options?.enabled
  });
}
