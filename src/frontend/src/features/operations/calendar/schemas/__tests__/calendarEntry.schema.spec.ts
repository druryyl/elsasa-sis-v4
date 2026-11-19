import { describe, expect, it } from 'vitest';
import { calendarEntrySchema } from '../calendarEntry.schema';

describe('calendarEntrySchema', () => {
  it('accepts valid calendar entry form values', () => {
    const result = calendarEntrySchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000101',
      entryDate: '2026-08-17',
      dayType: 'HOLIDAY',
      description: 'Independence Day'
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.dayType).toBe('HOLIDAY');
      expect(result.data.description).toBe('Independence Day');
    }
  });

  it('rejects missing academic year ID', () => {
    const result = calendarEntrySchema.safeParse({
      academicYearId: '',
      entryDate: '2026-08-17',
      dayType: 'HOLIDAY',
      description: 'Independence Day'
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes('academicYearId'))).toBe(true);
    }
  });

  it('rejects invalid date format', () => {
    const result = calendarEntrySchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000101',
      entryDate: '17-08-2026',
      dayType: 'HOLIDAY'
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes('entryDate'))).toBe(true);
    }
  });

  it('rejects unsupported day type', () => {
    const result = calendarEntrySchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000101',
      entryDate: '2026-08-17',
      dayType: 'WEEKEND'
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes('dayType'))).toBe(true);
    }
  });

  it('trims empty description to null', () => {
    const result = calendarEntrySchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000101',
      entryDate: '2026-08-17',
      dayType: 'SCHOOL_DAY',
      description: '   '
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.description).toBeNull();
    }
  });
});
