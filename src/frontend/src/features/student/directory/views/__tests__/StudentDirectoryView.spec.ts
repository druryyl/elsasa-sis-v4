import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import StudentDirectoryView from '../StudentDirectoryView.vue';

const server = setupServer();

const STUDENT_1_ID = '01900000-0000-7000-8000-000000000001';
const STUDENT_2_ID = '01900000-0000-7000-8000-000000000002';
const GUARDIAN_1_ID = '01900000-0000-7000-8000-000000000101';
const GUARDIAN_2_ID = '01900000-0000-7000-8000-000000000102';
const STUDY_GROUP_7A_ID = '01900000-0000-7000-8000-000000000701';
const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000901';
const TEACHER_BUDI_ID = '01900000-0000-7000-8000-000000000801';

function createSampleStudents() {
  return [
    {
      id: STUDENT_1_ID,
      candidateId: null,
      nis: '20260001',
      nisn: '0012345678',
      fullName: 'Ahmad Dahlan',
      nickname: 'Ahmad',
      dateOfBirth: '2012-05-10',
      gender: 'MALE',
      phone: '081234567890',
      email: 'ahmad@example.com',
      address: 'Jl. Merdeka No. 1',
      enrollmentDate: '2026-07-01',
      status: 'ACTIVE',
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: STUDY_GROUP_7A_ID,
      studyGroupName: '7A',
      primaryGuardianName: 'Bapak Dahlan',
      primaryGuardianPhone: '081298765432',
      guardians: [
        {
          id: GUARDIAN_1_ID,
          fullName: 'Bapak Dahlan',
          relationshipType: 'FATHER',
          phone: '081298765432',
          email: 'dahlan@example.com'
        }
      ],
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    },
    {
      id: STUDENT_2_ID,
      candidateId: null,
      nis: '20260002',
      nisn: '0012345679',
      fullName: 'Siti Fatimah',
      nickname: 'Siti',
      dateOfBirth: '2012-08-20',
      gender: 'FEMALE',
      phone: '081234567891',
      email: 'siti@example.com',
      address: 'Jl. Sudirman No. 5',
      enrollmentDate: '2026-07-01',
      status: 'ACTIVE',
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: STUDY_GROUP_7A_ID,
      studyGroupName: '7A',
      primaryGuardianName: 'Ibu Fatimah',
      primaryGuardianPhone: '081298765433',
      guardians: [
        {
          id: GUARDIAN_2_ID,
          fullName: 'Ibu Fatimah',
          relationshipType: 'MOTHER',
          phone: '081298765433',
          email: 'fatimah@example.com'
        }
      ],
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    }
  ];
}

function createSampleStudyGroups() {
  return [
    {
      id: STUDY_GROUP_7A_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      schoolLevel: 'SMP',
      grade: '7',
      name: '7A',
      homeroomTeacherId: TEACHER_BUDI_ID,
      homeroomTeacherName: 'Budi Santoso, S.Pd',
      studentCount: 2,
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    }
  ];
}

function createSampleRoster() {
  return {
    id: STUDY_GROUP_7A_ID,
    academicYearId: ACADEMIC_YEAR_ID,
    academicYearName: '2026/2027',
    schoolLevel: 'SMP',
    grade: '7',
    name: '7A',
    homeroomTeacherId: TEACHER_BUDI_ID,
    homeroomTeacherName: 'Budi Santoso, S.Pd',
    studentCount: 2,
    students: [
      {
        id: STUDENT_1_ID,
        placementId: '01900000-0000-7000-8000-000000000771',
        nis: '20260001',
        nisn: '0012345678',
        fullName: 'Ahmad Dahlan',
        nickname: 'Ahmad',
        dateOfBirth: '2012-05-10',
        gender: 'MALE',
        phone: '081234567890',
        email: 'ahmad@example.com',
        address: 'Jl. Merdeka No. 1',
        enrollmentDate: '2026-07-01',
        status: 'ACTIVE',
        guardians: [
          {
            id: GUARDIAN_1_ID,
            studentId: STUDENT_1_ID,
            fullName: 'Bapak Dahlan',
            relationshipType: 'FATHER',
            phone: '081298765432',
            email: 'dahlan@example.com',
            address: 'Jl. Merdeka No. 1',
            createdAt: '2026-07-01T08:00:00Z',
            updatedAt: '2026-07-01T08:00:00Z'
          }
        ]
      },
      {
        id: STUDENT_2_ID,
        placementId: '01900000-0000-7000-8000-000000000772',
        nis: '20260002',
        nisn: '0012345679',
        fullName: 'Siti Fatimah',
        nickname: 'Siti',
        dateOfBirth: '2012-08-20',
        gender: 'FEMALE',
        phone: '081234567891',
        email: 'siti@example.com',
        address: 'Jl. Sudirman No. 5',
        enrollmentDate: '2026-07-01',
        status: 'ACTIVE',
        guardians: [
          {
            id: GUARDIAN_2_ID,
            studentId: STUDENT_2_ID,
            fullName: 'Ibu Fatimah',
            relationshipType: 'MOTHER',
            phone: '081298765433',
            email: 'fatimah@example.com',
            address: 'Jl. Sudirman No. 5',
            createdAt: '2026-07-01T08:00:00Z',
            updatedAt: '2026-07-01T08:00:00Z'
          }
        ]
      }
    ]
  };
}

