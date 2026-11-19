import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { studentsApi } from './students.api';
import { studyGroupsApi } from './studyGroups.api';
import {
  studentKeys,
  studyGroupKeys,
  type StudentsListParams,
  type StudyGroupsListParams
} from './students.keys';

export function useStudentsQuery(
  params: MaybeRefOrGetter<StudentsListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => studentKeys.list(toValue(params))),
    queryFn: () => studentsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useStudentDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => studentKeys.detail(toValue(id) ?? '')),
    queryFn: () => studentsApi.getById(toValue(id)!),
    enabled: computed(() => Boolean(toValue(id)) && (options?.enabled ? toValue(options.enabled) : true))
  });
}

export function useStudyGroupsQuery(
  params: MaybeRefOrGetter<StudyGroupsListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => studyGroupKeys.list(toValue(params))),
    queryFn: () => studyGroupsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useStudyGroupRosterQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => studyGroupKeys.roster(toValue(id) ?? '')),
    queryFn: () => studyGroupsApi.getRoster(toValue(id)!),
    enabled: computed(() => Boolean(toValue(id)) && (options?.enabled ? toValue(options.enabled) : true))
  });
}
