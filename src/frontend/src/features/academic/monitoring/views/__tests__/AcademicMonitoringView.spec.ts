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
import type { AcademicMonitoringProgressDto } from '../../api/academicMonitoring.api';
import AcademicMonitoringView from '../AcademicMonitoringView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000100';
const OPERATIONAL_PERIOD_ID = '01900000-0000-7000-8000-000000000101';
const STUDY_GROUP_7A_ID = '01900000-0000-7000-8000-000000000201';
const STUDY_GROUP_8A_ID = '01900000-0000-7000-8000-000000000202';
const STUDY_GROUP_10A_ID = '01900000-0000-7000-8000-000000000203';

let capturedProgressRequests: Array<{ url: string; query: Record<string, string> }> = [];

function createSampleProgressData(): AcademicMonitoringProgressDto {
  return {
    overview: {
      attendanceRateTodayPercent: 88.5,
      totalEnrolledStudentsCount: 120,
      presentCount: 106,
      absentCount: 4,
      sickCount: 5,
      lateCount: 3,
      excusedCount: 2,
      gradingCompletenessPercent: 72.0,
      totalAssignmentsCount: 15,
      expectedScoresCount: 450,
      actualScoresCount: 324,
      reportCardsFinalizedPercent: 60.0,
      totalPlacedStudentsCount: 120,
      draftReportCardsCount: 48,
      finalizedReportCardsCount: 72
    },
    exceptions: {
      missingAttendanceStudyGroups: [
        {
          studyGroupId: STUDY_GROUP_7A_ID,
          studyGroupName: '7A',
          schoolLevel: 'SMP',
          grade: '7',
          homeroomTeacherName: 'Ibu Sari'
        }
      ],
      overdueGradingAssignments: [
        {
          assignmentId: '01900000-0000-7000-8000-000000000301',
          title: 'Ulangan Harian 1 MTK',
          subjectName: 'Matematika',
          teacherName: 'Budi Santoso',
          studyGroupName: '8A',
          daysOverdue: 10,
          missingScoresCount: 15
        }
      ],
      pendingReportCards: [
        {
          studyGroupId: STUDY_GROUP_10A_ID,
          studyGroupName: '10-RPL',
          schoolLevel: 'SMK',
          grade: '10',
          homeroomTeacherName: 'Pak Joko',
          draftCount: 20,
          totalCount: 30
        }
      ]
    },
    studyGroups: [
      {
        studyGroupId: STUDY_GROUP_7A_ID,
        studyGroupName: '7A',
        schoolLevel: 'SMP',
        grade: '7',
        homeroomTeacherName: 'Ibu Sari',
        attendanceTodayPercent: 0.0,
        assignmentsCount: 5,
        gradingCompletenessPercent: 80.0,
        reportCardsFinalizedRatio: '20/25',
        isAttendanceMissingToday: true,
        hasOverdueGrading: false,
        hasPendingReportCards: false
      },
      {
        studyGroupId: STUDY_GROUP_8A_ID,
        studyGroupName: '8A',
        schoolLevel: 'SMP',
        grade: '8',
        homeroomTeacherName: 'Budi Santoso',
        attendanceTodayPercent: 96.0,
        assignmentsCount: 6,
        gradingCompletenessPercent: 65.0,
        reportCardsFinalizedRatio: '25/25',
        isAttendanceMissingToday: false,
        hasOverdueGrading: true,
        hasPendingReportCards: false
      },
      {
        studyGroupId: STUDY_GROUP_10A_ID,
        studyGroupName: '10-RPL',
        schoolLevel: 'SMK',
        grade: '10',
        homeroomTeacherName: 'Pak Joko',
        attendanceTodayPercent: 92.5,
        assignmentsCount: 4,
        gradingCompletenessPercent: 90.0,
        reportCardsFinalizedRatio: '10/30',
        isAttendanceMissingToday: false,
        hasOverdueGrading: false,
        hasPendingReportCards: true
      }
    ]
  };
}

function setupDefaultMsw() {
  capturedProgressRequests = [];

  server.use(
    http.get('/api/opr/academic-years', () => {
      return HttpResponse.json({
        items: [
          {
            id: ACADEMIC_YEAR_ID,
            name: '2026/2027',
            status: 'ACTIVE',
            periods: [
              {
                id: OPERATIONAL_PERIOD_ID,
                academicYearId: ACADEMIC_YEAR_ID,
                name: 'Semester Ganjil',
                status: 'ACTIVE'
              }
            ]
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      });
    }),

    http.get('/api/acd/monitoring/progress', ({ request }) => {
      const url = new URL(request.url);
      const query: Record<string, string> = {};
      url.searchParams.forEach((val, key) => {
        query[key] = val;
      });
      capturedProgressRequests.push({ url: request.url, query });

      return HttpResponse.json(createSampleProgressData());
    })
  );
}

const AppWrapper = defineComponent({
  template: '<router-view />'
});

async function mountMonitoringView(options?: {
  initialPath?: string;
}) {
  configureAuthSessionReader({
    getUser: () => ({
      id: 'usr-admin',
      username: 'admin',
      displayName: 'School Admin',
      roles: ['SCHOOL_ADMIN'],
      permissions: ['Permissions.Academic.Monitoring.View'],
      teacherId: null
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    activePeriodId: OPERATIONAL_PERIOD_ID,
    selectedAcademicYearId: ACADEMIC_YEAR_ID,
    selectedPeriodId: OPERATIONAL_PERIOD_ID,
    isHistoricalPeriod: false,
    activeAcademicYearName: '2026/2027',
    activePeriodName: 'Semester Ganjil',
    currentContextLabel: '2026/2027 - Semester Ganjil'
  }));

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/academic/monitoring',
        name: 'academic.monitoring',
        component: AcademicMonitoringView
      },
      {
        path: '/academic/report-cards',
        name: 'academic.report-cards',
        component: { template: '<div>Report Cards</div>' }
      }
    ]
  });

  await router.push(options?.initialPath ?? '/academic/monitoring');
  await router.isReady();

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0
      }
    }
  });

  const wrapper = mount(AppWrapper, {
    global: {
      plugins: [
        router,
        [PrimeVue, { theme: { preset: Aura, options: { darkModeSelector: '.dark' } } }],
        ToastService,
        [VueQueryPlugin, { queryClient }]
      ]
    }
  });

  await flushPromises();
  return { wrapper, router, queryClient };
}

