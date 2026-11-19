import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { assignmentsApi } from './assignments.api';
import {
  teachingKeys,
  type AssignmentListParams,
  type WorkloadParams
} from './teaching.keys';

export function useAssignmentsQuery(
  params: MaybeRefOrGetter<AssignmentListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => teachingKeys.assignmentsList(toValue(params))),
    queryFn: () => assignmentsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useWorkloadQuery(
  params: MaybeRefOrGetter<WorkloadParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => teachingKeys.workload(toValue(params))),
    queryFn: () => assignmentsApi.getWorkload(toValue(params)),
    enabled: options?.enabled
  });
}
