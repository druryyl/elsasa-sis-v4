import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import { configureHttpClient } from '@/shared/services/http';
import type { CandidateDto, CandidateSummaryDto } from '../../api/admissions.api';
import AdmissionsQueueView from '../AdmissionsQueueView.vue';

const server = setupServer();

const YEAR_ACTIVE_ID = '01900000-0000-7000-8000-000000000101';
const CANDIDATE_1_ID = '01900000-0000-7000-8000-000000000301';
const CANDIDATE_2_ID = '01900000-0000-7000-8000-000000000302';
const ENROLLED_STUDENT_ID = '01900000-0000-7000-8000-000000000501';

function createSampleCandidates(): CandidateSummaryDto[] {
  return [
    {
      id: CANDIDATE_1_ID,
      admissionYearId: YEAR_ACTIVE_ID,
      admissionYearName: '2025/2026',
      fullName: 'Muhammad Farhan',
      nickname: 'Farhan',
      dateOfBirth: '2013-04-12',
      gender: 'MALE',
      phone: '081234567890',
      email: 'farhan@example.com',
      address: 'Jl. Merdeka No. 12, Bandung',
      targetSchoolLevel: 'SMP',
      targetGrade: '7',
      admissionStatus: 'REGISTERED',
      registeredAt: '2026-05-10T08:00:00Z',
      decidedAt: null,
      decisionNotes: null,
      enrolledStudentId: null
    },
    {
      id: CANDIDATE_2_ID,
      admissionYearId: YEAR_ACTIVE_ID,
      admissionYearName: '2025/2026',
      fullName: 'Aisyah Putri',
      nickname: 'Aisyah',
      dateOfBirth: '2013-08-22',
      gender: 'FEMALE',
      phone: '081298765432',
      email: 'aisyah@example.com',
      address: 'Jl. Sudirman No. 45, Bandung',
      targetSchoolLevel: 'SMP',
      targetGrade: '7',
      admissionStatus: 'ACCEPTED',
      registeredAt: '2026-05-11T09:30:00Z',
      decidedAt: '2026-05-15T14:00:00Z',
      decisionNotes: 'Approved by principal interview committee',
      enrolledStudentId: null
    }
  ];
}

function createSampleCandidateDetail(id: string): CandidateDto {
  const base = createSampleCandidates().find((c) => c.id === id) ?? {
    id,
    admissionYearId: YEAR_ACTIVE_ID,
    admissionYearName: '2025/2026',
    fullName: 'Budi Kurniawan',
    nickname: 'Budi',
    dateOfBirth: '2012-10-15',
    gender: 'MALE',
    phone: '081234567899',
    email: 'budi@example.com',
    address: 'Jl. Dago No. 10',
    targetSchoolLevel: 'SMP',
    targetGrade: '7',
    admissionStatus: 'REGISTERED',
    registeredAt: '2026-05-18T10:00:00Z',
    decidedAt: null,
    decisionNotes: null,
    enrolledStudentId: null
  };
  return {
    ...base,
    guardians: [
      {
        id: '01900000-0000-7000-8000-000000000401',
        candidateId: id,
        fullName: 'Hendra Setiawan',
        relationshipType: 'FATHER',
        phone: '081234567800',
        email: 'hendra@example.com',
        address: base.address
      },
      {
        id: '01900000-0000-7000-8000-000000000402',
        candidateId: id,
        fullName: 'Rina Marlina',
        relationshipType: 'MOTHER',
        phone: '081234567801',
        email: 'rina@example.com',
        address: base.address
      }
    ]
  };
}

function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/admissions/queue',
        name: 'admissions.queue',
        component: AdmissionsQueueView
      },
      {
        path: '/students/directory',
        name: 'students.directory',
        component: { template: '<div>Students Directory</div>' }
      }
    ]
  });

  return router;
}

