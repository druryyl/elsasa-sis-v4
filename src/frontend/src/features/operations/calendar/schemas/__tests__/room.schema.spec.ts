import { describe, expect, it } from 'vitest';
import { roomSchema } from '../room.schema';

describe('roomSchema', () => {
  it('accepts valid room form values', () => {
    const result = roomSchema.safeParse({
      name: 'Classroom 7A',
      roomType: 'CLASSROOM',
      capacity: 32
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.name).toBe('Classroom 7A');
      expect(result.data.capacity).toBe(32);
      expect(result.data.roomType).toBe('CLASSROOM');
    }
  });

  it('rejects empty room name', () => {
    const result = roomSchema.safeParse({
      name: '   ',
      roomType: 'LABORATORY',
      capacity: 25
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes('name'))).toBe(true);
    }
  });

  it('rejects unsupported room type', () => {
    const result = roomSchema.safeParse({
      name: 'Storage 1',
      roomType: 'STORAGE',
      capacity: 10
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes('roomType'))).toBe(true);
    }
  });

  it('rejects non-positive capacity', () => {
    const zeroResult = roomSchema.safeParse({
      name: 'Lab B',
      roomType: 'LABORATORY',
      capacity: 0
    });
    expect(zeroResult.success).toBe(false);

    const negativeResult = roomSchema.safeParse({
      name: 'Lab B',
      roomType: 'LABORATORY',
      capacity: -5
    });
    expect(negativeResult.success).toBe(false);
  });

  it('coerces string capacity into positive integer', () => {
    const result = roomSchema.safeParse({
      name: 'Auditorium',
      roomType: 'HALL',
      capacity: '150'
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.capacity).toBe(150);
    }
  });
});
