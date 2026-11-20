import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { attendanceApi, type AttendanceSheetParams } from './attendance.api';
import { attendanceKeys } from './attendance.keys';

export function useAttendanceSheetQuery(
  params: MaybeRefOrGetter<Partial<AttendanceSheetParams>>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => {
      const p = toValue(params);
      return attendanceKeys.sheet({
        subjectId: p.subjectId ?? '',
        studyGroupId: p.studyGroupId ?? '',
        attendanceDate: p.attendanceDate ?? '',
        operationalPeriodId: p.operationalPeriodId ?? ''
      });
    }),
    queryFn: () => {
      const p = toValue(params);
      return attendanceApi.getSheet({
        subjectId: p.subjectId!,
        studyGroupId: p.studyGroupId!,
        attendanceDate: p.attendanceDate!,
        operationalPeriodId: p.operationalPeriodId!
      });
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      const p = toValue(params);
      return (
        isEnabled &&
        Boolean(p.subjectId && p.studyGroupId && p.attendanceDate && p.operationalPeriodId)
      );
    })
  });
}
