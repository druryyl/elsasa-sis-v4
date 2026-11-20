import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import { pastoralKeys } from './keys';
import {
  pastoralApi,
  type RecordAchievementRequest,
  type RecordBehaviourNoteRequest,
  type RecordDisciplineIncidentRequest,
  type RecordEscalatedSanctionRequest
} from './pastoral.api';

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

export function useRecordBehaviourNoteMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RecordBehaviourNoteRequest) => pastoralApi.recordBehaviourNote(payload),
    onSuccess: async (data, variables) => {
      const targetStudentId = data.studentId ?? variables.studentId ?? '';
      if (targetStudentId) {
        await queryClient.invalidateQueries({ queryKey: pastoralKeys.dossier(targetStudentId) });
      }
      await queryClient.invalidateQueries({ queryKey: pastoralKeys.overview() });
      toast.add({
        severity: 'success',
        summary: 'Behaviour Note Recorded',
        detail: `Observation note saved for ${data.studentName ?? variables.studentName ?? 'student'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to record behaviour note');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRecordDisciplineIncidentMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RecordDisciplineIncidentRequest) =>
      pastoralApi.recordDisciplineIncident(payload),
    onSuccess: async (data, variables) => {
      const targetStudentId = data.studentId ?? variables.studentId ?? '';
      if (targetStudentId) {
        await queryClient.invalidateQueries({ queryKey: pastoralKeys.dossier(targetStudentId) });
      }
      await queryClient.invalidateQueries({ queryKey: pastoralKeys.overview() });
      toast.add({
        severity: 'success',
        summary: 'Discipline Incident Recorded',
        detail: `Disciplinary record logged for ${data.studentName ?? variables.studentName ?? 'student'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(
        error,
        'Failed to record discipline incident'
      );
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRecordEscalatedSanctionMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RecordEscalatedSanctionRequest) =>
      pastoralApi.recordEscalatedSanction(payload),
    onSuccess: async (data, variables) => {
      const targetStudentId = data.studentId ?? variables.studentId ?? '';
      if (targetStudentId) {
        await queryClient.invalidateQueries({ queryKey: pastoralKeys.dossier(targetStudentId) });
      }
      await queryClient.invalidateQueries({ queryKey: pastoralKeys.overview() });
      toast.add({
        severity: 'success',
        summary: 'Escalated Sanction Issued',
        detail: `Escalated sanction recorded for ${data.studentName ?? variables.studentName ?? 'student'}.`,
        life: 4500
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to issue escalated sanction');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRecordAchievementMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RecordAchievementRequest) => pastoralApi.recordAchievement(payload),
    onSuccess: async (data, variables) => {
      const targetStudentId = data.studentId ?? variables.studentId ?? '';
      if (targetStudentId) {
        await queryClient.invalidateQueries({ queryKey: pastoralKeys.dossier(targetStudentId) });
      }
      await queryClient.invalidateQueries({ queryKey: pastoralKeys.overview() });
      toast.add({
        severity: 'success',
        summary: 'Achievement Award Recorded',
        detail: `Award "${data.eventName ?? variables.eventName ?? ''}" saved.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to record achievement award');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
