import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  assignmentsApi,
  type AssignTeacherRequest
} from './assignments.api';
import { teachingKeys } from './teaching.keys';

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

export function useAssignTeacherMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: AssignTeacherRequest) => assignmentsApi.create(payload),
    onSuccess: async (data) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.assignments() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.workload() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Teacher Assigned',
        detail: `Assignment for "${data.teacherName ?? ''}" to ${data.studyGroupName ?? ''} (${data.subjectName ?? ''}) created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to assign teacher');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRemoveAssignmentMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => assignmentsApi.remove(id),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.assignments() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.workload() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() })
      ]);
      toast.add({
        severity: 'info',
        summary: 'Assignment Removed',
        detail: 'Teaching assignment has been removed.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to remove assignment');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
