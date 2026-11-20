import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { extracurricularApi } from './extracurricular.api';
import {
  extracurricularKeys,
  type ExtracurricularListParams,
  type ExtracurricularParticipantsParams,
  type ExtracurricularStudentSearchParams
} from './keys';

export function useExtracurricularActivitiesQuery(
  params: MaybeRefOrGetter<ExtracurricularListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => extracurricularKeys.list(toValue(params))),
    queryFn: () => extracurricularApi.listActivities(toValue(params)),
    enabled: options?.enabled
  });
}

export function useExtracurricularParticipantsQuery(
  activityId: MaybeRefOrGetter<string | null | undefined>,
  params: MaybeRefOrGetter<ExtracurricularParticipantsParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() =>
      extracurricularKeys.participants(toValue(activityId) ?? '', toValue(params))
    ),
    queryFn: () => extracurricularApi.listParticipants(toValue(activityId)!, toValue(params)),
    enabled: computed(
      () =>
        Boolean(toValue(activityId)) &&
        (options?.enabled !== undefined ? toValue(options.enabled) : true)
    )
  });
}

export function useEnrollmentStudentSearchQuery(
  params: MaybeRefOrGetter<ExtracurricularStudentSearchParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => extracurricularKeys.studentSearch(toValue(params))),
    queryFn: () => extracurricularApi.searchStudents(toValue(params)),
    enabled: options?.enabled
  });
}
