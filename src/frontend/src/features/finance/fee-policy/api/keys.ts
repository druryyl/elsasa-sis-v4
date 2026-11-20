export interface ChargeTypeListParams {
  academicYearId?: string;
  frequency?: string;
  status?: string;
  schoolLevel?: string;
  grade?: string;
}

export interface StudentDiscountsParams {
  studentId?: string;
  academicYearId?: string;
  chargeTypeId?: string;
  isActive?: boolean;
}

export interface DiscountStudentSearchParams {
  academicYearId?: string;
  search?: string;
}

export const feePolicyKeys = {
  all: ['finance', 'fee-policy'] as const,
  chargeTypes: () => [...feePolicyKeys.all, 'charge-types'] as const,
  chargeTypeList: (params?: ChargeTypeListParams) =>
    [...feePolicyKeys.chargeTypes(), params ?? {}] as const,
  chargeTypeDetail: (id: string) => [...feePolicyKeys.chargeTypes(), id] as const,
  discounts: () => [...feePolicyKeys.all, 'discounts'] as const,
  discountList: (params?: StudentDiscountsParams) =>
    [...feePolicyKeys.discounts(), params ?? {}] as const,
  studentSearch: (params?: DiscountStudentSearchParams) =>
    [...feePolicyKeys.all, 'student-search', params ?? {}] as const
};
