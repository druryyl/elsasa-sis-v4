import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import type { SubjectResponse } from '../../api/curriculum.api';
import CurriculumSetupView from '../CurriculumSetupView.vue';

const server = setupServer();

const SUBJECT_1_ID = '018f3a2b-1111-7000-8000-000000000001';
const STAGE_1_ID = '018f3a2b-2222-7000-8000-000000000001';
const UNIT_1_ID = '018f3a2b-3333-7000-8000-000000000001';
const GRADE_MAPPING_1_ID = '018f3a2b-4444-7000-8000-000000000001';

const STUDY_GROUP_7A_ID = '018f3a2b-5555-7000-8000-000000000001';
const OPERATIONAL_PERIOD_ID = '018f3a2b-6666-7000-8000-000000000001';
const ACADEMIC_YEAR_ID = '018f3a2b-7777-7000-8000-000000000001';

function createSampleCurriculum(): SubjectResponse[] {
  return [
    {
      id: SUBJECT_1_ID,
      name: 'Matematika',
      code: 'MTK',
      stages: [
        {
          id: STAGE_1_ID,
          subjectId: SUBJECT_1_ID,
          subjectName: 'Matematika',
          name: 'Fase D (Kelas 7)',
          sequenceOrder: 1,
          description: 'Matematika Tingkat Pertama',
          units: [
            {
              id: UNIT_1_ID,
              subjectStageId: STAGE_1_ID,
              name: 'Bilangan Bulat & Pecahan',
              sequenceOrder: 1,
              unitType: 'MODULE',
              description: 'Operasi dasar bilangan bulat'
            }
          ],
          gradeMappings: [
            {
              id: GRADE_MAPPING_1_ID,
              subjectStageId: STAGE_1_ID,
              schoolLevel: 'SMP',
              grade: '7'
            }
          ]
        }
      ]
    }
  ];
}

function configureAdminSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '018f3a2b-9999-7000-8000-000000000001',
      username: 'admin',
      displayName: 'Administrator',
      roles: ['SCHOOL_ADMIN'],
      permissions: [
        'Permissions.Academic.Curriculum.View',
        'Permissions.Academic.Curriculum.Manage'
      ]
    })
  });
}

function configureTeacherSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '018f3a2b-9999-7000-8000-000000000002',
      username: 'teacher.budi',
      displayName: 'Budi Santoso',
      roles: ['TEACHER'],
      permissions: ['Permissions.Academic.Curriculum.View']
    })
  });
}

function setupAppContext() {
  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    activePeriodId: OPERATIONAL_PERIOD_ID,
    selectedAcademicYearId: ACADEMIC_YEAR_ID,
    selectedPeriodId: OPERATIONAL_PERIOD_ID,
    activeAcademicYearName: '2026/2027',
    activePeriodName: 'Semester Ganjil',
    isHistoricalPeriod: false,
    currentContextLabel: '2026/2027 - Semester Ganjil'
  }));
}

