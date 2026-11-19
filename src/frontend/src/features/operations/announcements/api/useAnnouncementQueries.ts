import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { announcementsApi } from './announcements.api';
import { announcementKeys, type AnnouncementsListParams } from './announcements.keys';

export function useAnnouncementsQuery(
  params: MaybeRefOrGetter<AnnouncementsListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => announcementKeys.list(toValue(params))),
    queryFn: () => announcementsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}

export function useAnnouncementDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => announcementKeys.detail(toValue(id) ?? '')),
    queryFn: () => announcementsApi.getById(toValue(id)!),
    enabled: computed(
      () => Boolean(toValue(id)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}
