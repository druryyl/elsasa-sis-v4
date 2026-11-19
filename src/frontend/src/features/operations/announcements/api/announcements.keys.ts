export interface AnnouncementsListParams {
  academicYearId?: string;
  targetAudience?: string;
  status?: string;
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortOrder?: string;
  search?: string;
}

export const announcementKeys = {
  all: ['operations', 'announcements'] as const,
  lists: () => [...announcementKeys.all, 'list'] as const,
  list: (params: AnnouncementsListParams = {}) => [...announcementKeys.lists(), params] as const,
  details: () => [...announcementKeys.all, 'detail'] as const,
  detail: (id: string) => [...announcementKeys.details(), id] as const
};
