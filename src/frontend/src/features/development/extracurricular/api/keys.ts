export interface ExtracurricularListParams {
  academicYearId?: string;
  status?: string;
  supervisorId?: string;
}

export interface ExtracurricularParticipantsParams {
  academicYearId?: string;
  status?: string;
}

export interface ExtracurricularStudentSearchParams {
  search?: string;
  academicYearId?: string;
}

export const extracurricularKeys = {
  all: ['development', 'extracurricular'] as const,
  lists: () => [...extracurricularKeys.all, 'list'] as const,
  list: (params?: ExtracurricularListParams) =>
    [...extracurricularKeys.lists(), params ?? {}] as const,
  details: () => [...extracurricularKeys.all, 'detail'] as const,
  detail: (id: string) => [...extracurricularKeys.details(), id] as const,
  participants: (activityId: string, params?: ExtracurricularParticipantsParams) =>
    [...extracurricularKeys.detail(activityId), 'participants', params ?? {}] as const,
  studentSearch: (params?: ExtracurricularStudentSearchParams) =>
    [...extracurricularKeys.all, 'student-search', params ?? {}] as const
};
