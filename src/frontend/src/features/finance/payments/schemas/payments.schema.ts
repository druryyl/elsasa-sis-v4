import { z } from 'zod';
import { ApiError } from '@/shared/types/common';

export const PAYMENT_METHODS = ['CASH', 'BANK_TRANSFER'] as const;
export type PaymentMethod = (typeof PAYMENT_METHODS)[number];

export const PAYMENT_METHOD_OPTIONS: { label: string; value: PaymentMethod }[] = [
  { label: 'Cash', value: 'CASH' },
  { label: 'Bank Transfer', value: 'BANK_TRANSFER' }
];

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export const recordPaymentSchema = z.object({
  paymentDate: z
    .string()
    .trim()
    .regex(DATE_PATTERN, 'Payment date must be in YYYY-MM-DD format.'),
  paymentMethod: z.enum(PAYMENT_METHODS, {
    message: 'Payment method must be Cash or Bank Transfer.'
  }),
  bankReference: z
    .string()
    .trim()
    .max(120, 'Bank reference must be at most 120 characters.')
    .optional()
    .or(z.literal('')),
  totalAmount: z
    .number({ message: 'Payment amount is required.' })
    .finite('Payment amount must be a valid number.')
    .gt(0, 'Payment amount must be greater than zero.'),
  notes: z
    .string()
    .trim()
    .max(500, 'Notes must be at most 500 characters.')
    .optional()
    .or(z.literal(''))
});

export type RecordPaymentFormValues = z.infer<typeof recordPaymentSchema>;

export const allocateDepositSchema = z.object({
  sourceReceiptId: z.string().trim().min(1, 'Please select a source receipt holding deposit.'),
  allocationDate: z
    .string()
    .trim()
    .regex(DATE_PATTERN, 'Allocation date must be in YYYY-MM-DD format.')
});

export type AllocateDepositFormValues = z.infer<typeof allocateDepositSchema>;

export function extractApiFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.errors) {
    return {};
  }

  const result: Record<string, string> = {};
  for (const [key, messages] of Object.entries(error.errors)) {
    if (!Array.isArray(messages) || messages.length === 0) continue;
    const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
    result[normalizedKey] = messages[0] ?? '';
  }
  return result;
}

export function extractApiErrorMessage(
  error: unknown,
  fallback: string
): { summary: string; detail: string } {
  if (error instanceof ApiError) {
    return {
      summary: error.title || fallback,
      detail: error.detail || fallback
    };
  }
  if (error instanceof Error) {
    return {
      summary: fallback,
      detail: error.message || fallback
    };
  }
  return { summary: fallback, detail: fallback };
}
