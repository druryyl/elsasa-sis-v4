import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  extracurricularApi,
  type CreateExtracurricularActivityRequest,
  type EnrollExtracurricularParticipantRequest,
  type RecordExtracurricularSessionLogRequest,
  type UpdateExtracurricularActivityRequest
} from './extracurricular.api';
import { extracurricularKeys } from './keys';

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

export function useCreateExtracurricularMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateExtracurricularActivityRequest) =>
      extracurricularApi.createActivity(payload),
    onSuccess: async (activity) => {
      await queryClient.invalidateQueries({
        queryKey: extracurricularKeys.lists()
      });
      toast.add({
        severity: 'success',
        summary: 'Club Created',
        detail: `${activity.name ?? 'Extracurricular club'} has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create club');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateExtracurricularMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (input: { id: string; payload: UpdateExtracurricularActivityRequest }) =>
      extracurricularApi.updateActivity(input.id, input.payload),
    onSuccess: async (activity) => {
      await queryClient.invalidateQueries({
        queryKey: extracurricularKeys.all
      });
      toast.add({
        severity: 'success',
        summary: 'Club Updated',
        detail: `${activity.name ?? 'Extracurricular club'} has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update club');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useEnrollParticipantMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (input: {
      activityId: string;
      payload: EnrollExtracurricularParticipantRequest;
    }) => extracurricularApi.enrollParticipant(input.activityId, input.payload),
    onSuccess: async (participation, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: extracurricularKeys.lists()
        }),
        queryClient.invalidateQueries({
          queryKey: extracurricularKeys.detail(variables.activityId)
        })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Student Enrolled',
        detail: `${participation.studentName ?? 'Student'} enrolled in ${participation.activityName ?? 'club'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to enroll student');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useWithdrawParticipantMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (input: { activityId: string; participationId: string }) =>
      extracurricularApi.withdrawParticipant(input.activityId, input.participationId),
    onSuccess: async (participation, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: extracurricularKeys.lists()
        }),
        queryClient.invalidateQueries({
          queryKey: extracurricularKeys.detail(variables.activityId)
        })
      ]);
      toast.add({
        severity: 'info',
        summary: 'Participant Withdrawn',
        detail: `${participation.studentName ?? 'Student'} has been marked as withdrawn.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to withdraw participant');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCompleteParticipantMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (input: { activityId: string; participationId: string }) =>
      extracurricularApi.completeParticipant(input.activityId, input.participationId),
    onSuccess: async (participation, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: extracurricularKeys.lists()
        }),
        queryClient.invalidateQueries({
          queryKey: extracurricularKeys.detail(variables.activityId)
        })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Participation Completed',
        detail: `${participation.studentName ?? 'Student'} marked as completed.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to complete participation');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRecordSessionLogMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (input: {
      activityId: string;
      participationId: string;
      payload: RecordExtracurricularSessionLogRequest;
    }) =>
      extracurricularApi.recordSessionLog(
        input.activityId,
        input.participationId,
        input.payload
      ),
    onSuccess: async (participation, variables) => {
      await queryClient.invalidateQueries({
        queryKey: extracurricularKeys.detail(variables.activityId)
      });
      toast.add({
        severity: 'success',
        summary: 'Session Log Recorded',
        detail: `Activity log saved for ${participation.studentName ?? 'participant'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to record session log');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
