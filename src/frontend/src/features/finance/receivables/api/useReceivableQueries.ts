import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import {
  receivableKeys,
  type DailyCashierParams,
  type ReceivablesAgingParams,
  type ReceivablesSummaryParams
} from './keys';
import { receivablesApi } from './receivables.api';

export function useReceivablesSummaryQuery(
  params: MaybeRefOrGetter<ReceivablesSummaryParams | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => receivableKeys.summary(toValue(params))),
    queryFn: () => receivablesApi.getReceivablesSummary(toValue(params)),
    enabled: options?.enabled
  });
}

export function useReceivablesAgingQuery(
  params: MaybeRefOrGetter<ReceivablesAgingParams | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => receivableKeys.aging(toValue(params))),
    queryFn: () => receivablesApi.getReceivablesAging(toValue(params)),
    enabled: options?.enabled
  });
}

export function useDailyCashierSummaryQuery(
  params: MaybeRefOrGetter<DailyCashierParams>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => receivableKeys.dailyCashier(toValue(params))),
    queryFn: () => receivablesApi.getDailyCashierSummary(toValue(params)),
    enabled: options?.enabled
  });
}

export function useStudentStatementQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => receivableKeys.statement(toValue(studentId) ?? '')),
    queryFn: () => {
      const id = toValue(studentId);
      if (!id) {
        throw new Error('Student ID is required to load statement.');
      }
      return receivablesApi.getStudentStatement(id);
    },
    enabled: computed(() => {
      const id = toValue(studentId);
      const optionEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(id) && optionEnabled;
    })
  });
}
