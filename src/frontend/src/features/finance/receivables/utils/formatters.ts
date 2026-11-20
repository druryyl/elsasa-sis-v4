import type { StatusIntent } from '@/shared/components/feedback/StatusPill.vue';

export const SCHOOL_LEVEL_OPTIONS = [
  { label: 'SD (Elementary)', value: 'SD' },
  { label: 'SMP (Junior High)', value: 'SMP' },
  { label: 'SMA (Senior High)', value: 'SMA' },
  { label: 'SMK (Vocational)', value: 'SMK' }
] as const;

export const GRADES_BY_SCHOOL_LEVEL: Record<string, string[]> = {
  SD: ['1', '2', '3', '4', '5', '6'],
  SMP: ['7', '8', '9'],
  SMA: ['10', '11', '12'],
  SMK: ['10', '11', '12']
};

export const ALL_GRADES = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12'];

const IDR_FORMATTER = new Intl.NumberFormat('id-ID', {
  style: 'currency',
  currency: 'IDR',
  maximumFractionDigits: 0
});

const MONTH_LABEL_FORMATTER = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  year: 'numeric'
});

const CHARGE_STATUS_INTENTS: Record<string, StatusIntent> = {
  UNPAID: 'info',
  PARTIALLY_PAID: 'warning',
  PAID: 'success',
  CANCELLED: 'neutral'
};

export function formatIdr(amount: number | null | undefined): string {
  const safe = typeof amount === 'number' && Number.isFinite(amount) ? amount : 0;
  return IDR_FORMATTER.format(safe);
}

export function formatBillingMonth(month: string | null | undefined): string {
  if (!month) return '—';
  const match = /^(\d{4})-(0[1-9]|1[0-2])$/.exec(month.trim());
  if (!match) return month;
  const year = Number(match[1]);
  const monthIdx = Number(match[2]) - 1;
  return MONTH_LABEL_FORMATTER.format(new Date(year, monthIdx, 1));
}

export function formatPaymentMethod(method: string | null | undefined): string {
  if (!method) return '—';
  switch (method.trim().toUpperCase()) {
    case 'CASH':
      return 'Cash';
    case 'BANK_TRANSFER':
      return 'Bank Transfer';
    default:
      return method;
  }
}

export function getChargeStatusIntent(status: string | null | undefined): StatusIntent {
  if (!status) return 'neutral';
  return CHARGE_STATUS_INTENTS[status.trim().toUpperCase()] ?? 'neutral';
}

export function computeCollectionRate(
  totalCollected: number | null | undefined,
  totalBilled: number | null | undefined
): string {
  const billed = typeof totalBilled === 'number' && Number.isFinite(totalBilled) ? totalBilled : 0;
  const collected =
    typeof totalCollected === 'number' && Number.isFinite(totalCollected) ? totalCollected : 0;
  if (billed <= 0) return '0%';
  const pct = Math.round((collected / billed) * 100);
  return `${pct}%`;
}
