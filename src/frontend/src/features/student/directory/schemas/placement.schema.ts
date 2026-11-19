import { z } from 'zod';

export const placementSchema = z.object({
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
  studyGroupId: z
    .string({ required_error: 'Study group is required.' })
    .uuid('Study group is required.')
});

export type PlacementFormValues = z.infer<typeof placementSchema>;
