import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { roomsApi } from './rooms.api';
import { roomKeys, type RoomsListParams } from './rooms.keys';

export function useRoomsQuery(
  params: MaybeRefOrGetter<RoomsListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => roomKeys.list(toValue(params))),
    queryFn: () => roomsApi.list(toValue(params)),
    enabled: options?.enabled
  });
}