function mountCurriculumSetupView() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(CurriculumSetupView, {
    global: {
      plugins: [
        [VueQueryPlugin, { queryClient }],
        ToastService,
        [PrimeVue, { theme: { preset: Aura } }]
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
});

afterAll(() => {
  server.close();
});

describe('CurriculumSetupView.vue (SCR-ACD-CURRICULUM)', () => {
  it('renders 3-level hierarchical tree with Subjects, Stages, and Learning Units (UC-ADM-17)', async () => {
    configureAdminSession();
    setupAppContext();

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      })
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    expect(wrapper.find('[data-testid="curriculum-setup-view"]').exists()).toBe(true);

    // Subject node
    expect(wrapper.find(`[data-testid="subject-node-${SUBJECT_1_ID}"]`).exists()).toBe(true);
    expect(wrapper.text()).toContain('Matematika');
    expect(wrapper.text()).toContain('MTK');

    // Stage node
    expect(wrapper.find(`[data-testid="stage-node-${STAGE_1_ID}"]`).exists()).toBe(true);
    expect(wrapper.text()).toContain('Fase D (Kelas 7)');

    // Learning unit node
    expect(wrapper.find(`[data-testid="unit-node-${UNIT_1_ID}"]`).exists()).toBe(true);
    expect(wrapper.text()).toContain('Bilangan Bulat & Pecahan');
    expect(wrapper.text()).toContain('MODULE');

    // Right pane hero card
    expect(wrapper.find('[data-testid="selected-stage-hero"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="grade-mapping-canvas"]').exists()).toBe(true);
  });

  it('supports expanding and collapsing subject and stage nodes', async () => {
    configureAdminSession();
    setupAppContext();

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      })
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    // Learning unit should be initially visible
    expect(wrapper.find(`[data-testid="unit-node-${UNIT_1_ID}"]`).exists()).toBe(true);

    // Collapse stage units
    const toggleStageBtn = wrapper.find(`[data-testid="toggle-stage-${STAGE_1_ID}"]`);
    expect(toggleStageBtn.exists()).toBe(true);
    await toggleStageBtn.trigger('click');
    await flushPromises();

    // Learning unit should now be hidden
    expect(wrapper.find(`[data-testid="unit-node-${UNIT_1_ID}"]`).exists()).toBe(false);

    // Re-expand stage units
    await toggleStageBtn.trigger('click');
    await flushPromises();
    expect(wrapper.find(`[data-testid="unit-node-${UNIT_1_ID}"]`).exists()).toBe(true);
  });

  it('opens Add Subject modal and creates a new subject', async () => {
    configureAdminSession();
    setupAppContext();

    let createdPayload: unknown = null;

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      }),
      http.post('/api/acd/subjects', async ({ request }) => {
        createdPayload = await request.json();
        return HttpResponse.json({
          id: '018f3a2b-1111-7000-8000-000000000002',
          name: 'Bahasa Indonesia',
          code: 'BIN',
          stages: []
        });
      })
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    // Click Add Subject button
    const addSubjectBtn = wrapper.find('[data-testid="add-subject-btn"]');
    expect(addSubjectBtn.exists()).toBe(true);
    await addSubjectBtn.trigger('click');
    await flushPromises();

    // Fill form
    const nameInput = wrapper.find('[data-testid="subject-name-input"]');
    const codeInput = wrapper.find('[data-testid="subject-code-input"]');
    expect(nameInput.exists()).toBe(true);
    expect(codeInput.exists()).toBe(true);

    await nameInput.setValue('Bahasa Indonesia');
    await codeInput.setValue('BIN');

    // Submit form
    await wrapper.find('#subject-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdPayload).toEqual({
        name: 'Bahasa Indonesia',
        code: 'BIN'
      });
    });
  });

  it('manages Grade Mapping matrix: adds mapping and removes mapping with Tier 2 confirmation', async () => {
    configureAdminSession();
    setupAppContext();

    let mappedPayload: unknown = null;
    let removedMappingId = '';

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      }),
      http.post(
        `/api/acd/subjects/${SUBJECT_1_ID}/stages/${STAGE_1_ID}/grade-mappings`,
        async ({ request }) => {
          mappedPayload = await request.json();
          return HttpResponse.json({
            id: '018f3a2b-4444-7000-8000-000000000002',
            subjectStageId: STAGE_1_ID,
            schoolLevel: 'SMP',
            grade: '8'
          });
        }
      ),
      http.delete(
        `/api/acd/subjects/${SUBJECT_1_ID}/stages/${STAGE_1_ID}/grade-mappings/${GRADE_MAPPING_1_ID}`,
        () => {
          removedMappingId = GRADE_MAPPING_1_ID;
          return new HttpResponse(null, { status: 204 });
        }
      )
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    // Click SMP Grade 8 to map it
    const grade8Pill = wrapper.find('[data-testid="grade-pill-SMP-8"]');
    expect(grade8Pill.exists()).toBe(true);
    await grade8Pill.trigger('click');
    await flushPromises();

    expect(mappedPayload).toEqual({
      schoolLevel: 'SMP',
      grade: '8'
    });

    // Remove existing SMP Grade 7 mapping (triggers Tier 2 confirmation dialog)
    const activeMapping = wrapper.find(`[data-testid="active-mapping-${GRADE_MAPPING_1_ID}"]`);
    expect(activeMapping.exists()).toBe(true);
    const removeBtn = activeMapping.find('button');
    expect(removeBtn.exists()).toBe(true);
    await removeBtn.trigger('click');
    await flushPromises();

    // ConfirmActionDialog should appear
    expect(wrapper.text()).toContain('Remove Grade Mapping');
    const confirmBtn = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Remove Mapping'));
    expect(confirmBtn).toBeDefined();
    await confirmBtn!.trigger('click');
    await flushPromises();

    expect(removedMappingId).toBe(GRADE_MAPPING_1_ID);
  });

  it('deletes a Learning Unit via Tier 2 ConfirmActionDialog', async () => {
    configureAdminSession();
    setupAppContext();

    let deletedUnitId = '';

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      }),
      http.delete(
        `/api/acd/subjects/${SUBJECT_1_ID}/stages/${STAGE_1_ID}/units/${UNIT_1_ID}`,
        () => {
          deletedUnitId = UNIT_1_ID;
          return new HttpResponse(null, { status: 204 });
        }
      )
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    const deleteUnitBtn = wrapper.find(`[data-testid="delete-unit-btn-${UNIT_1_ID}"]`);
    expect(deleteUnitBtn.exists()).toBe(true);
    await deleteUnitBtn.trigger('click');
    await flushPromises();

    // Verify Tier 2 Dialog
    expect(wrapper.text()).toContain('Delete Learning Unit');
    const confirmDeleteBtn = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Delete Unit'));
    expect(confirmDeleteBtn).toBeDefined();
    await confirmDeleteBtn!.trigger('click');
    await flushPromises();

    expect(deletedUnitId).toBe(UNIT_1_ID);
  });

  it('executes batch student enrollment workflow with Tier 3 Pre-Flight Impact Summary Modal (UC-ADM-18, D-17)', async () => {
    configureAdminSession();
    setupAppContext();

    let batchEnrollPayload: unknown = null;

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      }),
      http.get('/api/std/study-groups', () => {
        return HttpResponse.json({
          items: [
            {
              id: STUDY_GROUP_7A_ID,
              name: 'Kelas 7A',
              schoolLevel: 'SMP',
              grade: '7'
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 100
        });
      }),
      http.get(`/api/std/study-groups/${STUDY_GROUP_7A_ID}/roster`, () => {
        return HttpResponse.json({
          studyGroupId: STUDY_GROUP_7A_ID,
          studyGroupName: 'Kelas 7A',
          students: [
            {
              studentId: '018f3a2b-aaaa-7000-8000-000000000001',
              fullName: 'Ahmad Fauzi',
              nis: '2026001',
              nisn: '0091234561'
            },
            {
              studentId: '018f3a2b-bbbb-7000-8000-000000000002',
              fullName: 'Budi Santoso',
              nis: '2026002',
              nisn: '0091234562'
            }
          ]
        });
      }),
      http.post('/api/acd/participations/batch', async ({ request }) => {
        batchEnrollPayload = await request.json();
        return HttpResponse.json({
          enrolledCount: 2,
          skippedCount: 0
        });
      })
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    // Click "Enroll Study Group" action button on stage hero card
    const enrollBtn = wrapper.find('[data-testid="stage-enroll-btn"]');
    expect(enrollBtn.exists()).toBe(true);
    await enrollBtn.trigger('click');
    await flushPromises();

    // Pre-Flight Modal should be visible
    const modal = wrapper.find('[data-testid="batch-enroll-modal"]');
    expect(modal.exists()).toBe(true);
    expect(modal.text()).toContain('Batch Student Enrollment (Pre-Flight)');
    expect(modal.text()).toContain('Tier 3 Impact Summary');

    // Duplicate skip explanation
    const skipExplanation = wrapper.find('[data-testid="skip-explanation"]');
    expect(skipExplanation.exists()).toBe(true);
    expect(skipExplanation.text()).toContain('Idempotent Duplicate Skip Behavior');

    // Select target study group
    const select = wrapper.findComponent({ name: 'Select' });
    expect(select.exists()).toBe(true);
    await select.vm.$emit('update:modelValue', STUDY_GROUP_7A_ID);
    await flushPromises();

    // Eligible student preview count
    const eligibleCount = wrapper.find('[data-testid="eligible-count"]');
    expect(eligibleCount.exists()).toBe(true);
    expect(eligibleCount.text()).toContain('2 Students');

    // Confirm button
    const confirmBtn = wrapper.find('[data-testid="confirm-enroll-button"]');
    expect(confirmBtn.exists()).toBe(true);
    expect(confirmBtn.text()).toContain('Enroll 2 Students');

    await confirmBtn.trigger('click');
    await flushPromises();

    expect(batchEnrollPayload).toEqual({
      operationalPeriodId: OPERATIONAL_PERIOD_ID,
      subjectStageId: STAGE_1_ID,
      studyGroupId: STUDY_GROUP_7A_ID
    });
  });

  it('opens Add Stage modal and creates a new subject stage', async () => {
    configureAdminSession();
    setupAppContext();

    let createdStagePayload: unknown = null;

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      }),
      http.post(
        `/api/acd/subjects/${SUBJECT_1_ID}/stages`,
        async ({ request }) => {
          createdStagePayload = await request.json();
          return HttpResponse.json({
            id: '018f3a2b-2222-7000-8000-000000000002',
            subjectId: SUBJECT_1_ID,
            name: 'Fase E (Kelas 10)',
            sequenceOrder: 2,
            description: 'Tingkat Menengah',
            units: [],
            gradeMappings: []
          });
        }
      )
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    const addStageBtn = wrapper.find(`[data-testid="add-stage-btn-${SUBJECT_1_ID}"]`);
    expect(addStageBtn.exists()).toBe(true);
    await addStageBtn.trigger('click');
    await flushPromises();

    const nameInput = wrapper.find('[data-testid="stage-name-input"]');
    expect(nameInput.exists()).toBe(true);
    await nameInput.setValue('Fase E (Kelas 10)');

    await wrapper.find('#subject-stage-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdStagePayload).toEqual({
        name: 'Fase E (Kelas 10)',
        sequenceOrder: 2,
        description: null
      });
    });
  });

  it('opens Add Unit modal and creates a new modular learning unit', async () => {
    configureAdminSession();
    setupAppContext();

    let createdUnitPayload: unknown = null;

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      }),
      http.post(
        `/api/acd/subjects/${SUBJECT_1_ID}/stages/${STAGE_1_ID}/units`,
        async ({ request }) => {
          createdUnitPayload = await request.json();
          return HttpResponse.json({
            id: '018f3a2b-3333-7000-8000-000000000002',
            subjectStageId: STAGE_1_ID,
            name: 'Geometri Transformasi',
            unitType: 'MODULE',
            sequenceOrder: 2,
            description: null
          });
        }
      )
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    const addUnitBtn = wrapper.find(`[data-testid="add-unit-btn-${STAGE_1_ID}"]`);
    expect(addUnitBtn.exists()).toBe(true);
    await addUnitBtn.trigger('click');
    await flushPromises();

    const unitNameInput = wrapper.find('[data-testid="unit-name-input"]');
    expect(unitNameInput.exists()).toBe(true);
    await unitNameInput.setValue('Geometri Transformasi');

    await wrapper.find('#learning-unit-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdUnitPayload).toEqual({
        name: 'Geometri Transformasi',
        unitType: 'MODULE',
        sequenceOrder: 2,
        description: null
      });
    });
  });

  it('enforces read-only view ("Hide by Role") for Teacher without manage permission', async () => {
    configureTeacherSession();
    setupAppContext();

    server.use(
      http.get('/api/acd/subjects', () => {
        return HttpResponse.json(createSampleCurriculum());
      }),
      http.get('/api/acd/participations', () => {
        return HttpResponse.json([]);
      })
    );

    const wrapper = mountCurriculumSetupView();
    await flushPromises();

    // Teacher cannot add subject
    expect(wrapper.find('[data-testid="add-subject-btn"]').exists()).toBe(false);

    // Teacher cannot add stage
    expect(wrapper.find(`[data-testid="add-stage-btn-${SUBJECT_1_ID}"]`).exists()).toBe(false);

    // Teacher cannot add unit
    expect(wrapper.find(`[data-testid="add-unit-btn-${STAGE_1_ID}"]`).exists()).toBe(false);

    // Teacher cannot enroll study group
    expect(wrapper.find('[data-testid="stage-enroll-btn"]').exists()).toBe(false);
  });
});
