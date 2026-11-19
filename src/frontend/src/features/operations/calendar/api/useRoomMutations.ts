import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  roomsApi,
  type CreateRoomRequest,
  type UpdateRoomRequest
} from './rooms.api';
import { roomKeys } from './rooms.keys';

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

export function useCreateRoomMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateRoomRequest) => roomsApi.create(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: roomKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Room Created',
        detail: `${data.name ?? 'Room'} has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create room');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateRoomMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateRoomRequest }) =>
      roomsApi.update(id, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: roomKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Room Updated',
        detail: `${data.name ?? 'Room'} has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update room');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
