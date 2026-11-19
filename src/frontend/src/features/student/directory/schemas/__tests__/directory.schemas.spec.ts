import { describe, expect, it } from 'vitest';
import { studentSchema } from '../student.schema';
import { guardianSchema } from '../guardian.schema';
import { studyGroupSchema } from '../studyGroup.schema';
import { placementSchema } from '../placement.schema';

describe('student.schema', () => {
  it('validates a correct student update payload', () => {
    const valid = {
      fullName: 'Ahmad Dahlan',
      nickname: 'Ahmad',
      dateOfBirth: '2010-05-15',
      gender: 'MALE' as const,
      nisn: '0012345678',
      phone: '081234567890',
      email: 'ahmad@example.com',
      address: 'Jl. Merdeka No. 10'
    };

    const result = studentSchema.safeParse(valid);
    expect(result.success).toBe(true);
  });

  it('fails when full name or date of birth or gender is missing', () => {
    const invalid = {
      fullName: '',
      dateOfBirth: '',
      gender: 'OTHER'
    };

    const result = studentSchema.safeParse(invalid);
    expect(result.success).toBe(false);
  });

  it('rejects a future date of birth', () => {
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 2);
    const dateStr = futureDate.toISOString().split('T')[0];

    const result = studentSchema.safeParse({
      fullName: 'Budi Santoso',
      dateOfBirth: dateStr,
      gender: 'MALE'
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.dateOfBirth).toBeDefined();
    }
  });
});

describe('guardian.schema', () => {
  it('validates a correct guardian payload', () => {
    const valid = {
      fullName: 'Siti Rahma',
      relationshipType: 'MOTHER' as const,
      phone: '081298765432',
      email: 'siti@example.com',
      address: 'Jl. Melati No. 4'
    };

    const result = guardianSchema.safeParse(valid);
    expect(result.success).toBe(true);
  });

  it('rejects invalid relationship type', () => {
    const invalid = {
      fullName: 'Uncle Bob',
      relationshipType: 'UNCLE'
    };

    const result = guardianSchema.safeParse(invalid);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.relationshipType).toBeDefined();
    }
  });
});

describe('studyGroup.schema', () => {
  it('validates a correct study group payload', () => {
    const valid = {
      academicYearId: '11111111-1111-1111-1111-111111111111',
      schoolLevel: 'SMP' as const,
      grade: '7',
      name: '7A',
      homeroomTeacherId: '22222222-2222-2222-2222-222222222222'
    };

    const result = studyGroupSchema.safeParse(valid);
    expect(result.success).toBe(true);
  });

  it('accepts study group with null or empty homeroom teacher', () => {
    const valid = {
      academicYearId: '11111111-1111-1111-1111-111111111111',
      schoolLevel: 'SMP' as const,
      grade: '7',
      name: '7B',
      homeroomTeacherId: ''
    };

    const result = studyGroupSchema.safeParse(valid);
    expect(result.success).toBe(true);
  });

  it('rejects invalid school level', () => {
    const invalid = {
      academicYearId: '11111111-1111-1111-1111-111111111111',
      schoolLevel: 'COLLEGE',
      grade: '1',
      name: 'Class 1'
    };

    const result = studyGroupSchema.safeParse(invalid);
    expect(result.success).toBe(false);
  });
});

describe('placement.schema', () => {
  it('validates a correct student placement payload', () => {
    const valid = {
      academicYearId: '11111111-1111-1111-1111-111111111111',
      schoolLevel: 'SMP' as const,
      grade: '7',
      studyGroupId: '33333333-3333-3333-3333-333333333333'
    };

    const result = placementSchema.safeParse(valid);
    expect(result.success).toBe(true);
  });

  it('requires studyGroupId as valid UUID', () => {
    const invalid = {
      academicYearId: '11111111-1111-1111-1111-111111111111',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: 'invalid-id'
    };

    const result = placementSchema.safeParse(invalid);
    expect(result.success).toBe(false);
  });
});
