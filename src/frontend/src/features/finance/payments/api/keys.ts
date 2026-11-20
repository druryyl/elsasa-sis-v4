export interface BillingLookupParams {
  search?: string;
}

export interface StudentStatementParams {
  academicYearId?: string;
}

export const paymentKeys = {
  all: ['finance', 'payments'] as const,
  billingLookup: (params?: BillingLookupParams) => [...paymentKeys.all, 'billing-lookup', params ?? {}] as const,
  statement: (studentId: string, params?: StudentStatementParams) =>
    [...paymentKeys.all, 'statement', studentId, params ?? {}] as const,
  receipts: () => [...paymentKeys.all, 'receipts'] as const,
  receiptDetail: (receiptId: string) => [...paymentKeys.receipts(), receiptId] as const,
  deposits: (studentId: string) => [...paymentKeys.all, 'deposits', studentId] as const
};
