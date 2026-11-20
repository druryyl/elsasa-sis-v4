import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  timetablesApi,
  timetableKeys,
  type ConfigureStudyGroupTimetableRequest,
  type CreateSmkBlockRequest
} from '../timetables.api';

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

describe('timetables.api', () => {
  describe('getGrid', () => {
    it('calls GET /api/opr/study-groups/:id/timetables with periodId and scheduleType', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/opr/study-groups/:id/timetables', ({ request, params }) => {
          capturedUrl = request.url;
          expect(params.id).toBe('sg-1');
          return HttpResponse.json([
            {
              id: 'slot-1',
              studyGroupId: 'sg-1',
              subjectId: 'sub-1',
              subjectName: 'Matematika',
              teacherId: 'tch-1',
              teacherName: 'Pak Budi',
              dayOfWeek: 1,
              periodSlot: 1,
              startTime: '07:30',
              endTime: '09:00',
              roomId: 'rm-1',
              roomNumber: '101',
              scheduleType: 'REGULAR',
              teacherConflict: false,
              roomConflict: false
            }
          ]);
        })
      );

      const slots = await timetablesApi.getGrid('sg-1', {
        periodId: 'per-1',
        scheduleType: 'REGULAR'
      });

      expect(slots).toHaveLength(1);
      expect(slots[0]?.id).toBe('slot-1');
      expect(slots[0]?.subjectName).toBe('Matematika');

      const url = new URL(capturedUrl);
      expect(url.searchParams.get('periodId')).toBe('per-1');
      expect(url.searchParams.get('scheduleType')).toBe('REGULAR');
    });

    it('omits scheduleType if not provided', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/opr/study-groups/:id/timetables', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json([]);
        })
      );

      const slots = await timetablesApi.getGrid('sg-1', { periodId: 'per-1' });
      expect(slots).toEqual([]);

      const url = new URL(capturedUrl);
      expect(url.searchParams.get('periodId')).toBe('per-1');
      expect(url.searchParams.has('scheduleType')).toBe(false);
    });
  });

  describe('saveSlot', () => {
    it('dispatches POST /api/opr/study-groups/:id/timetables with slot payload', async () => {
      let capturedBody: unknown = null;

      const payload: ConfigureStudyGroupTimetableRequest = {
        operationalPeriodId: 'per-1',
        dayOfWeek: 2,
        periodSlot: 3,
        startTime: '09:15',
        endTime: '10:45',
        scheduleType: 'GENERAL',
        subjectId: 'sub-2',
        teacherId: 'tch-2',
        roomId: 'rm-2'
      };

      server.use(
        http.post('/api/opr/study-groups/:id/timetables', async ({ request, params }) => {
          expect(params.id).toBe('sg-1');
          capturedBody = await request.json();
          return HttpResponse.json({
            id: 'slot-created',
            studyGroupId: 'sg-1',
            dayOfWeek: 2,
            periodSlot: 3
          });
        })
      );

      const result = await timetablesApi.saveSlot('sg-1', payload);

      expect(result.id).toBe('slot-created');
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('deleteSlot', () => {
    it('dispatches DELETE /api/opr/study-groups/:id/timetables/:scheduleId', async () => {
      let called = false;

      server.use(
        http.delete(
          '/api/opr/study-groups/:id/timetables/:scheduleId',
          ({ params }) => {
            expect(params.id).toBe('sg-1');
            expect(params.scheduleId).toBe('slot-1');
            called = true;
            return new HttpResponse(null, { status: 204 });
          }
        )
      );

      await timetablesApi.deleteSlot('sg-1', 'slot-1');
      expect(called).toBe(true);
    });
  });

  describe('getBlocks', () => {
    it('dispatches GET /api/opr/study-groups/:id/blocks with periodId', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/opr/study-groups/:id/blocks', ({ request, params }) => {
          capturedUrl = request.url;
          expect(params.id).toBe('sg-1');
          return HttpResponse.json([
            {
              id: 'blk-1',
              studyGroupId: 'sg-1',
              operationalPeriodId: 'per-1',
              startDate: '2026-08-01',
              endDate: '2026-08-31',
              scheduleType: 'GENERAL'
            }
          ]);
        })
      );

      const blocks = await timetablesApi.getBlocks('sg-1', { periodId: 'per-1' });

      expect(blocks).toHaveLength(1);
      expect(blocks[0]?.scheduleType).toBe('GENERAL');
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('periodId')).toBe('per-1');
    });
  });

  describe('createBlock', () => {
    it('dispatches POST /api/opr/study-groups/:id/blocks with payload', async () => {
      let capturedBody: unknown = null;

      const payload: CreateSmkBlockRequest = {
        operationalPeriodId: 'per-1',
        startDate: '2026-09-01',
        endDate: '2026-09-30',
        scheduleType: 'VOCATIONAL'
      };

      server.use(
        http.post('/api/opr/study-groups/:id/blocks', async ({ request, params }) => {
          expect(params.id).toBe('sg-1');
          capturedBody = await request.json();
          return HttpResponse.json({
            id: 'blk-created',
            studyGroupId: 'sg-1',
            ...payload
          });
        })
      );

      const result = await timetablesApi.createBlock('sg-1', payload);

      expect(result.id).toBe('blk-created');
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('deleteBlock', () => {
    it('dispatches DELETE /api/opr/study-groups/:id/blocks/:blockId', async () => {
      let called = false;

      server.use(
        http.delete('/api/opr/study-groups/:id/blocks/:blockId', ({ params }) => {
          expect(params.id).toBe('sg-1');
          expect(params.blockId).toBe('blk-1');
          called = true;
          return new HttpResponse(null, { status: 204 });
        })
      );

      await timetablesApi.deleteBlock('sg-1', 'blk-1');
      expect(called).toBe(true);
    });
  });

  describe('getTeacherSchedule', () => {
    it('dispatches GET /api/opr/teacher-schedule with periodId', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/opr/teacher-schedule', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            teacherId: 'tch-1',
            operationalPeriodId: 'per-1',
            scheduleGroups: [
              {
                scheduleType: 'VOCATIONAL',
                slots: [
                  {
                    id: 'slot-1',
                    studyGroupId: 'sg-1',
                    studyGroupName: 'X-RPL-1',
                    subjectId: 'sub-1',
                    subjectName: 'Pemrograman Web',
                    dayOfWeek: 1,
                    periodSlot: 1,
                    startTime: '07:30',
                    endTime: '10:30',
                    roomId: 'lab-1',
                    roomNumber: 'Lab Komputer 1',
                    scheduleType: 'VOCATIONAL',
                    teacherConflict: false,
                    roomConflict: false
                  }
                ]
              }
            ]
          });
        })
      );

      const schedule = await timetablesApi.getTeacherSchedule({ periodId: 'per-1' });

      expect(schedule.teacherId).toBe('tch-1');
      expect(schedule.scheduleGroups).toHaveLength(1);
      expect(schedule.scheduleGroups?.[0]?.slots).toHaveLength(1);
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('periodId')).toBe('per-1');
    });
  });

  describe('timetableKeys', () => {
    it('produces deterministic TanStack query keys', () => {
      expect(timetableKeys.all).toEqual(['operations', 'timetables']);
      expect(timetableKeys.grids()).toEqual(['operations', 'timetables', 'grid']);
      expect(
        timetableKeys.grid('sg-1', { periodId: 'per-1', scheduleType: 'REGULAR' })
      ).toEqual([
        'operations',
        'timetables',
        'grid',
        'sg-1',
        { periodId: 'per-1', scheduleType: 'REGULAR' }
      ]);
      expect(timetableKeys.blocks()).toEqual(['operations', 'timetables', 'blocks']);
      expect(timetableKeys.blocksByGroup('sg-1', { periodId: 'per-1' })).toEqual([
        'operations',
        'timetables',
        'blocks',
        'sg-1',
        { periodId: 'per-1' }
      ]);
      expect(timetableKeys.teacherSchedules()).toEqual([
        'operations',
        'timetables',
        'teacher-schedule'
      ]);
      expect(timetableKeys.teacherSchedule({ periodId: 'per-1' })).toEqual([
        'operations',
        'timetables',
        'teacher-schedule',
        { periodId: 'per-1' }
      ]);
    });
  });
});
