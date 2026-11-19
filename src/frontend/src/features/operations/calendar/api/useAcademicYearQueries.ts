import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { academicYearsApi } from './academicYears.api';
import { academicYearKeys, type AcademicYearsListParams } from './academicYears.keys';

export function useAcademicYearsQuery(
  params: MaybeRefOrGetter<AcademicYearsListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => academicYearKeys.list(toValue(params))),
    queryFn: () => academicYearsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useAcademicYearDetailQuery(id: MaybeRefOrGetter<string | null | undefined>) {
  return useQuery({
    queryKey: computed(() => academicYearKeys.detail(toValue(id) ?? '')),
    queryFn: () => academicYearsApi.getById(toValue(id)!),
    enabled: computed(() => Boolean(toValue(id)))
  });
}
