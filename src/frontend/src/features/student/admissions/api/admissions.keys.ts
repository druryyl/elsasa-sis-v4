export interface CandidatesListParams {
  admissionYearId?: string;
  status?: string;
  schoolLevel?: string;
  grade?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export const admissionsKeys = {
  all: ['student', 'admissions'] as const,
  candidates: (params: CandidatesListParams = {}) =>
    [...admissionsKeys.all, 'candidates', params] as const,
  candidate: (id: string) => [...admissionsKeys.all, 'candidate', id] as const,
  nextNis: (id: string, enrollmentDate?: string) =>
    [...admissionsKeys.all, 'next-nis', id, enrollmentDate ?? ''] as const
};
