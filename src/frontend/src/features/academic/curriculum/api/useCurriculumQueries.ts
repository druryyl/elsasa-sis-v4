import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import {
  curriculumApi,
  type ParticipationsListParams,
  type SubjectResponse,
  type SubjectStageParticipationDto
} from './curriculum.api';
import { curriculumKeys } from './curriculum.keys';

export function useCurriculumStructureQuery(options?: {
  enabled?: MaybeRefOrGetter<boolean>;
}) {
  return useQuery<SubjectResponse[]>({
    queryKey: curriculumKeys.subjects(),
    queryFn: () => curriculumApi.getSubjects(),
    enabled: options?.enabled
  });
}

export function useSubjectStageParticipationsQuery(
  params: MaybeRefOrGetter<ParticipationsListParams | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery<SubjectStageParticipationDto[]>({
    queryKey: computed(() => curriculumKeys.participationsList(toValue(params))),
    queryFn: () => curriculumApi.listParticipations(toValue(params)),
    enabled: options?.enabled
  });
}
