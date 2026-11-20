import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { extractApiErrorMessage } from '../schemas/payments.schema';
import { paymentKeys } from './keys';
import {
  paymentsApi,
  type AllocateAdvanceDepositRequest,
  type RecordPaymentReceiptRequest
} from './payments.api';

export function useRecordPaymentMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RecordPaymentReceiptRequest) => paymentsApi.recordPayment(payload),
    onSuccess: async (data, variables) => {
      const targetStudentId = data.studentId ?? variables.studentId ?? '';
      if (targetStudentId) {
        await queryClient.invalidateQueries({
          queryKey: [...paymentKeys.all, 'statement', targetStudentId]
        });
        await queryClient.invalidateQueries({
          queryKey: paymentKeys.deposits(targetStudentId)
        });
      }
      await queryClient.invalidateQueries({ queryKey: paymentKeys.receipts() });
      await queryClient.invalidateQueries({ queryKey: ['finance', 'receivables'] });
      await queryClient.invalidateQueries({ queryKey: ['finance', 'billing'] });

      toast.add({
        severity: 'success',
        summary: 'Payment Recorded',
        detail: `Receipt ${data.receiptNumber ?? ''} has been posted.`,
        life: 4500
      });
    },
    onError: (error) => {
      const { summary, detail } = extractApiErrorMessage(error, 'Failed to record payment');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useAllocateDepositMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: AllocateAdvanceDepositRequest) => paymentsApi.allocateDeposit(payload),
    onSuccess: async (data, variables) => {
      const targetStudentId = data.studentId ?? variables.studentId ?? '';
      if (targetStudentId) {
        await queryClient.invalidateQueries({
          queryKey: [...paymentKeys.all, 'statement', targetStudentId]
        });
        await queryClient.invalidateQueries({
          queryKey: paymentKeys.deposits(targetStudentId)
        });
      }
      await queryClient.invalidateQueries({ queryKey: paymentKeys.receipts() });
      await queryClient.invalidateQueries({ queryKey: ['finance', 'receivables'] });
      await queryClient.invalidateQueries({ queryKey: ['finance', 'billing'] });

      toast.add({
        severity: 'success',
        summary: 'Advance Deposit Applied',
        detail: `Deposit from receipt ${data.receiptNumber ?? ''} has been allocated.`,
        life: 4500
      });
    },
    onError: (error) => {
      const { summary, detail } = extractApiErrorMessage(error, 'Failed to apply deposit');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
