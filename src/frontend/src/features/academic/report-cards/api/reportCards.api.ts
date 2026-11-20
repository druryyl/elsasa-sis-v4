import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import {
  reportCardKeys,
  type HomeroomReportCardsParams,
  type ReportCardsOverviewParams
} from './keys';

export { reportCardKeys };
export type { HomeroomReportCardsParams, ReportCardsOverviewParams };

export type HomeroomReportCardDto = components['schemas']['HomeroomReportCardDto'];
export type ReportCardsOverviewDto = components['schemas']['ReportCardsOverviewDto'];
export type ReportCardOverviewItemDto = components['schemas']['ReportCardOverviewItemDto'];
export type ReportCardDetailDto = components['schemas']['ReportCardDetailDto'];
export type ReportCardEntryDetailDto = components['schemas']['ReportCardEntryDetailDto'];
export type AttendanceSummaryDto = components['schemas']['AttendanceSummaryDto'];

export type GenerateDraftReportCardsRequest =
  components['schemas']['GenerateDraftReportCardsRequest'];
export type GenerateDraftReportCardsResponse =
  components['schemas']['GenerateDraftReportCardsResponse'];
export type ProvideSubjectNotesRequest = components['schemas']['ProvideSubjectNotesRequest'];
export type WriteHomeroomRemarksRequest = components['schemas']['WriteHomeroomRemarksRequest'];
export type OverrideReportCardStatusRequest =
  components['schemas']['OverrideReportCardStatusRequest'];

export type AcademicYearDto = components['schemas']['AcademicYearDto'];
export type AcademicYearPaginatedResponse =
  components['schemas']['AcademicYearDtoPaginatedResponse'];

type GetHomeroomQuery = paths['/api/acd/report-cards/homeroom']['get']['parameters']['query'];
type GetOverviewQuery = paths['/api/acd/report-cards/overview']['get']['parameters']['query'];

function toHomeroomQuery(params: HomeroomReportCardsParams): GetHomeroomQuery {
  return {
    studyGroupId: params.studyGroupId,
    operationalPeriodId: params.operationalPeriodId
  };
}

function toOverviewQuery(params: ReportCardsOverviewParams): GetOverviewQuery {
  return {
    operationalPeriodId: params.operationalPeriodId,
    studyGroupId: params.studyGroupId || undefined
  };
}

export const reportCardsApi = {
  async getAcademicYears(): Promise<AcademicYearDto[]> {
    const response = (await apiClient.get('/opr/academic-years', {
      params: { Page: 1, PageSize: 100, SortField: 'StartDate', SortOrder: 'desc' }
    })) as unknown as AcademicYearPaginatedResponse;
    return response?.items ?? [];
  },

  async getHomeroom(params: HomeroomReportCardsParams): Promise<HomeroomReportCardDto[]> {
    const response = (await apiClient.get('/acd/report-cards/homeroom', {
      params: toHomeroomQuery(params)
    })) as unknown as HomeroomReportCardDto[];
    return response ?? [];
  },

  async getOverview(params: ReportCardsOverviewParams): Promise<ReportCardsOverviewDto> {
    return (await apiClient.get('/acd/report-cards/overview', {
      params: toOverviewQuery(params)
    })) as unknown as ReportCardsOverviewDto;
  },

  async getById(id: string): Promise<ReportCardDetailDto> {
    return (await apiClient.get(`/acd/report-cards/${id}`)) as unknown as ReportCardDetailDto;
  },

  async generateBatch(
    payload: GenerateDraftReportCardsRequest
  ): Promise<GenerateDraftReportCardsResponse> {
    return (await apiClient.post(
      '/acd/report-cards/generate-batch',
      payload
    )) as unknown as GenerateDraftReportCardsResponse;
  },

  async provideSubjectNotes(id: string, payload: ProvideSubjectNotesRequest): Promise<void> {
    await apiClient.post(`/acd/report-cards/${id}/subject-notes`, payload);
  },

  async writeHomeroomRemarks(id: string, payload: WriteHomeroomRemarksRequest): Promise<void> {
    await apiClient.post(`/acd/report-cards/${id}/homeroom-remarks`, payload);
  },

  async finalize(id: string): Promise<void> {
    await apiClient.post(`/acd/report-cards/${id}/finalize`);
  },

  async overrideStatus(id: string, payload: OverrideReportCardStatusRequest): Promise<void> {
    await apiClient.post(`/acd/report-cards/${id}/override`, payload);
  }
};
