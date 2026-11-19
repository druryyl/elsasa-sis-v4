import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import { lookupKeys } from '@/shared/services/lookups.api';
import {
  teachersApi,
  type AddTeachingCapabilityRequest,
  type CreateTeacherRequest,
  type UpdateTeacherRequest
} from './teachers.api';
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

export function useCreateTeacherMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateTeacherRequest) => teachersApi.create(payload),
    onSuccess: async (data) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() }),
        queryClient.invalidateQueries({ queryKey: lookupKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Teacher Registered',
        detail: `Teacher "${data.fullName ?? ''}" has been successfully registered.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to register teacher');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateTeacherMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTeacherRequest }) =>
      teachersApi.update(id, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.teacherDetail(variables.id) }),
        queryClient.invalidateQueries({ queryKey: lookupKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Profile Updated',
        detail: `Teacher "${data.fullName ?? ''}" profile updated successfully.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update teacher profile');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useActivateTeacherMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => teachersApi.activate(id),
    onSuccess: async (data, id) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.teacherDetail(id) }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.workload() }),
        queryClient.invalidateQueries({ queryKey: lookupKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Teacher Activated',
        detail: `Teacher "${data.fullName ?? ''}" is now active.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to activate teacher');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useDeactivateTeacherMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => teachersApi.deactivate(id),
    onSuccess: async (data, id) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.teacherDetail(id) }),
        queryClient.invalidateQueries({ queryKey: teachingKeys.workload() }),
        queryClient.invalidateQueries({ queryKey: lookupKeys.all })
      ]);
      toast.add({
        severity: 'warn',
        summary: 'Teacher Deactivated',
        detail: `Teacher "${data.fullName ?? ''}" is now deactivated. Note: existing teaching assignments remain active per BR-TCH-007.`,
        life: 5000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to deactivate teacher');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useAddCapabilityMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      teacherId,
      payload
    }: {
      teacherId: string;
      payload: AddTeachingCapabilityRequest;
    }) => teachersApi.addCapability(teacherId, payload),
    onSuccess: async (data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() }),
        queryClient.invalidateQueries({
          queryKey: teachingKeys.teacherDetail(variables.teacherId)
        }),
        queryClient.invalidateQueries({ queryKey: lookupKeys.all })
      ]);
      toast.add({
        severity: 'success',
        summary: 'Capability Added',
        detail: `Subject capability "${data.subjectName ?? ''}" assigned.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to add teaching capability');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRemoveCapabilityMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ teacherId, subjectId }: { teacherId: string; subjectId: string }) =>
      teachersApi.removeCapability(teacherId, subjectId),
    onSuccess: async (_data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: teachingKeys.teachers() }),
        queryClient.invalidateQueries({
          queryKey: teachingKeys.teacherDetail(variables.teacherId)
        }),
        queryClient.invalidateQueries({ queryKey: lookupKeys.all })
      ]);
      toast.add({
        severity: 'info',
        summary: 'Capability Removed',
        detail: 'Teaching capability has been removed.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(
        error,
        'Failed to remove teaching capability'
      );
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
