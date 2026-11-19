import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { AssignmentListParams, WorkloadParams } from './teaching.keys';

export type TeacherAssignmentDto = components['schemas']['TeacherAssignmentDto'];
export type TeacherAssignmentPaginatedResponse =
  components['schemas']['TeacherAssignmentDtoPaginatedResponse'];
export type AssignTeacherRequest = components['schemas']['AssignTeacherRequest'];
export type TeacherWorkloadResponse = components['schemas']['TeacherWorkloadResponse'];
export type TeacherWorkloadItemDto = components['schemas']['TeacherWorkloadItemDto'];

type GetAssignmentsQuery = paths['/api/tch/assignments']['get']['parameters']['query'];
type GetWorkloadQuery = paths['/api/tch/workload']['get']['parameters']['query'];

function toAssignmentsQueryParams(params?: AssignmentListParams): GetAssignmentsQuery {
  if (!params) {
    return undefined;
  }

  return {
    OperationalPeriodId: params.operationalPeriodId || undefined,
    AcademicYearId: params.academicYearId || undefined,
    StudyGroupId: params.studyGroupId || undefined,
    TeacherId: params.teacherId || undefined,
    SubjectId: params.subjectId || undefined,
    Status: params.status?.trim() || undefined,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

function toWorkloadQueryParams(params?: WorkloadParams): GetWorkloadQuery {
  if (!params) {
    return undefined;
  }

  return {
    OperationalPeriodId: params.operationalPeriodId || undefined
  };
}

export const assignmentsApi = {
  async list(params?: AssignmentListParams): Promise<TeacherAssignmentPaginatedResponse> {
    return (await apiClient.get('/tch/assignments', {
      params: toAssignmentsQueryParams(params)
    })) as unknown as TeacherAssignmentPaginatedResponse;
  },

  async create(payload: AssignTeacherRequest): Promise<TeacherAssignmentDto> {
    return (await apiClient.post('/tch/assignments', payload)) as unknown as TeacherAssignmentDto;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/tch/assignments/${id}`);
  },

  async getWorkload(params?: WorkloadParams): Promise<TeacherWorkloadResponse> {
    return (await apiClient.get('/tch/workload', {
      params: toWorkloadQueryParams(params)
    })) as unknown as TeacherWorkloadResponse;
  }
};
