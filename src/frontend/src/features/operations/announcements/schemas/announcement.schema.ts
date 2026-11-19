import { z } from 'zod';

export const ANNOUNCEMENT_AUDIENCES = ['ALL', 'TEACHERS', 'STUDENTS', 'PARENTS'] as const;
export type AnnouncementAudience = (typeof ANNOUNCEMENT_AUDIENCES)[number];

export const AUDIENCE_OPTIONS: { label: string; value: AnnouncementAudience }[] = [
  { label: 'All (School-Wide)', value: 'ALL' },
  { label: 'Teachers & Staff', value: 'TEACHERS' },
  { label: 'Students', value: 'STUDENTS' },
  { label: 'Parents & Guardians', value: 'PARENTS' }
];

export const announcementSchema = z.object({
  title: z
    .string({ required_error: 'Title is required' })
    .trim()
    .min(1, 'Title is required')
    .max(255, 'Title must not exceed 255 characters'),
  targetAudience: z.enum(ANNOUNCEMENT_AUDIENCES, {
    errorMap: () => ({ message: 'Please select a target audience' })
  }),
  content: z.string({ required_error: 'Content is required' }).trim().min(1, 'Content is required'),
  academicYearId: z.string().uuid('Invalid academic year ID').nullable().optional()
});

export type AnnouncementFormValues = z.infer<typeof announcementSchema>;
