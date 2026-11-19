import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  calendarEntriesApi,
  type CreateCalendarEntryRequest,
  type UpdateCalendarEntryRequest
} from './calendarEntries.api';
import { calendarEntryKeys } from './calendarEntries.keys';

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

export function useCreateCalendarEntryMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateCalendarEntryRequest) => calendarEntriesApi.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: calendarEntryKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Calendar Entry Created',
        detail: 'The calendar entry has been added.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create calendar entry');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateCalendarEntryMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateCalendarEntryRequest }) =>
      calendarEntriesApi.update(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: calendarEntryKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Calendar Entry Updated',
        detail: 'The calendar entry has been updated.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update calendar entry');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useDeleteCalendarEntryMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => calendarEntriesApi.delete(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: calendarEntryKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Calendar Entry Deleted',
        detail: 'The calendar entry has been removed.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to delete calendar entry');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
