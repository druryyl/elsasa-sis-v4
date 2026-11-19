import { describe, it, expect } from 'vitest';
import { ApiError } from '@/shared/types/common';

describe('ApiError', () => {
  it('parses top-level RFC 7807 ProblemDetails fields', () => {
    const err = ApiError.fromProblemDetails(422, {
      title: 'Validation Error',
      detail: 'One or more validation errors occurred.',
      errorCode: 'Request.ValidationFailed',
      errors: {
        name: ['Academic year name is required.']
      }
    });

    expect(err).toBeInstanceOf(ApiError);
    expect(err).toBeInstanceOf(Error);
    expect(err.status).toBe(422);
    expect(err.title).toBe('Validation Error');
    expect(err.detail).toBe('One or more validation errors occurred.');
    expect(err.errorCode).toBe('Request.ValidationFailed');
    expect(err.errors).toEqual({
      name: ['Academic year name is required.']
    });
  });

  it('parses errorCode and errors nested inside extensions', () => {
    const err = ApiError.fromProblemDetails(409, {
      title: 'Business Rule Conflict',
      detail: 'Cannot remove the active operational period.',
      extensions: {
        errorCode: 'Opr.CannotRemoveActivePeriod',
        errors: {
          periods: ['Active period must be retained.']
        }
      }
    });

    expect(err.status).toBe(409);
    expect(err.errorCode).toBe('Opr.CannotRemoveActivePeriod');
    expect(err.errors).toEqual({
      periods: ['Active period must be retained.']
    });
  });

  it('uses fallback values when payload is empty or null', () => {
    const err = ApiError.fromProblemDetails(500, null, 'Server crashed');

    expect(err.status).toBe(500);
    expect(err.title).toBe('An unexpected error occurred');
    expect(err.detail).toBe('Server crashed');
    expect(err.errorCode).toBeUndefined();
    expect(err.errors).toBeUndefined();
  });
});
