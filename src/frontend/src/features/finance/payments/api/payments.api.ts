import { apiClient } from '@/shared/services/http';
import type { components } from '@/shared/types/api/schema';

export type StudentBillingContactDto = components['schemas']['StudentBillingContactDto'];
export type StudentStatementDto = components['schemas']['StudentStatementDto'];
export type StudentStatementChargeDto = components['schemas']['StudentStatementChargeDto'];
export type StudentStatementReceiptDto = components['schemas']['StudentStatementReceiptDto'];
export type StudentStatementAllocationDto = components['schemas']['StudentStatementAllocationDto'];
export type PaymentReceiptDto = components['schemas']['PaymentReceiptDto'];
export type PaymentReceiptDetailDto = components['schemas']['PaymentReceiptDetailDto'];
export type PaymentReceiptAllocationDetailDto =
  components['schemas']['PaymentReceiptAllocationDetailDto'];
export type PaymentAllocationDto = components['schemas']['PaymentAllocationDto'];
export type RecordPaymentReceiptRequest = components['schemas']['RecordPaymentReceiptRequest'];
export type AllocateAdvanceDepositRequest = components['schemas']['AllocateAdvanceDepositRequest'];
export type PaymentAllocationItemRequest = components['schemas']['PaymentAllocationItemRequest'];

export const paymentsApi = {
  async searchBillingContacts(search = ''): Promise<StudentBillingContactDto[]> {
    const response = (await apiClient.get('/std/billing-lookup', {
      params: {
        Search: search.trim() || undefined
      }
    })) as unknown as StudentBillingContactDto[];

    return response ?? [];
  },

  async getStudentStatement(studentId: string): Promise<StudentStatementDto> {
    return (await apiClient.get(
      `/fin/students/${studentId}/statement`
    )) as unknown as StudentStatementDto;
  },

  async getPaymentReceiptDetail(receiptId: string): Promise<PaymentReceiptDetailDto> {
    return (await apiClient.get(
      `/fin/payments/${receiptId}`
    )) as unknown as PaymentReceiptDetailDto;
  },

  async recordPayment(payload: RecordPaymentReceiptRequest): Promise<PaymentReceiptDto> {
    return (await apiClient.post('/fin/payments', payload)) as unknown as PaymentReceiptDto;
  },

  async allocateDeposit(payload: AllocateAdvanceDepositRequest): Promise<PaymentReceiptDto> {
    return (await apiClient.post(
      '/fin/deposits/allocate',
      payload
    )) as unknown as PaymentReceiptDto;
  }
};
