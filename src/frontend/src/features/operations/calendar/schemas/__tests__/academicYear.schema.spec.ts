import { describe, expect, it } from 'vitest';
import { academicYearSchema } from '../academicYear.schema';

describe('academicYearSchema', () => {
  it('accepts a valid academic year with non-overlapping operational periods', () => {
    const result = academicYearSchema.safeParse({
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      periods: [
        {
          name: 'Semester 1',
          startDate: '2026-07-01',
          endDate: '2026-12-20'
        },
        {
          name: 'Semester 2',
          startDate: '2027-01-05',
          endDate: '2027-06-30'
        }
      ]
    });

    expect(result.success).toBe(true);
  });

  it('rejects when academic year endDate is not after startDate (BR-OPS-005)', () => {
    const result = academicYearSchema.safeParse({
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2026-07-01',
      periods: [
        {
          name: 'Semester 1',
          startDate: '2026-07-01',
          endDate: '2026-12-20'
        }
      ]
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.join('.') === 'endDate')).toBe(true);
    }
  });

  it('rejects when periods array is empty (BR-OPS-002)', () => {
    const result = academicYearSchema.safeParse({
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      periods: []
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.join('.') === 'periods')).toBe(true);
    }
  });

  it('rejects when a period falls outside academic year bounds (BR-OPS-003)', () => {
    const result = academicYearSchema.safeParse({
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      periods: [
        {
          name: 'Semester 1',
          startDate: '2026-06-15',
          endDate: '2026-12-20'
        }
      ]
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.join('.') === 'periods.0.startDate')).toBe(true);
    }
  });

  it('rejects overlapping operational periods (BR-OPS-004)', () => {
    const result = academicYearSchema.safeParse({
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      periods: [
        {
          name: 'Semester 1',
          startDate: '2026-07-01',
          endDate: '2026-12-31'
        },
        {
          name: 'Semester 2',
          startDate: '2026-12-31',
          endDate: '2027-06-30'
        }
      ]
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.join('.') === 'periods.1.startDate')).toBe(true);
    }
  });

  it('rejects duplicate period names case-insensitively', () => {
    const result = academicYearSchema.safeParse({
      name: '2026/2027',
      startDate: '2026-07-01',
      endDate: '2027-06-30',
      periods: [
        {
          name: 'Semester 1',
          startDate: '2026-07-01',
          endDate: '2026-12-20'
        },
        {
          name: 'semester 1',
          startDate: '2027-01-05',
          endDate: '2027-06-30'
        }
      ]
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.join('.') === 'periods.1.name')).toBe(true);
    }
  });
});
