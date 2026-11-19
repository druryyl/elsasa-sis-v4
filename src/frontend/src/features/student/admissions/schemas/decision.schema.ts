import { z } from 'zod';

export const decisionSchema = z.object({
  decision: z.enum(['ACCEPTED', 'REJECTED'], {
    errorMap: () => ({ message: 'Decision must be ACCEPTED or REJECTED' })
  }),
  decisionNotes: z
    .string()
    .trim()
    .max(500, 'Decision notes must be 500 characters or fewer')
    .optional()
    .nullable()
});

export type DecisionFormValues = z.infer<typeof decisionSchema>;
