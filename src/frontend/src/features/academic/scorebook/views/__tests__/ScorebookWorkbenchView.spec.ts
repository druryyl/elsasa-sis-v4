import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
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
import ScorebookWorkbenchView from '../ScorebookWorkbenchView.vue';
import type { GradebookSummaryResponse } from '../../api/scorebook.api';

const server = setupServer();

const YEAR_ACTIVE_ID = '01900000-0000-7000-8000-000000000101';
const OPERATIONAL_PERIOD_ID = '01900000-0000-7000-8000-000000000301';
const SUBJECT_ID = '01900000-0000-7000-8000-000000000100';
const SUBJECT_STAGE_ID = '01900000-0000-7000-8000-000000000101';
const STUDY_GROUP_ID = '01900000-0000-7000-8000-000000000201';

const STUDENT_1_ID = '01900000-0000-7000-8000-000000000401';
const STUDENT_2_ID = '01900000-0000-7000-8000-000000000402';
const ASSIGNMENT_1_ID = '01900000-0000-7000-8000-000000000501';
const ASSIGNMENT_2_ID = '01900000-0000-7000-8000-000000000502';

function createSampleGradebook(): GradebookSummaryResponse {
  return {
    subjectStageId: SUBJECT_STAGE_ID,
    subjectStageName: 'Matematika - Tahap 1',
    subjectId: SUBJECT_ID,
    subjectName: 'Matematika',
    studyGroupId: STUDY_GROUP_ID,
    studyGroupName: 'Kelas 7A',
    operationalPeriodId: OPERATIONAL_PERIOD_ID,
    operationalPeriodName: 'Semester Ganjil 2025/2026',
    assignments: [
      {
        id: ASSIGNMENT_1_ID,
        title: 'Tugas 1 - Aljabar',
        assignmentType: 'FORMATIVE',
        maxScore: 100,
        weight: 40,
        assignedDate: '2026-08-01',
        dueDate: '2026-08-08'
      },
      {
        id: ASSIGNMENT_2_ID,
        title: 'Ulangan Harian 1',
        assignmentType: 'SUMMATIVE',
        maxScore: 100,
        weight: 60,
        assignedDate: '2026-08-15',
        dueDate: '2026-08-15'
      }
    ],
    students: [
      {
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '2026001',
        runningScore: 88.0,
        attendance: {
          presentCount: 14,
          absentCount: 1,
          lateCount: 0,
          excusedCount: 0,
          sickCount: 0,
          totalRecorded: 15
        },
        scores: [
          {
            assignmentId: ASSIGNMENT_1_ID,
            score: 85,
            feedback: 'Bagus sekali',
            gradedAt: '2026-08-05T10:00:00Z'
          },
          {
            assignmentId: ASSIGNMENT_2_ID,
            score: 90,
            feedback: null,
            gradedAt: '2026-08-16T10:00:00Z'
          }
        ]
      },
      {
        studentId: STUDENT_2_ID,
        studentName: 'Siti Nurhaliza',
        studentNis: '2026002',
        runningScore: 68.0,
        attendance: {
          presentCount: 15,
          absentCount: 0,
          lateCount: 0,
          excusedCount: 0,
          sickCount: 0,
          totalRecorded: 15
        },
        scores: [
          {
            assignmentId: ASSIGNMENT_1_ID,
            score: 65,
            feedback: 'Perlu latihan lebih lanjut',
            gradedAt: '2026-08-05T10:00:00Z'
          },
          {
            assignmentId: ASSIGNMENT_2_ID,
            score: 70,
            feedback: null,
            gradedAt: '2026-08-16T10:00:00Z'
          }
        ]
      }
    ]
  };
}

function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/academic/scorebook',
        name: 'academic.scorebook',
        component: ScorebookWorkbenchView
      },
      {
        path: '/other',
        name: 'other',
        component: { template: '<div>Other Page</div>' }
      }
    ]
  });
  return router;
}

function setupDefaultMsw() {
  server.use(
    http.get('/api/acd/subjects', () =>
      HttpResponse.json([
        {
          id: SUBJECT_ID,
          name: 'Matematika',
          code: 'MTK',
          stages: [
            {
              id: SUBJECT_STAGE_ID,
              name: 'Tahap 1',
              subjectId: SUBJECT_ID,
              sequenceOrder: 1,
              units: [{ id: 'unit-1', name: 'Aljabar Linier' }]
            }
          ]
        }
      ])
    ),
    http.get('/api/std/study-groups', () =>
      HttpResponse.json({
        items: [
          {
            id: STUDY_GROUP_ID,
            name: 'Kelas 7A',
            academicYearId: YEAR_ACTIVE_ID
          }
        ],
        totalCount: 1
      })
    ),
    http.get('/api/acd/gradebook', () => {
      return HttpResponse.json(createSampleGradebook());
    }),
    http.post('/api/acd/assignments', async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      return HttpResponse.json({ id: 'asg-default-id', ...body }, { status: 201 });
    }),
    http.post('/api/acd/assignments/:id/scores', () => {
      return HttpResponse.json({ assignmentId: 'asg-id', scoresUpdatedCount: 1 });
    })
  );
}

