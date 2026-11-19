import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { RoomsListParams } from './rooms.keys';

export type RoomDto = components['schemas']['RoomDto'];
export type RoomPaginatedResponse = components['schemas']['RoomDtoPaginatedResponse'];
export type CreateRoomRequest = components['schemas']['CreateRoomRequest'];
export type UpdateRoomRequest = components['schemas']['UpdateRoomRequest'];

type GetRoomsQuery = paths['/api/opr/rooms']['get']['parameters']['query'];

function toQueryParams(params?: RoomsListParams): GetRoomsQuery {
  if (!params) {
    return undefined;
  }

  return {
    RoomType: params.roomType,
    Page: params.page,
    PageSize: params.pageSize,
    SortField: params.sortField,
    SortOrder: params.sortOrder,
    Search: params.search?.trim() || undefined
  };
}

export const roomsApi = {
  async list(params?: RoomsListParams): Promise<RoomPaginatedResponse> {
    return (await apiClient.get('/opr/rooms', {
      params: toQueryParams(params)
    })) as unknown as RoomPaginatedResponse;
  },

  async create(payload: CreateRoomRequest): Promise<RoomDto> {
    return (await apiClient.post('/opr/rooms', payload)) as unknown as RoomDto;
  },

  async update(id: string, payload: UpdateRoomRequest): Promise<RoomDto> {
    return (await apiClient.put(`/opr/rooms/${id}`, payload)) as unknown as RoomDto;
  }
};
