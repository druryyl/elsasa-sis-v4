import { z } from 'zod';

export const CHARGE_FREQUENCIES = ['MONTHLY', 'SEMESTER', 'YEARLY', 'ONE_TIME'] as const;
export type ChargeFrequency = (typeof CHARGE_FREQUENCIES)[number];

export const DISCOUNT_TYPES = ['PERCENTAGE', 'FIXED_AMOUNT'] as const;
export type DiscountType = (typeof DISCOUNT_TYPES)[number];

export const SCHOOL_LEVEL_OPTIONS = ['SD', 'SMP', 'SMA', 'SMK'] as const;

export const chargeTypeSchema = z.object({
  academicYearId: z.string().trim().min(1, 'Academic year is required.'),
  name: z
    .string()
    .trim()
    .min(1, 'Fee name is required.')
    .max(100, 'Fee name cannot exceed 100 characters.'),
  frequency: z.enum(CHARGE_FREQUENCIES, {
    errorMap: () => ({ message: 'Recurrence frequency is required.' })
  }),
  amount: z
    .number({ invalid_type_error: 'Standard amount must be a valid number.' })
    .min(0, 'Standard amount cannot be negative.'),
  schoolLevel: z.string().trim().optional().nullable(),
  grade: z.string().trim().max(20, 'Grade cannot exceed 20 characters.').optional().nullable(),
  studyGroupId: z.string().trim().optional().nullable()
});

export type ChargeTypeFormValues = z.infer<typeof chargeTypeSchema>;

export const grantDiscountSchema = z
  .object({
    studentId: z.string().trim().min(1, 'Please select a student.'),
    chargeTypeId: z.string().trim().min(1, 'Please select a fee structure.'),
    discountType: z.enum(DISCOUNT_TYPES, {
      errorMap: () => ({ message: 'Discount type is required.' })
    }),
    discountValue: z
      .number({ invalid_type_error: 'Discount value must be a valid number.' })
      .positive('Discount value must be greater than zero.'),
    reason: z
      .string()
      .trim()
      .min(1, 'Reason or scholarship description is required.')
      .max(500, 'Reason cannot exceed 500 characters.')
  })
  .superRefine((values, ctx) => {
    if (values.discountType === 'PERCENTAGE' && values.discountValue > 100) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['discountValue'],
        message: 'Percentage discount cannot exceed 100%.'
      });
    }
  });

export type GrantDiscountFormValues = z.infer<typeof grantDiscountSchema>;
