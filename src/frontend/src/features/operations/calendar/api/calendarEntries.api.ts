import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { CalendarEntriesListParams } from './calendarEntries.keys';

export type CalendarEntryDto = components['schemas']['CalendarEntryDto'];
export type CalendarEntryPaginatedResponse = components['schemas']['CalendarEntryDtoPaginatedResponse'];
export type CreateCalendarEntryRequest = components['schemas']['CreateCalendarEntryRequest'];
export type UpdateCalendarEntryRequest = components['schemas']['UpdateCalendarEntryRequest'];

type GetCalendarEntriesQuery = paths['/api/opr/calendar-entries']['get']['parameters']['query'];

function toQueryParams(params?: CalendarEntriesListParams): GetCalendarEntriesQuery {
  if (!params) {
    return undefined;
  }

  return {
    AcademicYearId: params.academicYearId,
    DayType: params.dayType,
    FromDate: params.fromDate,
    ToDate: params.toDate,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const calendarEntriesApi = {
  async list(params?: CalendarEntriesListParams): Promise<CalendarEntryPaginatedResponse> {
    return (await apiClient.get('/opr/calendar-entries', {
      params: toQueryParams(params)
    })) as unknown as CalendarEntryPaginatedResponse;
  },

  async create(payload: CreateCalendarEntryRequest): Promise<CalendarEntryDto> {
    return (await apiClient.post('/opr/calendar-entries', payload)) as unknown as CalendarEntryDto;
  },

  async update(id: string, payload: UpdateCalendarEntryRequest): Promise<CalendarEntryDto> {
    return (await apiClient.put(`/opr/calendar-entries/${id}`, payload)) as unknown as CalendarEntryDto;
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/opr/calendar-entries/${id}`);
  }
};
