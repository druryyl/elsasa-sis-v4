import { describe, it, expect } from 'vitest';
import { createAssessmentSchema } from '../scorebook.schema';

describe('createAssessmentSchema', () => {
  const validBase = {
    subjectStageId: '01900000-0000-7000-8000-000000000101',
    subjectId: '01900000-0000-7000-8000-000000000100',
    studyGroupId: '01900000-0000-7000-8000-000000000201',
    operationalPeriodId: '01900000-0000-7000-8000-000000000301',
    title: 'Tugas 1 - Logika Matematika',
    assignmentType: 'FORMATIVE' as const,
    maxScore: 100,
    weight: 20,
    assignedDate: '2026-08-01',
    dueDate: '2026-08-08'
  };

  it('validates a correct payload successfully', () => {
    const result = createAssessmentSchema.safeParse(validBase);
    expect(result.success).toBe(true);
  });

  it('allows null or undefined optional weight and dueDate', () => {
    const result = createAssessmentSchema.safeParse({
      ...validBase,
      weight: null,
      dueDate: null
    });
    expect(result.success).toBe(true);
  });

  it('rejects empty title', () => {
    const result = createAssessmentSchema.safeParse({
      ...validBase,
      title: '   '
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toContain('Title is required');
    }
  });

  it('rejects invalid assignmentType', () => {
    const result = createAssessmentSchema.safeParse({
      ...validBase,
      assignmentType: 'HOMEWORK'
    });
    expect(result.success).toBe(false);
  });

  it('rejects non-positive maxScore', () => {
    const resultZero = createAssessmentSchema.safeParse({
      ...validBase,
      maxScore: 0
    });
    expect(resultZero.success).toBe(false);

    const resultNegative = createAssessmentSchema.safeParse({
      ...validBase,
      maxScore: -10
    });
    expect(resultNegative.success).toBe(false);
  });

  it('rejects out of range weight', () => {
    const resultTooHigh = createAssessmentSchema.safeParse({
      ...validBase,
      weight: 105
    });
    expect(resultTooHigh.success).toBe(false);

    const resultNegative = createAssessmentSchema.safeParse({
      ...validBase,
      weight: -5
    });
    expect(resultNegative.success).toBe(false);
  });

  it('rejects dueDate before assignedDate', () => {
    const result = createAssessmentSchema.safeParse({
      ...validBase,
      assignedDate: '2026-08-10',
      dueDate: '2026-08-05'
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.message.includes('earlier than assigned date'))).toBe(true);
    }
  });
});
