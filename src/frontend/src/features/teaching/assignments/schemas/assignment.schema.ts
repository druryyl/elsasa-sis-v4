import { z } from 'zod';

export const assignTeacherSchema = z.object({
  teacherId: z
    .string({ required_error: 'Please select a teacher.' })
    .uuid('Please select a valid teacher.'),
  operationalPeriodId: z
    .string({ required_error: 'Please select an operational period.' })
    .uuid('Please select a valid operational period.'),
  subjectId: z
    .string({ required_error: 'Please select a subject.' })
    .uuid('Please select a valid subject.'),
  studyGroupId: z
    .string({ required_error: 'Please select a study group.' })
    .uuid('Please select a valid study group.')
});

export type AssignTeacherFormValues = z.infer<typeof assignTeacherSchema>;

export const addCapabilitySchema = z.object({
  subjectId: z
    .string({ required_error: 'Please select a subject.' })
    .uuid('Please select a valid subject.')
});

export type AddCapabilityFormValues = z.infer<typeof addCapabilitySchema>;
