import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { parentPortalApi } from './parentPortal.api';

export const parentPortalKeys = {
  all: ['portal-parent'] as const,
  myChildren: () => [...parentPortalKeys.all, 'my-children'] as const,
  childReportCards: (studentId: string) =>
    [...parentPortalKeys.all, 'report-cards', studentId] as const,
  childStatement: (studentId: string) =>
    [...parentPortalKeys.all, 'statement', studentId] as const,
  reportCardDetail: (reportCardId: string) =>
    [...parentPortalKeys.all, 'report-card-detail', reportCardId] as const,
  receiptDetail: (receiptId: string) =>
    [...parentPortalKeys.all, 'receipt-detail', receiptId] as const,
  announcements: (params?: Record<string, unknown>) =>
    [...parentPortalKeys.all, 'announcements', params ?? {}] as const,
  calendar: (params?: Record<string, unknown>) =>
    [...parentPortalKeys.all, 'calendar', params ?? {}] as const
};

export function useMyChildrenQuery(options?: { enabled?: MaybeRefOrGetter<boolean> }) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.myChildren()),
    queryFn: () => parentPortalApi.getMyChildren(),
    enabled: options?.enabled
  });
}

export function useChildReportCardsQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.childReportCards(toValue(studentId) ?? '')),
    queryFn: () => parentPortalApi.getChildReportCards(toValue(studentId)!),
    enabled: computed(
      () => Boolean(toValue(studentId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}

export function useChildStatementQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.childStatement(toValue(studentId) ?? '')),
    queryFn: () => parentPortalApi.getChildStatement(toValue(studentId)!),
    enabled: computed(
      () => Boolean(toValue(studentId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}

export function useReportCardDetailQuery(
  reportCardId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.reportCardDetail(toValue(reportCardId) ?? '')),
    queryFn: () => parentPortalApi.getReportCardDetail(toValue(reportCardId)!),
    enabled: computed(
      () => Boolean(toValue(reportCardId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}

export function useReceiptDetailQuery(
  receiptId: MaybeRefOrGetter<string | null | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.receiptDetail(toValue(receiptId) ?? '')),
    queryFn: () => parentPortalApi.getPaymentReceiptDetail(toValue(receiptId)!),
    enabled: computed(
      () => Boolean(toValue(receiptId)) && (options?.enabled ? toValue(options.enabled) : true)
    )
  });
}

export function useParentAnnouncementsQuery(
  params?: MaybeRefOrGetter<Record<string, unknown> | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.announcements(toValue(params))),
    queryFn: () => parentPortalApi.getParentAnnouncements(toValue(params)),
    enabled: options?.enabled
  });
}

export function useParentCalendarQuery(
  params?: MaybeRefOrGetter<Record<string, unknown> | undefined>,
  options?: { enabled?: MaybeRefOrGetter<boolean> }
) {
  return useQuery({
    queryKey: computed(() => parentPortalKeys.calendar(toValue(params))),
    queryFn: () => parentPortalApi.getParentCalendar(toValue(params)),
    enabled: options?.enabled
  });
}
