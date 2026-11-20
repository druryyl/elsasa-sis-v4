import type { StatusIntent } from '@/shared/components/feedback/StatusPill.vue';

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

export function getChargeStatusIntent(status: string | null | undefined): StatusIntent {
  if (!status) return 'neutral';
  return CHARGE_STATUS_INTENTS[status.trim().toUpperCase()] ?? 'neutral';
}

export function formatFrequencyLabel(frequency: string | null | undefined): string {
  if (!frequency) return '—';
  switch (frequency.trim().toUpperCase()) {
    case 'MONTHLY':
      return 'Monthly';
    case 'SEMESTER':
      return 'Semester';
    case 'YEARLY':
      return 'Yearly';
    case 'ONE_TIME':
      return 'One-Time';
    default:
      return frequency;
  }
}
