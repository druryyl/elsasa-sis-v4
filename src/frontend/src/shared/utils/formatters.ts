const IDR_FORMATTER = new Intl.NumberFormat('id-ID', {
  style: 'currency',
  currency: 'IDR',
  maximumFractionDigits: 0
});

const MONTH_LABEL_FORMATTER = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  year: 'numeric'
});

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

export function buildWhatsAppHref(
  whatsAppUrl: string | null | undefined,
  phoneNumber: string | null | undefined
): string | null {
  const candidate = whatsAppUrl?.trim();
  if (candidate && /^https?:\/\//i.test(candidate)) {
    return candidate;
  }

  const rawPhone = phoneNumber?.trim();
  if (!rawPhone) return null;

  const digits = rawPhone.replace(/\D/g, '');
  if (!digits) return null;

  const normalized = digits.startsWith('0')
    ? `62${digits.slice(1)}`
    : digits.startsWith('62')
      ? digits
      : `62${digits}`;

  return `https://wa.me/${normalized}`;
}
