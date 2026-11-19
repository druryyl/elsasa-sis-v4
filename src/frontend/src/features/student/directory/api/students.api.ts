import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { StudentsListParams } from './students.keys';

export type StudentListItemDto = components['schemas']['StudentListItemDto'];
export type StudentDetailDto = components['schemas']['StudentDetailDto'];
export type StudentDto = components['schemas']['StudentDto'];
export type StudentGuardianDto = components['schemas']['StudentGuardianDto'];
export type StudentGuardianContactDto = components['schemas']['StudentGuardianContactDto'];
export type StudentPlacementDto = components['schemas']['StudentPlacementDto'];
export type StudentPaginatedResponse = components['schemas']['StudentListItemDtoPaginatedResponse'];
export type UpdateStudentRequest = components['schemas']['UpdateStudentRequest'];
export type AddStudentGuardianRequest = components['schemas']['AddStudentGuardianRequest'];
export type UpdateStudentGuardianRequest = components['schemas']['UpdateStudentGuardianRequest'];
export type PlaceStudentRequest = components['schemas']['PlaceStudentRequest'];
export type PlaceStudentFlatRequest = components['schemas']['PlaceStudentFlatRequest'];

type GetStudentsQuery = paths['/api/std/students']['get']['parameters']['query'];

function toQueryParams(params?: StudentsListParams): GetStudentsQuery {
  if (!params) {
    return undefined;
  }

  return {
    AcademicYearId: params.academicYearId,
    SchoolLevel: params.schoolLevel,
    Grade: params.grade,
    StudyGroupId: params.studyGroupId,
    Status: params.status,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const studentsApi = {
  async list(params?: StudentsListParams): Promise<StudentPaginatedResponse> {
    return (await apiClient.get('/std/students', {
      params: toQueryParams(params)
    })) as unknown as StudentPaginatedResponse;
  },

  async getById(id: string): Promise<StudentDetailDto> {
    return (await apiClient.get(`/std/students/${id}`)) as unknown as StudentDetailDto;
  },

  async update(id: string, payload: UpdateStudentRequest): Promise<StudentDto> {
    return (await apiClient.put(`/std/students/${id}`, payload)) as unknown as StudentDto;
  },

  async addGuardian(
    studentId: string,
    payload: AddStudentGuardianRequest
  ): Promise<StudentGuardianDto> {
    return (await apiClient.post(
      `/std/students/${studentId}/guardians`,
      payload
    )) as unknown as StudentGuardianDto;
  },

  async updateGuardian(
    studentId: string,
    guardianId: string,
    payload: UpdateStudentGuardianRequest
  ): Promise<StudentGuardianDto> {
    return (await apiClient.put(
      `/std/students/${studentId}/guardians/${guardianId}`,
      payload
    )) as unknown as StudentGuardianDto;
  },

  async removeGuardian(studentId: string, guardianId: string): Promise<void> {
    await apiClient.delete(`/std/students/${studentId}/guardians/${guardianId}`);
  },

  async placeStudent(
    studentId: string,
    payload: PlaceStudentRequest
  ): Promise<StudentPlacementDto> {
    return (await apiClient.post(
      `/std/students/${studentId}/placements`,
      payload
    )) as unknown as StudentPlacementDto;
  },

  async placeStudentFlat(payload: PlaceStudentFlatRequest): Promise<StudentPlacementDto> {
    return (await apiClient.post('/std/placements', payload)) as unknown as StudentPlacementDto;
  }
};
