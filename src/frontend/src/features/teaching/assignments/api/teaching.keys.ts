export interface TeacherListParams {
  status?: string;
  subjectId?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export interface AssignmentListParams {
  operationalPeriodId?: string;
  academicYearId?: string;
  studyGroupId?: string;
  teacherId?: string;
  subjectId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export interface WorkloadParams {
  operationalPeriodId?: string;
}

export const teachingKeys = {
  all: ['teaching'] as const,
  teachers: () => [...teachingKeys.all, 'teachers'] as const,
  teachersList: (params?: TeacherListParams) =>
    [...teachingKeys.teachers(), 'list', params ?? {}] as const,
  teacherDetail: (id: string) => [...teachingKeys.teachers(), 'detail', id] as const,
  assignments: () => [...teachingKeys.all, 'assignments'] as const,
  assignmentsList: (params?: AssignmentListParams) =>
    [...teachingKeys.assignments(), 'list', params ?? {}] as const,
  workload: (params?: WorkloadParams) => [...teachingKeys.all, 'workload', params ?? {}] as const
};
