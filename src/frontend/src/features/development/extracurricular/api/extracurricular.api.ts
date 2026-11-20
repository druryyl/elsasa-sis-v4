import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type {
  ExtracurricularListParams,
  ExtracurricularParticipantsParams,
  ExtracurricularStudentSearchParams
} from './keys';

export type ExtracurricularActivityDto = components['schemas']['ExtracurricularActivityDto'];
export type ExtracurricularParticipationDto =
  components['schemas']['ExtracurricularParticipationDto'];
export type CreateExtracurricularActivityRequest =
  components['schemas']['CreateExtracurricularActivityRequest'];
export type UpdateExtracurricularActivityRequest =
  components['schemas']['UpdateExtracurricularActivityRequest'];
export type EnrollExtracurricularParticipantRequest =
  components['schemas']['EnrollExtracurricularParticipantRequest'];
export type RecordExtracurricularSessionLogRequest =
  components['schemas']['RecordExtracurricularSessionLogRequest'];
export type StudentLookupItem = components['schemas']['StudentListItemDto'];

type GetActivitiesQuery = paths['/api/dev/extracurriculars']['get']['parameters']['query'];
type GetParticipantsQuery =
  paths['/api/dev/extracurriculars/{activityId}/participants']['get']['parameters']['query'];
type GetStudentsQuery = paths['/api/std/students']['get']['parameters']['query'];

export const extracurricularApi = {
  async listActivities(
    params?: ExtracurricularListParams
  ): Promise<ExtracurricularActivityDto[]> {
    const query: GetActivitiesQuery = {
      Status: params?.status || undefined,
      SupervisorId: params?.supervisorId || undefined
    };

    return (await apiClient.get('/dev/extracurriculars', {
      params: query
    })) as unknown as ExtracurricularActivityDto[];
  },

  async createActivity(
    payload: CreateExtracurricularActivityRequest
  ): Promise<ExtracurricularActivityDto> {
    return (await apiClient.post(
      '/dev/extracurriculars',
      payload
    )) as unknown as ExtracurricularActivityDto;
  },

  async updateActivity(
    id: string,
    payload: UpdateExtracurricularActivityRequest
  ): Promise<ExtracurricularActivityDto> {
    return (await apiClient.put(
      `/dev/extracurriculars/${id}`,
      payload
    )) as unknown as ExtracurricularActivityDto;
  },

  async listParticipants(
    activityId: string,
    params?: ExtracurricularParticipantsParams
  ): Promise<ExtracurricularParticipationDto[]> {
    const query: GetParticipantsQuery = {
      AcademicYearId: params?.academicYearId || undefined
    };

    return (await apiClient.get(`/dev/extracurriculars/${activityId}/participants`, {
      params: query
    })) as unknown as ExtracurricularParticipationDto[];
  },

  async enrollParticipant(
    activityId: string,
    payload: EnrollExtracurricularParticipantRequest
  ): Promise<ExtracurricularParticipationDto> {
    return (await apiClient.post(
      `/dev/extracurriculars/${activityId}/participants`,
      payload
    )) as unknown as ExtracurricularParticipationDto;
  },

  async withdrawParticipant(
    activityId: string,
    participationId: string
  ): Promise<ExtracurricularParticipationDto> {
    return (await apiClient.post(
      `/dev/extracurriculars/${activityId}/participants/${participationId}/withdraw`
    )) as unknown as ExtracurricularParticipationDto;
  },

  async completeParticipant(
    activityId: string,
    participationId: string
  ): Promise<ExtracurricularParticipationDto> {
    return (await apiClient.post(
      `/dev/extracurriculars/${activityId}/participants/${participationId}/complete`
    )) as unknown as ExtracurricularParticipationDto;
  },

  async recordSessionLog(
    activityId: string,
    participationId: string,
    payload: RecordExtracurricularSessionLogRequest
  ): Promise<ExtracurricularParticipationDto> {
    return (await apiClient.post(
      `/dev/extracurriculars/${activityId}/participants/${participationId}/log`,
      payload
    )) as unknown as ExtracurricularParticipationDto;
  },

  async searchStudents(params?: ExtracurricularStudentSearchParams): Promise<StudentLookupItem[]> {
    const query: GetStudentsQuery = {
      AcademicYearId: params?.academicYearId || undefined,
      Status: 'ACTIVE',
      Search: params?.search?.trim() || undefined,
      Page: 1,
      PageSize: 50,
      SortField: 'FullName',
      SortOrder: 'asc'
    };

    const response = (await apiClient.get('/std/students', {
      params: query
    })) as unknown as components['schemas']['StudentListItemDtoPaginatedResponse'];

    return response?.items ?? [];
  }
};
