import { describe, expect, it } from 'vitest';
import { addCapabilitySchema, assignTeacherSchema } from '../assignment.schema';

describe('assignTeacherSchema & addCapabilitySchema', () => {
  const validAssignment = {
    teacherId: '01900000-0000-7000-8000-000000000001',
    operationalPeriodId: '01900000-0000-7000-8000-000000000002',
    subjectId: '01900000-0000-7000-8000-000000000003',
    studyGroupId: '01900000-0000-7000-8000-000000000004'
  };

  it('validates a correct assignment payload', () => {
    const result = assignTeacherSchema.safeParse(validAssignment);
    expect(result.success).toBe(true);
  });

  it('rejects invalid uuid in assignment', () => {
    const result = assignTeacherSchema.safeParse({
      ...validAssignment,
      teacherId: 'not-a-uuid'
    });
    expect(result.success).toBe(false);
  });

  it('rejects missing studyGroupId', () => {
    const result = assignTeacherSchema.safeParse({
      ...validAssignment,
      studyGroupId: ''
    });
    expect(result.success).toBe(false);
  });

  it('validates addCapabilitySchema', () => {
    const result = addCapabilitySchema.safeParse({
      subjectId: '01900000-0000-7000-8000-000000000003'
    });
    expect(result.success).toBe(true);
  });

  it('rejects non-uuid in addCapabilitySchema', () => {
    const result = addCapabilitySchema.safeParse({
      subjectId: '123'
    });
    expect(result.success).toBe(false);
  });
});
