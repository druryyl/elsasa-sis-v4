import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { AnnouncementsListParams } from './announcements.keys';

export type AnnouncementDto = components['schemas']['AnnouncementDto'];
export type AnnouncementPaginatedResponse =
  components['schemas']['AnnouncementDtoPaginatedResponse'];
export type CreateAnnouncementRequest = components['schemas']['CreateAnnouncementRequest'];
export type UpdateAnnouncementRequest = components['schemas']['UpdateAnnouncementRequest'];

type GetAnnouncementsQuery = paths['/api/opr/announcements']['get']['parameters']['query'];

function toQueryParams(params?: AnnouncementsListParams): GetAnnouncementsQuery {
  if (!params) {
    return undefined;
  }

  return {
    AcademicYearId: params.academicYearId || undefined,
    TargetAudience: params.targetAudience?.trim() || undefined,
    Status: params.status?.trim() || undefined,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const announcementsApi = {
  async list(params?: AnnouncementsListParams): Promise<AnnouncementPaginatedResponse> {
    return (await apiClient.get('/opr/announcements', {
      params: toQueryParams(params)
    })) as unknown as AnnouncementPaginatedResponse;
  },

  async getById(id: string): Promise<AnnouncementDto> {
    return (await apiClient.get(`/opr/announcements/${id}`)) as unknown as AnnouncementDto;
  },

  async create(payload: CreateAnnouncementRequest): Promise<AnnouncementDto> {
    return (await apiClient.post('/opr/announcements', payload)) as unknown as AnnouncementDto;
  },

  async update(id: string, payload: UpdateAnnouncementRequest): Promise<AnnouncementDto> {
    return (await apiClient.put(`/opr/announcements/${id}`, payload)) as unknown as AnnouncementDto;
  },

  async publish(id: string): Promise<AnnouncementDto> {
    return (await apiClient.post(`/opr/announcements/${id}/publish`)) as unknown as AnnouncementDto;
  }
};