function setupDefaultMsw() {
  server.use(
    http.get('/api/opr/academic-years', () =>
      HttpResponse.json({
        items: [
          {
            id: YEAR_ACTIVE_ID,
            name: '2025/2026',
            status: 'ACTIVE'
          }
        ],
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/std/candidates', ({ request }) => {
      const url = new URL(request.url);
      const status = url.searchParams.get('Status');
      const all = createSampleCandidates();
      const filtered = status ? all.filter((c) => c.admissionStatus === status) : all;
      return HttpResponse.json({
        items: filtered,
        page: 1,
        pageSize: 20,
        totalCount: filtered.length,
        totalPages: 1
      });
    }),
    http.get('/api/std/candidates/:id', ({ params }) => {
      const id = params.id as string;
      return HttpResponse.json(createSampleCandidateDetail(id));
    }),
    http.get('/api/std/candidates/:id/next-nis', () =>
      HttpResponse.json({ suggestedNis: '20260001' })
    )
  );
}

async function mountAdmissionsView(
  permissions: string[] = [
    'Permissions.Students.Admissions.View',
    'Permissions.Students.Admissions.Register',
    'Permissions.Students.Admissions.Decide',
    'Permissions.Students.Admissions.Approve',
    'Permissions.Students.Admissions.Enroll'
  ],
  initialPath = '/admissions/queue'
) {
  configureAuthSessionReader({
    getUser: () => ({
      id: 'admin-1',
      username: 'admin',
      displayName: 'Admin User',
      roles: ['SCHOOL_ADMIN'],
      permissions
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: YEAR_ACTIVE_ID,
    activeAcademicYearName: '2025/2026'
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  const router = createTestRouter();
  await router.push(initialPath);
  await router.isReady();

  const wrapper = mount(AdmissionsQueueView, {
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

  return { wrapper, router, queryClient };
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
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

describe('AdmissionsQueueView.vue (SCR-STD-ADMISSIONS)', () => {
  it('renders WorkbenchLayout with status tabs, search filter, and scannable cards', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountAdmissionsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Admissions Processing Queue');
      expect(wrapper.find('[data-testid="admissions-status-tabs"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-registered"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-accepted"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-rejected"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-enrolled"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="queue-year-filter"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="queue-search-input"]').exists()).toBe(true);
    });

    // Default status REGISTERED displays candidate 1
    await vi.waitFor(() => {
      const cards = wrapper.findAll('[data-testid="candidate-card"]');
      expect(cards.length).toBe(1);
      expect(wrapper.find('[data-testid="candidate-card-name"]').text()).toBe('Muhammad Farhan');
      expect(wrapper.text()).toContain('SMP • Grade 7');
    });
  });

  it('switches status tabs, filters candidates, and syncs URL query parameters', async () => {
    setupDefaultMsw();
    const { wrapper, router } = await mountAdmissionsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Muhammad Farhan');
    });

    // Switch to ACCEPTED tab
    const acceptedTab = wrapper.find('[data-testid="tab-accepted"]');
    await acceptedTab.trigger('click');

    await vi.waitFor(() => {
      expect(router.currentRoute.value.query.status).toBe('ACCEPTED');
      expect(wrapper.text()).toContain('Aisyah Putri');
    });
  });

  it('selects a candidate card and displays full demographics, guardians, and audit in workbench', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountAdmissionsView();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="candidate-card"]').exists()).toBe(true);
    });

    // Click candidate card
    await wrapper.find('[data-testid="candidate-card"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="workbench-candidate-name"]').text()).toContain(
        'Muhammad Farhan'
      );
      expect(wrapper.text()).toContain('Personal Demographics');
      expect(wrapper.text()).toContain('Guardian Contacts');
      expect(wrapper.text()).toContain('Hendra Setiawan');
      expect(wrapper.text()).toContain('Rina Marlina');
      expect(wrapper.text()).toContain('Admission & Enrollment Audit');
    });
  });

  it('handles candidate intake via RegisterCandidateDrawer and enforces BR-STU-007 dynamic guardians', async () => {
    setupDefaultMsw();

    let createdPayload: unknown = null;
    server.use(
      http.post('/api/std/candidates', async ({ request }) => {
        createdPayload = await request.json();
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000000303',
            admissionYearId: YEAR_ACTIVE_ID,
            fullName: 'Budi Kurniawan',
            nickname: 'Budi',
            dateOfBirth: '2012-10-15',
            gender: 'MALE',
            targetSchoolLevel: 'SMP',
            targetGrade: '7',
            admissionStatus: 'REGISTERED',
            registeredAt: '2026-05-18T10:00:00Z',
            guardians: [
              {
                id: '01900000-0000-7000-8000-000000000403',
                fullName: 'Slamet Kurniawan',
                relationshipType: 'FATHER'
              }
            ]
          },
          { status: 201 }
        );
      })
    );

    const { wrapper } = await mountAdmissionsView();

    const registerBtn = wrapper.find('[data-testid="register-candidate-btn"]');
    expect(registerBtn.exists()).toBe(true);
    await registerBtn.trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="register-candidate-drawer"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="guardian-item"]').exists()).toBe(true);
    });

    // Verify BR-STU-007: Only 1 guardian, remove button must not be visible
    expect(wrapper.findAll('[data-testid="remove-guardian-button"]').length).toBe(0);

    // Add a second guardian
    await wrapper.find('[data-testid="add-guardian-button"]').trigger('click');
    expect(wrapper.findAll('[data-testid="guardian-item"]').length).toBe(2);
    // Now remove button should be present
    expect(wrapper.findAll('[data-testid="remove-guardian-button"]').length).toBe(2);
    // Remove the second guardian to test BR-STU-007 removal behavior
    const removeButtons = wrapper.findAll('[data-testid="remove-guardian-button"]');
    await removeButtons[1]?.trigger('click');
    expect(wrapper.findAll('[data-testid="guardian-item"]').length).toBe(1);

    // Wait for academic years options to load in select
    await vi.waitFor(() => {
      expect(wrapper.find(`option[value="${YEAR_ACTIVE_ID}"]`).exists()).toBe(true);
    });

    // Fill in candidate form
    await wrapper.find('[data-testid="candidate-year-select"]').setValue(YEAR_ACTIVE_ID);
    await wrapper.find('[data-testid="candidate-fullname-input"]').setValue('Budi Kurniawan');
    await wrapper.find('[data-testid="candidate-dob-input"]').setValue('2012-10-15');
    await wrapper.find('[data-testid="candidate-grade-input"]').setValue('7');
    await wrapper.find('[data-testid="guardian-fullname-input"]').setValue('Slamet Kurniawan');

    // Submit candidate intake
    await wrapper.find('#register-candidate-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdPayload).toBeTruthy();
      expect((createdPayload as { fullName: string }).fullName).toBe('Budi Kurniawan');
    });
  });

  it('records admission decision (accept / reject) with Tier 2 confirmation tone on reject', async () => {
    setupDefaultMsw();

    let decisionPayload: unknown = null;
    server.use(
      http.post('/api/std/candidates/:id/decision', async ({ request }) => {
        decisionPayload = await request.json();
        return HttpResponse.json({
          ...createSampleCandidateDetail(CANDIDATE_1_ID),
          admissionStatus: (decisionPayload as { decision: string }).decision,
          decidedAt: '2026-05-18T11:00:00Z',
          decisionNotes: (decisionPayload as { decisionNotes: string }).decisionNotes
        });
      })
    );

    const { wrapper } = await mountAdmissionsView();

    // Select candidate 1 (REGISTERED)
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="candidate-card"]').exists()).toBe(true);
    });
    await wrapper.find('[data-testid="candidate-card"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="workbench-decision-btn"]').exists()).toBe(true);
    });

    // Open decision modal
    await wrapper.find('[data-testid="workbench-decision-btn"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="admission-decision-modal"]').exists()).toBe(true);
    });

    // Select REJECTED decision
    await wrapper.find('[data-testid="decision-reject-btn"]').trigger('click');

    // Assert Tier 2 Confirmation Tone warning banner is rendered
    expect(wrapper.find('[data-testid="decision-reject-warning"]').exists()).toBe(true);

    // Enter decision notes and submit
    await wrapper
      .find('[data-testid="decision-notes-input"]')
      .setValue('Capacity full for Grade 7');
    await wrapper.find('[data-testid="decision-submit-btn"]').trigger('click');

    await vi.waitFor(() => {
      expect(decisionPayload).toEqual({
        decision: 'REJECTED',
        decisionNotes: 'Capacity full for Grade 7'
      });
    });
  });

  it('enrolls an accepted candidate with pre-filled sequential NIS, handles 409 conflict, and links to directory', async () => {
    setupDefaultMsw();

    let enrollAttempts = 0;
    server.use(
      http.post('/api/std/candidates/:id/enroll', async ({ request }) => {
        enrollAttempts++;
        const payload = (await request.json()) as { nis: string };

        if (payload.nis === '20260001' && enrollAttempts === 1) {
          // First attempt: simulate 409 duplicate NIS conflict
          return HttpResponse.json(
            {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
              title: 'Conflict',
              status: 409,
              detail: 'Student Identification Number (NIS) 20260001 is already assigned.'
            },
            { status: 409, headers: { 'Content-Type': 'application/problem+json' } }
          );
        }

        return HttpResponse.json(
          {
            id: ENROLLED_STUDENT_ID,
            candidateId: CANDIDATE_2_ID,
            nis: payload.nis,
            fullName: 'Aisyah Putri',
            status: 'ACTIVE',
            enrollmentDate: '2026-05-18'
          },
          { status: 201 }
        );
      })
    );

    const { wrapper, router } = await mountAdmissionsView(
      [
        'Permissions.Students.Admissions.View',
        'Permissions.Students.Admissions.Enroll',
        'Permissions.Students.Directory.View'
      ],
      '/admissions/queue?status=ACCEPTED'
    );

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="candidate-card"]').exists()).toBe(true);
    });

    // Select candidate 2 (ACCEPTED)
    await wrapper.find('[data-testid="candidate-card"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="workbench-enroll-btn"]').exists()).toBe(true);
    });

    // Open Enroll Modal
    await wrapper.find('[data-testid="workbench-enroll-btn"]').trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="enroll-candidate-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="enroll-impact-notice"]').exists()).toBe(true);
    });

    // Assert suggested NIS is pre-filled from GET /api/std/candidates/:id/next-nis
    await vi.waitFor(() => {
      const nisInput = wrapper.find<HTMLInputElement>('[data-testid="enroll-nis-input"]');
      expect(nisInput.element.value).toBe('20260001');
    });

    // Click submit with 20260001 (triggers 409 conflict)
    await wrapper.find('[data-testid="enroll-submit-button"]').trigger('click');

    // Assert clean 409 conflict alert banner is rendered
    await vi.waitFor(() => {
      const conflictAlert = wrapper.find('[data-testid="enroll-conflict-alert"]');
      expect(conflictAlert.exists()).toBe(true);
      expect(conflictAlert.text()).toContain('is already assigned');
    });

    // Override NIS to 20260002 and submit again
    await wrapper.find('[data-testid="enroll-nis-input"]').setValue('20260002');
    await wrapper.find('[data-testid="enroll-submit-button"]').trigger('click');

    // Modal closes and updates candidate to ENROLLED
    server.use(
      http.get('/api/std/candidates/:id', () =>
        HttpResponse.json({
          ...createSampleCandidateDetail(CANDIDATE_2_ID),
          admissionStatus: 'ENROLLED',
          enrolledStudentId: ENROLLED_STUDENT_ID
        })
      )
    );

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="enroll-candidate-modal"]').exists()).toBe(false);
    });

    // Post-enrollment deep-link button to Student Directory
    await vi.waitFor(() => {
      const viewDirBtn = wrapper.find('[data-testid="workbench-view-directory-btn"]');
      expect(viewDirBtn.exists()).toBe(true);
    });

    const pushSpy = vi.spyOn(router, 'push');
    await wrapper.find('[data-testid="workbench-view-directory-btn"]').trigger('click');

    expect(pushSpy).toHaveBeenCalledWith({
      name: 'students.directory',
      query: { studentId: ENROLLED_STUDENT_ID }
    });
  });
});
