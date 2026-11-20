import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createMemoryHistory, createRouter } from 'vue-router';
import { defineComponent } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import type {
  HomeroomReportCardDto,
  ReportCardDetailDto,
  ReportCardsOverviewDto
} from '../../api/reportCards.api';
import ReportCardsWorkbenchView from '../ReportCardsWorkbenchView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000100';
const OPERATIONAL_PERIOD_ID = '01900000-0000-7000-8000-000000000101';
const STUDY_GROUP_7A_ID = '01900000-0000-7000-8000-000000000201';
const TEACHER_HOMEROOM_ID = '01900000-0000-7000-8000-000000000301';

const CARD_DRAFT_ID = '01900000-0000-7000-8000-000000000401';
const CARD_FINALIZED_ID = '01900000-0000-7000-8000-000000000402';
const STUDENT_1_ID = '01900000-0000-7000-8000-000000000501';
const STUDENT_2_ID = '01900000-0000-7000-8000-000000000502';
const SUBJECT_MTK_ID = '01900000-0000-7000-8000-000000000601';
const SUBJECT_STAGE_MTK_ID = '01900000-0000-7000-8000-000000000701';

function createDraftCardDetail(): ReportCardDetailDto {
  return {
    id: CARD_DRAFT_ID,
    studentId: STUDENT_1_ID,
    studentName: 'Ahmad Dahlan',
    studentNis: '2026001',
    studentNisn: '0098765432',
    academicYearId: ACADEMIC_YEAR_ID,
    academicYearName: '2026/2027',
    operationalPeriodId: OPERATIONAL_PERIOD_ID,
    operationalPeriodName: 'Semester 1',
    studyGroupId: STUDY_GROUP_7A_ID,
    studyGroupName: '7A',
    homeroomTeacherId: TEACHER_HOMEROOM_ID,
    homeroomTeacherName: 'Ibu Sari',
    status: 'DRAFT',
    overrideReason: null,
    generalRemarks: 'Shows strong curiosity in mathematics.',
    finalizedAt: null,
    averageScore: 88.5,
    totalSubjects: 1,
    attendance: {
      presentCount: 92,
      absentCount: 1,
      sickCount: 2,
      lateCount: 0,
      excusedCount: 1,
      totalRecordedDays: 96
    },
    entries: [
      {
        id: '01900000-0000-7000-8000-000000000801',
        subjectId: SUBJECT_MTK_ID,
        subjectName: 'Matematika',
        subjectCode: 'MTK',
        subjectStageId: SUBJECT_STAGE_MTK_ID,
        subjectStageName: 'Fase D - Kelas 7',
        teacherId: '01900000-0000-7000-8000-000000000302',
        teacherName: 'Budi Santoso',
        numericScore: 88.5,
        letterGrade: 'B+',
        teacherNotes: 'Good algebraic reasoning.'
      }
    ]
  };
}

function createFinalizedCardDetail(): ReportCardDetailDto {
  return {
    ...createDraftCardDetail(),
    id: CARD_FINALIZED_ID,
    studentId: STUDENT_2_ID,
    studentName: 'Siti Nurhaliza',
    studentNis: '2026002',
    studentNisn: '0098765433',
    status: 'FINALIZED',
    finalizedAt: '2026-11-18T10:00:00Z',
    averageScore: 93.0,
    generalRemarks: 'Consistent exemplary academic performance.'
  };
}

function createSampleOverview(): ReportCardsOverviewDto {
  return {
    totalReportCards: 2,
    draftCount: 1,
    finalizedCount: 1,
    schoolAverageScore: 90.75,
    items: [
      {
        id: CARD_DRAFT_ID,
        studentId: STUDENT_1_ID,
        studentName: 'Ahmad Dahlan',
        studentNis: '2026001',
        studyGroupId: STUDY_GROUP_7A_ID,
        studyGroupName: '7A',
        homeroomTeacherId: TEACHER_HOMEROOM_ID,
        homeroomTeacherName: 'Ibu Sari',
        status: 'DRAFT',
        averageScore: 88.5,
        totalSubjects: 1,
        completedNotesCount: 1,
        isHomeroomRemarksCompleted: true
      },
      {
        id: CARD_FINALIZED_ID,
        studentId: STUDENT_2_ID,
        studentName: 'Siti Nurhaliza',
        studentNis: '2026002',
        studyGroupId: STUDY_GROUP_7A_ID,
        studyGroupName: '7A',
        homeroomTeacherId: TEACHER_HOMEROOM_ID,
        homeroomTeacherName: 'Ibu Sari',
        status: 'FINALIZED',
        finalizedAt: '2026-11-18T10:00:00Z',
        averageScore: 93.0,
        totalSubjects: 1,
        completedNotesCount: 1,
        isHomeroomRemarksCompleted: true
      }
    ]
  };
}

