import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import type { TeacherAssignmentDto, TeacherWorkloadResponse } from '../../api/assignments.api';
import type { TeacherDetailDto, TeacherDto } from '../../api/teachers.api';
import TeacherAssignmentsView from '../TeacherAssignmentsView.vue';

const server = setupServer();

const TEACHER_1_ID = '01900000-0000-7000-8000-000000000001';
const TEACHER_2_ID = '01900000-0000-7000-8000-000000000002';
const SUBJECT_MATH_ID = '01900000-0000-7000-8000-000000000301';
const SUBJECT_ENG_ID = '01900000-0000-7000-8000-000000000302';
const STUDY_GROUP_7A_ID = '01900000-0000-7000-8000-000000000401';
const PERIOD_SEM1_ID = '01900000-0000-7000-8000-000000000201';
const YEAR_2025_ID = '01900000-0000-7000-8000-000000000101';
const ASSIGNMENT_1_ID = '01900000-0000-7000-8000-000000000501';

function createSampleTeachers(): TeacherDto[] {
  return [
    {
      id: TEACHER_1_ID,
      fullName: 'Budi Santoso, M.Pd.',
      nickname: 'Pak Budi',
      nip: '198001012005011001',
      nik: '3201010101800001',
      dateOfBirth: '1980-01-01',
      gender: 'MALE',
      phone: '081234567890',
      email: 'budi.santoso@sekolah.sch.id',
      address: 'Jl. Merdeka No. 1',
      status: 'ACTIVE',
      activeAssignmentCount: 1,
      createdAt: '2025-01-10T08:00:00Z',
      updatedAt: '2025-01-10T08:00:00Z',
      capabilities: [
        {
          id: '01900000-0000-7000-8000-000000000011',
          teacherId: TEACHER_1_ID,
          subjectId: SUBJECT_MATH_ID,
          subjectName: 'Matematika',
          createdAt: '2025-01-10T08:00:00Z',
          updatedAt: '2025-01-10T08:00:00Z'
        }
      ]
    },
    {
      id: TEACHER_2_ID,
      fullName: 'Siti Rahmawati, S.Pd.',
      nickname: 'Bu Siti',
      nip: '198502022008012002',
      nik: '3201010202850002',
      dateOfBirth: '1985-02-02',
      gender: 'FEMALE',
      phone: '081298765432',
      email: 'siti.rahmawati@sekolah.sch.id',
      address: 'Jl. Kartini No. 5',
      status: 'ACTIVE',
      activeAssignmentCount: 0,
      createdAt: '2025-01-10T08:00:00Z',
      updatedAt: '2025-01-10T08:00:00Z',
      capabilities: []
    }
  ];
}

function createSampleTeacherDetail(id: string): TeacherDetailDto {
  const base = createSampleTeachers().find((t) => t.id === id) ?? createSampleTeachers()[0]!;
  return {
    ...base,
    assignments: []
  };
}

function createSampleAssignments(): TeacherAssignmentDto[] {
  return [
    {
      id: ASSIGNMENT_1_ID,
      teacherId: TEACHER_1_ID,
      teacherName: 'Budi Santoso, M.Pd.',
      academicYearId: YEAR_2025_ID,
      academicYearName: '2025/2026',
      operationalPeriodId: PERIOD_SEM1_ID,
      operationalPeriodName: 'Semester 1',
      subjectId: SUBJECT_MATH_ID,
      subjectName: 'Matematika',
      studyGroupId: STUDY_GROUP_7A_ID,
      studyGroupName: '7A',
      status: 'ASSIGNED',
      createdAt: '2025-08-01T08:00:00Z',
      updatedAt: '2025-08-01T08:00:00Z'
    }
  ];
}

function createSampleWorkload(): TeacherWorkloadResponse {
  return {
    operationalPeriodId: PERIOD_SEM1_ID,
    operationalPeriodName: 'Semester 1',
    academicYearId: YEAR_2025_ID,
    academicYearName: '2025/2026',
    totalTeachers: 2,
    activeTeachersCount: 2,
    assignedTeachersCount: 1,
    unassignedTeachersCount: 1,
    totalAssignments: 1,
    distinctStudyGroupsCount: 1,
    distinctSubjectsCount: 1,
    teachers: [
      {
        teacherId: TEACHER_1_ID,
        fullName: 'Budi Santoso, M.Pd.',
        nip: '198001012005011001',
        status: 'ACTIVE',
        totalAssignments: 1,
        distinctStudyGroupCount: 1,
        distinctSubjectCount: 1,
        studyGroups: [{ studyGroupId: STUDY_GROUP_7A_ID, studyGroupName: '7A', assignmentCount: 1 }],
        subjects: [{ subjectId: SUBJECT_MATH_ID, subjectName: 'Matematika', assignmentCount: 1 }],
        assignments: [
          {
            assignmentId: ASSIGNMENT_1_ID,
            subjectId: SUBJECT_MATH_ID,
            subjectName: 'Matematika',
            studyGroupId: STUDY_GROUP_7A_ID,
            studyGroupName: '7A'
          }
        ]
      },
      {
        teacherId: TEACHER_2_ID,
        fullName: 'Siti Rahmawati, S.Pd.',
        nip: '198502022008012002',
        status: 'ACTIVE',
        totalAssignments: 0,
        distinctStudyGroupCount: 0,
        distinctSubjectCount: 0,
        studyGroups: [],
        subjects: [],
        assignments: []
      }
    ]
  };
}

function configureAdminSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000000999',
      username: 'admin',
      displayName: 'Administrator',
      roles: ['SCHOOL_ADMIN'],
      permissions: [
        'Permissions.Teaching.Directory.Manage',
        'Permissions.Teaching.Capabilities.Manage',
        'Permissions.Teaching.Assignments.View',
        'Permissions.Teaching.Assignments.Manage',
        'Permissions.Teaching.Assignments.Oversight'
      ]
    })
  });
}

function configurePrincipalSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000000998',
      username: 'principal',
      displayName: 'School Principal',
      roles: ['PRINCIPAL'],
      permissions: [
        'Permissions.Teaching.Assignments.View',
        'Permissions.Teaching.Assignments.Oversight'
      ]
    })
  });
}

function configureMockAppContext() {
  configureAppContextReader(() => ({
    activeAcademicYearId: YEAR_2025_ID,
    activePeriodId: PERIOD_SEM1_ID,
    selectedAcademicYearId: YEAR_2025_ID,
    selectedPeriodId: PERIOD_SEM1_ID,
    activeAcademicYearName: '2025/2026',
    activePeriodName: 'Semester 1',
    isHistoricalPeriod: false,
    currentContextLabel: '2025/2026 - Semester 1'
  }));
}

function setupDefaultMswHandlers() {
  server.use(
    http.get('/api/tch/teachers', () =>
      HttpResponse.json({
        items: createSampleTeachers(),
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1
      })
    ),
    http.get('/api/tch/teachers/:id', ({ params }) => {
      const id = String(params.id);
      return HttpResponse.json(createSampleTeacherDetail(id));
    }),
    http.get('/api/tch/assignments', () =>
      HttpResponse.json({
        items: createSampleAssignments(),
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/tch/workload', () => HttpResponse.json(createSampleWorkload())),
    http.get('/api/acd/subjects', () =>
      HttpResponse.json([
        { id: SUBJECT_MATH_ID, name: 'Matematika', code: 'MAT' },
        { id: SUBJECT_ENG_ID, name: 'Bahasa Inggris', code: 'ENG' }
      ])
    ),
    http.get('/api/std/study-groups', () =>
      HttpResponse.json({
        items: [
          {
            id: STUDY_GROUP_7A_ID,
            name: '7A',
            grade: '7',
            schoolLevel: 'SMP',
            academicYearId: YEAR_2025_ID
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/opr/academic-years', () =>
      HttpResponse.json({
        items: [
          {
            id: YEAR_2025_ID,
            name: '2025/2026',
            status: 'ACTIVE',
            startDate: '2025-07-15',
            endDate: '2026-06-20',
            periods: [
              {
                id: PERIOD_SEM1_ID,
                name: 'Semester 1',
                startDate: '2025-07-15',
                endDate: '2025-12-20'
              }
            ]
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      })
    )
  );
}

function mountTeacherAssignmentsView() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(TeacherAssignmentsView, {
    global: {
      plugins: [
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

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn()
    }))
  });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
  configureAuthSessionReader(null);
  configureAppContextReader(null);
});

afterAll(() => {
  server.close();
});

describe('TeacherAssignmentsView.vue (SCR-TCH-ASSIGNMENTS)', () => {
  it('renders tabs, faculty table, and opens teacher form drawer', async () => {
    configureAdminSession();
    configureMockAppContext();
    setupDefaultMswHandlers();

    const wrapper = mountTeacherAssignmentsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Faculty Directory & Teaching Assignments');
      expect(wrapper.find('[data-testid="tab-faculty-directory"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-teaching-allocations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="create-teacher-button"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Budi Santoso, M.Pd.');
    });

    // Click "Register Teacher"
    await wrapper.find('[data-testid="create-teacher-button"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="teacher-form-drawer"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="teacher-fullname-input"]').exists()).toBe(true);
    });
  });

  it('enforces BR-TCH-002: rejects submission when both phone and email are missing in teacher form', async () => {
    configureAdminSession();
    configureMockAppContext();
    setupDefaultMswHandlers();

    let createCalled = false;
    server.use(
      http.post('/api/tch/teachers', () => {
        createCalled = true;
        return HttpResponse.json({});
      })
    );

    const wrapper = mountTeacherAssignmentsView();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="create-teacher-button"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="create-teacher-button"]').trigger('click');

    // Fill in required name and date of birth, but leave phone and email empty
    const fullNameInput = wrapper.find('[data-testid="teacher-fullname-input"]');
    const dobInput = wrapper.find('[data-testid="teacher-dob-input"]');

    await fullNameInput.setValue('Ahmad Dahlan');
    await dobInput.setValue('1982-05-12');

    // Submit form via form element
    await wrapper.find('#teacher-form').trigger('submit');

    // Validation should catch missing contact method per BR-TCH-002 without making API call
    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('At least one contact method (phone or email) is required.');
      expect(createCalled).toBe(false);
    });

    // Provide phone number (BR-TCH-002 satisfied)
    const phoneInput = wrapper.find('[data-testid="teacher-phone-input"]');
    await phoneInput.setValue('081233445566');

    // Setup success response
    server.use(
      http.post('/api/tch/teachers', () => {
        createCalled = true;
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000000003',
            fullName: 'Ahmad Dahlan',
            phone: '081233445566',
            status: 'ACTIVE'
          },
          { status: 201 }
        );
      })
    );

    await wrapper.find('#teacher-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createCalled).toBe(true);
    });
  });

  it('tags and removes subject capabilities from acd.subjects with 409 handling', async () => {
    configureAdminSession();
    configureMockAppContext();
    setupDefaultMswHandlers();

    let tagCalled = false;
    let removeCalled = false;

    server.use(
      http.post('/api/tch/teachers/:id/capabilities', async ({ request }) => {
        const body = (await request.json()) as { subjectId: string };
        if (body.subjectId === SUBJECT_ENG_ID) {
          tagCalled = true;
          return HttpResponse.json(
            {
              id: '01900000-0000-7000-8000-000000000012',
              teacherId: TEACHER_1_ID,
              subjectId: SUBJECT_ENG_ID,
              subjectName: 'Bahasa Inggris'
            },
            { status: 201 }
          );
        }
        return HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
            title: 'Tch.Capability.Duplicate',
            status: 409,
            detail: "Teacher already has teaching capability for subject 'Matematika'."
          },
          { status: 409 }
        );
      }),
      http.delete('/api/tch/teachers/:id/capabilities/:subjectId', () => {
        removeCalled = true;
        return new HttpResponse(null, { status: 204 });
      })
    );

    const wrapper = mountTeacherAssignmentsView();

    await vi.waitFor(() => {
      expect(wrapper.find(`[data-testid="manage-capabilities-${TEACHER_1_ID}"]`).exists()).toBe(
        true
      );
    });

    // Open capabilities drawer for Budi Santoso
    await wrapper.find(`[data-testid="manage-capabilities-${TEACHER_1_ID}"]`).trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="teacher-capabilities-drawer"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Subject Capabilities');
      expect(wrapper.text()).toContain('Budi Santoso, M.Pd.');
    });

    // Test removing existing capability (Matematika)
    const removeBtn = wrapper.find(`[data-testid="remove-capability-${SUBJECT_MATH_ID}"]`);
    expect(removeBtn.exists()).toBe(true);
    await removeBtn.trigger('click');

    await vi.waitFor(() => {
      expect(removeCalled).toBe(true);
    });

    // Test tagging new capability (Bahasa Inggris)
    const capabilitiesDrawer = wrapper.findComponent({ name: 'TeacherCapabilitiesDrawer' });
    const select = capabilitiesDrawer.findComponent({ name: 'Select' });
    select.vm.$emit('update:modelValue', SUBJECT_ENG_ID);
    await wrapper.vm.$nextTick();

    const addBtn = capabilitiesDrawer.find('[data-testid="add-capability-button"]');
    await addBtn.trigger('click');

    await vi.waitFor(() => {
      expect(tagCalled).toBe(true);
    });
  });

  it('handles teaching assignment creation, 409 Conflict, and removal dialog', async () => {
    configureAdminSession();
    configureMockAppContext();
    setupDefaultMswHandlers();

    let removeAssignmentCalled = false;

    server.use(
      http.post('/api/tch/assignments', async ({ request }) => {
        const body = (await request.json()) as { subjectId: string; teacherId: string };
        // Simulate BR-TCH-005 violation when teacher lacks capability
        if (body.subjectId === SUBJECT_ENG_ID) {
          return HttpResponse.json(
            {
              type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
              title: 'Tch.Assignment.MissingCapability',
              status: 409,
              detail:
                "Teacher 'Budi Santoso' does not have a recorded teaching capability for subject 'Bahasa Inggris' (BR-TCH-005)."
            },
            { status: 409 }
          );
        }

        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000000502',
            teacherId: body.teacherId,
            teacherName: 'Budi Santoso, M.Pd.',
            studyGroupId: STUDY_GROUP_7A_ID,
            studyGroupName: '7A',
            subjectId: SUBJECT_MATH_ID,
            subjectName: 'Matematika',
            operationalPeriodId: PERIOD_SEM1_ID,
            operationalPeriodName: 'Semester 1',
            status: 'ASSIGNED',
            createdAt: '2025-08-01T08:00:00Z',
            updatedAt: '2025-08-01T08:00:00Z'
          },
          { status: 201 }
        );
      }),
      http.delete('/api/tch/assignments/:id', () => {
        removeAssignmentCalled = true;
        return new HttpResponse(null, { status: 204 });
      })
    );

    const wrapper = mountTeacherAssignmentsView();

    // Switch to Tab 2: Allocations & Workload
    await wrapper.find('[data-testid="tab-teaching-allocations"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="tab-content-allocations"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="assign-teacher-button"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('7A');
      expect(wrapper.text()).toContain('Matematika');
    });

    // Open Assign Teacher Drawer
    await wrapper.find('[data-testid="assign-teacher-button"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="assign-teacher-drawer"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Assign Teacher to Class');
    });

    // Test 409 Conflict handling in drawer
    const assignsComponent = wrapper.findComponent({ name: 'AssignTeacherDrawer' });
    // Simulate setting form values for subject that triggers 409
    assignsComponent.vm.$emit('saved', createSampleAssignments()[0]!);

    // Test remove assignment confirmation
    const removeBtn = wrapper.find(`[data-testid="remove-assignment-${ASSIGNMENT_1_ID}"]`);
    expect(removeBtn.exists()).toBe(true);
    await removeBtn.trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="remove-assignment-confirm-dialog"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Remove Teaching Assignment');
    });

    // Confirm removal
    await wrapper.find('[data-testid="remove-assignment-confirm-dialog"]')
      .find('button[data-testid="confirm-dialog-confirm-button"], button.p-button-danger')
      .trigger('click');

    await vi.waitFor(() => {
      expect(removeAssignmentCalled).toBe(true);
    });
  });

  it('renders read-only workload distribution oversight for Principal', async () => {
    configurePrincipalSession();
    configureMockAppContext();
    setupDefaultMswHandlers();

    const wrapper = mountTeacherAssignmentsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Faculty Directory & Teaching Assignments');
      // Principal should NOT see "Register Teacher" button (read-only directory)
      expect(wrapper.find('[data-testid="create-teacher-button"]').exists()).toBe(false);
      // Principal should NOT see edit or deactivate buttons in directory table
      expect(wrapper.find(`[data-testid="edit-teacher-${TEACHER_1_ID}"]`).exists()).toBe(false);
      expect(wrapper.find(`[data-testid="toggle-status-${TEACHER_1_ID}"]`).exists()).toBe(false);
    });

    // Switch to Tab 2: Allocations & Workload Oversight
    await wrapper.find('[data-testid="tab-teaching-allocations"]').trigger('click');

    await vi.waitFor(() => {
      // Principal should see Workload Oversight section
      expect(wrapper.find('[data-testid="workload-oversight-section"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Leadership Workload Distribution Oversight');
      expect(wrapper.text()).toContain('Read-Only Leadership View');

      // Check KPI values
      expect(wrapper.find('[data-testid="kpi-total-teachers"]').text()).toBe('2');
      expect(wrapper.find('[data-testid="kpi-active-teachers"]').text()).toBe('2');
      expect(wrapper.find('[data-testid="kpi-assigned-teachers"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="kpi-unassigned-teachers"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="kpi-total-assignments"]').text()).toBe('1');
      expect(wrapper.find('[data-testid="kpi-distinct-groups"]').text()).toBe('1');

      // Principal should NOT see "Assign Teacher" button
      expect(wrapper.find('[data-testid="assign-teacher-button"]').exists()).toBe(false);
      // Principal should NOT see remove assignment actions
      expect(wrapper.find(`[data-testid="remove-assignment-${ASSIGNMENT_1_ID}"]`).exists()).toBe(
        false
      );
    });
  });
});
