import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import {
  timetablesApi,
  timetableKeys,
  type BlocksParams,
  type TeacherScheduleParams,
  type TimetableGridParams
} from './timetables.api';

export function useTimetableGridQuery(
  studyGroupId: MaybeRefOrGetter<string | null | undefined>,
  params: MaybeRefOrGetter<TimetableGridParams>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => {
      const groupId = toValue(studyGroupId) ?? '';
      const queryParams = toValue(params);
      return timetableKeys.grid(groupId, queryParams);
    }),
    queryFn: () => {
      const groupId = toValue(studyGroupId);
      if (!groupId) return [];
      return timetablesApi.getGrid(groupId, toValue(params));
    },
    enabled: computed(() => {
      const explicitEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      const groupId = toValue(studyGroupId);
      const queryParams = toValue(params);
      return Boolean(explicitEnabled && groupId && queryParams.periodId);
    })
  });
}

export function useSmkBlocksQuery(
  studyGroupId: MaybeRefOrGetter<string | null | undefined>,
  params: MaybeRefOrGetter<BlocksParams>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => {
      const groupId = toValue(studyGroupId) ?? '';
      const queryParams = toValue(params);
      return timetableKeys.blocksByGroup(groupId, queryParams);
    }),
    queryFn: () => {
      const groupId = toValue(studyGroupId);
      if (!groupId) return [];
      return timetablesApi.getBlocks(groupId, toValue(params));
    },
    enabled: computed(() => {
      const explicitEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      const groupId = toValue(studyGroupId);
      const queryParams = toValue(params);
      return Boolean(explicitEnabled && groupId && queryParams.periodId);
    })
  });
}

export function useTeacherPersonalScheduleQuery(
  params: MaybeRefOrGetter<TeacherScheduleParams>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => timetableKeys.teacherSchedule(toValue(params))),
    queryFn: () => timetablesApi.getTeacherSchedule(toValue(params)),
    enabled: computed(() => {
      const explicitEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      const queryParams = toValue(params);
      return Boolean(explicitEnabled && queryParams.periodId);
    })
  });
}
