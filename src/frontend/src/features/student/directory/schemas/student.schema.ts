import { z } from 'zod';

export const studentSchema = z.object({
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
    .nullable(),
  dateOfBirth: z
    .string({ required_error: 'Date of birth is required.' })
    .min(1, 'Date of birth is required.')
    .refine(
      (val) => {
        if (!val) return false;
        const d = new Date(val);
        const today = new Date();
        today.setHours(23, 59, 59, 999);
        return !isNaN(d.getTime()) && d <= today;
      },
      { message: 'Date of birth must be a valid date not in the future.' }
    ),
  gender: z.enum(['MALE', 'FEMALE'], {
    errorMap: () => ({ message: 'Gender must be MALE or FEMALE.' })
  }),
  nisn: z
    .string()
    .trim()
    .max(20, 'National student number (NISN) must not exceed 20 characters.')
    .optional()
    .nullable(),
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

export type StudentFormValues = z.infer<typeof studentSchema>;
