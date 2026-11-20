import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  scorebookApi,
  scorebookKeys,
  type CreateAssignmentRequest,
  type EnterStudentScoresRequest,
  type GradebookSummaryParams
} from '../scorebook.api';

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

describe('scorebook.api', () => {
  describe('createAssignment', () => {
    it('dispatches POST /api/acd/assignments with assignment payload and returns created assignment', async () => {
      let capturedBody: unknown = null;

      const payload: CreateAssignmentRequest = {
        subjectStageId: 'stg-1',
        studyGroupId: 'sg-1',
        operationalPeriodId: 'per-1',
        title: 'Tugas 1 - Aljabar Linier',
        assignmentType: 'FORMATIVE',
        maxScore: 100,
        weight: 10,
        assignedDate: '2026-08-01',
        dueDate: '2026-08-08'
      };

      server.use(
        http.post('/api/acd/assignments', async ({ request }) => {
          capturedBody = await request.json();
          return HttpResponse.json(
            {
              id: 'asg-1',
              subjectStageId: payload.subjectStageId,
              studyGroupId: payload.studyGroupId,
              operationalPeriodId: payload.operationalPeriodId,
              title: payload.title,
              assignmentType: payload.assignmentType,
              maxScore: payload.maxScore,
              weight: payload.weight,
              assignedDate: payload.assignedDate,
              dueDate: payload.dueDate
            },
            { status: 201 }
          );
        })
      );

      const res = await scorebookApi.createAssignment(payload);

      expect(res.id).toBe('asg-1');
      expect(res.title).toBe('Tugas 1 - Aljabar Linier');
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('saveScores', () => {
    it('dispatches POST /api/acd/assignments/:id/scores with scores list and returns processed count', async () => {
      let capturedBody: unknown = null;

      const payload: EnterStudentScoresRequest = {
        items: [
          {
            studentId: 'std-1',
            score: 85.5,
            feedback: 'Sangat baik'
          },
          {
            studentId: 'std-2',
            score: 65,
            feedback: 'Perlu bimbingan aljabar'
          }
        ]
      };

      server.use(
        http.post('/api/acd/assignments/:id/scores', async ({ request, params }) => {
          expect(params.id).toBe('asg-1');
          capturedBody = await request.json();
          return HttpResponse.json({
            assignmentId: 'asg-1',
            scoresUpdatedCount: 2
          });
        })
      );

      const res = await scorebookApi.saveScores('asg-1', payload);

      expect(res.assignmentId).toBe('asg-1');
      expect(res.scoresUpdatedCount).toBe(2);
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('getGradebookSummary', () => {
    it('dispatches GET /api/acd/gradebook with required query params and returns full gradebook projection', async () => {
      let capturedUrl = '';

      const params: GradebookSummaryParams = {
        subjectStageId: 'stg-1',
        studyGroupId: 'sg-1',
        operationalPeriodId: 'per-1'
      };

      server.use(
        http.get('/api/acd/gradebook', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            subjectStageId: 'stg-1',
            subjectStageName: 'Tahap 1 Aljabar',
            subjectId: 'sub-1',
            subjectName: 'Matematika',
            studyGroupId: 'sg-1',
            studyGroupName: '7A',
            operationalPeriodId: 'per-1',
            operationalPeriodName: 'Ganjil 2026/2027',
            assignments: [
              {
                id: 'asg-1',
                title: 'Tugas 1',
                assignmentType: 'FORMATIVE',
                maxScore: 100,
                weight: 20
              }
            ],
            students: [
              {
                studentId: 'std-1',
                nis: '1001',
                studentName: 'Ahmad Siswa',
                scores: [
                  {
                    assignmentId: 'asg-1',
                    score: 90,
                    feedback: null
                  }
                ],
                attendance: {
                  presentCount: 18,
                  absentCount: 0,
                  lateCount: 1,
                  excusedCount: 1,
                  sickCount: 0,
                  totalRecorded: 20
                },
                runningScore: 90
              }
            ]
          });
        })
      );

      const summary = await scorebookApi.getGradebookSummary(params);

      expect(summary.subjectStageId).toBe('stg-1');
      expect(summary.assignments).toHaveLength(1);
      expect(summary.students).toHaveLength(1);
      expect(summary.students?.[0]?.attendance?.presentCount).toBe(18);
      expect(summary.students?.[0]?.runningScore).toBe(90);

      const url = new URL(capturedUrl);
      expect(url.searchParams.get('subjectStageId')).toBe('stg-1');
      expect(url.searchParams.get('studyGroupId')).toBe('sg-1');
      expect(url.searchParams.get('operationalPeriodId')).toBe('per-1');
    });
  });

  describe('scorebookKeys', () => {
    it('produces deterministic TanStack query keys', () => {
      expect(scorebookKeys.all).toEqual(['academic', 'scorebook']);
      expect(scorebookKeys.gradebooks()).toEqual(['academic', 'scorebook', 'gradebook']);
      const params: GradebookSummaryParams = {
        subjectStageId: 'stg-1',
        studyGroupId: 'sg-1',
        operationalPeriodId: 'per-1'
      };
      expect(scorebookKeys.gradebook(params)).toEqual([
        'academic',
        'scorebook',
        'gradebook',
        params
      ]);
    });
  });
});
