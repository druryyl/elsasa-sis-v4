export interface AcademicYearsListParams {
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export const academicYearKeys = {
  all: ['operations', 'academic-years'] as const,
  lists: () => [...academicYearKeys.all, 'list'] as const,
  list: (params: AcademicYearsListParams = {}) => [...academicYearKeys.lists(), params] as const,
  details: () => [...academicYearKeys.all, 'detail'] as const,
  detail: (id: string) => [...academicYearKeys.details(), id] as const
};
