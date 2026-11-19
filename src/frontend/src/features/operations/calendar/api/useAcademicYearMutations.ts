import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  academicYearsApi,
  type CreateAcademicYearRequest,
  type UpdateAcademicYearRequest
} from './academicYears.api';
import { academicYearKeys } from './academicYears.keys';

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

export function useCreateAcademicYearMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateAcademicYearRequest) => academicYearsApi.create(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Academic Year Created',
        detail: `${data.name ?? 'Academic year'} has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create academic year');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateAcademicYearMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateAcademicYearRequest }) =>
      academicYearsApi.update(id, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Academic Year Updated',
        detail: `${data.name ?? 'Academic year'} has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update academic year');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useActivateAcademicYearMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => academicYearsApi.activate(id),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Academic Year Activated',
        detail: `${data.name ?? 'Academic year'} is now the active academic year.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to activate academic year');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useActivateOperationalPeriodMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ academicYearId, periodId }: { academicYearId: string; periodId: string }) =>
      academicYearsApi.activatePeriod(academicYearId, periodId),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      const activePeriod = data.periods?.find((p) => p.status === 'ACTIVE');
      toast.add({
        severity: 'success',
        summary: 'Operational Period Activated',
        detail: activePeriod?.name
          ? `${activePeriod.name} is now the active period.`
          : 'Operational period has been activated.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(
        error,
        'Failed to activate operational period'
      );
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
