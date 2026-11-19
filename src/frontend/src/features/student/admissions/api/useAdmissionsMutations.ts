import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  admissionsApi,
  type EnrollCandidateRequest,
  type RecordAdmissionDecisionRequest,
  type RegisterCandidateRequest
} from './admissions.api';
import { admissionsKeys } from './admissions.keys';

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

export function useRegisterCandidateMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RegisterCandidateRequest) => admissionsApi.register(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: admissionsKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Candidate Registered',
        detail: `${data.fullName ?? 'Candidate'} has been successfully registered.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to register candidate');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRecordAdmissionDecisionMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      id,
      payload
    }: {
      id: string;
      payload: RecordAdmissionDecisionRequest;
    }) => admissionsApi.recordDecision(id, payload),
    onSuccess: async (data, variables) => {
      await queryClient.invalidateQueries({ queryKey: admissionsKeys.all });
      const decisionLabel =
        variables.payload.decision?.toUpperCase() === 'ACCEPTED' ? 'Accepted' : 'Rejected';
      toast.add({
        severity: variables.payload.decision?.toUpperCase() === 'ACCEPTED' ? 'success' : 'warn',
        summary: `Candidate ${decisionLabel}`,
        detail: `Admission decision recorded for ${data.fullName ?? 'Candidate'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(
        error,
        'Failed to record admission decision'
      );
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useEnrollCandidateMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: EnrollCandidateRequest }) =>
      admissionsApi.enroll(id, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: admissionsKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Candidate Enrolled',
        detail: `Official student record created with NIS ${data.nis ?? ''}.`,
        life: 5000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to enroll candidate');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
