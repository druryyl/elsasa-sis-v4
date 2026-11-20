import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import {
  curriculumApi,
  type CreateLearningUnitRequest,
  type CreateSubjectRequest,
  type CreateSubjectStageRequest,
  type EnrollStudentsInSubjectStagesRequest,
  type MapSubjectStageGradeRequest,
  type UpdateLearningUnitRequest,
  type UpdateSubjectRequest,
  type UpdateSubjectStageRequest
} from './curriculum.api';
import { curriculumKeys } from './curriculum.keys';

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

export function useCreateSubjectMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: CreateSubjectRequest) => curriculumApi.createSubject(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Subject Created',
        detail: `Subject "${data.name ?? ''}" has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create subject');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateSubjectMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateSubjectRequest }) =>
      curriculumApi.updateSubject(id, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Subject Updated',
        detail: `Subject "${data.name ?? ''}" has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update subject');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCreateSubjectStageMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      payload
    }: {
      subjectId: string;
      payload: CreateSubjectStageRequest;
    }) => curriculumApi.createSubjectStage(subjectId, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Subject Stage Created',
        detail: `Stage "${data.name ?? ''}" has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create subject stage');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateSubjectStageMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      stageId,
      payload
    }: {
      subjectId: string;
      stageId: string;
      payload: UpdateSubjectStageRequest;
    }) => curriculumApi.updateSubjectStage(subjectId, stageId, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Subject Stage Updated',
        detail: `Stage "${data.name ?? ''}" has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update subject stage');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useCreateLearningUnitMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      stageId,
      payload
    }: {
      subjectId: string;
      stageId: string;
      payload: CreateLearningUnitRequest;
    }) => curriculumApi.createLearningUnit(subjectId, stageId, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Learning Unit Created',
        detail: `Learning unit "${data.name ?? ''}" has been created.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to create learning unit');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useUpdateLearningUnitMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      stageId,
      unitId,
      payload
    }: {
      subjectId: string;
      stageId: string;
      unitId: string;
      payload: UpdateLearningUnitRequest;
    }) => curriculumApi.updateLearningUnit(subjectId, stageId, unitId, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Learning Unit Updated',
        detail: `Learning unit "${data.name ?? ''}" has been updated.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to update learning unit');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useDeleteLearningUnitMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      stageId,
      unitId
    }: {
      subjectId: string;
      stageId: string;
      unitId: string;
    }) => curriculumApi.deleteLearningUnit(subjectId, stageId, unitId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Learning Unit Deleted',
        detail: 'The learning unit has been removed.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to delete learning unit');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useMapGradeMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      stageId,
      payload
    }: {
      subjectId: string;
      stageId: string;
      payload: MapSubjectStageGradeRequest;
    }) => curriculumApi.mapGrade(subjectId, stageId, payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Grade Mapped',
        detail: `Mapped ${data.schoolLevel ?? ''} Grade ${data.grade ?? ''}.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to map grade');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useRemoveGradeMappingMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      subjectId,
      stageId,
      mappingId
    }: {
      subjectId: string;
      stageId: string;
      mappingId: string;
    }) => curriculumApi.removeGradeMapping(subjectId, stageId, mappingId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.subjects() });
      toast.add({
        severity: 'success',
        summary: 'Grade Mapping Removed',
        detail: 'The grade mapping has been removed.',
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to remove grade mapping');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}

export function useBatchEnrollMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: EnrollStudentsInSubjectStagesRequest) =>
      curriculumApi.batchEnroll(payload),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: curriculumKeys.all });
      toast.add({
        severity: 'success',
        summary: 'Batch Enrollment Complete',
        detail: `Enrolled ${data.enrolledCount} students (${data.skippedCount} skipped).`,
        life: 5000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to complete batch enrollment');
      toast.add({ severity: 'error', summary, detail, life: 5000 });
    }
  });
}
