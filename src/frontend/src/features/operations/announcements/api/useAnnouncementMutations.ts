import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  announcementsApi,
  type CreateAnnouncementRequest,
  type UpdateAnnouncementRequest
} from './announcements.api';
import { announcementKeys } from './announcements.keys';

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

export function useCreateAnnouncementMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateAnnouncementRequest) => announcementsApi.create(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: announcementKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Draft Created',
        detail: `Announcement "${data.title ?? ''}" saved as draft.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create announcement draft');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateAnnouncementMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateAnnouncementRequest }) =>
      announcementsApi.update(id, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: announcementKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Announcement Updated',
        detail: `Announcement "${data.title ?? ''}" has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update announcement');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function usePublishAnnouncementMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => announcementsApi.publish(id),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: announcementKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Announcement Published',
        detail: `Announcement "${data.title ?? ''}" has been published successfully.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to publish announcement');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
