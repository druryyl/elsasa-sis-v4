import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  attendanceApi,
  attendanceKeys,
  type AttendanceSheetParams,
  type RecordAttendanceRequest
} from '../attendance.api';

const server = setupServer();

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});

describe('attendance.api', () => {
  describe('getSheet', () => {
    it('dispatches GET /api/acd/attendance with required query params and returns sheet', async () => {
      let capturedUrl = '';

      const params: AttendanceSheetParams = {
        subjectId: 'sub-1',
        studyGroupId: 'sg-1',
        attendanceDate: '2026-08-10',
        operationalPeriodId: 'per-1'
      };

      server.use(
        http.get('/api/acd/attendance', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            subjectId: 'sub-1',
            subjectName: 'Matematika',
            studyGroupId: 'sg-1',
            studyGroupName: '7A',
            attendanceDate: '2026-08-10',
            operationalPeriodId: 'per-1',
            items: [
              {
                studentId: 'std-1',
                nis: '1001',
                studentName: 'Ahmad Siswa',
                status: 'PRESENT',
                notes: null,
                recordedAt: '2026-08-10T08:00:00Z',
                recordedByUserId: 'usr-1'
              },
              {
                studentId: 'std-2',
                nis: '1002',
                studentName: 'Budi Siswa',
                status: 'SICK',
                notes: 'Flu',
                recordedAt: '2026-08-10T08:00:00Z',
                recordedByUserId: 'usr-1'
              }
            ]
          });
        })
      );

      const sheet = await attendanceApi.getSheet(params);

      expect(sheet.subjectId).toBe('sub-1');
      expect(sheet.items).toHaveLength(2);
      expect(sheet.items?.[0]?.status).toBe('PRESENT');
      expect(sheet.items?.[1]?.status).toBe('SICK');

      const url = new URL(capturedUrl);
      expect(url.searchParams.get('subjectId')).toBe('sub-1');
      expect(url.searchParams.get('studyGroupId')).toBe('sg-1');
      expect(url.searchParams.get('attendanceDate')).toBe('2026-08-10');
      expect(url.searchParams.get('operationalPeriodId')).toBe('per-1');
    });
  });

  describe('batchRecord', () => {
    it('dispatches POST /api/acd/attendance with batch payload and returns recorded count', async () => {
      let capturedBody: unknown = null;

      const payload: RecordAttendanceRequest = {
        subjectId: 'sub-1',
        studyGroupId: 'sg-1',
        attendanceDate: '2026-08-10',
        operationalPeriodId: 'per-1',
        items: [
          {
            studentId: 'std-1',
            status: 'PRESENT',
            notes: null
          },
          {
            studentId: 'std-2',
            status: 'ABSENT',
            notes: 'Tanpa keterangan'
          }
        ]
      };

      server.use(
        http.post('/api/acd/attendance', async ({ request }) => {
          capturedBody = await request.json();
          return HttpResponse.json({
            recordedCount: 2
          });
        })
      );

      const res = await attendanceApi.batchRecord(payload);

      expect(res.recordedCount).toBe(2);
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('attendanceKeys', () => {
    it('produces deterministic TanStack query keys', () => {
      expect(attendanceKeys.all).toEqual(['academic', 'attendance']);
      expect(attendanceKeys.sheets()).toEqual(['academic', 'attendance', 'sheets']);
      const params: AttendanceSheetParams = {
        subjectId: 'sub-1',
        studyGroupId: 'sg-1',
        attendanceDate: '2026-08-10',
        operationalPeriodId: 'per-1'
      };
      expect(attendanceKeys.sheet(params)).toEqual([
        'academic',
        'attendance',
        'sheets',
        params
      ]);
    });
  });
});
