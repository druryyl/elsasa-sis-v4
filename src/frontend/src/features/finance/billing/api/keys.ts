export interface BillingChargesParams {
  studentId?: string;
  academicYearId?: string;
  status?: string;
  chargeTypeId?: string;
  month?: string;
  billingMonth?: string;
  schoolLevel?: string;
  grade?: string;
  studyGroupId?: string;
}

export interface BillingStudentSearchParams {
  academicYearId?: string;
  search?: string;
}

export const billingKeys = {
  all: ['finance', 'billing'] as const,
  charges: () => [...billingKeys.all, 'charges'] as const,
  chargesList: (params?: BillingChargesParams) => [...billingKeys.charges(), params ?? {}] as const,
  chargeDetail: (id: string) => [...billingKeys.charges(), id] as const,
  chargeTypes: (academicYearId?: string) =>
    [...billingKeys.all, 'charge-types', academicYearId ?? 'all'] as const,
  studentSearch: (params?: BillingStudentSearchParams) =>
    [...billingKeys.all, 'student-search', params ?? {}] as const
};
