export interface AttendanceSheetParams {
  subjectId: string;
  studyGroupId: string;
  attendanceDate: string;
  operationalPeriodId: string;
}

export const attendanceKeys = {
  all: ['academic', 'attendance'] as const,
  sheets: () => [...attendanceKeys.all, 'sheets'] as const,
  sheet: (params: AttendanceSheetParams) => [...attendanceKeys.sheets(), params] as const
};
