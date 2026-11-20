import { z } from 'zod';
import { ApiError } from '@/shared/types/common';

export interface PastoralStudentTarget {
  id: string;
  fullName: string;
  nis: string;
  academicYearId?: string | null;
  academicYearName?: string | null;
  schoolLevel?: string | null;
  grade?: string | null;
  studyGroupName?: string | null;
}

export const behaviourNoteSchema = z.object({
  behaviourType: z.enum(['POSITIVE', 'NEGATIVE'], {
    required_error: 'Behaviour type is required.'
  }),
  observedDate: z.string().min(1, 'Observation date is required.'),
  description: z
    .string()
    .trim()
    .min(1, 'Description is required.')
    .max(1000, 'Description cannot exceed 1000 characters.')
});

export type BehaviourNoteFormValues = z.infer<typeof behaviourNoteSchema>;

export const disciplineIncidentSchema = z.object({
  incidentDate: z.string().min(1, 'Incident date is required.'),
  violationType: z
    .string()
    .trim()
    .min(1, 'Violation type is required.')
    .max(50, 'Violation type cannot exceed 50 characters.'),
  sanction: z.enum(['VERBAL_WARNING', 'WRITTEN_WARNING'], {
    required_error: 'Sanction is required.'
  }),
  description: z
    .string()
    .trim()
    .min(1, 'Description is required.')
    .max(1000, 'Description cannot exceed 1000 characters.')
});

export type DisciplineIncidentFormValues = z.infer<typeof disciplineIncidentSchema>;

export const escalatedSanctionSchema = z.object({
  incidentDate: z.string().min(1, 'Incident date is required.'),
  violationType: z
    .string()
    .trim()
    .min(1, 'Violation type is required.')
    .max(50, 'Violation type cannot exceed 50 characters.'),
  sanction: z.enum(['PARENT_MEETING', 'SUSPENSION'], {
    required_error: 'Escalated sanction is required.'
  }),
  description: z
    .string()
    .trim()
    .min(1, 'Description is required.')
    .max(1000, 'Description cannot exceed 1000 characters.')
});

export type EscalatedSanctionFormValues = z.infer<typeof escalatedSanctionSchema>;

export const achievementAwardSchema = z.object({
  eventName: z
    .string()
    .trim()
    .min(1, 'Event name is required.')
    .max(200, 'Event name cannot exceed 200 characters.'),
  achievementType: z.enum(['SPORTS', 'ARTS', 'SCIENCE', 'COMMUNITY', 'OTHER'], {
    required_error: 'Achievement type is required.'
  }),
  recognitionLevel: z.enum(['SCHOOL', 'CITY', 'PROVINCE', 'NATIONAL'], {
    required_error: 'Recognition level is required.'
  }),
  achievementDate: z.string().min(1, 'Achievement date is required.'),
  description: z
    .string()
    .trim()
    .max(500, 'Description cannot exceed 500 characters.')
    .optional()
    .or(z.literal(''))
});

export type AchievementAwardFormValues = z.infer<typeof achievementAwardSchema>;

export const BEHAVIOUR_TYPE_OPTIONS = [
  { label: 'Positive Observation', value: 'POSITIVE' as const },
  { label: 'Negative Observation', value: 'NEGATIVE' as const }
];

export const CLASSROOM_SANCTION_OPTIONS = [
  { label: 'Verbal Warning', value: 'VERBAL_WARNING' as const },
  { label: 'Written Warning', value: 'WRITTEN_WARNING' as const }
];

export const ESCALATED_SANCTION_OPTIONS = [
  { label: 'Parent Meeting', value: 'PARENT_MEETING' as const },
  { label: 'Suspension', value: 'SUSPENSION' as const }
];

export const COMMON_VIOLATION_TYPES = [
  'Late Arrival',
  'Uniform Violation',
  'Classroom Disruption',
  'Incomplete Homework',
  'Device Misuse',
  'Academic Dishonesty'
];

export const ACHIEVEMENT_TYPE_OPTIONS = [
  { label: 'Science & Academic', value: 'SCIENCE' as const },
  { label: 'Sports', value: 'SPORTS' as const },
  { label: 'Arts & Culture', value: 'ARTS' as const },
  { label: 'Community Service', value: 'COMMUNITY' as const },
  { label: 'Other', value: 'OTHER' as const }
];

export const RECOGNITION_LEVEL_OPTIONS = [
  { label: 'School Level', value: 'SCHOOL' as const },
  { label: 'City / Regency Level', value: 'CITY' as const },
  { label: 'Province Level', value: 'PROVINCE' as const },
  { label: 'National Level', value: 'NATIONAL' as const }
];

export function getTodayIsoDate(): string {
  const today = new Date();
  const y = today.getFullYear();
  const m = String(today.getMonth() + 1).padStart(2, '0');
  const d = String(today.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function extractSubmissionError(
  error: unknown,
  fallbackMessage: string
): { banner: string; fieldErrors: Record<string, string> } {
  if (error instanceof ApiError) {
    const fieldErrors: Record<string, string> = {};
    if (error.errors) {
      for (const [key, messages] of Object.entries(error.errors)) {
        const firstMessage = messages?.[0];
        if (firstMessage) {
          const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
          fieldErrors[camelKey] = firstMessage;
        }
      }
    }
    return {
      banner: error.detail || error.title || fallbackMessage,
      fieldErrors
    };
  }

  return {
    banner: error instanceof Error ? error.message || fallbackMessage : fallbackMessage,
    fieldErrors: {}
  };
}
