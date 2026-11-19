import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { teachersApi } from './teachers.api';
import { teachingKeys, type TeacherListParams } from './teaching.keys';

export function useTeachersQuery(
  params: MaybeRefOrGetter<TeacherListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => teachingKeys.teachersList(toValue(params))),
    queryFn: () => teachersApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useTeacherDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => teachingKeys.teacherDetail(toValue(id) ?? '')),
    queryFn: () => teachersApi.getById(toValue(id)!),
    enabled: computed(
      () => Boolean(toValue(id)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}
