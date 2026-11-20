import { describe, expect, it } from 'vitest';
import { slotFormSchema, smkBlockSchema } from '../timetable.schema';

describe('timetable.schema', () => {
  describe('slotFormSchema', () => {
    const validSlot = {
      operationalPeriodId: '01932f2c-7b00-7000-8000-000000000001',
      scheduleType: 'REGULAR' as const,
      dayOfWeek: 1,
      periodSlot: 1,
      startTime: '07:30',
      endTime: '08:15',
      subjectId: '01932f2c-7b00-7000-8000-000000000002',
      teacherId: '01932f2c-7b00-7000-8000-000000000003',
      roomId: '01932f2c-7b00-7000-8000-000000000004'
    };

    it('passes validation for valid slot values', () => {
      const result = slotFormSchema.safeParse(validSlot);
      expect(result.success).toBe(true);
    });

    it('allows null room id', () => {
      const result = slotFormSchema.safeParse({
        ...validSlot,
        roomId: null
      });
      expect(result.success).toBe(true);
    });

    it('rejects missing subject or teacher', () => {
      const result = slotFormSchema.safeParse({
        ...validSlot,
        subjectId: '',
        teacherId: ''
      });
      expect(result.success).toBe(false);
      if (!result.success) {
        const issues = result.error.flatten().fieldErrors;
        expect(issues.subjectId).toBeDefined();
        expect(issues.teacherId).toBeDefined();
      }
    });

    it('rejects invalid dayOfWeek (< 1 or > 7)', () => {
      const under = slotFormSchema.safeParse({ ...validSlot, dayOfWeek: 0 });
      const over = slotFormSchema.safeParse({ ...validSlot, dayOfWeek: 8 });
      expect(under.success).toBe(false);
      expect(over.success).toBe(false);
    });

    it('rejects invalid periodSlot (< 1 or > 10)', () => {
      const under = slotFormSchema.safeParse({ ...validSlot, periodSlot: 0 });
      const over = slotFormSchema.safeParse({ ...validSlot, periodSlot: 11 });
      expect(under.success).toBe(false);
      expect(over.success).toBe(false);
    });

    it('rejects endTime that is not strictly after startTime', () => {
      const sameTime = slotFormSchema.safeParse({
        ...validSlot,
        startTime: '08:00',
        endTime: '08:00'
      });
      expect(sameTime.success).toBe(false);

      const beforeTime = slotFormSchema.safeParse({
        ...validSlot,
        startTime: '09:00',
        endTime: '08:00'
      });
      expect(beforeTime.success).toBe(false);
    });

    it('accepts valid schedule types REGULAR, GENERAL, VOCATIONAL', () => {
      for (const type of ['REGULAR', 'GENERAL', 'VOCATIONAL'] as const) {
        const result = slotFormSchema.safeParse({ ...validSlot, scheduleType: type });
        expect(result.success).toBe(true);
      }
    });

    it('rejects invalid schedule type', () => {
      const result = slotFormSchema.safeParse({
        ...validSlot,
        scheduleType: 'INVALID'
      });
      expect(result.success).toBe(false);
    });
  });

  describe('smkBlockSchema', () => {
    const validBlock = {
      operationalPeriodId: '01932f2c-7b00-7000-8000-000000000001',
      scheduleType: 'GENERAL' as const,
      startDate: '2026-08-01',
      endDate: '2026-08-14'
    };

    it('passes validation for valid block range', () => {
      const result = smkBlockSchema.safeParse(validBlock);
      expect(result.success).toBe(true);
    });

    it('accepts same start and end date (1-day block)', () => {
      const result = smkBlockSchema.safeParse({
        ...validBlock,
        startDate: '2026-08-01',
        endDate: '2026-08-01'
      });
      expect(result.success).toBe(true);
    });

    it('rejects endDate prior to startDate', () => {
      const result = smkBlockSchema.safeParse({
        ...validBlock,
        startDate: '2026-08-15',
        endDate: '2026-08-01'
      });
      expect(result.success).toBe(false);
    });

    it('rejects invalid date format', () => {
      const result = smkBlockSchema.safeParse({
        ...validBlock,
        startDate: '01-08-2026'
      });
      expect(result.success).toBe(false);
    });

    it('accepts only GENERAL and VOCATIONAL for block types', () => {
      expect(smkBlockSchema.safeParse({ ...validBlock, scheduleType: 'GENERAL' }).success).toBe(true);
      expect(smkBlockSchema.safeParse({ ...validBlock, scheduleType: 'VOCATIONAL' }).success).toBe(true);
      expect(smkBlockSchema.safeParse({ ...validBlock, scheduleType: 'REGULAR' }).success).toBe(false);
    });
  });
});
