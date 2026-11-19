import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { TeacherListParams } from './teaching.keys';

export type TeacherDto = components['schemas']['TeacherDto'];
export type TeacherDetailDto = components['schemas']['TeacherDetailDto'];
export type TeacherPaginatedResponse = components['schemas']['TeacherDtoPaginatedResponse'];
export type CreateTeacherRequest = components['schemas']['CreateTeacherRequest'];
export type UpdateTeacherRequest = components['schemas']['UpdateTeacherRequest'];
export type AddTeachingCapabilityRequest = components['schemas']['AddTeachingCapabilityRequest'];
export type TeachingCapabilityDto = components['schemas']['TeachingCapabilityDto'];

type GetTeachersQuery = paths['/api/tch/teachers']['get']['parameters']['query'];

function toQueryParams(params?: TeacherListParams): GetTeachersQuery {
  if (!params) {
    return undefined;
  }

  return {
    Status: params.status?.trim() || undefined,
    SubjectId: params.subjectId || undefined,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const teachersApi = {
  async list(params?: TeacherListParams): Promise<TeacherPaginatedResponse> {
    return (await apiClient.get('/tch/teachers', {
      params: toQueryParams(params)
    })) as unknown as TeacherPaginatedResponse;
  },

  async getById(id: string): Promise<TeacherDetailDto> {
    return (await apiClient.get(`/tch/teachers/${id}`)) as unknown as TeacherDetailDto;
  },

  async create(payload: CreateTeacherRequest): Promise<TeacherDto> {
    return (await apiClient.post('/tch/teachers', payload)) as unknown as TeacherDto;
  },

  async update(id: string, payload: UpdateTeacherRequest): Promise<TeacherDto> {
    return (await apiClient.put(`/tch/teachers/${id}`, payload)) as unknown as TeacherDto;
  },

  async activate(id: string): Promise<TeacherDto> {
    return (await apiClient.post(`/tch/teachers/${id}/activate`)) as unknown as TeacherDto;
  },

  async deactivate(id: string): Promise<TeacherDto> {
    return (await apiClient.post(`/tch/teachers/${id}/deactivate`)) as unknown as TeacherDto;
  },

  async addCapability(
    teacherId: string,
    payload: AddTeachingCapabilityRequest
  ): Promise<TeachingCapabilityDto> {
    return (await apiClient.post(
      `/tch/teachers/${teacherId}/capabilities`,
      payload
    )) as unknown as TeachingCapabilityDto;
  },

  async removeCapability(teacherId: string, subjectId: string): Promise<void> {
    await apiClient.delete(`/tch/teachers/${teacherId}/capabilities/${subjectId}`);
  }
};
