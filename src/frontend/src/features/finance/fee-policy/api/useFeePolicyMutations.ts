import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  feePolicyApi,
  type CreateChargeTypeRequest,
  type CreateStudentDiscountRequest,
  type UpdateChargeTypeRequest
} from './feePolicy.api';
import { feePolicyKeys } from './keys';

function extractErrorMessage(
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

export function useCreateChargeTypeMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateChargeTypeRequest) => feePolicyApi.createChargeType(payload),
    onSuccess: async (chargeType) => {
      await queryClient.invalidateQueries({
        queryKey: feePolicyKeys.chargeTypes()
      });
      toast.add({
        severity: 'success',
        summary: 'Fee Structure Created',
        detail: `${chargeType.name ?? 'Fee structure'} saved as DRAFT.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create fee structure');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateChargeTypeMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (input: { id: string; payload: UpdateChargeTypeRequest }) =>
      feePolicyApi.updateChargeType(input.id, input.payload),
    onSuccess: async (chargeType) => {
      await queryClient.invalidateQueries({
        queryKey: feePolicyKeys.chargeTypes()
      });
      toast.add({
        severity: 'success',
        summary: 'Fee Structure Updated',
        detail: `${chargeType.name ?? 'Fee structure'} has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update fee structure');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useActivateChargeTypeMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => feePolicyApi.activateChargeType(id),
    onSuccess: async (chargeType) => {
      await queryClient.invalidateQueries({
        queryKey: feePolicyKeys.chargeTypes()
      });
      toast.add({
        severity: 'success',
        summary: 'Fee Structure Activated',
        detail: `${chargeType.name ?? 'Fee structure'} is now ACTIVE and locked for billing.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to activate fee structure');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCreateStudentDiscountMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateStudentDiscountRequest) =>
      feePolicyApi.createStudentDiscount(payload),
    onSuccess: async (discount) => {
      await queryClient.invalidateQueries({
        queryKey: feePolicyKeys.discounts()
      });
      toast.add({
        severity: 'success',
        summary: 'Standing Discount Granted',
        detail: `Discount applied for ${discount.studentName ?? 'student'} on ${discount.chargeTypeName ?? 'fee'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to grant student discount');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRevokeStudentDiscountMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => feePolicyApi.revokeStudentDiscount(id),
    onSuccess: async (discount) => {
      await queryClient.invalidateQueries({
        queryKey: feePolicyKeys.discounts()
      });
      toast.add({
        severity: 'info',
        summary: 'Standing Discount Revoked',
        detail: `Discount for ${discount.studentName ?? 'student'} has been revoked.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to revoke student discount');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
