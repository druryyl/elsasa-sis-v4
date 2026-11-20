import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import { scorebookKeys, type GradebookSummaryParams } from './scorebook.keys';

export { scorebookKeys };
export type { GradebookSummaryParams };

export type AssignmentDto = components['schemas']['AssignmentDto'];
export type CreateAssignmentRequest = components['schemas']['CreateAssignmentRequest'];
export type EnterStudentScoresRequest = components['schemas']['EnterStudentScoresRequest'];
export type EnterStudentScoresResponse = components['schemas']['EnterStudentScoresResponse'];
export type ScoreEntryItemDto = components['schemas']['ScoreEntryItemDto'];
export type GradebookSummaryResponse = components['schemas']['GradebookSummaryResponse'];
export type GradebookStudentRowDto = components['schemas']['GradebookStudentRowDto'];
export type GradebookAssignmentDto = components['schemas']['GradebookAssignmentDto'];
export type GradebookAttendanceMetricsDto =
  components['schemas']['GradebookAttendanceMetricsDto'];
export type GradebookStudentScoreDto = components['schemas']['GradebookStudentScoreDto'];

type GetGradebookQuery = paths['/api/acd/gradebook']['get']['parameters']['query'];

function toGradebookQuery(params: GradebookSummaryParams): GetGradebookQuery {
  return {
    subjectStageId: params.subjectStageId,
    studyGroupId: params.studyGroupId,
    operationalPeriodId: params.operationalPeriodId
  };
}

export const scorebookApi = {
  async createAssignment(payload: CreateAssignmentRequest): Promise<AssignmentDto> {
    return (await apiClient.post('/acd/assignments', payload)) as unknown as AssignmentDto;
  },

  async saveScores(
    assignmentId: string,
    payload: EnterStudentScoresRequest
  ): Promise<EnterStudentScoresResponse> {
    return (await apiClient.post(
      `/acd/assignments/${assignmentId}/scores`,
      payload
    )) as unknown as EnterStudentScoresResponse;
  },

  async getGradebookSummary(
    params: GradebookSummaryParams
  ): Promise<GradebookSummaryResponse> {
    return (await apiClient.get('/acd/gradebook', {
      params: toGradebookQuery(params)
    })) as unknown as GradebookSummaryResponse;
  }
};
