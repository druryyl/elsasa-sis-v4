export interface CalendarEntriesListParams {
  academicYearId?: string;
  dayType?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export const calendarEntryKeys = {
  all: ['operations', 'calendar-entries'] as const,
  lists: () => [...calendarEntryKeys.all, 'list'] as const,
  list: (params: CalendarEntriesListParams = {}) => [...calendarEntryKeys.lists(), params] as const,
  details: () => [...calendarEntryKeys.all, 'detail'] as const,
  detail: (id: string) => [...calendarEntryKeys.details(), id] as const
};
