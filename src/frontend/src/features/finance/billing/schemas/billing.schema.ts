import { z } from 'zod';
import { ApiError } from '@/shared/types/common';

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

export const CHARGE_STATUS_OPTIONS = [
  { label: 'Unpaid', value: 'UNPAID' },
  { label: 'Partially Paid', value: 'PARTIALLY_PAID' },
  { label: 'Paid', value: 'PAID' },
  { label: 'Cancelled', value: 'CANCELLED' }
] as const;

const BILLING_MONTH_REGEX = /^\d{4}-(0[1-9]|1[0-2])$/;

export const batchBillingSchema = z.object({
  academicYearId: z.string().trim().min(1, 'Academic year is required.'),
  billingMonth: z
    .string()
    .trim()
    .min(1, 'Billing month is required.')
    .regex(BILLING_MONTH_REGEX, 'Billing month must be in YYYY-MM format.'),
  schoolLevel: z.string().nullable().optional(),
  grade: z.string().nullable().optional(),
  studyGroupId: z.string().nullable().optional(),
  dueDate: z.string().nullable().optional()
});

export type BatchBillingFormValues = z.infer<typeof batchBillingSchema>;

export const adHocChargeSchema = z.object({
  academicYearId: z.string().trim().min(1, 'Academic year is required.'),
  studentId: z.string().trim().min(1, 'Student is required.'),
  chargeTypeId: z.string().trim().min(1, 'Charge type is required.'),
  description: z
    .string()
    .trim()
    .min(1, 'Description is required.')
    .max(500, 'Description cannot exceed 500 characters.'),
  unitAmount: z
    .number({ invalid_type_error: 'Unit amount must be a number.' })
    .min(0, 'Unit amount cannot be negative.'),
  quantity: z
    .number({ invalid_type_error: 'Quantity must be a whole number.' })
    .int('Quantity must be a whole number.')
    .min(1, 'Quantity must be greater than zero.'),
  billingMonth: z
    .string()
    .trim()
    .refine((val) => val === '' || BILLING_MONTH_REGEX.test(val), {
      message: 'Billing month must be in YYYY-MM format.'
    })
    .nullable()
    .optional(),
  dueDate: z.string().nullable().optional()
});

export type AdHocChargeFormValues = z.infer<typeof adHocChargeSchema>;

export const cancelChargeSchema = z.object({
  cancellationReason: z
    .string()
    .trim()
    .min(1, 'Cancellation reason is required.')
    .max(500, 'Cancellation reason cannot exceed 500 characters.')
});

export type CancelChargeFormValues = z.infer<typeof cancelChargeSchema>;

export function getCurrentBillingMonth(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, '0');
  return `${y}-${m}`;
}

export function extractSubmissionError(
  error: unknown,
  fallbackMessage: string
): { banner: string; fieldErrors: Record<string, string> } {
  if (error instanceof ApiError) {
    const fieldErrors: Record<string, string> = {};
    if (error.errors) {
      for (const [key, messages] of Object.entries(error.errors)) {
        const firstMessage = messages?.[0];
        if (firstMessage) {
          const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
          fieldErrors[camelKey] = firstMessage;
        }
      }
    }
    return {
      banner: error.detail || error.title || fallbackMessage,
      fieldErrors
    };
  }

  return {
    banner: error instanceof Error ? error.message || fallbackMessage : fallbackMessage,
    fieldErrors: {}
  };
}
