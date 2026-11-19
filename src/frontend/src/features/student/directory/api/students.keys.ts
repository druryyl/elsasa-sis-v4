export interface StudentsListParams {
  academicYearId?: string;
  schoolLevel?: string;
  grade?: string;
  studyGroupId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export interface StudyGroupsListParams {
  academicYearId?: string;
  schoolLevel?: string;
  grade?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export const studentKeys = {
  all: ['students'] as const,
  lists: () => [...studentKeys.all, 'list'] as const,
  list: (params?: StudentsListParams) => [...studentKeys.lists(), params ?? {}] as const,
  details: () => [...studentKeys.all, 'detail'] as const,
  detail: (id: string) => [...studentKeys.details(), id] as const
};

export const studyGroupKeys = {
  all: ['study-groups'] as const,
  lists: () => [...studyGroupKeys.all, 'list'] as const,
  list: (params?: StudyGroupsListParams) => [...studyGroupKeys.lists(), params ?? {}] as const,
  rosters: () => [...studyGroupKeys.all, 'roster'] as const,
  roster: (id: string) => [...studyGroupKeys.rosters(), id] as const
};
