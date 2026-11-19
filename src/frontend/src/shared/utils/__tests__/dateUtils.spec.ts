import { describe, it, expect } from 'vitest';
import {
  formatDisplayDate,
  formatDisplayDateTime,
  formatBackendDateOnly,
  formatBackendTimeOnly,
  parseDateInput
} from '@/shared/utils/dateUtils';

describe('dateUtils', () => {
  it('formats ISO date strings for display (dd MMM yyyy)', () => {
    expect(formatDisplayDate('2026-11-12')).toBe('12 Nov 2026');
  });

  it('formats Date instances for display datetime (dd MMM yyyy, HH:mm)', () => {
    const dt = new Date(2026, 10, 12, 14, 30, 0);
    expect(formatDisplayDateTime(dt)).toBe('12 Nov 2026, 14:30');
  });

  it('formats backend DateOnly (yyyy-MM-dd) and TimeOnly (HH:mm:ss)', () => {
    const dt = new Date(2026, 6, 15, 8, 5, 9);
    expect(formatBackendDateOnly(dt)).toBe('2026-07-15');
    expect(formatBackendTimeOnly(dt)).toBe('08:05:09');
  });

  it('returns fallback for null, undefined, or invalid dates', () => {
    expect(parseDateInput(null)).toBeNull();
    expect(parseDateInput(undefined)).toBeNull();
    expect(parseDateInput('not-a-date')).toBeNull();
    expect(formatDisplayDate(null, '—')).toBe('—');
    expect(formatDisplayDateTime('invalid', 'N/A')).toBe('N/A');
    expect(formatBackendDateOnly(undefined)).toBe('');
  });
});
