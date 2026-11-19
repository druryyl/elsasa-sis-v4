import { z } from 'zod';

export const CALENDAR_DAY_TYPES = ['SCHOOL_DAY', 'HOLIDAY', 'CLOSURE'] as const;
export type CalendarDayType = (typeof CALENDAR_DAY_TYPES)[number];

export const calendarEntrySchema = z.object({
  academicYearId: z
    .string()
    .min(1, 'Academic year is required'),
  entryDate: z
    .string()
    .min(1, 'Date is required')
    .regex(/^\d{4}-\d{2}-\d{2}$/, 'Date must be formatted as YYYY-MM-DD'),
  dayType: z.enum(CALENDAR_DAY_TYPES, {
    errorMap: () => ({ message: 'Day type must be SCHOOL_DAY, HOLIDAY, or CLOSURE' })
  }),
  description: z
    .string()
    .max(500, 'Description must be 500 characters or fewer')
    .nullable()
    .optional()
    .transform((val) => val?.trim() || null)
});

export type CalendarEntryFormValues = z.infer<typeof calendarEntrySchema>;
