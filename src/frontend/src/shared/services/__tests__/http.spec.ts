import { describe, it, expect, beforeAll, afterAll, afterEach, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { apiClient, configureHttpClient } from '@/shared/services/http';
import { ApiError } from '@/shared/types/common';

const server = setupServer();

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
});

afterAll(() => {
  server.close();
});

describe('http.ts (apiClient)', () => {
  it('attaches Authorization, context headers, and X-Correlation-ID', async () => {
    let capturedAuth: string | null = null;
    let capturedPeriod: string | null = null;
    let capturedCorrelation: string | null = null;

    server.use(
      http.get('/api/test-headers', ({ request }) => {
        capturedAuth = request.headers.get('Authorization');
        capturedPeriod = request.headers.get('X-Active-Period-ID');
        capturedCorrelation = request.headers.get('X-Correlation-ID');
        return HttpResponse.json({ ok: true });
      })
    );

    configureHttpClient({
      getAccessToken: () => 'initial-token',
      getContextHeaders: () => ({ 'X-Active-Period-ID': 'period-123' }),
      refreshAccessToken: async () => null,
      onSessionExpired: vi.fn()
    });

    const data = await apiClient.get('/test-headers');
    expect(data).toEqual({ ok: true });
    expect(capturedAuth).toBe('Bearer initial-token');
    expect(capturedPeriod).toBe('period-123');
    expect(capturedCorrelation).toBeTruthy();
  });

  it('performs single-flight refresh on concurrent 401 responses and retries both requests', async () => {
    let currentToken = 'expired-token';
    const refreshSpy = vi.fn(async () => {
      await new Promise((resolve) => setTimeout(resolve, 20));
      currentToken = 'fresh-token';
      return 'fresh-token';
    });
    const sessionExpiredSpy = vi.fn();

    server.use(
      http.get('/api/protected-a', ({ request }) => {
        if (request.headers.get('Authorization') !== 'Bearer fresh-token') {
          return HttpResponse.json({ title: 'Unauthorized' }, { status: 401 });
        }
        return HttpResponse.json({ resource: 'a' });
      }),
      http.get('/api/protected-b', ({ request }) => {
        if (request.headers.get('Authorization') !== 'Bearer fresh-token') {
          return HttpResponse.json({ title: 'Unauthorized' }, { status: 401 });
        }
        return HttpResponse.json({ resource: 'b' });
      })
    );

    configureHttpClient({
      getAccessToken: () => currentToken,
      getContextHeaders: () => ({}),
      refreshAccessToken: refreshSpy,
      onSessionExpired: sessionExpiredSpy
    });

    const [resA, resB] = await Promise.all([
      apiClient.get('/protected-a'),
      apiClient.get('/protected-b')
    ]);

    expect(resA).toEqual({ resource: 'a' });
    expect(resB).toEqual({ resource: 'b' });
    expect(refreshSpy).toHaveBeenCalledTimes(1);
    expect(sessionExpiredSpy).not.toHaveBeenCalled();
  });

  it('calls onSessionExpired when refresh returns null', async () => {
    const refreshSpy = vi.fn(async () => null);
    const sessionExpiredSpy = vi.fn();

    server.use(
      http.get('/api/protected-fail', () =>
        HttpResponse.json({ title: 'Unauthorized' }, { status: 401 })
      )
    );

    configureHttpClient({
      getAccessToken: () => 'expired-token',
      getContextHeaders: () => ({}),
      refreshAccessToken: refreshSpy,
      onSessionExpired: sessionExpiredSpy
    });

    await expect(apiClient.get('/protected-fail')).rejects.toMatchObject({
      name: 'ApiError',
      status: 401
    });

    expect(refreshSpy).toHaveBeenCalledTimes(1);
    expect(sessionExpiredSpy).toHaveBeenCalledTimes(1);
  });

  it('does not trigger refresh for /auth/login 401 responses', async () => {
    const refreshSpy = vi.fn(async () => 'new-token');
    const sessionExpiredSpy = vi.fn();

    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({ title: 'Unauthorized' }, { status: 401 })
      )
    );

    configureHttpClient({
      getAccessToken: () => null,
      getContextHeaders: () => ({}),
      refreshAccessToken: refreshSpy,
      onSessionExpired: sessionExpiredSpy
    });

    await expect(apiClient.post('/auth/login', {})).rejects.toMatchObject({
      name: 'ApiError',
      status: 401
    });

    expect(refreshSpy).not.toHaveBeenCalled();
    expect(sessionExpiredSpy).toHaveBeenCalledTimes(1);
  });

  it('maps RFC 7807 422 ProblemDetails into ApiError', async () => {
    server.use(
      http.post('/api/opr/academic-years', () =>
        HttpResponse.json(
          {
            title: 'Validation Error',
            detail: 'Invalid academic year dates.',
            errorCode: 'Request.ValidationFailed',
            errors: {
              endDate: ['End date must be after start date.']
            }
          },
          { status: 422 }
        )
      )
    );

    let caught: unknown;
    try {
      await apiClient.post('/opr/academic-years', {});
    } catch (err) {
      caught = err;
    }

    expect(caught).toBeInstanceOf(ApiError);
    const apiErr = caught as ApiError;
    expect(apiErr.status).toBe(422);
    expect(apiErr.errorCode).toBe('Request.ValidationFailed');
    expect(apiErr.errors).toEqual({
      endDate: ['End date must be after start date.']
    });
  });
});
