import { describe, expect, it } from 'vitest';
import { registerCandidateSchema } from '../candidate.schema';
import { decisionSchema } from '../decision.schema';
import { enrollmentSchema } from '../enrollment.schema';

describe('Admissions Schemas', () => {
  describe('registerCandidateSchema', () => {
    const validCandidate = {
      admissionYearId: '01900000-0000-7000-8000-000000000101',
      fullName: 'Ahmad Dahlan',
      nickname: 'Ahmad',
      dateOfBirth: '2012-05-14',
      gender: 'MALE' as const,
      phone: '081234567890',
      email: 'ahmad@example.com',
      address: 'Jl. Merdeka No. 10',
      targetSchoolLevel: 'SMP' as const,
      targetGrade: '7',
      guardians: [
        {
          fullName: 'Siti Rahma',
          relationshipType: 'MOTHER' as const,
          phone: '081298765432',
          email: 'siti@example.com',
          address: 'Jl. Merdeka No. 10'
        }
      ]
    };

    it('passes for valid candidate and single guardian (BR-STU-007)', () => {
      const result = registerCandidateSchema.safeParse(validCandidate);
      expect(result.success).toBe(true);
    });

    it('passes for candidate with multiple guardians', () => {
      const result = registerCandidateSchema.safeParse({
        ...validCandidate,
        guardians: [
          ...validCandidate.guardians,
          {
            fullName: 'Bambang Sudirman',
            relationshipType: 'FATHER' as const,
            phone: '081211112222',
            email: 'bambang@example.com',
            address: 'Jl. Merdeka No. 10'
          }
        ]
      });
      expect(result.success).toBe(true);
    });

    it('fails when guardians array is empty (BR-STU-007)', () => {
      const result = registerCandidateSchema.safeParse({
        ...validCandidate,
        guardians: []
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path[0] === 'guardians')).toBe(true);
      }
    });

    it('fails when candidate date of birth is in the future', () => {
      const futureDate = new Date();
      futureDate.setFullYear(futureDate.getFullYear() + 1);
      const futureDateStr = futureDate.toISOString().split('T')[0];

      const result = registerCandidateSchema.safeParse({
        ...validCandidate,
        dateOfBirth: futureDateStr
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((i) => i.path[0] === 'dateOfBirth')).toBe(true);
      }
    });

    it('fails when required fields are missing or invalid', () => {
      const result = registerCandidateSchema.safeParse({
        ...validCandidate,
        fullName: '',
        gender: 'UNKNOWN',
        targetSchoolLevel: 'COLLEGE'
      });
      expect(result.success).toBe(false);
    });
  });

  describe('decisionSchema', () => {
    it('accepts ACCEPTED with decision notes', () => {
      const result = decisionSchema.safeParse({
        decision: 'ACCEPTED',
        decisionNotes: 'Approved based on entrance interview'
      });
      expect(result.success).toBe(true);
    });

    it('accepts REJECTED with empty decision notes', () => {
      const result = decisionSchema.safeParse({
        decision: 'REJECTED',
        decisionNotes: null
      });
      expect(result.success).toBe(true);
    });

    it('fails on invalid decision status', () => {
      const result = decisionSchema.safeParse({
        decision: 'PENDING'
      });
      expect(result.success).toBe(false);
    });

    it('fails when decision notes exceed 500 characters', () => {
      const result = decisionSchema.safeParse({
        decision: 'REJECTED',
        decisionNotes: 'a'.repeat(501)
      });
      expect(result.success).toBe(false);
    });
  });

  describe('enrollmentSchema', () => {
    it('validates correct NIS and enrollment date', () => {
      const result = enrollmentSchema.safeParse({
        nis: '20260001',
        nisn: '0012345678',
        enrollmentDate: '2026-07-01'
      });
      expect(result.success).toBe(true);
    });

    it('fails when NIS is missing', () => {
      const result = enrollmentSchema.safeParse({
        nis: '',
        enrollmentDate: '2026-07-01'
      });
      expect(result.success).toBe(false);
    });

    it('fails when NIS exceeds 30 characters', () => {
      const result = enrollmentSchema.safeParse({
        nis: '1234567890123456789012345678901',
        enrollmentDate: '2026-07-01'
      });
      expect(result.success).toBe(false);
    });
  });
});