function createStudentDetail(studentId: string, guardianCount = 1) {
  const gList = [
    {
      id: GUARDIAN_1_ID,
      studentId,
      fullName: 'Bapak Dahlan',
      relationshipType: 'FATHER',
      phone: '081298765432',
      email: 'dahlan@example.com',
      address: 'Jl. Merdeka No. 1',
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    }
  ];

  if (guardianCount > 1) {
    gList.push({
      id: GUARDIAN_2_ID,
      studentId,
      fullName: 'Ibu Dahlan',
      relationshipType: 'MOTHER',
      phone: '081298765433',
      email: 'ibu.dahlan@example.com',
      address: 'Jl. Merdeka No. 1',
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    });
  }

  return {
    id: studentId,
    candidateId: null,
    nis: '20260001',
    nisn: '0012345678',
    fullName: 'Ahmad Dahlan',
    nickname: 'Ahmad',
    dateOfBirth: '2012-05-10',
    gender: 'MALE',
    phone: '081234567890',
    email: 'ahmad@example.com',
    address: 'Jl. Merdeka No. 1',
    enrollmentDate: '2026-07-01',
    status: 'ACTIVE',
    createdAt: '2026-07-01T08:00:00Z',
    updatedAt: '2026-07-01T08:00:00Z',
    guardians: gList,
    placements: [
      {
        id: '01900000-0000-7000-8000-000000000771',
        studentId,
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        schoolLevel: 'SMP',
        grade: '7',
        studyGroupId: STUDY_GROUP_7A_ID,
        studyGroupName: '7A',
        homeroomTeacherId: TEACHER_BUDI_ID,
        homeroomTeacherName: 'Budi Santoso, S.Pd'
      }
    ]
  };
}

async function mountStudentDirectoryView(initialQuery: Record<string, string> = {}) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/students/directory',
        name: 'students.directory',
        component: StudentDirectoryView
      }
    ]
  });

  await router.push({ path: '/students/directory', query: initialQuery });
  await router.isReady();

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(StudentDirectoryView, {
    global: {
      plugins: [
        router,
        [VueQueryPlugin, { queryClient }],
        ToastService,
        [
          PrimeVue,
          {
            theme: {
              preset: Aura
            }
          }
        ]
      ],
      stubs: {
        Teleport: true
      }
    }
  });
}

class MockResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

beforeAll(() => {
  if (typeof globalThis.ResizeObserver === 'undefined') {
    globalThis.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;
  }
  if (typeof window.matchMedia === 'undefined') {
    window.matchMedia = vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn()
    }));
  }
  server.listen({ onUnhandledRequest: 'bypass' });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
  configureAuthSessionReader(null);
});

afterAll(() => {
  server.close();
});

