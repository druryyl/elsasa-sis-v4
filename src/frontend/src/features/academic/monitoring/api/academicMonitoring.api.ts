import { apiClient } from '@/shared/services/http';
import type { components } from '@/shared/types/api/schema';

export type AcademicMonitoringProgressDto = components['schemas']['AcademicMonitoringProgressDto'];
export type AcademicMonitoringOverviewDto = components['schemas']['AcademicMonitoringOverviewDto'];
export type AcademicMonitoringExceptionsDto = components['schemas']['AcademicMonitoringExceptionsDto'];
export type MissingAttendanceAlertDto = components['schemas']['MissingAttendanceAlertDto'];
export type OverdueGradingAlertDto = components['schemas']['OverdueGradingAlertDto'];
export type PendingReportCardAlertDto = components['schemas']['PendingReportCardAlertDto'];
export type StudyGroupMonitoringRowDto = components['schemas']['StudyGroupMonitoringRowDto'];
export type AcademicYearDto = components['schemas']['AcademicYearDto'];
export type AcademicYearPaginatedResponse = components['schemas']['AcademicYearDtoPaginatedResponse'];

export interface AcademicMonitoringQueryParams {
  academicYearId?: string;
  operationalPeriodId?: string;
  date?: string;
}

export const academicMonitoringApi = {
  async getProgress(params?: AcademicMonitoringQueryParams): Promise<AcademicMonitoringProgressDto> {
    const cleanParams: Record<string, string> = {};
    if (params?.academicYearId) cleanParams.academicYearId = params.academicYearId;
    if (params?.operationalPeriodId) cleanParams.operationalPeriodId = params.operationalPeriodId;
    if (params?.date) cleanParams.date = params.date;

    const response = (await apiClient.get('/acd/monitoring/progress', {
      params: cleanParams
    })) as unknown as AcademicMonitoringProgressDto;
    return response;
  },

  async getAcademicYears(): Promise<AcademicYearDto[]> {
    const response = (await apiClient.get('/opr/academic-years', {
      params: { Page: 1, PageSize: 100, SortField: 'StartDate', SortOrder: 'desc' }
    })) as unknown as AcademicYearPaginatedResponse;
    return response?.items ?? [];
  }
};
