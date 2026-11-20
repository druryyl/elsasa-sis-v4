import { z } from 'zod';

export const SCHEDULE_TYPES = ['REGULAR', 'GENERAL', 'VOCATIONAL'] as const;
export type ScheduleType = (typeof SCHEDULE_TYPES)[number];

export const SMK_BLOCK_SCHEDULE_TYPES = ['GENERAL', 'VOCATIONAL'] as const;
export type SmkBlockScheduleType = (typeof SMK_BLOCK_SCHEDULE_TYPES)[number];

export const slotFormSchema = z
  .object({
    operationalPeriodId: z.string().min(1, 'Operational period is required'),
    scheduleType: z.enum(SCHEDULE_TYPES, {
      errorMap: () => ({ message: 'Schedule type must be REGULAR, GENERAL, or VOCATIONAL' })
    }),
    dayOfWeek: z.coerce
      .number({ invalid_type_error: 'Day of week is required' })
      .int()
      .min(1, 'Day must be between 1 and 7')
      .max(7, 'Day must be between 1 and 7'),
    periodSlot: z.coerce
      .number({ invalid_type_error: 'Period slot is required' })
      .int()
      .min(1, 'Period slot must be between 1 and 10')
      .max(10, 'Period slot must be between 1 and 10'),
    startTime: z
      .string()
      .trim()
      .min(1, 'Start time is required')
      .regex(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/, 'Format must be HH:mm or HH:mm:ss'),
    endTime: z
      .string()
      .trim()
      .min(1, 'End time is required')
      .regex(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/, 'Format must be HH:mm or HH:mm:ss'),
    subjectId: z.string().min(1, 'Subject is required'),
    teacherId: z.string().min(1, 'Teacher is required'),
    roomId: z.string().nullable().optional()
  })
  .refine((data) => data.endTime > data.startTime, {
    message: 'End time must be strictly after start time',
    path: ['endTime']
  });

export type SlotFormValues = z.infer<typeof slotFormSchema>;

export const smkBlockSchema = z
  .object({
    operationalPeriodId: z.string().min(1, 'Operational period is required'),
    scheduleType: z.enum(SMK_BLOCK_SCHEDULE_TYPES, {
      errorMap: () => ({ message: 'Block schedule type must be GENERAL or VOCATIONAL' })
    }),
    startDate: z
      .string()
      .trim()
      .min(1, 'Start date is required')
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'Start date must be YYYY-MM-DD'),
    endDate: z
      .string()
      .trim()
      .min(1, 'End date is required')
      .regex(/^\d{4}-\d{2}-\d{2}$/, 'End date must be YYYY-MM-DD')
  })
  .refine((data) => data.endDate >= data.startDate, {
    message: 'End date must be on or after start date',
    path: ['endDate']
  });

export type SmkBlockFormValues = z.infer<typeof smkBlockSchema>;
