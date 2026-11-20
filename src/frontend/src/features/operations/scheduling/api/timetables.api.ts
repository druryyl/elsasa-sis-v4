import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import {
  timetableKeys,
  type BlocksParams,
  type TeacherScheduleParams,
  type TimetableGridParams
} from './timetables.keys';

export { timetableKeys };
export type { BlocksParams, TeacherScheduleParams, TimetableGridParams };

export type StudyGroupTimetableSlotDto = components['schemas']['StudyGroupTimetableSlotDto'];
export type ConfigureStudyGroupTimetableRequest =
  components['schemas']['ConfigureStudyGroupTimetableRequest'];
export type StudyGroupScheduleDto = components['schemas']['StudyGroupScheduleDto'];
export type StudyGroupTimetableBlockDto = components['schemas']['StudyGroupTimetableBlockDto'];
export type CreateSmkBlockRequest = components['schemas']['CreateSmkBlockRequest'];
export type TeacherPersonalScheduleResponse =
  components['schemas']['TeacherPersonalScheduleResponse'];
export type TeacherPersonalScheduleSlotDto =
  components['schemas']['TeacherPersonalScheduleSlotDto'];
export type TeacherScheduleGroupDto = components['schemas']['TeacherScheduleGroupDto'];

type GetTimetablesQuery =
  paths['/api/opr/study-groups/{id}/timetables']['get']['parameters']['query'];
type GetBlocksQuery = paths['/api/opr/study-groups/{id}/blocks']['get']['parameters']['query'];
type GetTeacherScheduleQuery = paths['/api/opr/teacher-schedule']['get']['parameters']['query'];

function toTimetableGridQuery(params: TimetableGridParams): GetTimetablesQuery {
  return {
    periodId: params.periodId,
    scheduleType: params.scheduleType?.trim() || undefined
  };
}

function toBlocksQuery(params: BlocksParams): GetBlocksQuery {
  return {
    periodId: params.periodId
  };
}

function toTeacherScheduleQuery(params: TeacherScheduleParams): GetTeacherScheduleQuery {
  return {
    periodId: params.periodId
  };
}

export const timetablesApi = {
  async getGrid(
    studyGroupId: string,
    params: TimetableGridParams
  ): Promise<StudyGroupTimetableSlotDto[]> {
    return (await apiClient.get(`/opr/study-groups/${studyGroupId}/timetables`, {
      params: toTimetableGridQuery(params)
    })) as unknown as StudyGroupTimetableSlotDto[];
  },

  async saveSlot(
    studyGroupId: string,
    payload: ConfigureStudyGroupTimetableRequest
  ): Promise<StudyGroupScheduleDto> {
    return (await apiClient.post(
      `/opr/study-groups/${studyGroupId}/timetables`,
      payload
    )) as unknown as StudyGroupScheduleDto;
  },

  async deleteSlot(studyGroupId: string, scheduleId: string): Promise<void> {
    await apiClient.delete(`/opr/study-groups/${studyGroupId}/timetables/${scheduleId}`);
  },

  async getBlocks(
    studyGroupId: string,
    params: BlocksParams
  ): Promise<StudyGroupTimetableBlockDto[]> {
    return (await apiClient.get(`/opr/study-groups/${studyGroupId}/blocks`, {
      params: toBlocksQuery(params)
    })) as unknown as StudyGroupTimetableBlockDto[];
  },

  async createBlock(
    studyGroupId: string,
    payload: CreateSmkBlockRequest
  ): Promise<StudyGroupTimetableBlockDto> {
    return (await apiClient.post(
      `/opr/study-groups/${studyGroupId}/blocks`,
      payload
    )) as unknown as StudyGroupTimetableBlockDto;
  },

  async deleteBlock(studyGroupId: string, blockId: string): Promise<void> {
    await apiClient.delete(`/opr/study-groups/${studyGroupId}/blocks/${blockId}`);
  },

  async getTeacherSchedule(
    params: TeacherScheduleParams
  ): Promise<TeacherPersonalScheduleResponse> {
    return (await apiClient.get('/opr/teacher-schedule', {
      params: toTeacherScheduleQuery(params)
    })) as unknown as TeacherPersonalScheduleResponse;
  }
};
