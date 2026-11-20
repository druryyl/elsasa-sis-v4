import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import { billingKeys } from './keys';
import {
  billingApi,
  type BatchBillingPreviewRequest,
  type CancelStudentChargeRequest,
  type CreateAdHocChargeRequest,
  type GenerateBatchTuitionChargesRequest
} from './billing.api';

function extractToastError(
  error: unknown,
  fallback: string
): { summary: string; detail: string } {
  if (error instanceof ApiError) {
    return {
      summary: error.title || fallback,
      detail: error.detail || fallback
    };
  }
  if (error instanceof Error) {
    return {
      summary: fallback,
      detail: error.message || fallback
    };
  }
  return { summary: fallback, detail: fallback };
}

export function usePreviewBatchBillingMutation() {
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: BatchBillingPreviewRequest) => billingApi.previewBatchBilling(payload),
    onError: (error) => {
      const { summary, detail } = extractToastError(
        error,
        'Failed to load batch billing pre-flight preview'
      );
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useGenerateBatchBillingMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: GenerateBatchTuitionChargesRequest) =>
      billingApi.generateBatchBilling(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: billingKeys.charges() });
      const generated = data.generatedCount ?? 0;
      const skipped = data.skippedCount ?? 0;
      toast.add({
        severity: 'success',
        summary: 'Monthly Billing Generated',
        detail: `Generated ${generated} charge${generated === 1 ? '' : 's'} (${skipped} already billed skipped).`,
        life: 4500
      });
    },
    onError: (error) => {
      const { summary, detail } = extractToastError(
        error,
        'Failed to generate batch tuition charges'
      );
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCreateAdHocChargeMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateAdHocChargeRequest) => billingApi.createAdHocCharge(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: billingKeys.charges() });
      toast.add({
        severity: 'success',
        summary: 'Ad-Hoc Charge Created',
        detail: `Created "${data.chargeTypeName ?? 'charge'}" for ${data.studentName ?? 'student'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractToastError(error, 'Failed to create ad-hoc charge');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCancelChargeMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (params: { id: string; payload: CancelStudentChargeRequest }) =>
      billingApi.cancelCharge(params.id, params.payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: billingKeys.charges() });
      toast.add({
        severity: 'success',
        summary: 'Charge Cancelled',
        detail: `Cancelled charge "${data.chargeTypeName ?? ''}" for ${data.studentName ?? 'student'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractToastError(error, 'Failed to cancel charge');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