describe('AcademicMonitoringView.vue (SCR-ACD-MONITORING)', () => {
  beforeAll(() => {
    server.listen({ onUnhandledRequest: 'error' });
  });

  afterAll(() => {
    server.close();
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
    setupDefaultMsw();
  });

  afterEach(() => {
    server.resetHandlers();
    vi.clearAllMocks();
  });

  it('renders master dashboard view and prominent executive metric tiles', async () => {
    const { wrapper } = await mountMonitoringView();

    expect(wrapper.find('[data-testid="academic-monitoring-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="kpi-metrics-grid"]').exists()).toBe(true);

    const kpiAttendance = wrapper.find('[data-testid="kpi-attendance"]');
    expect(kpiAttendance.exists()).toBe(true);
    expect(kpiAttendance.text()).toContain('88.5%');
    expect(kpiAttendance.text()).toContain('106'); // Present count

    const kpiGrading = wrapper.find('[data-testid="kpi-grading"]');
    expect(kpiGrading.exists()).toBe(true);
    expect(kpiGrading.text()).toContain('72.0%');
    expect(kpiGrading.text()).toContain('324'); // Actual scores

    const kpiReportCards = wrapper.find('[data-testid="kpi-report-cards"]');
    expect(kpiReportCards.exists()).toBe(true);
    expect(kpiReportCards.text()).toContain('60.0%');
    expect(kpiReportCards.text()).toContain('72'); // Finalized count
  });

  it('test changing date filter refetches monitoring data', async () => {
    const { wrapper } = await mountMonitoringView();

    expect(capturedProgressRequests.length).toBeGreaterThanOrEqual(1);
    const initialRequestCount = capturedProgressRequests.length;

    const datePicker = wrapper.find<HTMLInputElement>('[data-testid="date-picker"]');
    expect(datePicker.exists()).toBe(true);

    // Change date to '2026-11-25'
    await datePicker.setValue('2026-11-25');
    await flushPromises();

    expect(capturedProgressRequests.length).toBeGreaterThan(initialRequestCount);
    const latestRequest = capturedProgressRequests[capturedProgressRequests.length - 1];
    expect(latestRequest?.query.date).toBe('2026-11-25');
  });

  it('test exception banners render when alerts are present', async () => {
    const { wrapper } = await mountMonitoringView();

    const exceptionBanners = wrapper.find('[data-testid="exception-alert-banners"]');
    expect(exceptionBanners.exists()).toBe(true);

    // Missing attendance banner
    const missingAttendanceAlert = wrapper.find('[data-testid="alert-missing-attendance"]');
    expect(missingAttendanceAlert.exists()).toBe(true);
    expect(missingAttendanceAlert.text()).toContain('7A');
    expect(missingAttendanceAlert.text()).toContain('Ibu Sari');

    // Overdue grading alert
    const overdueGradingAlert = wrapper.find('[data-testid="alert-overdue-grading"]');
    expect(overdueGradingAlert.exists()).toBe(true);
    expect(overdueGradingAlert.text()).toContain('Ulangan Harian 1 MTK');
    expect(overdueGradingAlert.text()).toContain('+10 hari');
    expect(overdueGradingAlert.text()).toContain('15 skor tertunda');

    // Pending report cards alert
    const pendingReportCardsAlert = wrapper.find('[data-testid="alert-pending-report-cards"]');
    expect(pendingReportCardsAlert.exists()).toBe(true);
    expect(pendingReportCardsAlert.text()).toContain('10-RPL');
    expect(pendingReportCardsAlert.text()).toContain('Pak Joko');
    expect(pendingReportCardsAlert.text()).toContain('20 / 30 Draft');
  });

  it('test study group table filtering', async () => {
    const { wrapper } = await mountMonitoringView();

    const table = wrapper.find('[data-testid="study-group-progress-table"]');
    expect(table.exists()).toBe(true);

    // Initially all 3 study groups are present in table text
    expect(table.text()).toContain('7A');
    expect(table.text()).toContain('8A');
    expect(table.text()).toContain('10-RPL');

    // Filter by global search: search '10-RPL'
    const searchInput = wrapper.find<HTMLInputElement>('[data-testid="input-search"]');
    expect(searchInput.exists()).toBe(true);
    await searchInput.setValue('10-RPL');
    await flushPromises();

    const filteredTable = wrapper.find('[data-testid="study-group-progress-table"]');
    expect(filteredTable.text()).toContain('10-RPL');
    expect(filteredTable.text()).not.toContain('7A');
    expect(filteredTable.text()).not.toContain('8A');

    // Reset search
    await searchInput.setValue('');
    await flushPromises();

    // Verify row highlighting for study groups with exceptions
    expect(wrapper.html()).toContain('Presensi Kosong');
    expect(wrapper.html()).toContain('Nilai Overdue');
    expect(wrapper.html()).toContain('Rapor Draf');
  });
});
