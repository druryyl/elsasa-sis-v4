export interface TimetableGridParams {
  periodId: string;
  scheduleType?: string;
}

export interface BlocksParams {
  periodId: string;
}

export interface TeacherScheduleParams {
  periodId: string;
}

export const timetableKeys = {
  all: ['operations', 'timetables'] as const,
  grids: () => [...timetableKeys.all, 'grid'] as const,
  grid: (studyGroupId: string, params: TimetableGridParams) =>
    [...timetableKeys.grids(), studyGroupId, params] as const,
  blocks: () => [...timetableKeys.all, 'blocks'] as const,
  blocksByGroup: (studyGroupId: string, params: BlocksParams) =>
    [...timetableKeys.blocks(), studyGroupId, params] as const,
  teacherSchedules: () => [...timetableKeys.all, 'teacher-schedule'] as const,
  teacherSchedule: (params: TeacherScheduleParams) =>
    [...timetableKeys.teacherSchedules(), params] as const
};
