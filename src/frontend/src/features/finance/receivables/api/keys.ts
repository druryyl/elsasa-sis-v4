export interface ReceivablesSummaryParams {
  academicYearId?: string;
}

export interface ReceivablesAgingParams {
  academicYearId?: string;
  schoolLevel?: string;
  grade?: string;
  studyGroupId?: string;
  asOfDate?: string;
}

export interface DailyCashierParams {
  date: string;
}

export const receivableKeys = {
  all: ['finance', 'receivables'] as const,
  summary: (params?: ReceivablesSummaryParams) => [...receivableKeys.all, 'summary', params ?? {}] as const,
  aging: (params?: ReceivablesAgingParams) => [...receivableKeys.all, 'aging', params ?? {}] as const,
  dailyCashier: (params: DailyCashierParams) => [...receivableKeys.all, 'daily-cashier', params] as const,
  statement: (studentId: string) => [...receivableKeys.all, 'statement', studentId] as const
};
