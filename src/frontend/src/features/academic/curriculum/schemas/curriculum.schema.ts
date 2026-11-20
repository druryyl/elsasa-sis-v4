import { z } from 'zod';

export const UNIT_TYPES = [
  'MODULE',
  'PACE',
  'LESSON',
  'PRESENTATION',
  'BOOK',
  'OTHER'
] as const;

export type UnitType = (typeof UNIT_TYPES)[number];

export const SCHOOL_LEVELS = ['SD', 'SMP', 'SMA', 'SMK'] as const;
export type SchoolLevel = (typeof SCHOOL_LEVELS)[number];

export const VALID_GRADES_BY_LEVEL: Record<SchoolLevel, number[]> = {
  SD: [1, 2, 3, 4, 5, 6],
  SMP: [7, 8, 9],
  SMA: [10, 11, 12],
  SMK: [10, 11, 12]
};

export const subjectSchema = z.object({
  name: z
    .string({ required_error: 'Subject name is required' })
    .trim()
    .min(1, 'Subject name is required')
    .max(100, 'Subject name cannot exceed 100 characters'),
  code: z
    .string({ required_error: 'Subject code is required' })
    .trim()
    .min(1, 'Subject code is required')
    .max(20, 'Subject code cannot exceed 20 characters')
});

export type SubjectFormValues = z.infer<typeof subjectSchema>;

export const subjectStageSchema = z.object({
  name: z
    .string({ required_error: 'Stage name is required' })
    .trim()
    .min(1, 'Stage name is required')
    .max(100, 'Stage name cannot exceed 100 characters'),
  sequenceOrder: z.coerce
    .number({ required_error: 'Sequence order is required' })
    .int('Sequence order must be an integer')
    .min(1, 'Sequence order must be at least 1'),
  description: z.string().trim().max(500, 'Description cannot exceed 500 characters').optional().nullable()
});

export type SubjectStageFormValues = z.infer<typeof subjectStageSchema>;

export const learningUnitSchema = z.object({
  name: z
    .string({ required_error: 'Unit name is required' })
    .trim()
    .min(1, 'Unit name is required')
    .max(150, 'Unit name cannot exceed 150 characters'),
  unitType: z.enum(UNIT_TYPES, {
    required_error: 'Unit type is required'
  }),
  sequenceOrder: z.coerce
    .number({ required_error: 'Sequence order is required' })
    .int('Sequence order must be an integer')
    .min(1, 'Sequence order must be at least 1'),
  description: z.string().trim().max(500, 'Description cannot exceed 500 characters').optional().nullable()
});

export type LearningUnitFormValues = z.infer<typeof learningUnitSchema>;

export const batchEnrollSchema = z.object({
  studyGroupId: z
    .string({ required_error: 'Study group is required' })
    .min(1, 'Study group is required'),
  operationalPeriodId: z
    .string({ required_error: 'Operational period is required' })
    .min(1, 'Operational period is required')
});

export type BatchEnrollFormValues = z.infer<typeof batchEnrollSchema>;
