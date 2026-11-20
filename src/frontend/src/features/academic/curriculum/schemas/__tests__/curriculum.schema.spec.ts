import { describe, expect, it } from 'vitest';
import {
  batchEnrollSchema,
  learningUnitSchema,
  subjectSchema,
  subjectStageSchema,
  VALID_GRADES_BY_LEVEL
} from '../curriculum.schema';

describe('curriculum schemas', () => {
  describe('subjectSchema', () => {
    it('validates correct subject payload', () => {
      const parsed = subjectSchema.safeParse({
        name: 'Matematika',
        code: 'MTK'
      });
      expect(parsed.success).toBe(true);
    });

    it('rejects empty name or code', () => {
      const emptyName = subjectSchema.safeParse({ name: '', code: 'MTK' });
      expect(emptyName.success).toBe(false);

      const emptyCode = subjectSchema.safeParse({ name: 'Matematika', code: ' ' });
      expect(emptyCode.success).toBe(false);
    });
  });

  describe('subjectStageSchema', () => {
    it('validates correct subject stage payload', () => {
      const parsed = subjectStageSchema.safeParse({
        name: 'Fase D (Kelas 7)',
        sequenceOrder: 1,
        description: 'Matematika Fase D'
      });
      expect(parsed.success).toBe(true);
    });

    it('rejects non-positive or missing sequence order', () => {
      const negativeSeq = subjectStageSchema.safeParse({
        name: 'Stage 1',
        sequenceOrder: 0
      });
      expect(negativeSeq.success).toBe(false);
    });
  });

  describe('learningUnitSchema', () => {
    it('validates correct learning unit payload', () => {
      const parsed = learningUnitSchema.safeParse({
        name: 'Aljabar Dasar',
        unitType: 'MODULE',
        sequenceOrder: 1,
        description: 'Pengenalan variabel'
      });
      expect(parsed.success).toBe(true);
    });

    it('rejects invalid unit type', () => {
      const invalidType = learningUnitSchema.safeParse({
        name: 'Unit 1',
        unitType: 'UNKNOWN_TYPE',
        sequenceOrder: 1
      });
      expect(invalidType.success).toBe(false);
    });
  });

  describe('batchEnrollSchema', () => {
    it('validates correct batch enroll payload', () => {
      const parsed = batchEnrollSchema.safeParse({
        studyGroupId: '018f3a2b-1111-7000-8000-000000000001',
        operationalPeriodId: '018f3a2b-2222-7000-8000-000000000002'
      });
      expect(parsed.success).toBe(true);
    });

    it('rejects empty IDs', () => {
      const empty = batchEnrollSchema.safeParse({
        studyGroupId: '',
        operationalPeriodId: ''
      });
      expect(empty.success).toBe(false);
    });
  });

  describe('VALID_GRADES_BY_LEVEL', () => {
    it('has accurate grades mapping for Indonesian school levels', () => {
      expect(VALID_GRADES_BY_LEVEL.SD).toEqual([1, 2, 3, 4, 5, 6]);
      expect(VALID_GRADES_BY_LEVEL.SMP).toEqual([7, 8, 9]);
      expect(VALID_GRADES_BY_LEVEL.SMA).toEqual([10, 11, 12]);
      expect(VALID_GRADES_BY_LEVEL.SMK).toEqual([10, 11, 12]);
    });
  });
});
