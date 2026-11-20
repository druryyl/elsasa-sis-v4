import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import { attendanceKeys, type AttendanceSheetParams } from './attendance.keys';

export { attendanceKeys };
export type { AttendanceSheetParams };

export type SubjectAttendanceSheetResponse =
  components['schemas']['SubjectAttendanceSheetResponse'];
export type AttendanceSheetItemDto = components['schemas']['AttendanceSheetItemDto'];
export type RecordAttendanceRequest = components['schemas']['RecordAttendanceRequest'];
export type RecordAttendanceItemRequest =
  components['schemas']['RecordAttendanceItemRequest'];
export type RecordAttendanceResponse = components['schemas']['RecordAttendanceResponse'];

type GetAttendanceQuery = paths['/api/acd/attendance']['get']['parameters']['query'];

function toAttendanceQuery(params: AttendanceSheetParams): GetAttendanceQuery {
  return {
    subjectId: params.subjectId,
    studyGroupId: params.studyGroupId,
    attendanceDate: params.attendanceDate,
    operationalPeriodId: params.operationalPeriodId
  };
}

export const attendanceApi = {
  async getSheet(params: AttendanceSheetParams): Promise<SubjectAttendanceSheetResponse> {
    return (await apiClient.get('/acd/attendance', {
      params: toAttendanceQuery(params)
    })) as unknown as SubjectAttendanceSheetResponse;
  },

  async batchRecord(payload: RecordAttendanceRequest): Promise<RecordAttendanceResponse> {
    return (await apiClient.post(
      '/acd/attendance',
      payload
    )) as unknown as RecordAttendanceResponse;
  }
};
