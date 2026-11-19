import { z } from 'zod';

export const studyGroupSchema = z.object({
  academicYearId: z
    .string({ required_error: 'Academic year is required.' })
    .uuid('Academic year is required.'),
  schoolLevel: z.enum(['SD', 'SMP', 'SMA', 'SMK'], {
    errorMap: () => ({ message: 'School level must be SD, SMP, SMA, or SMK.' })
  }),
  grade: z
    .string({ required_error: 'Grade is required.' })
    .trim()
    .min(1, 'Grade is required.')
    .max(20, 'Grade must not exceed 20 characters.'),
  name: z
    .string({ required_error: 'Study group name is required.' })
    .trim()
    .min(1, 'Study group name is required.')
    .max(50, 'Study group name must not exceed 50 characters.'),
  homeroomTeacherId: z
    .string()
    .uuid('Invalid teacher ID.')
    .or(z.literal(''))
    .optional()
    .nullable()
});

export type StudyGroupFormValues = z.infer<typeof studyGroupSchema>;
