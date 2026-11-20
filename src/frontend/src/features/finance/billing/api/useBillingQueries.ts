import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import {
  billingKeys,
  type BillingChargesParams,
  type BillingStudentSearchParams
} from './keys';
import { billingApi } from './billing.api';

export function useStudentChargesQuery(
  params: MaybeRefOrGetter<BillingChargesParams | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => billingKeys.chargesList(toValue(params))),
    queryFn: () => billingApi.listCharges(toValue(params)),
    enabled: options?.enabled
  });
}

export function useActiveChargeTypesQuery(
  academicYearId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => billingKeys.chargeTypes(toValue(academicYearId) || undefined)),
    queryFn: () => billingApi.listActiveChargeTypes(toValue(academicYearId) || undefined),
    enabled: options?.enabled
  });
}

export function useBillingStudentSearchQuery(
  params: MaybeRefOrGetter<BillingStudentSearchParams | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => billingKeys.studentSearch(toValue(params))),
    queryFn: () => billingApi.searchStudents(toValue(params)),
    enabled: options?.enabled
  });
}
