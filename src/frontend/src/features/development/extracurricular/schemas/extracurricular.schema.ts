import { z } from 'zod';

const DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/;

export const activityEditorSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Activity name is required.')
    .max(100, 'Activity name cannot exceed 100 characters.'),
  description: z
    .string()
    .trim()
    .max(500, 'Description cannot exceed 500 characters.')
    .optional()
    .or(z.literal('')),
  supervisorId: z.string().min(1, 'Supervisor is required.'),
  status: z.enum(['ACTIVE', 'INACTIVE']).default('ACTIVE')
});

export type ActivityEditorFormValues = z.infer<typeof activityEditorSchema>;

export const enrollmentSchema = z.object({
  studentId: z.string().min(1, 'Please select a student to enroll.'),
  joinedDate: z
    .string()
    .min(1, 'Joined date is required.')
    .regex(DATE_REGEX, 'Joined date must be in YYYY-MM-DD format.')
});

export type EnrollmentFormValues = z.infer<typeof enrollmentSchema>;

export const sessionLogSchema = z.object({
  sessionDate: z
    .string()
    .min(1, 'Session date is required.')
    .regex(DATE_REGEX, 'Session date must be in YYYY-MM-DD format.'),
  log: z
    .string()
    .trim()
    .min(1, 'Session log content cannot be empty.')
    .max(1000, 'Session log cannot exceed 1000 characters.')
});

export type SessionLogFormValues = z.infer<typeof sessionLogSchema>;
