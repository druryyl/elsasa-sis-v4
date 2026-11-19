import { describe, expect, it } from 'vitest';
import { loginSchema } from '../login.schema';

describe('loginSchema', () => {
  it('accepts valid username and password and trims username', () => {
    const result = loginSchema.safeParse({
      username: '  admin  ',
      password: 'Password123!'
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data).toEqual({
        username: 'admin',
        password: 'Password123!'
      });
    }
  });

  it('rejects empty or whitespace-only username', () => {
    const result = loginSchema.safeParse({
      username: '   ',
      password: 'Password123!'
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.username).toEqual(['Username is required']);
    }
  });

  it('rejects empty password', () => {
    const result = loginSchema.safeParse({
      username: 'admin',
      password: ''
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.password).toEqual(['Password is required']);
    }
  });
});
