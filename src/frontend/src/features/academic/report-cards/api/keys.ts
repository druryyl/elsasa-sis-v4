export interface HomeroomReportCardsParams {
  studyGroupId: string;
  operationalPeriodId: string;
}

export interface ReportCardsOverviewParams {
  operationalPeriodId: string;
  studyGroupId?: string;
}

export const reportCardKeys = {
  all: ['academic', 'report-cards'] as const,
  homeroom: (params: HomeroomReportCardsParams) => [...reportCardKeys.all, 'homeroom', params] as const,
  overview: (params: ReportCardsOverviewParams) => [...reportCardKeys.all, 'overview', params] as const,
  details: () => [...reportCardKeys.all, 'detail'] as const,
  detail: (id: string) => [...reportCardKeys.details(), id] as const
};
