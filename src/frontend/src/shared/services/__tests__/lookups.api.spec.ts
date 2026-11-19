import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  fetchAcademicYearsLookup,
  fetchSubjectsLookup,
  fetchActiveTeachersLookup,
  fetchStudyGroupsLookup,
  lookupsApi,
  lookupKeys,
  academicYearsLookupQuery,
  subjectsLookupQuery,
  activeTeachersLookupQuery,
  studyGroupsLookupQuery
} from '@/shared/services/lookups.api';

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

describe('lookups.api.ts', () => {
  describe('fetchAcademicYearsLookup', () => {
    it('calls /api/opr/academic-years with default query params and returns items array', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/opr/academic-years', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            items: [
              {
                id: 'ay-1',
                name: '2026/2027',
                startDate: '2026-07-01',
                endDate: '2027-06-30',
                status: 'ACTIVE'
              }
            ],
            totalCount: 1,
            page: 1,
            pageSize: 100,
            totalPages: 1
          });
        })
      );

      const items = await fetchAcademicYearsLookup();

      expect(items).toHaveLength(1);
      expect(items[0]).toEqual({
        id: 'ay-1',
        name: '2026/2027',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'ACTIVE'
      });
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('Page')).toBe('1');
      expect(url.searchParams.get('PageSize')).toBe('100');
      expect(url.searchParams.get('SortField')).toBe('StartDate');
      expect(url.searchParams.get('SortOrder')).toBe('desc');
    });

    it('passes custom search and pagination params and falls back to empty array if items is null', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/opr/academic-years', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            items: null,
            totalCount: 0,
            page: 2,
            pageSize: 20
          });
        })
      );

      const items = await fetchAcademicYearsLookup({
        search: ' 2025 ',
        page: 2,
        pageSize: 20,
        sortField: 'Name',
        sortOrder: 'asc'
      });

      expect(items).toEqual([]);
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('Search')).toBe('2025');
      expect(url.searchParams.get('Page')).toBe('2');
      expect(url.searchParams.get('PageSize')).toBe('20');
      expect(url.searchParams.get('SortField')).toBe('Name');
      expect(url.searchParams.get('SortOrder')).toBe('asc');
    });
  });

  describe('fetchSubjectsLookup', () => {
    it('calls /api/acd/subjects and returns subject items array', async () => {
      server.use(
        http.get('/api/acd/subjects', () => {
          return HttpResponse.json([
            { id: 'sub-1', name: 'Mathematics', code: 'MATH' },
            { id: 'sub-2', name: 'Bahasa Indonesia', code: 'BIND' }
          ]);
        })
      );

      const subjects = await fetchSubjectsLookup();

      expect(subjects).toHaveLength(2);
      expect(subjects[0]).toEqual({ id: 'sub-1', name: 'Mathematics', code: 'MATH' });
      expect(subjects[1]).toEqual({ id: 'sub-2', name: 'Bahasa Indonesia', code: 'BIND' });
    });

    it('handles empty response gracefully', async () => {
      server.use(
        http.get('/api/acd/subjects', () => {
          return HttpResponse.json([]);
        })
      );

      const subjects = await fetchSubjectsLookup();
      expect(subjects).toEqual([]);
    });
  });

  describe('fetchActiveTeachersLookup', () => {
    it('calls /api/tch/teachers enforcing Status=ACTIVE and returns teacher items array', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/tch/teachers', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            items: [
              {
                id: 'tch-1',
                fullName: 'Budi Santoso',
                nickname: 'Pak Budi',
                nip: '198501012010011001',
                status: 'ACTIVE',
                activeAssignmentCount: 3
              }
            ],
            totalCount: 1,
            page: 1,
            pageSize: 100
          });
        })
      );

      const teachers = await fetchActiveTeachersLookup({
        subjectId: 'sub-1',
        search: 'Budi'
      });

      expect(teachers).toHaveLength(1);
      expect(teachers[0]?.fullName).toBe('Budi Santoso');
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('Status')).toBe('ACTIVE');
      expect(url.searchParams.get('SubjectId')).toBe('sub-1');
      expect(url.searchParams.get('Search')).toBe('Budi');
      expect(url.searchParams.get('Page')).toBe('1');
      expect(url.searchParams.get('PageSize')).toBe('100');
    });

    it('falls back to empty array if response items is null', async () => {
      server.use(
        http.get('/api/tch/teachers', () => {
          return HttpResponse.json({
            items: null,
            totalCount: 0
          });
        })
      );

      const teachers = await fetchActiveTeachersLookup();
      expect(teachers).toEqual([]);
    });
  });

  describe('fetchStudyGroupsLookup', () => {
    it('calls /api/std/study-groups with filter parameters and returns study group items array', async () => {
      let capturedUrl = '';

      server.use(
        http.get('/api/std/study-groups', ({ request }) => {
          capturedUrl = request.url;
          return HttpResponse.json({
            items: [
              {
                id: 'sg-1',
                academicYearId: 'ay-1',
                academicYearName: '2026/2027',
                schoolLevel: 'SMP',
                grade: '7',
                name: '7A',
                homeroomTeacherId: 'tch-1',
                homeroomTeacherName: 'Budi Santoso',
                studentCount: 28
              }
            ],
            totalCount: 1,
            page: 1,
            pageSize: 100
          });
        })
      );

      const studyGroups = await fetchStudyGroupsLookup({
        academicYearId: 'ay-1',
        schoolLevel: 'SMP',
        grade: '7'
      });

      expect(studyGroups).toHaveLength(1);
      expect(studyGroups[0]?.name).toBe('7A');
      expect(studyGroups[0]?.homeroomTeacherName).toBe('Budi Santoso');
      const url = new URL(capturedUrl);
      expect(url.searchParams.get('AcademicYearId')).toBe('ay-1');
      expect(url.searchParams.get('SchoolLevel')).toBe('SMP');
      expect(url.searchParams.get('Grade')).toBe('7');
    });

    it('falls back to empty array if response items is null', async () => {
      server.use(
        http.get('/api/std/study-groups', () => {
          return HttpResponse.json({
            items: null,
            totalCount: 0
          });
        })
      );

      const studyGroups = await fetchStudyGroupsLookup();
      expect(studyGroups).toEqual([]);
    });
  });

  describe('lookupsApi export object', () => {
    it('contains all 4 lookup functions', () => {
      expect(lookupsApi.fetchAcademicYearsLookup).toBe(fetchAcademicYearsLookup);
      expect(lookupsApi.fetchSubjectsLookup).toBe(fetchSubjectsLookup);
      expect(lookupsApi.fetchActiveTeachersLookup).toBe(fetchActiveTeachersLookup);
      expect(lookupsApi.fetchStudyGroupsLookup).toBe(fetchStudyGroupsLookup);
    });
  });

  describe('TanStack Query keys and Query Options', () => {
    it('builds deterministic TanStack Query keys', () => {
      expect(lookupKeys.academicYears()).toEqual(['lookups', 'academic-years', {}]);
      expect(lookupKeys.academicYears({ search: '2026' })).toEqual([
        'lookups',
        'academic-years',
        { search: '2026' }
      ]);
      expect(lookupKeys.subjects()).toEqual(['lookups', 'subjects']);
      expect(lookupKeys.activeTeachers()).toEqual(['lookups', 'active-teachers', {}]);
      expect(lookupKeys.activeTeachers({ subjectId: 'sub-1' })).toEqual([
        'lookups',
        'active-teachers',
        { subjectId: 'sub-1' }
      ]);
      expect(lookupKeys.studyGroups({ academicYearId: 'ay-1' })).toEqual([
        'lookups',
        'study-groups',
        { academicYearId: 'ay-1' }
      ]);
    });

    it('builds valid queryOptions configurations', async () => {
      server.use(
        http.get('/api/acd/subjects', () => HttpResponse.json([{ id: 'sub-1', name: 'Math' }]))
      );

      const subjectsOptions = subjectsLookupQuery();
      expect(subjectsOptions.queryKey).toEqual(['lookups', 'subjects']);
      expect(typeof subjectsOptions.queryFn).toBe('function');
      if (typeof subjectsOptions.queryFn === 'function') {
        const queryFn = subjectsOptions.queryFn as () => Promise<unknown[]>;
        const data = await queryFn();
        expect(data).toHaveLength(1);
      }

      const ayOptions = academicYearsLookupQuery({ search: '2026' });
      expect(ayOptions.queryKey).toEqual(['lookups', 'academic-years', { search: '2026' }]);

      const tchOptions = activeTeachersLookupQuery({ subjectId: 'sub-1' });
      expect(tchOptions.queryKey).toEqual(['lookups', 'active-teachers', { subjectId: 'sub-1' }]);

      const sgOptions = studyGroupsLookupQuery({ academicYearId: 'ay-1' });
      expect(sgOptions.queryKey).toEqual([
        'lookups',
        'study-groups',
        { academicYearId: 'ay-1' }
      ]);
    });
  });
});
