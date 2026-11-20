import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { ApiError } from '@/shared/types/common';
import { attendanceApi, type RecordAttendanceRequest } from './attendance.api';
import { attendanceKeys } from './attendance.keys';

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

export function useRecordAttendanceMutation() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (payload: RecordAttendanceRequest) => attendanceApi.batchRecord(payload),
    onSuccess: async (data, variables) => {
      await queryClient.invalidateQueries({
        queryKey: attendanceKeys.sheets()
      });
      toast.add({
        severity: 'success',
        summary: 'Attendance Recorded',
        detail: `Successfully recorded attendance for ${data.recordedCount ?? variables.items?.length ?? 0} students.`,
        life: 4000
      });
    },
    onError: (error) => {
      const { summary, detail } = extractErrorMessage(error, 'Failed to record attendance');
      toast.add({
        severity: 'error',
        summary,
        detail,
        life: 5000
      });
    }
  });
}
