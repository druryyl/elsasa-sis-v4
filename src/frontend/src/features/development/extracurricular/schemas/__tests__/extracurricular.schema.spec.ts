import { describe, expect, it } from 'vitest';
import {
  activityEditorSchema,
  enrollmentSchema,
  sessionLogSchema
} from '../extracurricular.schema';

describe('extracurricular.schema', () => {
  describe('activityEditorSchema', () => {
    it('accepts valid club creation payload', () => {
      const result = activityEditorSchema.safeParse({
        name: 'Pramuka',
        description: 'Weekly scouting club',
        supervisorId: '01900000-0000-7000-8000-000000000001',
        status: 'ACTIVE'
      });
      expect(result.success).toBe(true);
    });

    it('rejects empty name or missing supervisor', () => {
      const result = activityEditorSchema.safeParse({
        name: '   ',
        description: '',
        supervisorId: '',
        status: 'ACTIVE'
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.issues.map((i) => i.path[0]);
        expect(paths).toContain('name');
        expect(paths).toContain('supervisorId');
      }
    });

    it('rejects names longer than 100 characters', () => {
      const result = activityEditorSchema.safeParse({
        name: 'A'.repeat(101),
        supervisorId: '01900000-0000-7000-8000-000000000001',
        status: 'ACTIVE'
      });
      expect(result.success).toBe(false);
    });
  });

  describe('enrollmentSchema', () => {
    it('accepts valid student enrollment input', () => {
      const result = enrollmentSchema.safeParse({
        studentId: '01900000-0000-7000-8000-000000000101',
        joinedDate: '2026-11-20'
      });
      expect(result.success).toBe(true);
    });

    it('rejects invalid joined date format or missing student', () => {
      const result = enrollmentSchema.safeParse({
        studentId: '',
        joinedDate: '20-11-2026'
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.issues.map((i) => i.path[0]);
        expect(paths).toContain('studentId');
        expect(paths).toContain('joinedDate');
      }
    });
  });

  describe('sessionLogSchema', () => {
    it('accepts valid session log input', () => {
      const result = sessionLogSchema.safeParse({
        sessionDate: '2026-11-20',
        log: 'Practiced knot tying and teamwork drills.'
      });
      expect(result.success).toBe(true);
    });

    it('rejects empty log or log exceeding 1000 characters', () => {
      const emptyResult = sessionLogSchema.safeParse({
        sessionDate: '2026-11-20',
        log: '   '
      });
      expect(emptyResult.success).toBe(false);

      const longResult = sessionLogSchema.safeParse({
        sessionDate: '2026-11-20',
        log: 'X'.repeat(1001)
      });
      expect(longResult.success).toBe(false);
    });
  });
});
