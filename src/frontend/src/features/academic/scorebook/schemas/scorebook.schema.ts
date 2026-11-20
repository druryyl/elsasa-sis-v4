import { z } from 'zod';

export const createAssessmentSchema = z
  .object({
    subjectStageId: z
      .string({ required_error: 'Subject stage is required.' })
      .min(1, 'Subject stage is required.'),
    subjectId: z
      .string({ required_error: 'Subject is required.' })
      .min(1, 'Subject is required.'),
    studyGroupId: z
      .string({ required_error: 'Study group is required.' })
      .min(1, 'Study group is required.'),
    operationalPeriodId: z
      .string({ required_error: 'Operational period is required.' })
      .min(1, 'Operational period is required.'),
    learningUnitId: z.string().nullable().optional(),
    title: z
      .string({ required_error: 'Title is required.' })
      .trim()
      .min(1, 'Title is required.')
      .max(150, 'Title cannot exceed 150 characters.'),
    assignmentType: z.enum(['FORMATIVE', 'SUMMATIVE', 'EXAM'], {
      errorMap: () => ({ message: 'Assignment type must be FORMATIVE, SUMMATIVE, or EXAM.' })
    }),
    maxScore: z
      .number({ required_error: 'Max score is required.', invalid_type_error: 'Max score must be a number.' })
      .positive('Max score must be greater than 0.'),
    weight: z
      .number({ invalid_type_error: 'Weight must be a number.' })
      .min(0, 'Weight must be between 0.00 and 100.00.')
      .max(100, 'Weight must be between 0.00 and 100.00.')
      .nullable()
      .optional(),
    assignedDate: z
      .string({ required_error: 'Assigned date is required.' })
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Assigned date must be in YYYY-MM-DD format.'),
    dueDate: z
      .string()
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Due date must be in YYYY-MM-DD format.')
      .nullable()
      .optional()
      .or(z.literal(''))
  })
  .refine(
    (data) => {
      if (data.dueDate && data.assignedDate && data.dueDate.trim() !== '') {
        return data.dueDate >= data.assignedDate;
      }
      return true;
    },
    {
      message: 'Due date cannot be earlier than assigned date.',
      path: ['dueDate']
    }
  );

export type CreateAssessmentFormValues = z.infer<typeof createAssessmentSchema>;
