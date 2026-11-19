import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { admissionsApi } from './admissions.api';
import { admissionsKeys, type CandidatesListParams } from './admissions.keys';

export function useCandidatesQuery(
  params: MaybeRefOrGetter<CandidatesListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => admissionsKeys.candidates(toValue(params))),
    queryFn: () => admissionsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useCandidateDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => admissionsKeys.candidate(toValue(id) ?? '')),
    queryFn: () => admissionsApi.getById(toValue(id)!),
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled && Boolean(toValue(id));
    })
  });
}

export function useNextNisQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  enrollmentDate?: MaybeRefOrGetter<string | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() =>
      admissionsKeys.nextNis(toValue(id) ?? '', toValue(enrollmentDate))
    ),
    queryFn: () => admissionsApi.getNextNis(toValue(id)!, toValue(enrollmentDate)),
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled && Boolean(toValue(id));
    })
  });
}
