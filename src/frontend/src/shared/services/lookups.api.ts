import { queryOptions } from '@tanstack/vue-query';
import { apiClient } from '@/shared/services/http';
import type { components } from '@/shared/types/api/schema';

export type AcademicYearLookupItem = components['schemas']['AcademicYearDto'];
export type SubjectLookupItem = components['schemas']['SubjectResponse'];
export type TeacherLookupItem = components['schemas']['TeacherDto'];
export type StudyGroupLookupItem = components['schemas']['StudyGroupListItemDto'];

export interface AcademicYearsLookupParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortField?: string;
  sortOrder?: string;
}

export interface ActiveTeachersLookupParams {
  subjectId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
}

export interface StudyGroupsLookupParams {
  academicYearId?: string;
  schoolLevel?: string;
  grade?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
}

export const lookupKeys = {
  all: ['lookups'] as const,
  academicYears: (params?: AcademicYearsLookupParams) =>
    [...lookupKeys.all, 'academic-years', params ?? {}] as const,
  subjects: () => [...lookupKeys.all, 'subjects'] as const,
  activeTeachers: (params?: ActiveTeachersLookupParams) =>
    [...lookupKeys.all, 'active-teachers', params ?? {}] as const,
  studyGroups: (params?: StudyGroupsLookupParams) =>
    [...lookupKeys.all, 'study-groups', params ?? {}] as const
};

export async function fetchAcademicYearsLookup(
  params?: AcademicYearsLookupParams
): Promise<AcademicYearLookupItem[]> {
  const response = (await apiClient.get('/opr/academic-years', {
    params: {
      Page: params?.page ?? 1,
      PageSize: params?.pageSize ?? 100,
      Search: params?.search?.trim() || undefined,
      SortField: params?.sortField ?? 'StartDate',
      SortOrder: params?.sortOrder ?? 'desc'
    }
  })) as unknown as components['schemas']['AcademicYearDtoPaginatedResponse'];

  return response?.items ?? [];
}

export async function fetchSubjectsLookup(): Promise<SubjectLookupItem[]> {
  const response = (await apiClient.get('/acd/subjects')) as unknown as SubjectLookupItem[];
  return response ?? [];
}

export async function fetchActiveTeachersLookup(
  params?: ActiveTeachersLookupParams
): Promise<TeacherLookupItem[]> {
  const response = (await apiClient.get('/tch/teachers', {
    params: {
      Status: 'ACTIVE',
      SubjectId: params?.subjectId,
      Search: params?.search?.trim() || undefined,
      Page: params?.page ?? 1,
      PageSize: params?.pageSize ?? 100,
      SortField: params?.sortField ?? 'FullName',
      SortOrder: params?.sortOrder ?? 'asc'
    }
  })) as unknown as components['schemas']['TeacherDtoPaginatedResponse'];

  return response?.items ?? [];
}

export async function fetchStudyGroupsLookup(
  params?: StudyGroupsLookupParams
): Promise<StudyGroupLookupItem[]> {
  const response = (await apiClient.get('/std/study-groups', {
    params: {
      AcademicYearId: params?.academicYearId,
      SchoolLevel: params?.schoolLevel,
      Grade: params?.grade,
      Search: params?.search?.trim() || undefined,
      Page: params?.page ?? 1,
      PageSize: params?.pageSize ?? 100,
      SortField: params?.sortField ?? 'Name',
      SortOrder: params?.sortOrder ?? 'asc'
    }
  })) as unknown as components['schemas']['StudyGroupListItemDtoPaginatedResponse'];

  return response?.items ?? [];
}

export const lookupsApi = {
  fetchAcademicYearsLookup,
  fetchSubjectsLookup,
  fetchActiveTeachersLookup,
  fetchStudyGroupsLookup
};

export const academicYearsLookupQuery = (params?: AcademicYearsLookupParams) =>
  queryOptions({
    queryKey: lookupKeys.academicYears(params),
    queryFn: () => fetchAcademicYearsLookup(params),
    staleTime: 5 * 60 * 1000
  });

export const subjectsLookupQuery = () =>
  queryOptions({
    queryKey: lookupKeys.subjects(),
    queryFn: () => fetchSubjectsLookup(),
    staleTime: 10 * 60 * 1000
  });

export const activeTeachersLookupQuery = (params?: ActiveTeachersLookupParams) =>
  queryOptions({
    queryKey: lookupKeys.activeTeachers(params),
    queryFn: () => fetchActiveTeachersLookup(params),
    staleTime: 2 * 60 * 1000
  });

export const studyGroupsLookupQuery = (params?: StudyGroupsLookupParams) =>
  queryOptions({
    queryKey: lookupKeys.studyGroups(params),
    queryFn: () => fetchStudyGroupsLookup(params),
    staleTime: 2 * 60 * 1000
  });
