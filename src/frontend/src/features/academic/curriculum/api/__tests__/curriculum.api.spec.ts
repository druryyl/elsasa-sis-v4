import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  curriculumApi,
  curriculumKeys,
  type CreateSubjectRequest,
  type UpdateSubjectRequest,
  type CreateSubjectStageRequest,
  type UpdateSubjectStageRequest,
  type CreateLearningUnitRequest,
  type UpdateLearningUnitRequest,
  type MapSubjectStageGradeRequest,
  type EnrollStudentsInSubjectStagesRequest
} from '../curriculum.api';

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

describe('curriculum.api', () => {
  describe('subjects', () => {
    it('getSubjects dispatches GET /api/acd/subjects and returns hierarchy', async () => {
      server.use(
        http.get('/api/acd/subjects', () => {
          return HttpResponse.json([
            {
              id: 'sub-1',
              name: 'Matematika',
              code: 'MATH',
              stages: [
                {
                  id: 'stg-1',
                  subjectId: 'sub-1',
                  subjectName: 'Matematika',
                  name: 'Aljabar Dasar',
                  sequenceOrder: 1,
                  description: 'Stage 1',
                  units: [
                    {
                      id: 'unit-1',
                      subjectStageId: 'stg-1',
                      name: 'Operasi Hitung',
                      sequenceOrder: 1,
                      unitType: 'THEORY',
                      description: 'Unit 1'
                    }
                  ],
                  gradeMappings: [
                    {
                      id: 'gm-1',
                      subjectStageId: 'stg-1',
                      schoolLevel: 'SMP',
                      grade: '7'
                    }
                  ]
                }
              ]
            }
          ]);
        })
      );

      const subjects = await curriculumApi.getSubjects();
      expect(subjects).toHaveLength(1);
      expect(subjects[0]?.name).toBe('Matematika');
      expect(subjects[0]?.stages).toHaveLength(1);
      expect(subjects[0]?.stages?.[0]?.units).toHaveLength(1);
      expect(subjects[0]?.stages?.[0]?.gradeMappings).toHaveLength(1);
    });

    it('createSubject dispatches POST /api/acd/subjects with payload', async () => {
      let capturedBody: unknown = null;
      const payload: CreateSubjectRequest = {
        name: 'Bahasa Indonesia',
        code: 'BIND'
      };

      server.use(
        http.post('/api/acd/subjects', async ({ request }) => {
          capturedBody = await request.json();
          return HttpResponse.json(
            { id: 'sub-created', name: payload.name, code: payload.code, stages: [] },
            { status: 201 }
          );
        })
      );

      const res = await curriculumApi.createSubject(payload);
      expect(res.id).toBe('sub-created');
      expect(capturedBody).toEqual(payload);
    });

    it('updateSubject dispatches PUT /api/acd/subjects/:id with payload', async () => {
      let capturedBody: unknown = null;
      const payload: UpdateSubjectRequest = {
        name: 'Matematika Terapan',
        code: 'MATH-ADV'
      };

      server.use(
        http.put('/api/acd/subjects/:id', async ({ request, params }) => {
          expect(params.id).toBe('sub-1');
          capturedBody = await request.json();
          return HttpResponse.json({
            id: 'sub-1',
            name: payload.name,
            code: payload.code,
            stages: []
          });
        })
      );

      const res = await curriculumApi.updateSubject('sub-1', payload);
      expect(res.name).toBe('Matematika Terapan');
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('subject stages', () => {
    it('getSubjectStages dispatches GET /api/acd/subjects/:id/stages', async () => {
      server.use(
        http.get('/api/acd/subjects/:id/stages', ({ params }) => {
          expect(params.id).toBe('sub-1');
          return HttpResponse.json([
            {
              id: 'stg-1',
              subjectId: 'sub-1',
              name: 'Tahap 1',
              sequenceOrder: 1,
              units: [],
              gradeMappings: []
            }
          ]);
        })
      );

      const stages = await curriculumApi.getSubjectStages('sub-1');
      expect(stages).toHaveLength(1);
      expect(stages[0]?.id).toBe('stg-1');
    });

    it('createSubjectStage dispatches POST /api/acd/subjects/:id/stages', async () => {
      let capturedBody: unknown = null;
      const payload: CreateSubjectStageRequest = {
        name: 'Tahap 2',
        sequenceOrder: 2,
        description: 'Tahap Lanjutan'
      };

      server.use(
        http.post('/api/acd/subjects/:id/stages', async ({ request, params }) => {
          expect(params.id).toBe('sub-1');
          capturedBody = await request.json();
          return HttpResponse.json({
            id: 'stg-2',
            subjectId: 'sub-1',
            ...payload
          });
        })
      );

      const res = await curriculumApi.createSubjectStage('sub-1', payload);
      expect(res.id).toBe('stg-2');
      expect(capturedBody).toEqual(payload);
    });

    it('updateSubjectStage dispatches PUT /api/acd/subjects/:id/stages/:stageId', async () => {
      let capturedBody: unknown = null;
      const payload: UpdateSubjectStageRequest = {
        name: 'Tahap 2 Diperbarui',
        sequenceOrder: 2,
        description: 'Deskripsi baru'
      };

      server.use(
        http.put(
          '/api/acd/subjects/:id/stages/:stageId',
          async ({ request, params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-2');
            capturedBody = await request.json();
            return HttpResponse.json({
              id: 'stg-2',
              subjectId: 'sub-1',
              ...payload
            });
          }
        )
      );

      const res = await curriculumApi.updateSubjectStage('sub-1', 'stg-2', payload);
      expect(res.name).toBe('Tahap 2 Diperbarui');
      expect(capturedBody).toEqual(payload);
    });
  });

  describe('learning units', () => {
    it('getLearningUnits dispatches GET /api/acd/subjects/:id/stages/:stageId/units', async () => {
      server.use(
        http.get(
          '/api/acd/subjects/:id/stages/:stageId/units',
          ({ params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-1');
            return HttpResponse.json([
              {
                id: 'unit-1',
                subjectStageId: 'stg-1',
                name: 'Modul Aljabar',
                sequenceOrder: 1,
                unitType: 'THEORY'
              }
            ]);
          }
        )
      );

      const units = await curriculumApi.getLearningUnits('sub-1', 'stg-1');
      expect(units).toHaveLength(1);
      expect(units[0]?.id).toBe('unit-1');
    });

    it('createLearningUnit dispatches POST /api/acd/subjects/:id/stages/:stageId/units', async () => {
      let capturedBody: unknown = null;
      const payload: CreateLearningUnitRequest = {
        name: 'Modul Geometri',
        sequenceOrder: 2,
        unitType: 'PRACTICE',
        description: 'Praktik gambar ruang'
      };

      server.use(
        http.post(
          '/api/acd/subjects/:id/stages/:stageId/units',
          async ({ request, params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-1');
            capturedBody = await request.json();
            return HttpResponse.json({
              id: 'unit-2',
              subjectStageId: 'stg-1',
              ...payload
            });
          }
        )
      );

      const res = await curriculumApi.createLearningUnit('sub-1', 'stg-1', payload);
      expect(res.id).toBe('unit-2');
      expect(capturedBody).toEqual(payload);
    });

    it('updateLearningUnit dispatches PUT /api/acd/subjects/:id/stages/:stageId/units/:unitId', async () => {
      let capturedBody: unknown = null;
      const payload: UpdateLearningUnitRequest = {
        name: 'Modul Geometri Lanjut',
        sequenceOrder: 2,
        unitType: 'PRACTICE',
        description: 'Revisi deskripsi'
      };

      server.use(
        http.put(
          '/api/acd/subjects/:id/stages/:stageId/units/:unitId',
          async ({ request, params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-1');
            expect(params.unitId).toBe('unit-2');
            capturedBody = await request.json();
            return HttpResponse.json({
              id: 'unit-2',
              subjectStageId: 'stg-1',
              ...payload
            });
          }
        )
      );

      const res = await curriculumApi.updateLearningUnit('sub-1', 'stg-1', 'unit-2', payload);
      expect(res.name).toBe('Modul Geometri Lanjut');
      expect(capturedBody).toEqual(payload);
    });

    it('deleteLearningUnit dispatches DELETE /api/acd/subjects/:id/stages/:stageId/units/:unitId', async () => {
      let called = false;
      server.use(
        http.delete(
          '/api/acd/subjects/:id/stages/:stageId/units/:unitId',
          ({ params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-1');
            expect(params.unitId).toBe('unit-2');
            called = true;
            return new HttpResponse(null, { status: 204 });
          }
        )
      );

      await curriculumApi.deleteLearningUnit('sub-1', 'stg-1', 'unit-2');
      expect(called).toBe(true);
    });
  });

  describe('grade mappings', () => {
    it('mapGrade dispatches POST /api/acd/subjects/:id/stages/:stageId/grade-mappings', async () => {
      let capturedBody: unknown = null;
      const payload: MapSubjectStageGradeRequest = {
        schoolLevel: 'SMP',
        grade: '7'
      };

      server.use(
        http.post(
          '/api/acd/subjects/:id/stages/:stageId/grade-mappings',
          async ({ request, params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-1');
            capturedBody = await request.json();
            return HttpResponse.json(
              {
                id: 'gm-created',
                subjectStageId: 'stg-1',
                schoolLevel: payload.schoolLevel,
                grade: payload.grade
              },
              { status: 201 }
            );
          }
        )
      );

      const res = await curriculumApi.mapGrade('sub-1', 'stg-1', payload);
      expect(res.id).toBe('gm-created');
      expect(capturedBody).toEqual(payload);
    });

    it('removeGradeMapping dispatches DELETE /api/acd/subjects/:id/stages/:stageId/grade-mappings/:mappingId', async () => {
      let called = false;
      server.use(
        http.delete(
          '/api/acd/subjects/:id/stages/:stageId/grade-mappings/:mappingId',
          ({ params }) => {
            expect(params.id).toBe('sub-1');
            expect(params.stageId).toBe('stg-1');
            expect(params.mappingId).toBe('gm-1');
            called = true;
            return new HttpResponse(null, { status: 204 });
          }
        )
      );

      await curriculumApi.removeGradeMapping('sub-1', 'stg-1', 'gm-1');
      expect(called).toBe(true);
    });
  });

  describe('participations & batch enrollment', () => {
    it('batchEnroll dispatches POST /api/acd/participations/batch', async () => {
      let capturedBody: unknown = null;
      const payload: EnrollStudentsInSubjectStagesRequest = {
        subjectStageId: 'stg-1',
        studyGroupId: 'sg-1',
        operationalPeriodId: 'per-1'
      };

      server.use(
        http.post('/api/acd/participations/batch', async ({ request }) => {
          capturedBody = await request.json();
          return HttpResponse.json({
            enrolledCount: 30,
            skippedCount: 2
          });
        })
      );

      const res = await curriculumApi.batchEnroll(payload);
      expect(res.enrolledCount).toBe(30);
      expect(res.skippedCount).toBe(2);
      expect(capturedBody).toEqual(payload);
    });

    it('listParticipations dispatches GET /api/acd/participations with filters', async () => {
      let capturedUrl = '';
      server.use(
        http.get('/api/acd/participations', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json([
            {
              id: 'part-1',
              studentId: 'std-1',
              studentNis: '1001',
              studentName: 'Ahmad Siswa',
              subjectStageId: 'stg-1',
              subjectStageName: 'Tahap 1',
              subjectId: 'sub-1',
              subjectName: 'Matematika',
              studyGroupId: 'sg-1',
              studyGroupName: '7A',
              operationalPeriodId: 'per-1',
              operationalPeriodName: 'Ganjil 2026/2027',
              enrolledAt: '2026-07-15T08:00:00Z',
              status: 'ENROLLED'
            }
          ]);
        })
      );

      const list = await curriculumApi.listParticipations({
        subjectStageId: 'stg-1',
        studyGroupId: 'sg-1',
        operationalPeriodId: 'per-1'
      });

      expect(list).toHaveLength(1);
      expect(list[0]?.studentName).toBe('Ahmad Siswa');
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('subjectStageId')).toBe('stg-1');
      expect(url.searchParams.get('studyGroupId')).toBe('sg-1');
      expect(url.searchParams.get('operationalPeriodId')).toBe('per-1');
    });
  });

  describe('curriculumKeys', () => {
    it('produces deterministic TanStack query keys', () => {
      expect(curriculumKeys.all).toEqual(['academic', 'curriculum']);
      expect(curriculumKeys.subjects()).toEqual(['academic', 'curriculum', 'subjects']);
      expect(curriculumKeys.subject('sub-1')).toEqual([
        'academic',
        'curriculum',
        'subjects',
        'sub-1'
      ]);
      expect(curriculumKeys.stages('sub-1')).toEqual([
        'academic',
        'curriculum',
        'subjects',
        'sub-1',
        'stages'
      ]);
      expect(curriculumKeys.stage('sub-1', 'stg-1')).toEqual([
        'academic',
        'curriculum',
        'subjects',
        'sub-1',
        'stages',
        'stg-1'
      ]);
      expect(curriculumKeys.units('sub-1', 'stg-1')).toEqual([
        'academic',
        'curriculum',
        'subjects',
        'sub-1',
        'stages',
        'stg-1',
        'units'
      ]);
      expect(curriculumKeys.participations()).toEqual([
        'academic',
        'curriculum',
        'participations'
      ]);
      expect(
        curriculumKeys.participationsList({ subjectStageId: 'stg-1', studyGroupId: 'sg-1' })
      ).toEqual([
        'academic',
        'curriculum',
        'participations',
        { subjectStageId: 'stg-1', studyGroupId: 'sg-1' }
      ]);
    });
  });
});
