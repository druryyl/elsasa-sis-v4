import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  timetablesApi,
  timetableKeys,
  type ConfigureStudyGroupTimetableRequest,
  type CreateSmkBlockRequest
} from './timetables.api';

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

export function useSaveTimetableSlotMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studyGroupId,
      payload
    }: {
      studyGroupId: string;
      payload: ConfigureStudyGroupTimetableRequest;
    }) => timetablesApi.saveSlot(studyGroupId, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableKeys.grids() });
      await queryClient.invalidateQueries({ queryKey: timetableKeys.teacherSchedules() });
      toast.add({
        severity: 'success',
        summary: 'Slot Saved',
        detail: 'Timetable slot configured successfully.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to save timetable slot');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useDeleteTimetableSlotMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studyGroupId,
      scheduleId
    }: {
      studyGroupId: string;
      scheduleId: string;
    }) => timetablesApi.deleteSlot(studyGroupId, scheduleId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableKeys.grids() });
      await queryClient.invalidateQueries({ queryKey: timetableKeys.teacherSchedules() });
      toast.add({
        severity: 'success',
        summary: 'Slot Deleted',
        detail: 'Timetable slot removed successfully.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to delete timetable slot');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCreateSmkBlockMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studyGroupId,
      payload
    }: {
      studyGroupId: string;
      payload: CreateSmkBlockRequest;
    }) => timetablesApi.createBlock(studyGroupId, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableKeys.blocks() });
      toast.add({
        severity: 'success',
        summary: 'SMK Block Added',
        detail: 'SMK rotation block added successfully.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to add SMK block');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useDeleteSmkBlockMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      studyGroupId,
      blockId
    }: {
      studyGroupId: string;
      blockId: string;
    }) => timetablesApi.deleteBlock(studyGroupId, blockId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: timetableKeys.blocks() });
      toast.add({
        severity: 'success',
        summary: 'SMK Block Removed',
        detail: 'SMK rotation block removed successfully.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to delete SMK block');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
