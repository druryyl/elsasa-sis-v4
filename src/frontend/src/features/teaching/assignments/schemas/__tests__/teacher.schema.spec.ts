import { describe, expect, it } from 'vitest';
import { teacherSchema } from '../teacher.schema';

describe('teacherSchema', () => {
  const validBase = {
    fullName: 'Budi Santoso',
    nickname: 'Budi',
    nip: '198501012010011001',
    nik: '3201010101850001',
    dateOfBirth: '1985-01-01',
    gender: 'MALE' as const,
    phone: '081234567890',
    email: 'budi@example.com',
    address: 'Jl. Merdeka No. 10'
  };

  it('validates a complete valid teacher payload with both phone and email', () => {
    const result = teacherSchema.safeParse(validBase);
    expect(result.success).toBe(true);
  });

  it('validates teacher with only phone (BR-TCH-002 satisfied)', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      email: ''
    });
    expect(result.success).toBe(true);
  });

  it('validates teacher with only email (BR-TCH-002 satisfied)', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      phone: ''
    });
    expect(result.success).toBe(true);
  });

  it('rejects when both phone and email are missing/empty (enforcing BR-TCH-002)', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      phone: '',
      email: ''
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      const messages = result.error.errors.map((e) => e.message);
      expect(messages).toContain('At least one contact method (phone or email) is required.');
    }
  });

  it('rejects when both phone and email are null (enforcing BR-TCH-002)', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      phone: null,
      email: null
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      const messages = result.error.errors.map((e) => e.message);
      expect(messages).toContain('At least one contact method (phone or email) is required.');
    }
  });

  it('rejects when fullName is empty (BR-TCH-001)', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      fullName: '  '
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.errors.some((e) => e.path.includes('fullName'))).toBe(true);
    }
  });

  it('rejects when fullName exceeds 150 characters', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      fullName: 'A'.repeat(151)
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.errors[0]?.message).toContain('150');
    }
  });

  it('rejects when dateOfBirth is in the future', () => {
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 1);
    const dateStr = futureDate.toISOString().split('T')[0];

    const result = teacherSchema.safeParse({
      ...validBase,
      dateOfBirth: dateStr
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.errors.some((e) => e.message.includes('future'))).toBe(true);
    }
  });

  it('rejects invalid dateOfBirth format', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      dateOfBirth: '01-01-1985'
    });
    expect(result.success).toBe(false);
  });

  it('rejects invalid gender (BR-TCH-008)', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      gender: 'OTHER'
    });
    expect(result.success).toBe(false);
  });

  it('rejects invalid email format when email is provided', () => {
    const result = teacherSchema.safeParse({
      ...validBase,
      email: 'not-an-email'
    });
    expect(result.success).toBe(false);
  });
});
