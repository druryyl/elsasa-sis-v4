import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  studentsApi,
  type AddStudentGuardianRequest,
  type PlaceStudentRequest,
  type PlaceStudentFlatRequest,
  type UpdateStudentGuardianRequest,
  type UpdateStudentRequest
} from './students.api';
import {
  studyGroupsApi,
  type CreateStudyGroupRequest,
  type UpdateStudyGroupRequest
} from './studyGroups.api';
import { studentKeys, studyGroupKeys } from './students.keys';

function extractErrorMessage(error: unknown, fallback: string): { summary: string; detail: string } {
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

export function useUpdateStudentMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateStudentRequest }) =>
      studentsApi.update(id, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studentKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: studentKeys.detail(variables.id) }),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.rosters() })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Student Profile Updated',
        detail: `${data.fullName ?? 'Student'} profile has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update student profile');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useAddGuardianMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studentId,
      payload
    }: {
      studentId: string;
      payload: AddStudentGuardianRequest;
    }) => studentsApi.addGuardian(studentId, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studentKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: studentKeys.detail(variables.studentId) }),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.rosters() })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Guardian Added',
        detail: `${data.fullName ?? 'Guardian'} has been registered.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to add guardian');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateGuardianMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studentId,
      guardianId,
      payload
    }: {
      studentId: string;
      guardianId: string;
      payload: UpdateStudentGuardianRequest;
    }) => studentsApi.updateGuardian(studentId, guardianId, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studentKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: studentKeys.detail(variables.studentId) }),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.rosters() })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Guardian Updated',
        detail: `${data.fullName ?? 'Guardian'} has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update guardian');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRemoveGuardianMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ studentId, guardianId }: { studentId: string; guardianId: string }) =>
      studentsApi.removeGuardian(studentId, guardianId),
    onSuccess: async (_, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studentKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: studentKeys.detail(variables.studentId) }),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.rosters() })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Guardian Removed',
        detail: 'Guardian record has been removed.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to remove guardian');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function usePlaceStudentMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studentId,
      payload
    }: {
      studentId: string;
      payload: PlaceStudentRequest;
    }) => studentsApi.placeStudent(studentId, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studentKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: studentKeys.detail(variables.studentId) }),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Placement Assigned',
        detail: `Student placed into ${data.studyGroupName ?? 'study group'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to assign placement');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function usePlaceStudentFlatMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: PlaceStudentFlatRequest) => studentsApi.placeStudentFlat(payload),
    onSuccess: async (data) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studentKeys.lists() }),
        ...(data.studentId ? [queryClient.invalidateQueries({ queryKey: studentKeys.detail(data.studentId) })] : []),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Placement Assigned',
        detail: `Student placed into ${data.studyGroupName ?? 'study group'}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to assign placement');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCreateStudyGroupMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateStudyGroupRequest) => studyGroupsApi.create(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: studyGroupKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Study Group Created',
        detail: `Class ${data.name ?? ''} has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create study group');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateStudyGroupMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateStudyGroupRequest }) =>
      studyGroupsApi.update(id, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.all }),
        queryClient.invalidateQueries({ queryKey: studyGroupKeys.roster(variables.id) })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Study Group Updated',
        detail: `Class ${data.name ?? ''} has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update study group');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
