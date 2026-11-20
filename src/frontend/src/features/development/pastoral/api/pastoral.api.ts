import { apiClient } from '@/shared/services/http';
import type { components } from '@/shared/types/api/schema';

export type BehaviourNoteDto = components['schemas']['BehaviourNoteDto'];
export type DisciplineRecordDto = components['schemas']['DisciplineRecordDto'];
export type AchievementDto = components['schemas']['AchievementDto'];
export type ExtracurricularParticipationDto =
  components['schemas']['ExtracurricularParticipationDto'];
export type DossierTimelineItemDto = components['schemas']['DossierTimelineItemDto'];
export type StudentDossierDto = components['schemas']['StudentDossierDto'];
export type DevelopmentOverviewDto = components['schemas']['DevelopmentOverviewDto'];
export type StudentListItemDto = components['schemas']['StudentListItemDto'];
export type StudentPaginatedResponse =
  components['schemas']['StudentListItemDtoPaginatedResponse'];

export type RecordBehaviourNoteRequest = components['schemas']['RecordBehaviourNoteRequest'];
export type RecordDisciplineIncidentRequest =
  components['schemas']['RecordDisciplineIncidentRequest'];
export type RecordEscalatedSanctionRequest =
  components['schemas']['RecordEscalatedSanctionRequest'];
export type RecordAchievementRequest = components['schemas']['RecordAchievementRequest'];

export const pastoralApi = {
  async searchStudents(search = '', pageSize = 15): Promise<StudentListItemDto[]> {
    const response = (await apiClient.get('/std/students', {
      params: {
        Search: search.trim() || undefined,
        Status: 'ACTIVE',
        Page: 1,
        PageSize: pageSize,
        SortField: 'full_name',
        SortOrder: 'asc'
      }
    })) as unknown as StudentPaginatedResponse;

    return response?.items ?? [];
  },

  async getStudentDossier(studentId: string): Promise<StudentDossierDto> {
    return (await apiClient.get(
      `/dev/students/${studentId}/dossier`
    )) as unknown as StudentDossierDto;
  },

  async getOverview(): Promise<DevelopmentOverviewDto> {
    return (await apiClient.get('/dev/overview')) as unknown as DevelopmentOverviewDto;
  },

  async recordBehaviourNote(payload: RecordBehaviourNoteRequest): Promise<BehaviourNoteDto> {
    return (await apiClient.post('/dev/behaviour', payload)) as unknown as BehaviourNoteDto;
  },

  async recordDisciplineIncident(
    payload: RecordDisciplineIncidentRequest
  ): Promise<DisciplineRecordDto> {
    return (await apiClient.post('/dev/discipline', payload)) as unknown as DisciplineRecordDto;
  },

  async recordEscalatedSanction(
    payload: RecordEscalatedSanctionRequest
  ): Promise<DisciplineRecordDto> {
    return (await apiClient.post(
      '/dev/discipline/escalated',
      payload
    )) as unknown as DisciplineRecordDto;
  },

  async recordAchievement(payload: RecordAchievementRequest): Promise<AchievementDto> {
    return (await apiClient.post('/dev/achievements', payload)) as unknown as AchievementDto;
  }
};