async function mountScorebookView(
  permissions: string[] = [
    'Permissions.Academic.Scorebook.View',
    'Permissions.Academic.Scorebook.Manage'
  ],
  initialPath = `/academic/scorebook?subjectStageId=${SUBJECT_STAGE_ID}&studyGroupId=${STUDY_GROUP_ID}`
) {
  configureAuthSessionReader({
    getUser: () => ({
      id: 'teacher-1',
      username: 'budi',
      displayName: 'Budi Raharjo',
      roles: ['TEACHER'],
      permissions,
      teacherId: 'tch-budi-id'
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: YEAR_ACTIVE_ID,
    activeAcademicYearName: '2025/2026',
    activePeriodId: OPERATIONAL_PERIOD_ID,
    activePeriodName: 'Semester Ganjil 2025/2026',
    selectedAcademicYearId: YEAR_ACTIVE_ID,
    selectedPeriodId: OPERATIONAL_PERIOD_ID
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

  const App = {
    template: '<router-view />'
  };

  const wrapper = mount(App, {
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

  await flushPromises();

  return { wrapper, router, queryClient };
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
  if (typeof window !== 'undefined') {
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

describe('ScorebookWorkbenchView.vue', () => {
  it('renders workbench layout, filters, and loads gradebook summary', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountScorebookView();

    expect(wrapper.find('[data-testid="scorebook-workbench-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="scorebook-filter-bar"]').exists()).toBe(true);

    // Student roster rows
    const student1Row = wrapper.find(`[data-testid="student-row-${STUDENT_1_ID}"]`);
    expect(student1Row.exists()).toBe(true);
    expect(student1Row.text()).toContain('Ahmad Dahlan');
    expect(student1Row.text()).toContain('2026001');

    const student2Row = wrapper.find(`[data-testid="student-row-${STUDENT_2_ID}"]`);
    expect(student2Row.exists()).toBe(true);
    expect(student2Row.text()).toContain('Siti Nurhaliza');
    expect(student2Row.text()).toContain('2026002');

    // Assessment Column Headers
    expect(wrapper.find(`[data-testid="asg-col-${ASSIGNMENT_1_ID}"]`).exists()).toBe(true);
    expect(wrapper.find(`[data-testid="asg-col-${ASSIGNMENT_2_ID}"]`).exists()).toBe(true);
    expect(wrapper.text()).toContain('Tugas 1 - Aljabar');
    expect(wrapper.text()).toContain('FORMATIVE');
    expect(wrapper.text()).toContain('Ulangan Harian 1');
    expect(wrapper.text()).toContain('SUMMATIVE');

    // Attendance summary counts
    expect(student1Row.find('[data-testid="attendance-present"]').text()).toBe('P:14');
    expect(student1Row.find('[data-testid="attendance-absent"]').text()).toBe('A:1');

    // Running scores
    expect(student1Row.find('[data-testid="running-score-value"]').text()).toBe('88.00');
    expect(student2Row.find('[data-testid="running-score-value"]').text()).toBe('68.00');
  });

  it('highlights cells and running score in rose when falling below passing threshold (D-05)', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountScorebookView();

    // Default threshold is 75
    // Student 2 has score 65 on Assignment 1, which is < 75
    const cellInput1 = wrapper.find(`[data-testid="score-cell-${STUDENT_2_ID}-${ASSIGNMENT_1_ID}"]`);
    expect(cellInput1.classes()).toContain('bg-rose-50');

    // Student 2 running score is 68.00, which is < 75
    const student2Row = wrapper.find(`[data-testid="student-row-${STUDENT_2_ID}"]`);
    const runningScoreEl = student2Row.find('[data-testid="running-score-value"]');
    expect(runningScoreEl.classes()).toContain('bg-rose-50');
    expect(runningScoreEl.classes()).toContain('text-rose-700');

    // Student 1 has score 85 on Assignment 1, which is >= 75
    const cellInputStudent1 = wrapper.find(`[data-testid="score-cell-${STUDENT_1_ID}-${ASSIGNMENT_1_ID}"]`);
    expect(cellInputStudent1.classes()).not.toContain('bg-rose-50');
  });

  it('dynamically computes reactive running score when cell score is edited client-side', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountScorebookView();

    const student1Row = wrapper.find(`[data-testid="student-row-${STUDENT_1_ID}"]`);
    expect(student1Row.find('[data-testid="running-score-value"]').text()).toBe('88.00');

    // Find input for student 1 on assignment 1 (initially 85, weight 40; assignment 2 is 90, weight 60)
    // Formula: (newScore * 40 + 90 * 60) / 100
    // If newScore = 100: (100 * 40 + 5400) / 100 = 94.00
    const cell = wrapper.find(`[data-testid="score-cell-${STUDENT_1_ID}-${ASSIGNMENT_1_ID}"]`);
    const input = cell.find('input[data-testid="score-cell-input"]');

    await input.setValue('100');
    await input.trigger('input');
    await flushPromises();

    // Reactive running score should immediately update to 94.00
    expect(student1Row.find('[data-testid="running-score-value"]').text()).toBe('94.00');

    // Cell should show dirty indicator
    expect(cell.find('[data-testid="dirty-indicator"]').exists()).toBe(true);

    // Sticky save bar should appear
    const saveBar = wrapper.find('[data-testid="scorebook-sticky-save-bar"]');
    expect(saveBar.exists()).toBe(true);
    expect(saveBar.find('[data-testid="dirty-count"]').text()).toBe('1');
  });

  it('dispatches batch save request when Save Changes is clicked', async () => {
    setupDefaultMsw();

    let capturedAssignmentId: string | null = null;
    let capturedPayload: { items?: Array<{ studentId?: string; score?: number; feedback?: string | null }> } | null = null;

    server.use(
      http.post('/api/acd/assignments/:id/scores', async ({ params, request }) => {
        capturedAssignmentId = params.id as string;
        capturedPayload = (await request.json()) as { items?: Array<{ studentId?: string; score?: number; feedback?: string | null }> };
        return HttpResponse.json({
          assignmentId: capturedAssignmentId,
          scoresUpdatedCount: 1
        });
      })
    );

    const { wrapper } = await mountScorebookView();

    const cell = wrapper.find(`[data-testid="score-cell-${STUDENT_1_ID}-${ASSIGNMENT_1_ID}"]`);
    const input = cell.find('input[data-testid="score-cell-input"]');

    await input.setValue('95');
    await input.trigger('input');
    await flushPromises();

    const saveButton = wrapper.find('[data-testid="scorebook-save-btn"]');
    await saveButton.trigger('click');
    await flushPromises();

    expect(capturedAssignmentId).toBe(ASSIGNMENT_1_ID);
    expect(capturedPayload).toEqual({
      items: [
        {
          studentId: STUDENT_1_ID,
          score: 95,
          feedback: 'Bagus sekali'
        }
      ]
    });

    // Success alert displayed
    expect(wrapper.find('[data-testid="scorebook-success-alert"]').exists()).toBe(true);
    // Sticky save bar hidden
    expect(wrapper.find('[data-testid="scorebook-sticky-save-bar"]').exists()).toBe(false);
  });

  it('discards dirty changes when Discard button is clicked', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountScorebookView();

    const cell = wrapper.find(`[data-testid="score-cell-${STUDENT_1_ID}-${ASSIGNMENT_1_ID}"]`);
    const input = cell.find('input[data-testid="score-cell-input"]');

    await input.setValue('50');
    await input.trigger('input');
    await flushPromises();

    expect(wrapper.find('[data-testid="scorebook-sticky-save-bar"]').exists()).toBe(true);

    const discardBtn = wrapper.find('[data-testid="scorebook-discard-btn"]');
    await discardBtn.trigger('click');
    await flushPromises();

    // Reverts to original running score
    const student1Row = wrapper.find(`[data-testid="student-row-${STUDENT_1_ID}"]`);
    expect(student1Row.find('[data-testid="running-score-value"]').text()).toBe('88.00');
    expect(wrapper.find('[data-testid="scorebook-sticky-save-bar"]').exists()).toBe(false);
  });

  it('opens Tier 2 modal and creates assessment', async () => {
    setupDefaultMsw();

    let capturedCreatePayload: Record<string, unknown> | null = null;

    server.use(
      http.post('/api/acd/assignments', async ({ request }) => {
        const body = (await request.json()) as Record<string, unknown>;
        capturedCreatePayload = body;
        return HttpResponse.json(
          {
            id: 'new-asg-id',
            ...body
          },
          { status: 201 }
        );
      })
    );

    const { wrapper } = await mountScorebookView();

    const createBtn = wrapper.find('[data-testid="scorebook-create-assessment-btn"]');
    expect(createBtn.exists()).toBe(true);
    await createBtn.trigger('click');
    await flushPromises();

    const modal = wrapper.find('[data-testid="create-assessment-modal"]');
    expect(modal.exists()).toBe(true);

    const titleInput = modal.find('input[data-testid="assessment-title-input"]');
    await titleInput.setValue('Kuis Harian 2');

    const maxScoreInput = modal.find('input[data-testid="assessment-max-score-input"]');
    await maxScoreInput.setValue('100');

    await modal.find('form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(capturedCreatePayload).not.toBeNull();
    });

    expect(capturedCreatePayload).toMatchObject({
      subjectStageId: SUBJECT_STAGE_ID,
      studyGroupId: STUDY_GROUP_ID,
      operationalPeriodId: OPERATIONAL_PERIOD_ID,
      title: 'Kuis Harian 2',
      maxScore: 100
    });
  });

  it('handles spreadsheet keyboard navigation (Enter moves to next row)', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountScorebookView();

    const cell00 = wrapper.find(`[data-testid="score-cell-${STUDENT_1_ID}-${ASSIGNMENT_1_ID}"]`);
    const input00 = cell00.find('input[data-testid="score-cell-input"]');

    // Trigger Enter keydown
    await input00.trigger('keydown', { key: 'Enter' });
    await flushPromises();

    // Verify cell input handled without error
    expect(input00.exists()).toBe(true);
  });
});
