import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { AcademicYearsListParams } from './academicYears.keys';

export type AcademicYearDto = components['schemas']['AcademicYearDto'];
export type OperationalPeriodDto = components['schemas']['OperationalPeriodDto'];
export type AcademicYearPaginatedResponse = components['schemas']['AcademicYearDtoPaginatedResponse'];
export type CreateAcademicYearRequest = components['schemas']['CreateAcademicYearRequest'];
export type CreateOperationalPeriodRequest =
  components['schemas']['CreateOperationalPeriodRequest'];
export type UpdateAcademicYearRequest = components['schemas']['UpdateAcademicYearRequest'];
export type UpdateOperationalPeriodRequest =
  components['schemas']['UpdateOperationalPeriodRequest'];

type GetAcademicYearsQuery = paths['/api/opr/academic-years']['get']['parameters']['query'];

function toQueryParams(params?: AcademicYearsListParams): GetAcademicYearsQuery {
  if (!params) {
    return undefined;
  }

  return {
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const academicYearsApi = {
  async list(params?: AcademicYearsListParams): Promise<AcademicYearPaginatedResponse> {
    return (await apiClient.get('/opr/academic-years', {
      params: toQueryParams(params)
    })) as unknown as AcademicYearPaginatedResponse;
  },

  async getById(id: string): Promise<AcademicYearDto> {
    return (await apiClient.get(`/opr/academic-years/${id}`)) as unknown as AcademicYearDto;
  },

  async create(payload: CreateAcademicYearRequest): Promise<AcademicYearDto> {
    return (await apiClient.post('/opr/academic-years', payload)) as unknown as AcademicYearDto;
  },

  async update(id: string, payload: UpdateAcademicYearRequest): Promise<AcademicYearDto> {
    return (await apiClient.put(`/opr/academic-years/${id}`, payload)) as unknown as AcademicYearDto;
  },

  async activate(id: string): Promise<AcademicYearDto> {
    return (await apiClient.post(`/opr/academic-years/${id}/activate`)) as unknown as AcademicYearDto;
  },

  async activatePeriod(academicYearId: string, periodId: string): Promise<AcademicYearDto> {
    return (await apiClient.post(
      `/opr/academic-years/${academicYearId}/periods/${periodId}/activate`
    )) as unknown as AcademicYearDto;
  }
};
