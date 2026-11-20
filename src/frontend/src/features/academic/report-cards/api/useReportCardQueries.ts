import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import {
  reportCardsApi,
  reportCardKeys,
  type HomeroomReportCardsParams,
  type ReportCardsOverviewParams
} from './reportCards.api';

export function useReportCardAcademicYearsQuery(options?: {
  enabled?: MaybeRefOrGetter<boolean>;
}) {
  return useQuery({
    queryKey: [...reportCardKeys.all, 'academic-years'] as const,
    queryFn: () => reportCardsApi.getAcademicYears(),
    staleTime: 5 * 60 * 1000,
    enabled: options?.enabled
  });
}

export function useHomeroomReportCardsQuery(
  params: MaybeRefOrGetter<Partial<HomeroomReportCardsParams>>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => {
      const p = toValue(params);
      return reportCardKeys.homeroom({
        studyGroupId: p.studyGroupId ?? '',
        operationalPeriodId: p.operationalPeriodId ?? ''
      });
    }),
    queryFn: () => {
      const p = toValue(params);
      return reportCardsApi.getHomeroom({
        studyGroupId: p.studyGroupId!,
        operationalPeriodId: p.operationalPeriodId!
      });
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      const p = toValue(params);
      return isEnabled && Boolean(p.studyGroupId && p.operationalPeriodId);
    })
  });
}

export function useReportCardsOverviewQuery(
  params: MaybeRefOrGetter<Partial<ReportCardsOverviewParams>>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => {
      const p = toValue(params);
      return reportCardKeys.overview({
        operationalPeriodId: p.operationalPeriodId ?? '',
        studyGroupId: p.studyGroupId || undefined
      });
    }),
    queryFn: () => {
      const p = toValue(params);
      return reportCardsApi.getOverview({
        operationalPeriodId: p.operationalPeriodId!,
        studyGroupId: p.studyGroupId || undefined
      });
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      const p = toValue(params);
      return isEnabled && Boolean(p.operationalPeriodId);
    })
  });
}

export function useReportCardDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => reportCardKeys.detail(toValue(id) ?? '')),
    queryFn: () => reportCardsApi.getById(toValue(id)!),
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled && Boolean(toValue(id));
    })
  });
}