function createSampleHomeroomCards(): HomeroomReportCardDto[] {
  return [
    {
      id: CARD_DRAFT_ID,
      studentId: STUDENT_1_ID,
      studentName: 'Ahmad Dahlan',
      studentNis: '2026001',
      studentNisn: '0098765432',
      studyGroupId: STUDY_GROUP_7A_ID,
      studyGroupName: '7A',
      homeroomTeacherId: TEACHER_HOMEROOM_ID,
      homeroomTeacherName: 'Ibu Sari',
      status: 'DRAFT',
      generalRemarks: 'Shows strong curiosity in mathematics.',
      totalSubjects: 1,
      completedNotesCount: 1,
      isHomeroomRemarksCompleted: true,
      averageScore: 88.5
    },
    {
      id: CARD_FINALIZED_ID,
      studentId: STUDENT_2_ID,
      studentName: 'Siti Nurhaliza',
      studentNis: '2026002',
      studentNisn: '0098765433',
      studyGroupId: STUDY_GROUP_7A_ID,
      studyGroupName: '7A',
      homeroomTeacherId: TEACHER_HOMEROOM_ID,
      homeroomTeacherName: 'Ibu Sari',
      status: 'FINALIZED',
      finalizedAt: '2026-11-18T10:00:00Z',
      generalRemarks: 'Consistent exemplary academic performance.',
      totalSubjects: 1,
      completedNotesCount: 1,
      isHomeroomRemarksCompleted: true,
      averageScore: 93.0
    }
  ];
}

