import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { paymentKeys } from './keys';
import { paymentsApi } from './payments.api';

export function useBillingLookupQuery(
  search: MaybeRefOrGetter<string>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() =>
      paymentKeys.billingLookup({ search: toValue(search).trim() || undefined })
    ),
    queryFn: () => paymentsApi.searchBillingContacts(toValue(search)),
    enabled: options?.enabled
  });
}

export function useStudentStatementQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => paymentKeys.statement(toValue(studentId) ?? '')),
    queryFn: () => paymentsApi.getStudentStatement(toValue(studentId)!),
    enabled: computed(
      () => Boolean(toValue(studentId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}

export function usePaymentReceiptDetailQuery(
  receiptId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => paymentKeys.receiptDetail(toValue(receiptId) ?? '')),
    queryFn: () => paymentsApi.getPaymentReceiptDetail(toValue(receiptId)!),
    enabled: computed(
      () => Boolean(toValue(receiptId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}
