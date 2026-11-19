import { z } from 'zod';

export const guardianSchema = z.object({
  fullName: z
    .string({ required_error: 'Guardian full name is required.' })
    .trim()
    .min(1, 'Guardian full name is required.')
    .max(150, 'Guardian full name must not exceed 150 characters.'),
  relationshipType: z.enum(['FATHER', 'MOTHER', 'GUARDIAN'], {
    errorMap: () => ({ message: 'Relationship type must be FATHER, MOTHER, or GUARDIAN.' })
  }),
  phone: z
    .string()
    .trim()
    .max(50, 'Phone must not exceed 50 characters.')
    .optional()
    .nullable(),
  email: z
    .string()
    .trim()
    .max(100, 'Email must not exceed 100 characters.')
    .email('Email is not a valid email address.')
    .or(z.literal(''))
    .optional()
    .nullable(),
  address: z
    .string()
    .trim()
    .max(500, 'Address must not exceed 500 characters.')
    .optional()
    .nullable()
});

export type GuardianFormValues = z.infer<typeof guardianSchema>;
