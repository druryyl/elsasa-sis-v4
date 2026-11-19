import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { StudyGroupsListParams } from './students.keys';

export type StudyGroupListItemDto = components['schemas']['StudyGroupListItemDto'];
export type StudyGroupDto = components['schemas']['StudyGroupDto'];
export type StudyGroupRosterDto = components['schemas']['StudyGroupRosterDto'];
export type StudyGroupRosterStudentDto = components['schemas']['StudyGroupRosterStudentDto'];
export type StudyGroupPaginatedResponse = components['schemas']['StudyGroupListItemDtoPaginatedResponse'];
export type CreateStudyGroupRequest = components['schemas']['CreateStudyGroupRequest'];
export type UpdateStudyGroupRequest = components['schemas']['UpdateStudyGroupRequest'];

type GetStudyGroupsQuery = paths['/api/std/study-groups']['get']['parameters']['query'];

function toQueryParams(params?: StudyGroupsListParams): GetStudyGroupsQuery {
  if (!params) {
    return undefined;
  }

  return {
    AcademicYearId: params.academicYearId,
    SchoolLevel: params.schoolLevel,
    Grade: params.grade,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const studyGroupsApi = {
  async list(params?: StudyGroupsListParams): Promise<StudyGroupPaginatedResponse> {
    return (await apiClient.get('/std/study-groups', {
      params: toQueryParams(params)
    })) as unknown as StudyGroupPaginatedResponse;
  },

  async create(payload: CreateStudyGroupRequest): Promise<StudyGroupDto> {
    return (await apiClient.post('/std/study-groups', payload)) as unknown as StudyGroupDto;
  },

  async update(id: string, payload: UpdateStudyGroupRequest): Promise<StudyGroupDto> {
    return (await apiClient.put(`/std/study-groups/${id}`, payload)) as unknown as StudyGroupDto;
  },

  async getRoster(id: string): Promise<StudyGroupRosterDto> {
    return (await apiClient.get(`/std/study-groups/${id}/roster`)) as unknown as StudyGroupRosterDto;
  }
};
