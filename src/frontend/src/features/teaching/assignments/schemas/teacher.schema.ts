import { z } from 'zod';

export const GENDER_OPTIONS = [
  { label: 'Male', value: 'MALE' as const },
  { label: 'Female', value: 'FEMALE' as const }
];

export type GenderType = (typeof GENDER_OPTIONS)[number]['value'];

export const teacherSchema = z
  .object({
    fullName: z
      .string({ required_error: 'Full name is required.' })
      .trim()
      .min(1, 'Full name is required.')
      .max(150, 'Full name must not exceed 150 characters.'),
    nickname: z
      .string()
      .trim()
      .max(50, 'Nickname must not exceed 50 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    nip: z
      .string()
      .trim()
      .max(30, 'NIP must not exceed 30 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    nik: z
      .string()
      .trim()
      .max(30, 'NIK must not exceed 30 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    dateOfBirth: z
      .string({ required_error: 'Date of birth is required.' })
      .trim()
      .min(1, 'Date of birth is required.')
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Date of birth must be in YYYY-MM-DD format.'),
    gender: z.enum(['MALE', 'FEMALE'], {
      errorMap: () => ({ message: 'Teacher gender must be MALE or FEMALE.' })
    }),
    phone: z
      .string()
      .trim()
      .max(30, 'Phone must not exceed 30 characters.')
      .optional()
      .nullable()
      .or(z.literal('')),
    email: z
      .string()
      .trim()
      .max(150, 'Email must not exceed 150 characters.')
      .optional()
      .nullable()
      .or(z.literal(''))
      .refine(
        (val) => !val || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val),
        { message: 'Please enter a valid email address.' }
      ),
    address: z
      .string()
      .trim()
      .max(500, 'Address must not exceed 500 characters.')
      .optional()
      .nullable()
      .or(z.literal(''))
  })
  .superRefine((data, ctx) => {
    if (data.dateOfBirth) {
      const date = new Date(data.dateOfBirth);
      const today = new Date();
      today.setHours(23, 59, 59, 999);
      if (date > today) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['dateOfBirth'],
          message: 'Teacher date of birth cannot be in the future.'
        });
      }
    }

    const hasPhone = Boolean(data.phone && data.phone.trim().length > 0);
    const hasEmail = Boolean(data.email && data.email.trim().length > 0);
    if (!hasPhone && !hasEmail) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['phone'],
        message: 'At least one contact method (phone or email) is required.'
      });
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['email'],
        message: 'At least one contact method (phone or email) is required.'
      });
    }
  });

export type TeacherFormValues = z.infer<typeof teacherSchema>;
