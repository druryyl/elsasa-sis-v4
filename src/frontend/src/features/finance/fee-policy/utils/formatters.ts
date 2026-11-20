import type { ChargeTypeDto } from '../api/feePolicy.api';

const IDR_FORMATTER = new Intl.NumberFormat('id-ID', {
  style: 'currency',
  currency: 'IDR',
  maximumFractionDigits: 0
});

const FREQUENCY_LABELS: Record<string, string> = {
  MONTHLY: 'Monthly',
  SEMESTER: 'Per Semester',
  YEARLY: 'Yearly',
  ONE_TIME: 'One-Time'
};

export function formatIdr(amount: number | null | undefined): string {
  const safe = typeof amount === 'number' && Number.isFinite(amount) ? amount : 0;
  return IDR_FORMATTER.format(safe);
}

export function formatFrequencyLabel(frequency: string | null | undefined): string {
  if (!frequency) return '—';
  const normalized = frequency.trim().toUpperCase();
  return FREQUENCY_LABELS[normalized] ?? frequency;
}

export function formatCohortScope(
  chargeType: Pick<ChargeTypeDto, 'schoolLevel' | 'grade' | 'studyGroupName'>
): string {
  const parts: string[] = [];
  if (chargeType.schoolLevel) {
    parts.push(chargeType.schoolLevel);
  }
  if (chargeType.grade) {
    parts.push(`Grade ${chargeType.grade}`);
  }
  if (chargeType.studyGroupName) {
    parts.push(`Class ${chargeType.studyGroupName}`);
  }
  return parts.length > 0 ? parts.join(' • ') : 'All Students';
}

export function formatDiscountValue(
  discountType: string | null | undefined,
  discountValue: number | null | undefined
): string {
  const value =
    typeof discountValue === 'number' && Number.isFinite(discountValue) ? discountValue : 0;
  if (discountType?.trim().toUpperCase() === 'PERCENTAGE') {
    return `${value}%`;
  }
  return formatIdr(value);
}
