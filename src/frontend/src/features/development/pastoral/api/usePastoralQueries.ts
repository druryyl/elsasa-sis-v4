import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { pastoralKeys } from './keys';
import { pastoralApi } from './pastoral.api';

export function useStudentDossierQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => pastoralKeys.dossier(toValue(studentId) ?? '')),
    queryFn: () => pastoralApi.getStudentDossier(toValue(studentId)!),
    enabled: computed(
      () => Boolean(toValue(studentId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}

export function useDevelopmentOverviewQuery(options?: { enabled?: MaybeRefOrGetter<boolean> }) {
  return useQuery({
    queryKey: pastoralKeys.overview(),
    queryFn: () => pastoralApi.getOverview(),
    enabled: options?.enabled
  });
}

export function usePastoralStudentSearchQuery(
  search: MaybeRefOrGetter<string>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => [...pastoralKeys.all, 'student-search', toValue(search).trim()]),
    queryFn: () => pastoralApi.searchStudents(toValue(search)),
    enabled: options?.enabled
  });
}
