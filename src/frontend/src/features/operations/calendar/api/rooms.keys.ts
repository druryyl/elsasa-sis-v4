export interface RoomsListParams {
  roomType?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export const roomKeys = {
  all: ['operations', 'rooms'] as const,
  lists: () => [...roomKeys.all, 'list'] as const,
  list: (params: RoomsListParams = {}) => [...roomKeys.lists(), params] as const,
  details: () => [...roomKeys.all, 'detail'] as const,
  detail: (id: string) => [...roomKeys.details(), id] as const
};
