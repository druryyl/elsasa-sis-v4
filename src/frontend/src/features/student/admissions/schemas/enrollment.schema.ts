import { z } from 'zod';

export const enrollmentSchema = z.object({
  nis: z
    .string()
    .trim()
    .min(1, 'NIS is required')
    .max(30, 'NIS must be 30 characters or fewer'),
  nisn: z
    .string()
    .trim()
    .max(20, 'NISN must be 20 characters or fewer')
    .optional()
    .nullable(),
  enrollmentDate: z.string().trim().min(1, 'Enrollment date is required')
});

export type EnrollmentFormValues = z.infer<typeof enrollmentSchema>;
