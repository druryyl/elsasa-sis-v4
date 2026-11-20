export interface ParticipationsListParams {
  subjectStageId?: string;
  studyGroupId?: string;
  operationalPeriodId?: string;
}

export const curriculumKeys = {
  all: ['academic', 'curriculum'] as const,
  subjects: () => [...curriculumKeys.all, 'subjects'] as const,
  subject: (id: string) => [...curriculumKeys.subjects(), id] as const,
  stages: (subjectId: string) => [...curriculumKeys.subject(subjectId), 'stages'] as const,
  stage: (subjectId: string, stageId: string) =>
    [...curriculumKeys.stages(subjectId), stageId] as const,
  units: (subjectId: string, stageId: string) =>
    [...curriculumKeys.stage(subjectId, stageId), 'units'] as const,
  participations: () => [...curriculumKeys.all, 'participations'] as const,
  participationsList: (params?: ParticipationsListParams) =>
    [...curriculumKeys.participations(), params ?? {}] as const
};