function setupDefaultMsw() {
  server.use(
    http.get('/api/opr/academic-years', () =>
      HttpResponse.json({
        items: [
          {
            id: ACADEMIC_YEAR_ID,
            name: '2026/2027',
            status: 'ACTIVE',
            periods: [
              {
                id: OPERATIONAL_PERIOD_ID,
                academicYearId: ACADEMIC_YEAR_ID,
                name: 'Semester 1',
                status: 'ACTIVE'
              }
            ]
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/std/study-groups', () =>
      HttpResponse.json({
        items: [
          {
            id: STUDY_GROUP_7A_ID,
            academicYearId: ACADEMIC_YEAR_ID,
            academicYearName: '2026/2027',
            schoolLevel: 'SMP',
            grade: '7',
            name: '7A',
            homeroomTeacherId: TEACHER_HOMEROOM_ID,
            homeroomTeacherName: 'Ibu Sari',
            studentCount: 25
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/acd/report-cards/overview', () =>
      HttpResponse.json(createSampleOverview())
    ),
    http.get('/api/acd/report-cards/homeroom', () =>
      HttpResponse.json(createSampleHomeroomCards())
    ),
    http.get(`/api/acd/report-cards/${CARD_DRAFT_ID}`, () =>
      HttpResponse.json(createDraftCardDetail())
    ),
    http.get(`/api/acd/report-cards/${CARD_FINALIZED_ID}`, () =>
      HttpResponse.json(createFinalizedCardDetail())
    )
  );
}

const AppWrapper = defineComponent({
  template: '<router-view />'
});

async function mountWorkbench(options?: {
  permissions?: string[];
  roles?: string[];
  teacherId?: string | null;
  initialPath?: string;
  isHistorical?: boolean;
}) {
  const permissions = options?.permissions ?? [
    'Permissions.Academic.ReportCards.View',
    'Permissions.Academic.ReportCards.Generate',
    'Permissions.Academic.ReportCards.ProvideNotes',
    'Permissions.Academic.ReportCards.Remarks',
    'Permissions.Academic.ReportCards.Finalize',
    'Permissions.Academic.ReportCards.Override'
  ];

  configureAuthSessionReader({
    getUser: () => ({
      id: 'usr-1',
      username: 'admin',
      displayName: 'School Admin',
      roles: options?.roles ?? ['SCHOOL_ADMIN'],
      permissions,
      teacherId: options?.teacherId ?? null
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    activePeriodId: OPERATIONAL_PERIOD_ID,
    selectedAcademicYearId: ACADEMIC_YEAR_ID,
    selectedPeriodId: OPERATIONAL_PERIOD_ID,
    currentContextLabel: '2026/2027 • Semester 1',
    isHistoricalPeriod: options?.isHistorical ?? false
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/academic/report-cards',
        name: 'academic.report-cards',
        component: ReportCardsWorkbenchView
      }
    ]
  });
  await router.push(options?.initialPath ?? '/academic/report-cards');
  await router.isReady();

  const wrapper = mount(AppWrapper, {
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

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

beforeEach(() => {
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
});

afterEach(() => {
  server.resetHandlers();
  vi.restoreAllMocks();
});

afterAll(() => {
  server.close();
});

describe('ReportCardsWorkbenchView', () => {
  it('renders school overview KPIs, student roster, and auto-loads first report card detail for Admin', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountWorkbench();

    expect(wrapper.get('[data-testid="report-cards-kpi-strip"]').text()).toContain('90.75');
    expect(wrapper.get('[data-testid="report-cards-roster"]').text()).toContain('Ahmad Dahlan');
    expect(wrapper.get('[data-testid="report-cards-roster"]').text()).toContain('Siti Nurhaliza');
    expect(wrapper.get('[data-testid="report-card-detail-panel"]').text()).toContain(
      'Good algebraic reasoning.'
    );
  });

  it('triggers Batch Draft Generation via Tier 3 Pre-Flight Modal (UC-ADM-19)', async () => {
    setupDefaultMsw();
    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/acd/report-cards/generate-batch', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ generatedCount: 23, skippedCount: 2 });
      })
    );

    const { wrapper } = await mountWorkbench();

    await wrapper.get('[data-testid="open-batch-generate-btn"]').trigger('click');
    await flushPromises();

    const modal = wrapper.get('[data-testid="batch-generate-modal"]');
    expect(modal.text()).toContain('Pre-Flight Impact Summary');
    expect(modal.text()).toContain('25');

    await wrapper.get('[data-testid="batch-generate-confirm-btn"]').trigger('click');
    await flushPromises();

    expect(capturedBody).toEqual({
      studyGroupId: STUDY_GROUP_7A_ID,
      operationalPeriodId: OPERATIONAL_PERIOD_ID
    });
  });

  it('allows Subject Teacher to submit qualitative feedback on a DRAFT report card (UC-TCH-04) without calling /overview', async () => {
    setupDefaultMsw();
    let overviewCalled = false;
    let capturedNotePayload: Record<string, unknown> | null = null;

    server.use(
      http.get('/api/acd/report-cards/overview', () => {
        overviewCalled = true;
        return HttpResponse.json(createSampleOverview());
      }),
      http.post(`/api/acd/report-cards/${CARD_DRAFT_ID}/subject-notes`, async ({ request }) => {
        capturedNotePayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ message: 'Subject notes updated successfully.' });
      })
    );

    const { wrapper } = await mountWorkbench({
      roles: ['TEACHER'],
      permissions: ['Permissions.Academic.ReportCards.ProvideNotes'],
      teacherId: '01900000-0000-7000-8000-000000000302'
    });

    expect(overviewCalled).toBe(false);
    expect(wrapper.find('[data-testid="open-batch-generate-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="save-homeroom-remarks-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="finalize-card-btn"]').exists()).toBe(false);

    await wrapper.get(`[data-testid="edit-subject-note-btn-${SUBJECT_STAGE_MTK_ID}"]`).trigger('click');
    await wrapper
      .get('[data-testid="subject-note-textarea"]')
      .setValue('Excellent progress in quadratic equations.');
    await wrapper.get('[data-testid="subject-note-save-btn"]').trigger('click');
    await flushPromises();

    expect(capturedNotePayload).toEqual({
      subjectStageId: SUBJECT_STAGE_MTK_ID,
      teacherNotes: 'Excellent progress in quadratic equations.'
    });
  });

  it('allows Homeroom Teacher to update remarks (UC-HRM-02) and finalize a DRAFT card (UC-HRM-03)', async () => {
    setupDefaultMsw();
    let capturedRemarks: Record<string, unknown> | null = null;
    let finalizeCalled = false;

    server.use(
      http.post(`/api/acd/report-cards/${CARD_DRAFT_ID}/homeroom-remarks`, async ({ request }) => {
        capturedRemarks = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ message: 'Homeroom remarks updated successfully.' });
      }),
      http.post(`/api/acd/report-cards/${CARD_DRAFT_ID}/finalize`, () => {
        finalizeCalled = true;
        return HttpResponse.json({ message: 'Report card finalized successfully.' });
      })
    );

    const { wrapper } = await mountWorkbench({
      roles: ['HOMEROOM_TEACHER'],
      permissions: [
        'Permissions.Academic.ReportCards.ProvideNotes',
        'Permissions.Academic.ReportCards.Remarks',
        'Permissions.Academic.ReportCards.Finalize'
      ],
      teacherId: TEACHER_HOMEROOM_ID
    });

    const textarea = wrapper.get('[data-testid="homeroom-remarks-textarea"]');
    await textarea.setValue('Active in class discussions and leadership.');
    await wrapper.get('[data-testid="save-homeroom-remarks-btn"]').trigger('click');
    await flushPromises();

    expect(capturedRemarks).toEqual({
      remarks: 'Active in class discussions and leadership.'
    });

    await wrapper.get('[data-testid="finalize-card-btn"]').trigger('click');
    await flushPromises();

    const finalizeDialog = wrapper.get('[data-testid="finalize-confirm-dialog"]');
    const confirmBtn = finalizeDialog.findAll('button').find((b) => b.text().includes('Finalize'));
    expect(confirmBtn).toBeDefined();
    await confirmBtn!.trigger('click');
    await flushPromises();

    expect(finalizeCalled).toBe(true);
  });

  it('enforces mandatory override reason for Admin unlocking a FINALIZED report card (UC-ADM-20)', async () => {
    setupDefaultMsw();
    let capturedOverride: Record<string, unknown> | null = null;

    server.use(
      http.post(`/api/acd/report-cards/${CARD_FINALIZED_ID}/override`, async ({ request }) => {
        capturedOverride = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ message: 'Report card unlocked to draft successfully.' });
      })
    );

    const { wrapper } = await mountWorkbench();

    await wrapper.get(`[data-testid="roster-row-${CARD_FINALIZED_ID}"]`).trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="finalized-lock-banner"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="homeroom-remarks-textarea"]').exists()).toBe(false);

    await wrapper.get('[data-testid="override-card-btn"]').trigger('click');
    await flushPromises();

    const overrideDialog = wrapper.get('[data-testid="override-confirm-dialog"]');
    const unlockBtn = overrideDialog.findAll('button').find((b) => b.text().includes('Unlock'));
    expect(unlockBtn).toBeDefined();
    expect((unlockBtn!.element as HTMLButtonElement).disabled).toBe(true);

    await overrideDialog.get('textarea').setValue('Typo in Mathematics scorebook entry.');
    expect((unlockBtn!.element as HTMLButtonElement).disabled).toBe(false);

    await unlockBtn!.trigger('click');
    await flushPromises();

    expect(capturedOverride).toEqual({
      overrideReason: 'Typo in Mathematics scorebook entry.'
    });
  });

  it('renders Principal overview in strict read-only mode without mutation triggers (UC-PRC-02)', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountWorkbench({
      roles: ['PRINCIPAL'],
      permissions: ['Permissions.Academic.ReportCards.View']
    });

    expect(wrapper.find('[data-testid="open-batch-generate-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="finalize-card-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="override-card-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="homeroom-remarks-textarea"]').exists()).toBe(false);
    expect(wrapper.get('[data-testid="homeroom-remarks-readonly"]').text()).toContain(
      'Shows strong curiosity in mathematics.'
    );
  });

  it('opens official A4 ReportCardPrintView and invokes window.print()', async () => {
    setupDefaultMsw();
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});
    const { wrapper } = await mountWorkbench();

    await wrapper.get('[data-testid="detail-print-btn"]').trigger('click');
    await flushPromises();

    const printView = wrapper.get('[data-testid="report-card-print-view"]');
    expect(printView.text()).toContain('Official Semester Report Card');
    expect(printView.text()).toContain('Ahmad Dahlan');
    expect(printView.text()).toContain('0098765432');
    expect(printView.text()).toContain('92 day(s)');

    await wrapper.get('[data-testid="print-trigger-btn"]').trigger('click');
    expect(printSpy).toHaveBeenCalledTimes(1);

    await wrapper.get('[data-testid="print-close-btn"]').trigger('click');
    expect(wrapper.find('[data-testid="report-card-print-view"]').exists()).toBe(false);
  });

  it('locks all report card mutations when viewing a historical period', async () => {
    setupDefaultMsw();
    const { wrapper } = await mountWorkbench({ isHistorical: true });

    expect(wrapper.find('[data-testid="open-batch-generate-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="finalize-card-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="homeroom-remarks-textarea"]').exists()).toBe(false);
  });
});
