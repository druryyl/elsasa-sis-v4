import { apiClient } from '@/shared/services/http';
import type { components } from '@/shared/types/api/schema';

export type LinkedChildDto = components['schemas']['LinkedChildDto'];
export type StudentReportCardSummaryDto = components['schemas']['StudentReportCardSummaryDto'];
export type ReportCardDetailDto = components['schemas']['ReportCardDetailDto'];
export type StudentStatementDto = components['schemas']['StudentStatementDto'];
export type StudentStatementChargeDto = components['schemas']['StudentStatementChargeDto'];
export type StudentStatementReceiptDto = components['schemas']['StudentStatementReceiptDto'];
export type StudentStatementAllocationDto = components['schemas']['StudentStatementAllocationDto'];
export type PaymentReceiptDetailDto = components['schemas']['PaymentReceiptDetailDto'];
export type AnnouncementDto = components['schemas']['AnnouncementDto'];
export type CalendarEntryDto = components['schemas']['CalendarEntryDto'];

type AnnouncementPaginatedResponse = components['schemas']['AnnouncementDtoPaginatedResponse'];
type CalendarEntryPaginatedResponse = components['schemas']['CalendarEntryDtoPaginatedResponse'];

export const parentPortalApi = {
  async getMyChildren(): Promise<LinkedChildDto[]> {
    const response = (await apiClient.get('/std/students/my-children')) as unknown as LinkedChildDto[];
    return response ?? [];
  },

  async getChildReportCards(studentId: string): Promise<StudentReportCardSummaryDto[]> {
    if (!studentId) return [];
    const response = (await apiClient.get(
      `/acd/report-cards/student/${studentId}`
    )) as unknown as StudentReportCardSummaryDto[];
    return response ?? [];
  },

  async getChildStatement(studentId: string): Promise<StudentStatementDto> {
    return (await apiClient.get(
      `/fin/students/${studentId}/statement`
    )) as unknown as StudentStatementDto;
  },

  async getReportCardDetail(reportCardId: string): Promise<ReportCardDetailDto> {
    return (await apiClient.get(
      `/acd/report-cards/${reportCardId}`
    )) as unknown as ReportCardDetailDto;
  },

  async getPaymentReceiptDetail(receiptId: string): Promise<PaymentReceiptDetailDto> {
    return (await apiClient.get(
      `/fin/payments/${receiptId}`
    )) as unknown as PaymentReceiptDetailDto;
  },

  async getParentAnnouncements(params?: Record<string, unknown>): Promise<AnnouncementDto[]> {
    const response = (await apiClient.get('/opr/announcements', {
      params: {
        Page: 1,
        PageSize: 50,
        SortField: 'CreatedAt',
        SortOrder: 'desc',
        ...params
      }
    })) as unknown as AnnouncementPaginatedResponse | AnnouncementDto[];

    if (Array.isArray(response)) {
      return response;
    }
    return (response as AnnouncementPaginatedResponse)?.items ?? [];
  },

  async getParentCalendar(params?: Record<string, unknown>): Promise<CalendarEntryDto[]> {
    try {
      const response = (await apiClient.get('/opr/calendar-entries', {
        params: {
          Page: 1,
          PageSize: 100,
          SortField: 'EntryDate',
          SortOrder: 'asc',
          ...params
        }
      })) as unknown as CalendarEntryPaginatedResponse | CalendarEntryDto[];

      if (Array.isArray(response)) {
        return response;
      }
      return (response as CalendarEntryPaginatedResponse)?.items ?? [];
    } catch {
      // Fallback in case route is mapped as /opr/calendar/entries
      const response = (await apiClient.get('/opr/calendar/entries', {
        params
      })) as unknown as CalendarEntryPaginatedResponse | CalendarEntryDto[];
      if (Array.isArray(response)) {
        return response;
      }
      return (response as CalendarEntryPaginatedResponse)?.items ?? [];
    }
  }
};