describe('StudentDirectoryView.vue (SCR-STD-DIRECTORY)', () => {
  it('renders Full Admin Directory Mode with 2 tabs and student roster', async () => {
    configureAuthSessionReader({
      getUser: () => ({
        id: 'admin-id',
        username: 'admin',
        displayName: 'School Administrator',
        roles: ['SCHOOL_ADMIN'],
        permissions: [
          'Permissions.Students.Directory.View',
          'Permissions.Students.Directory.Manage',
          'Permissions.Students.Placements.Manage'
        ]
      }),
      getPermissions: () => [
        'Permissions.Students.Directory.View',
        'Permissions.Students.Directory.Manage',
        'Permissions.Students.Placements.Manage'
      ],
      hasPermission: (perm: string) => [
        'Permissions.Students.Directory.View',
        'Permissions.Students.Directory.Manage',
        'Permissions.Students.Placements.Manage'
      ].includes(perm)
    });

    server.use(
      http.get('/api/std/students', () =>
        HttpResponse.json({
          items: createSampleStudents(),
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      ),
      http.get('/api/std/study-groups', () =>
        HttpResponse.json({
          items: createSampleStudyGroups(),
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: ACADEMIC_YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = await mountStudentDirectoryView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Student Directory & Placements');
      expect(wrapper.text()).toContain('Student Roster Directory');
      expect(wrapper.text()).toContain('Study Groups & Placement Registry');
      expect(wrapper.text()).toContain('Ahmad Dahlan');
      expect(wrapper.text()).toContain('20260001');
      expect(wrapper.text()).toContain('Class 7A');
      expect(wrapper.text()).toContain('Bapak Dahlan');
    });
  });

  it('renders Homeroom Teacher Scoped Mode (UC-HRM-05) with scoped badge and emergency phone numbers', async () => {
    configureAuthSessionReader({
      getUser: () => ({
        id: 'teacher-user-id',
        username: 'teacher.budi',
        displayName: 'Budi Santoso',
        roles: ['HOMEROOM_TEACHER', 'TEACHER'],
        teacherId: TEACHER_BUDI_ID,
        permissions: [
          'Permissions.Students.Roster.ViewOwn',
          'Permissions.Operations.Calendar.View'
        ]
      }),
      getPermissions: () => [
        'Permissions.Students.Roster.ViewOwn',
        'Permissions.Operations.Calendar.View'
      ],
      hasPermission: (perm: string) =>
        ['Permissions.Students.Roster.ViewOwn', 'Permissions.Operations.Calendar.View'].includes(perm)
    });

    server.use(
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: ACADEMIC_YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/std/study-groups', () =>
        HttpResponse.json({
          items: createSampleStudyGroups(),
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get(`/api/std/study-groups/${STUDY_GROUP_7A_ID}/roster`, () =>
        HttpResponse.json(createSampleRoster())
      )
    );

    const wrapper = await mountStudentDirectoryView();

    await vi.waitFor(() => {
      // 1. Scoped header badge must be prominently displayed
      expect(wrapper.text()).toContain('Homeroom Student Directory');
      expect(wrapper.text()).toContain('My Homeroom Class (7A)');

      // 2. Roster table displays students
      expect(wrapper.text()).toContain('Ahmad Dahlan');
      expect(wrapper.text()).toContain('Siti Fatimah');

      // 3. Guardian emergency phone numbers clearly highlighted (UC-HRM-05)
      expect(wrapper.text()).toContain('081298765432');
      expect(wrapper.text()).toContain('081298765433');
      expect(wrapper.text()).toContain('Bapak Dahlan');
      expect(wrapper.text()).toContain('Ibu Fatimah');

      // 4. Admin controls must be hidden
      expect(wrapper.text()).not.toContain('New Study Group');
      expect(wrapper.find('button[aria-label="View dossier"]').exists()).toBe(false);
    });
  });

  it('enforces last-guardian protection (BR-STU-003): disables delete button when only 1 guardian remains', async () => {
    configureAuthSessionReader({
      getUser: () => ({
        id: 'admin-id',
        username: 'admin',
        displayName: 'Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: [
          'Permissions.Students.Directory.View',
          'Permissions.Students.Directory.Manage',
          'Permissions.Students.Placements.Manage'
        ]
      }),
      getPermissions: () => [
        'Permissions.Students.Directory.View',
        'Permissions.Students.Directory.Manage',
        'Permissions.Students.Placements.Manage'
      ],
      hasPermission: () => true
    });

    server.use(
      http.get('/api/std/students', () =>
        HttpResponse.json({
          items: createSampleStudents(),
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      ),
      http.get(`/api/std/students/${STUDENT_1_ID}`, () =>
        HttpResponse.json(createStudentDetail(STUDENT_1_ID, 1))
      ),
      http.get('/api/std/study-groups', () =>
        HttpResponse.json({
          items: createSampleStudyGroups(),
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: ACADEMIC_YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = await mountStudentDirectoryView({ studentId: STUDENT_1_ID });

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Guardians & Contacts (1)');
      expect(wrapper.text()).toContain('Ahmad Dahlan');
    });

    // Switch to Guardians sub-tab
    const guardianTabButton = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Guardians & Contacts'));

    if (guardianTabButton) {
      await guardianTabButton.trigger('click');
    }

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Parents & Guardians');
      expect(wrapper.text()).toContain('Bapak Dahlan');
    });

    // Check delete guardian button
    const deleteButton = wrapper.find('button[aria-label="Remove guardian"]');
    expect(deleteButton.exists()).toBe(true);
    expect(deleteButton.attributes('disabled')).toBeDefined();
  });

  it('handles 409 Conflict when attempting to delete last guardian or updating duplicate NISN', async () => {
    configureAuthSessionReader({
      getUser: () => ({
        id: 'admin-id',
        username: 'admin',
        displayName: 'Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: [
          'Permissions.Students.Directory.View',
          'Permissions.Students.Directory.Manage',
          'Permissions.Students.Placements.Manage'
        ]
      }),
      getPermissions: () => [
        'Permissions.Students.Directory.View',
        'Permissions.Students.Directory.Manage',
        'Permissions.Students.Placements.Manage'
      ],
      hasPermission: () => true
    });

    server.use(
      http.get('/api/std/students', () =>
        HttpResponse.json({
          items: createSampleStudents(),
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      ),
      http.get(`/api/std/students/${STUDENT_1_ID}`, () =>
        HttpResponse.json(createStudentDetail(STUDENT_1_ID, 2))
      ),
      http.delete(`/api/std/students/${STUDENT_1_ID}/guardians/${GUARDIAN_1_ID}`, () =>
        HttpResponse.json(
          {
            type: 'https://httpstatuses.com/409',
            title: 'Conflict',
            status: 409,
            detail: 'Cannot remove the last guardian. Every student must have at least one registered parent/guardian (BR-STU-003).'
          },
          { status: 409 }
        )
      ),
      http.get('/api/std/study-groups', () =>
        HttpResponse.json({
          items: createSampleStudyGroups(),
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: ACADEMIC_YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = await mountStudentDirectoryView({ studentId: STUDENT_1_ID });

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Guardians & Contacts (2)');
    });

    const guardianTabButton = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Guardians & Contacts'));

    if (guardianTabButton) {
      await guardianTabButton.trigger('click');
    }

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Ibu Dahlan');
    });

    // Delete button should now be enabled since there are 2 guardians
    const deleteButtons = wrapper.findAll('button[aria-label="Remove guardian"]');
    expect(deleteButtons.length).toBeGreaterThan(0);
    const firstDeleteBtn = deleteButtons[0]!;
    expect(firstDeleteBtn.attributes('disabled')).toBeUndefined();

    // Trigger delete confirmation
    await firstDeleteBtn.trigger('click');

    // Confirm in dialog
    const confirmButton = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Remove Guardian'));
    if (confirmButton) {
      await confirmButton.trigger('click');
    }

    await vi.waitFor(() => {
      // Expect 409 conflict banner
      expect(wrapper.text()).toContain('Cannot remove the last guardian');
    });
  });

  it('renders Study Groups & Placement Registry tab with roster drawer trigger', async () => {
    configureAuthSessionReader({
      getUser: () => ({
        id: 'admin-id',
        username: 'admin',
        displayName: 'Admin',
        roles: ['SCHOOL_ADMIN'],
        permissions: [
          'Permissions.Students.Directory.View',
          'Permissions.Students.Directory.Manage',
          'Permissions.Students.Placements.Manage'
        ]
      }),
      getPermissions: () => [
        'Permissions.Students.Directory.View',
        'Permissions.Students.Directory.Manage',
        'Permissions.Students.Placements.Manage'
      ],
      hasPermission: () => true
    });

    server.use(
      http.get('/api/std/students', () =>
        HttpResponse.json({
          items: createSampleStudents(),
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      ),
      http.get('/api/std/study-groups', () =>
        HttpResponse.json({
          items: createSampleStudyGroups(),
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get(`/api/std/study-groups/${STUDY_GROUP_7A_ID}/roster`, () =>
        HttpResponse.json(createSampleRoster())
      ),
      http.get('/api/opr/academic-years', () =>
        HttpResponse.json({
          items: [{ id: ACADEMIC_YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/tch/teachers', () =>
        HttpResponse.json({
          items: [{ id: TEACHER_BUDI_ID, fullName: 'Budi Santoso, S.Pd', nip: '198501012010011001' }],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = await mountStudentDirectoryView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Study Groups & Placement Registry');
    });

    // Switch to Study Groups tab
    const studyGroupTab = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Study Groups & Placement Registry'));
    if (studyGroupTab) {
      await studyGroupTab.trigger('click');
    }

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Class 7A');
      expect(wrapper.text()).toContain('Budi Santoso, S.Pd');
      expect(wrapper.text()).toContain('2 students');
    });

    // Click Roster button on the row
    const rosterButton = wrapper
      .findAll('button')
      .find((b) => b.attributes('aria-label') === 'View roster');
    expect(rosterButton?.exists()).toBe(true);

    if (rosterButton) {
      await rosterButton.trigger('click');
    }

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Class Roster — 7A');
      expect(wrapper.text()).toContain('Ahmad Dahlan');
    });
  });
});
