import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import { curriculumKeys, type ParticipationsListParams } from './curriculum.keys';

export { curriculumKeys };
export type { ParticipationsListParams };

export type SubjectResponse = components['schemas']['SubjectResponse'];
export type CreateSubjectRequest = components['schemas']['CreateSubjectRequest'];
export type UpdateSubjectRequest = components['schemas']['UpdateSubjectRequest'];
export type SubjectStageResponse = components['schemas']['SubjectStageResponse'];
export type CreateSubjectStageRequest = components['schemas']['CreateSubjectStageRequest'];
export type UpdateSubjectStageRequest = components['schemas']['UpdateSubjectStageRequest'];
export type LearningUnitResponse = components['schemas']['LearningUnitResponse'];
export type CreateLearningUnitRequest = components['schemas']['CreateLearningUnitRequest'];
export type UpdateLearningUnitRequest = components['schemas']['UpdateLearningUnitRequest'];
export type SubjectStageGradeResponse = components['schemas']['SubjectStageGradeResponse'];
export type MapSubjectStageGradeRequest = components['schemas']['MapSubjectStageGradeRequest'];
export type EnrollStudentsInSubjectStagesRequest =
  components['schemas']['EnrollStudentsInSubjectStagesRequest'];
export type EnrollStudentsInSubjectStagesResponse =
  components['schemas']['EnrollStudentsInSubjectStagesResponse'];
export type SubjectStageParticipationDto =
  components['schemas']['SubjectStageParticipationDto'];

type GetParticipationsQuery = paths['/api/acd/participations']['get']['parameters']['query'];

function toParticipationsQuery(params?: ParticipationsListParams): GetParticipationsQuery {
  if (!params) {
    return undefined;
  }

  return {
    subjectStageId: params.subjectStageId || undefined,
    studyGroupId: params.studyGroupId || undefined,
    operationalPeriodId: params.operationalPeriodId || undefined
  };
}

export const curriculumApi = {
  async getSubjects(): Promise<SubjectResponse[]> {
    return (await apiClient.get('/acd/subjects')) as unknown as SubjectResponse[];
  },

  async createSubject(payload: CreateSubjectRequest): Promise<SubjectResponse> {
    return (await apiClient.post('/acd/subjects', payload)) as unknown as SubjectResponse;
  },

  async updateSubject(id: string, payload: UpdateSubjectRequest): Promise<SubjectResponse> {
    return (await apiClient.put(`/acd/subjects/${id}`, payload)) as unknown as SubjectResponse;
  },

  async getSubjectStages(subjectId: string): Promise<SubjectStageResponse[]> {
    return (await apiClient.get(
      `/acd/subjects/${subjectId}/stages`
    )) as unknown as SubjectStageResponse[];
  },

  async createSubjectStage(
    subjectId: string,
    payload: CreateSubjectStageRequest
  ): Promise<SubjectStageResponse> {
    return (await apiClient.post(
      `/acd/subjects/${subjectId}/stages`,
      payload
    )) as unknown as SubjectStageResponse;
  },

  async updateSubjectStage(
    subjectId: string,
    stageId: string,
    payload: UpdateSubjectStageRequest
  ): Promise<SubjectStageResponse> {
    return (await apiClient.put(
      `/acd/subjects/${subjectId}/stages/${stageId}`,
      payload
    )) as unknown as SubjectStageResponse;
  },

  async getLearningUnits(
    subjectId: string,
    stageId: string
  ): Promise<LearningUnitResponse[]> {
    return (await apiClient.get(
      `/acd/subjects/${subjectId}/stages/${stageId}/units`
    )) as unknown as LearningUnitResponse[];
  },

  async createLearningUnit(
    subjectId: string,
    stageId: string,
    payload: CreateLearningUnitRequest
  ): Promise<LearningUnitResponse> {
    return (await apiClient.post(
      `/acd/subjects/${subjectId}/stages/${stageId}/units`,
      payload
    )) as unknown as LearningUnitResponse;
  },

  async updateLearningUnit(
    subjectId: string,
    stageId: string,
    unitId: string,
    payload: UpdateLearningUnitRequest
  ): Promise<LearningUnitResponse> {
    return (await apiClient.put(
      `/acd/subjects/${subjectId}/stages/${stageId}/units/${unitId}`,
      payload
    )) as unknown as LearningUnitResponse;
  },

  async deleteLearningUnit(
    subjectId: string,
    stageId: string,
    unitId: string
  ): Promise<void> {
    await apiClient.delete(
      `/acd/subjects/${subjectId}/stages/${stageId}/units/${unitId}`
    );
  },

  async mapGrade(
    subjectId: string,
    stageId: string,
    payload: MapSubjectStageGradeRequest
  ): Promise<SubjectStageGradeResponse> {
    return (await apiClient.post(
      `/acd/subjects/${subjectId}/stages/${stageId}/grade-mappings`,
      payload
    )) as unknown as SubjectStageGradeResponse;
  },

  async removeGradeMapping(
    subjectId: string,
    stageId: string,
    mappingId: string
  ): Promise<void> {
    await apiClient.delete(
      `/acd/subjects/${subjectId}/stages/${stageId}/grade-mappings/${mappingId}`
    );
  },

  async batchEnroll(
    payload: EnrollStudentsInSubjectStagesRequest
  ): Promise<EnrollStudentsInSubjectStagesResponse> {
    return (await apiClient.post(
      '/acd/participations/batch',
      payload
    )) as unknown as EnrollStudentsInSubjectStagesResponse;
  },

  async listParticipations(
    params?: ParticipationsListParams
  ): Promise<SubjectStageParticipationDto[]> {
    return (await apiClient.get('/acd/participations', {
      params: toParticipationsQuery(params)
    })) as unknown as SubjectStageParticipationDto[];
  }
};
