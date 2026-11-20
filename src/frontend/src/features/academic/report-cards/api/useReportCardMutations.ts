import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  reportCardsApi,
  reportCardKeys,
  type GenerateDraftReportCardsRequest,
  type OverrideReportCardStatusRequest,
  type ProvideSubjectNotesRequest,
  type WriteHomeroomRemarksRequest
} from './reportCards.api';

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

export function useGenerateDraftReportCardsMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: GenerateDraftReportCardsRequest) =>
      reportCardsApi.generateBatch(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({
        queryKey: reportCardKeys.all
      });
      toast.add({
        severity: 'success',
        summary: 'Draft Report Cards Generated',
        detail: `Generated ${data.generatedCount ?? 0} draft report card(s). Skipped ${data.skippedCount ?? 0} existing card(s).`,
        life: 5000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(
        error,
        'Failed to generate draft report cards'
      );
      toast.add({
        severity: 'error',
        summary,
        detail,
        life: 5000
      });
    }
  });
}

export function useProvideSubjectNotesMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (args: { id: string; payload: ProvideSubjectNotesRequest }) =>
      reportCardsApi.provideSubjectNotes(args.id, args.payload),
    onSuccess: async (_data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: reportCardKeys.detail(variables.id) }),
        queryClient.invalidateQueries({ queryKey: reportCardKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Subject Feedback Saved',
        detail: 'Subject teacher note has been updated on the draft report card.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to save subject feedback');
      toast.add({
        severity: 'error',
        summary,
        detail,
        life: 5000
      });
    }
  });
}

export function useWriteHomeroomRemarksMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (args: { id: string; payload: WriteHomeroomRemarksRequest }) =>
      reportCardsApi.writeHomeroomRemarks(args.id, args.payload),
    onSuccess: async (_data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: reportCardKeys.detail(variables.id) }),
        queryClient.invalidateQueries({ queryKey: reportCardKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Homeroom Remarks Saved',
        detail: 'Holistic homeroom remarks have been updated.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to save homeroom remarks');
      toast.add({
        severity: 'error',
        summary,
        detail,
        life: 5000
      });
    }
  });
}

export function useFinalizeReportCardMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => reportCardsApi.finalize(id),
    onSuccess: async (_data, id) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: reportCardKeys.detail(id) }),
        queryClient.invalidateQueries({ queryKey: reportCardKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Report Card Finalized',
        detail: 'Report card has been finalized and locked against further edits.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to finalize report card');
      toast.add({
        severity: 'error',
        summary,
        detail,
        life: 5000
      });
    }
  });
}

export function useOverrideReportCardMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (args: { id: string; payload: OverrideReportCardStatusRequest }) =>
      reportCardsApi.overrideStatus(args.id, args.payload),
    onSuccess: async (_data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: reportCardKeys.detail(variables.id) }),
        queryClient.invalidateQueries({ queryKey: reportCardKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Report Card Unlocked',
        detail: 'Report card has been unlocked back to DRAFT with audit reason recorded.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to unlock report card');
      toast.add({
        severity: 'error',
        summary,
        detail,
        life: 5000
      });
    }
  });
}
