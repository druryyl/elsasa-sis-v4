import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { CandidatesListParams } from './admissions.keys';

export type CandidateSummaryDto = components['schemas']['CandidateSummaryDto'];
export type CandidateSummaryDtoPaginatedResponse =
  components['schemas']['CandidateSummaryDtoPaginatedResponse'];
export type CandidateDto = components['schemas']['CandidateDto'];
export type CandidateGuardianDto = components['schemas']['CandidateGuardianDto'];
export type RegisterCandidateRequest = components['schemas']['RegisterCandidateRequest'];
export type RegisterCandidateGuardianRequest =
  components['schemas']['RegisterCandidateGuardianRequest'];
export type RecordAdmissionDecisionRequest =
  components['schemas']['RecordAdmissionDecisionRequest'];
export type EnrollCandidateRequest = components['schemas']['EnrollCandidateRequest'];
export type EnrolledStudentResponse = components['schemas']['EnrolledStudentResponse'];
export type NextNisResponse = components['schemas']['NextNisResponse'];

type GetCandidatesQuery = paths['/api/std/candidates']['get']['parameters']['query'];

function toCandidatesQueryParams(params?: CandidatesListParams): GetCandidatesQuery {
  if (!params) {
    return undefined;
  }

  return {
    AdmissionYearId: params.admissionYearId,
    Status: params.status,
    SchoolLevel: params.schoolLevel,
    Grade: params.grade,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const admissionsApi = {
  async list(params?: CandidatesListParams): Promise<CandidateSummaryDtoPaginatedResponse> {
    return (await apiClient.get('/std/candidates', {
      params: toCandidatesQueryParams(params)
    })) as unknown as CandidateSummaryDtoPaginatedResponse;
  },

  async getById(id: string): Promise<CandidateDto> {
    return (await apiClient.get(`/std/candidates/${id}`)) as unknown as CandidateDto;
  },

  async register(payload: RegisterCandidateRequest): Promise<CandidateDto> {
    return (await apiClient.post('/std/candidates', payload)) as unknown as CandidateDto;
  },

  async recordDecision(
    id: string,
    payload: RecordAdmissionDecisionRequest
  ): Promise<CandidateDto> {
    return (await apiClient.post(
      `/std/candidates/${id}/decision`,
      payload
    )) as unknown as CandidateDto;
  },

  async getNextNis(id: string, enrollmentDate?: string): Promise<NextNisResponse> {
    return (await apiClient.get(`/std/candidates/${id}/next-nis`, {
      params: enrollmentDate ? { enrollmentDate } : undefined
    })) as unknown as NextNisResponse;
  },

  async enroll(id: string, payload: EnrollCandidateRequest): Promise<EnrolledStudentResponse> {
    return (await apiClient.post(
      `/std/candidates/${id}/enroll`,
      payload
    )) as unknown as EnrolledStudentResponse;
  }
};
