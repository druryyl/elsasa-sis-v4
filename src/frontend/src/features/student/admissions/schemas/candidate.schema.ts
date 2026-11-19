import { z } from 'zod';

export const candidateGuardianSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(1, 'Guardian full name is required')
    .max(150, 'Guardian full name must be 150 characters or fewer'),
  relationshipType: z.enum(['FATHER', 'MOTHER', 'GUARDIAN'], {
    errorMap: () => ({ message: 'Relationship type must be FATHER, MOTHER, or GUARDIAN' })
  }),
  phone: z
    .string()
    .trim()
    .max(30, 'Phone must be 30 characters or fewer')
    .optional()
    .nullable(),
  email: z
    .string()
    .trim()
    .max(150, 'Email must be 150 characters or fewer')
    .email('Invalid email address')
    .or(z.literal(''))
    .optional()
    .nullable(),
  address: z
    .string()
    .trim()
    .max(500, 'Address must be 500 characters or fewer')
    .optional()
    .nullable()
});

export const registerCandidateSchema = z
  .object({
    admissionYearId: z.string().trim().min(1, 'Admission year is required'),
    fullName: z
      .string()
      .trim()
      .min(1, 'Candidate full name is required')
      .max(150, 'Candidate full name must be 150 characters or fewer'),
    nickname: z
      .string()
      .trim()
      .max(50, 'Nickname must be 50 characters or fewer')
      .optional()
      .nullable(),
    dateOfBirth: z.string().trim().min(1, 'Date of birth is required'),
    gender: z.enum(['MALE', 'FEMALE'], {
      errorMap: () => ({ message: 'Gender must be MALE or FEMALE' })
    }),
    phone: z
      .string()
      .trim()
      .max(30, 'Phone must be 30 characters or fewer')
      .optional()
      .nullable(),
    email: z
      .string()
      .trim()
      .max(150, 'Email must be 150 characters or fewer')
      .email('Invalid email address')
      .or(z.literal(''))
      .optional()
      .nullable(),
    address: z
      .string()
      .trim()
      .max(500, 'Address must be 500 characters or fewer')
      .optional()
      .nullable(),
    targetSchoolLevel: z.enum(['SD', 'SMP', 'SMA', 'SMK'], {
      errorMap: () => ({ message: 'Target school level must be SD, SMP, SMA, or SMK' })
    }),
    targetGrade: z
      .string()
      .trim()
      .min(1, 'Target grade is required')
      .max(20, 'Target grade must be 20 characters or fewer'),
    guardians: z
      .array(candidateGuardianSchema)
      .min(1, 'At least one guardian contact is required') // BR-STU-007
  })
  .superRefine((data, ctx) => {
    if (data.dateOfBirth) {
      const dob = new Date(data.dateOfBirth);
      const today = new Date();
      // Set to midnight for date-only comparison
      today.setHours(23, 59, 59, 999);
      if (dob > today) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['dateOfBirth'],
          message: 'Candidate date of birth cannot be in the future'
        });
      }
    }
  });

export type CandidateGuardianFormValues = z.infer<typeof candidateGuardianSchema>;
export type RegisterCandidateFormValues = z.infer<typeof registerCandidateSchema>;
