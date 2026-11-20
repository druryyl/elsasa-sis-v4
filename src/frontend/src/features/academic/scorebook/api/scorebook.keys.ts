export interface GradebookSummaryParams {
  subjectStageId: string;
  studyGroupId: string;
  operationalPeriodId: string;
}

export const scorebookKeys = {
  all: ['academic', 'scorebook'] as const,
  gradebooks: () => [...scorebookKeys.all, 'gradebook'] as const,
  gradebook: (params: GradebookSummaryParams) =>
    [...scorebookKeys.gradebooks(), params] as const
};
