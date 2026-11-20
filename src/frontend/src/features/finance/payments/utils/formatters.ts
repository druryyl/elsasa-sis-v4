import type { StatusIntent } from '@/shared/components/feedback/StatusPill.vue';

export {
  formatIdr,
  formatBillingMonth,
  formatPaymentMethod,
  buildWhatsAppHref
} from '@/shared/utils/formatters';

const CHARGE_STATUS_INTENTS: Record<string, StatusIntent> = {
  UNPAID: 'info',
  PARTIALLY_PAID: 'warning',
  PAID: 'success',
  CANCELLED: 'neutral'
};

export function getChargeStatusIntent(status: string | null | undefined): StatusIntent {
  if (!status) return 'neutral';
  return CHARGE_STATUS_INTENTS[status.trim().toUpperCase()] ?? 'neutral';
}

