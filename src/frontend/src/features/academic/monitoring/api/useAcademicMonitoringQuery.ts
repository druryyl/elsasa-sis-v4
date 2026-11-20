import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import {
  academicMonitoringApi,
  type AcademicMonitoringProgressDto,
  type AcademicMonitoringQueryParams,
  type AcademicYearDto
} from './academicMonitoring.api';

export const academicMonitoringKeys = {
  all: ['academic-monitoring'] as const,
  progress: (params?: AcademicMonitoringQueryParams) =>
    [...academicMonitoringKeys.all, 'progress', params ?? {}] as const,
  academicYears: () => [...academicMonitoringKeys.all, 'academic-years'] as const
};

export function useAcademicMonitoringProgressQuery(
  params: MaybeRefOrGetter<AcademicMonitoringQueryParams>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery<AcademicMonitoringProgressDto>({
    queryKey: computed(() => academicMonitoringKeys.progress(toValue(params))),
    queryFn: () => academicMonitoringApi.getProgress(toValue(params)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: 30_000
  });
}

export function useAcademicMonitoringYearsQuery(options?: {
  enabled?: MaybeRefOrGetter<boolean>;
}) {
  return useQuery<AcademicYearDto[]>({
    queryKey: academicMonitoringKeys.academicYears(),
    queryFn: () => academicMonitoringApi.getAcademicYears(),
    staleTime: 5 * 60 * 1000,
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true))
  });
}
