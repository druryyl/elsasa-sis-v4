import { z } from 'zod';

export const operationalPeriodSchema = z.object({
  id: z.string().nullable().optional(),
  name: z
    .string()
    .trim()
    .min(1, 'Period name is required')
    .max(100, 'Period name must be 100 characters or fewer'),
  startDate: z.string().min(1, 'Period start date is required'),
  endDate: z.string().min(1, 'Period end date is required')
});

export const academicYearSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, 'Academic year name is required')
      .max(100, 'Academic year name must be 100 characters or fewer'),
    startDate: z.string().min(1, 'Start date is required'),
    endDate: z.string().min(1, 'End date is required'),
    periods: z
      .array(operationalPeriodSchema)
      .min(1, 'At least one operational period is required')
  })
  .superRefine((data, ctx) => {
    // BR-OPS-005: Academic Year endDate > startDate
    if (data.startDate && data.endDate && data.endDate <= data.startDate) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['endDate'],
        message: 'End date must be after start date'
      });
    }

    const seenNames = new Map<string, number>();

    data.periods.forEach((period, index) => {
      // Unique period names (case-insensitive)
      const normalizedName = period.name.trim().toLowerCase();
      if (normalizedName) {
        if (seenNames.has(normalizedName)) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ['periods', index, 'name'],
            message: 'Period names must be unique within an academic year'
          });
        } else {
          seenNames.set(normalizedName, index);
        }
      }

      // BR-OPS-006: Period endDate > startDate
      if (period.startDate && period.endDate && period.endDate <= period.startDate) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['periods', index, 'endDate'],
          message: 'Period end date must be after start date'
        });
      }

      // BR-OPS-003: Period dates must fall within Academic Year bounds
      if (data.startDate && period.startDate && period.startDate < data.startDate) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['periods', index, 'startDate'],
          message: 'Period start date must be within academic year bounds'
        });
      }

      if (data.endDate && period.endDate && period.endDate > data.endDate) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['periods', index, 'endDate'],
          message: 'Period end date must be within academic year bounds'
        });
      }
    });

    // BR-OPS-004: Operational Periods must not overlap
    const indexedPeriods = data.periods
      .map((period, index) => ({ ...period, index }))
      .filter((period) => period.startDate && period.endDate && period.endDate > period.startDate)
      .sort((a, b) => a.startDate.localeCompare(b.startDate));

    for (let i = 0; i < indexedPeriods.length - 1; i++) {
      const current = indexedPeriods[i];
      const next = indexedPeriods[i + 1];
      if (current && next && next.startDate <= current.endDate) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['periods', next.index, 'startDate'],
          message: 'Operational periods must not overlap'
        });
      }
    }
  });

export type OperationalPeriodFormValues = z.infer<typeof operationalPeriodSchema>;
export type AcademicYearFormValues = z.infer<typeof academicYearSchema>;
