import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { feePolicyApi } from './feePolicy.api';
import {
  feePolicyKeys,
  type ChargeTypeListParams,
  type DiscountStudentSearchParams,
  type StudentDiscountsParams
} from './keys';

export function useChargeTypesQuery(
  params: MaybeRefOrGetter<ChargeTypeListParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => feePolicyKeys.chargeTypeList(toValue(params))),
    queryFn: () => feePolicyApi.listChargeTypes(toValue(params)),
    enabled: options?.enabled
  });
}

export function useStudentDiscountsQuery(
  params: MaybeRefOrGetter<StudentDiscountsParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => feePolicyKeys.discountList(toValue(params))),
    queryFn: () => feePolicyApi.listStudentDiscounts(toValue(params)),
    enabled: options?.enabled
  });
}

export function useDiscountStudentSearchQuery(
  params: MaybeRefOrGetter<DiscountStudentSearchParams> = {},
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => feePolicyKeys.studentSearch(toValue(params))),
    queryFn: () => feePolicyApi.searchStudents(toValue(params)),
    enabled: options?.enabled
  });
}
