import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import Select from 'primevue/select';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import type {
  DailyCashierSummaryDto,
  ReceivablesAgingDto,
  ReceivablesSummaryDto,
  StudentStatementDto
} from '../../api/receivables.api';
import ReceivablesAgingView from '../ReceivablesAgingView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000100';
const STUDY_GROUP_7A_ID = '01900000-0000-7000-8000-000000000601';
const STUDY_GROUP_10A_ID = '01900000-0000-7000-8000-000000000602';
const STUDENT_SITI_ID = '01900000-0000-7000-8000-000000000302';
const STUDENT_BUDI_ID = '01900000-0000-7000-8000-000000000303';

function createSummaryFixture(): ReceivablesSummaryDto {
  return {
    totalBilled: 5000000,
    totalCollected: 3250000,
    totalOutstanding: 1750000,
    totalStudentsCount: 12,
    totalChargesCount: 24,
    breakdown: [
      {
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        schoolLevel: 'SMP',
        totalBilled: 3000000,
        totalCollected: 2100000,
        totalOutstanding: 900000,
        studentCount: 8,
        chargeCount: 16
      },
      {
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        schoolLevel: 'SMA',
        totalBilled: 2000000,
        totalCollected: 1150000,
        totalOutstanding: 850000,
        studentCount: 4,
        chargeCount: 8
      }
    ]
  };
}

function createAgingFixture(filters?: {
  schoolLevel?: string | null;
  studyGroupId?: string | null;
}): ReceivablesAgingDto {
  const allStudents = [
    {
      studentId: STUDENT_SITI_ID,
      studentName: 'Siti Nurhaliza',
      studentNis: '20260002',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: STUDY_GROUP_7A_ID,
      studyGroupName: '7A',
      currentAmount: 200000,
      days1To30Amount: 150000,
      days31To60Amount: 350000,
      days61To90Amount: 0,
      over90DaysAmount: 200000,
      totalOutstanding: 900000,
      unpaidChargeCount: 3
    },
    {
      studentId: STUDENT_BUDI_ID,
      studentName: 'Budi Santoso',
      studentNis: '20260003',
      schoolLevel: 'SMA',
      grade: '10',
      studyGroupId: STUDY_GROUP_10A_ID,
      studyGroupName: '10A',
      currentAmount: 450000,
      days1To30Amount: 0,
      days31To60Amount: 0,
      days61To90Amount: 400000,
      over90DaysAmount: 0,
      totalOutstanding: 850000,
      unpaidChargeCount: 2
    }
  ];

  const filtered = allStudents.filter((s) => {
    if (filters?.schoolLevel && s.schoolLevel !== filters.schoolLevel) return false;
    if (filters?.studyGroupId && s.studyGroupId !== filters.studyGroupId) return false;
    return true;
  });

  const currentAmount = filtered.reduce((acc, s) => acc + s.currentAmount, 0);
  const days1To30Amount = filtered.reduce((acc, s) => acc + s.days1To30Amount, 0);
  const days31To60Amount = filtered.reduce((acc, s) => acc + s.days31To60Amount, 0);
  const days61To90Amount = filtered.reduce((acc, s) => acc + s.days61To90Amount, 0);
  const over90DaysAmount = filtered.reduce((acc, s) => acc + s.over90DaysAmount, 0);
  const totalOutstanding = filtered.reduce((acc, s) => acc + s.totalOutstanding, 0);

  return {
    summary: {
      currentAmount,
      days1To30Amount,
      days31To60Amount,
      days61To90Amount,
      over90DaysAmount,
      totalOutstanding,
      arrearsOver30Days: days31To60Amount + days61To90Amount + over90DaysAmount,
      totalStudentsCount: filtered.length,
      totalChargesCount: filtered.reduce((acc, s) => acc + s.unpaidChargeCount, 0)
    },
    students: filtered
  };
}

function createStatementFixture(studentId: string): StudentStatementDto {
  return {
    studentId,
    studentName: 'Siti Nurhaliza',
    studentNis: '20260002',
    studyGroupId: STUDY_GROUP_7A_ID,
    studyGroupName: '7A',
    totalBilled: 1180000,
    totalPaid: 280000,
    totalOutstanding: 900000,
    totalDepositBalance: 50000,
    charges: [
      {
        id: '01900000-0000-7000-8000-000000000701',
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        billingMonth: '2026-07',
        chargeTypeId: '01900000-0000-7000-8000-000000000201',
        chargeTypeName: 'SPP Bulanan SMP',
        chargeFrequency: 'MONTHLY',
        unitAmount: 350000,
        quantity: 1,
        grossAmount: 350000,
        discountAmount: 70000,
        discountReason: 'Sibling discount 20%',
        netAmount: 280000,
        paidAmount: 100000,
        outstandingAmount: 180000,
        status: 'PARTIALLY_PAID',
        dueDate: '2026-07-10',
        createdAt: '2026-07-01T08:00:00Z'
      }
    ],
    receipts: [
      {
        id: '01900000-0000-7000-8000-000000000801',
        receiptNumber: 'KWT-202607-0001',
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        paymentDate: '2026-07-05',
        paymentMethod: 'CASH',
        bankReference: null,
        totalAmount: 150000,
        allocatedAmount: 100000,
        unallocatedAmount: 50000,
        receivedById: 'usr-fin-1',
        receivedByName: 'Finance Officer',
        notes: 'Initial partial settlement + deposit',
        createdAt: '2026-07-05T09:15:00Z',
        allocations: [
          {
            id: '01900000-0000-7000-8000-000000000901',
            studentChargeId: '01900000-0000-7000-8000-000000000701',
            chargeTypeName: 'SPP Bulanan SMP',
            billingMonth: '2026-07',
            allocatedAmount: 100000,
            allocationDate: '2026-07-05',
            createdAt: '2026-07-05T09:15:00Z'
          }
        ]
      }
    ]
  };
}

function createDailyCashierFixture(date: string): DailyCashierSummaryDto {
  if (date === '2026-07-05') {
    return {
      date: '2026-07-05',
      totalCashCollections: 450000,
      totalBankTransferCollections: 700000,
      totalCollections: 1150000,
      totalDepositsHeld: 50000,
      totalAllocated: 1100000,
      receiptCount: 2,
      paymentMethodSummaries: [
        { paymentMethod: 'BANK_TRANSFER', totalAmount: 700000, receiptCount: 1 },
        { paymentMethod: 'CASH', totalAmount: 450000, receiptCount: 1 }
      ],
      receipts: [
        {
          id: '01900000-0000-7000-8000-000000000801',
          receiptNumber: 'KWT-202607-0001',
          academicYearId: ACADEMIC_YEAR_ID,
          academicYearName: '2026/2027',
          studentId: STUDENT_SITI_ID,
          studentName: 'Siti Nurhaliza',
          studentNis: '20260002',
          studyGroupId: STUDY_GROUP_7A_ID,
          studyGroupName: '7A',
          paymentDate: '2026-07-05',
          paymentMethod: 'CASH',
          bankReference: null,
          totalAmount: 450000,
          allocatedAmount: 400000,
          unallocatedAmount: 50000,
          receivedById: 'usr-fin-1',
          receivedByName: 'Dewi Cashier',
          notes: 'Counter cash payment',
          createdAt: '2026-07-05T09:15:00Z'
        },
        {
          id: '01900000-0000-7000-8000-000000000802',
          receiptNumber: 'KWT-202607-0002',
          academicYearId: ACADEMIC_YEAR_ID,
          academicYearName: '2026/2027',
          studentId: STUDENT_BUDI_ID,
          studentName: 'Budi Santoso',
          studentNis: '20260003',
          studyGroupId: STUDY_GROUP_10A_ID,
          studyGroupName: '10A',
          paymentDate: '2026-07-05',
          paymentMethod: 'BANK_TRANSFER',
          bankReference: 'TRF-BCA-8821',
          totalAmount: 700000,
          allocatedAmount: 700000,
          unallocatedAmount: 0,
          receivedById: 'usr-fin-1',
          receivedByName: 'Finance Officer',
          notes: null,
          createdAt: '2026-07-05T11:20:00Z'
        }
      ]
    };
  }

  return {
    date,
    totalCashCollections: 350000,
    totalBankTransferCollections: 0,
    totalCollections: 350000,
    totalDepositsHeld: 25000,
    totalAllocated: 325000,
    receiptCount: 1,
    paymentMethodSummaries: [
      { paymentMethod: 'CASH', totalAmount: 350000, receiptCount: 1 }
    ],
    receipts: [
      {
        id: '01900000-0000-7000-8000-000000000803',
        receiptNumber: 'KWT-202611-0001',
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        studentId: STUDENT_SITI_ID,
        studentName: 'Siti Nurhaliza',
        studentNis: '20260002',
        studyGroupId: STUDY_GROUP_7A_ID,
        studyGroupName: '7A',
        paymentDate: date,
        paymentMethod: 'CASH',
        bankReference: null,
        totalAmount: 350000,
        allocatedAmount: 325000,
        unallocatedAmount: 25000,
        receivedById: 'usr-fin-1',
        receivedByName: 'Finance Officer',
        notes: null,
        createdAt: `${date}T08:30:00Z`
      }
    ]
  };
}

function setupDefaultMsw(options?: {
  onAgingRequest?: (params: URLSearchParams) => void;
  onDailyCashierRequest?: (date: string) => void;
  onStatementRequest?: (studentId: string) => void;
}) {
  server.use(
    http.get('/api/opr/academic-years', () =>
      HttpResponse.json({
        items: [
          {
            id: ACADEMIC_YEAR_ID,
            name: '2026/2027',
            status: 'ACTIVE'
          }
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1
      })
    ),
    http.get('/api/std/study-groups', ({ request }) => {
      const url = new URL(request.url);
      const schoolLevel = url.searchParams.get('SchoolLevel');
      const items = [
        {
          id: STUDY_GROUP_7A_ID,
          academicYearId: ACADEMIC_YEAR_ID,
          name: '7A',
          schoolLevel: 'SMP',
          grade: '7'
        },
        {
          id: STUDY_GROUP_10A_ID,
          academicYearId: ACADEMIC_YEAR_ID,
          name: '10A',
          schoolLevel: 'SMA',
          grade: '10'
        }
      ].filter((sg) => !schoolLevel || sg.schoolLevel === schoolLevel);

      return HttpResponse.json({
        items,
        page: 1,
        pageSize: 100,
        totalCount: items.length,
        totalPages: 1
      });
    }),
    http.get('/api/fin/reports/receivables-summary', () =>
      HttpResponse.json(createSummaryFixture())
    ),
    http.get('/api/fin/reports/receivables-aging', ({ request }) => {
      const url = new URL(request.url);
      options?.onAgingRequest?.(url.searchParams);
      return HttpResponse.json(
        createAgingFixture({
          schoolLevel: url.searchParams.get('schoolLevel'),
          studyGroupId: url.searchParams.get('studyGroupId')
        })
      );
    }),
    http.get('/api/fin/reports/daily-cashier', ({ request }) => {
      const url = new URL(request.url);
      const date = url.searchParams.get('date') ?? '2026-11-20';
      options?.onDailyCashierRequest?.(date);
      return HttpResponse.json(createDailyCashierFixture(date));
    }),
    http.get('/api/fin/students/:studentId/statement', ({ params }) => {
      const studentId = String(params.studentId);
      options?.onStatementRequest?.(studentId);
      return HttpResponse.json(createStatementFixture(studentId));
    })
  );
}

function mountView(options?: { roles?: string[]; permissions?: string[] }) {
  const roles = options?.roles ?? ['FINANCE_OFFICER'];
  const permissions = options?.permissions ?? [
    'Permissions.Finance.Reports.View',
    'Permissions.Finance.Statements.View'
  ];

  configureAuthSessionReader({
    getUser: () => ({
      id: 'usr-fin-1',
      username: 'finance.officer',
      displayName: 'Finance Officer',
      roles,
      permissions
    })
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    selectedAcademicYearId: ACADEMIC_YEAR_ID,
    activeAcademicYearName: '2026/2027',
    currentContextLabel: '2026/2027 • Semester 1'
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(ReceivablesAgingView, {
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

class MockResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}

beforeAll(() => {
  vi.stubGlobal('ResizeObserver', MockResizeObserver);
  if (typeof window !== 'undefined' && !window.matchMedia) {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn()
      }))
    );
  }
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  configureAuthSessionReader(null);
  configureAppContextReader(null);
});

afterAll(() => {
  server.close();
  vi.unstubAllGlobals();
});

beforeEach(() => {
  setupDefaultMsw();
});

describe('ReceivablesAgingView (SCR-FIN-RECEIVABLES)', () => {
  it('shows Access Restricted empty state when user lacks Permissions.Finance.Reports.View', async () => {
    const wrapper = mountView({
      roles: ['TEACHER'],
      permissions: ['Permissions.Academic.Attendance.View']
    });
    await flushPromises();

    expect(wrapper.text()).toContain('Access Restricted');
    expect(wrapper.find('[data-testid="receivables-tab"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="daily-cashier-tab"]').exists()).toBe(false);
  });

  it('renders receivables KPI cards, aging buckets strip, and aged arrears student table', async () => {
    const wrapper = mountView();
    await flushPromises();

    const totalBilledCard = wrapper.find('[data-testid="kpi-total-billed"]');
    const totalCollectedCard = wrapper.find('[data-testid="kpi-total-collected"]');
    const totalOutstandingCard = wrapper.find('[data-testid="kpi-total-outstanding"]');
    const arrearsOver30Card = wrapper.find('[data-testid="kpi-arrears-over-30"]');

    expect(totalBilledCard.text()).toContain('5.000.000');
    expect(totalCollectedCard.text()).toContain('3.250.000');
    expect(totalCollectedCard.text()).toContain('65%');
    expect(totalOutstandingCard.text()).toContain('1.750.000');
    expect(arrearsOver30Card.text()).toContain('950.000');

    expect(wrapper.find('[data-testid="aging-buckets-strip"]').text()).toContain('650.000');
    expect(wrapper.find('[data-testid="level-breakdown-section"]').text()).toContain('SMP');
    expect(wrapper.find('[data-testid="level-breakdown-section"]').text()).toContain('SMA');

    const tableText = wrapper.find('[data-testid="receivables-aging-table"]').text();
    expect(tableText).toContain('Siti Nurhaliza');
    expect(tableText).toContain('20260002');
    expect(tableText).toContain('Budi Santoso');
    expect(tableText).toContain('20260003');
  });

  it('filters aged arrears by school level and student search', async () => {
    const agingCalls: string[] = [];
    setupDefaultMsw({
      onAgingRequest: (params) => {
        agingCalls.push(params.toString());
      }
    });

    const wrapper = mountView();
    await flushPromises();

    // Filter by School Level = SMP
    const schoolLevelSelect = wrapper
      .findAllComponents(Select)
      .find((s) => s.attributes('data-testid') === 'filter-school-level');
    expect(schoolLevelSelect).toBeDefined();
    schoolLevelSelect!.vm.$emit('update:modelValue', 'SMP');
    await flushPromises();

    expect(agingCalls.some((q) => q.includes('schoolLevel=SMP'))).toBe(true);
    let tableText = wrapper.find('[data-testid="receivables-aging-table"]').text();
    expect(tableText).toContain('Siti Nurhaliza');
    expect(tableText).not.toContain('Budi Santoso');

    // Reset filter and test client-side search
    await wrapper.find('[data-testid="reset-filters-btn"]').trigger('click');
    await flushPromises();

    const searchInput = wrapper.find('[data-testid="filter-student-search"]');
    await searchInput.setValue('Budi');
    await flushPromises();

    tableText = wrapper.find('[data-testid="receivables-aging-table"]').text();
    expect(tableText).toContain('Budi Santoso');
    expect(tableText).not.toContain('Siti Nurhaliza');
  });

  it('opens StudentStatementDrawer on row Statement click and loads student ledger', async () => {
    const requestedStatements: string[] = [];
    setupDefaultMsw({
      onStatementRequest: (studentId) => requestedStatements.push(studentId)
    });

    const wrapper = mountView();
    await flushPromises();

    const stmtBtn = wrapper.find(`[data-testid="view-statement-btn-${STUDENT_SITI_ID}"]`);
    expect(stmtBtn.exists()).toBe(true);
    expect((stmtBtn.element as HTMLButtonElement).disabled).toBe(false);

    await stmtBtn.trigger('click');
    await flushPromises();

    expect(requestedStatements).toContain(STUDENT_SITI_ID);
    const drawer = wrapper.find('[data-testid="student-statement-drawer"]');
    expect(drawer.exists()).toBe(true);
    expect(drawer.find('[data-testid="drawer-total-billed"]').text()).toContain('1.180.000');
    expect(drawer.find('[data-testid="drawer-total-paid"]').text()).toContain('280.000');
    expect(drawer.find('[data-testid="drawer-total-outstanding"]').text()).toContain('900.000');
    expect(drawer.find('[data-testid="drawer-total-deposit"]').text()).toContain('50.000');
    expect(drawer.text()).toContain('SPP Bulanan SMP');
    expect(drawer.text()).toContain('KWT-202607-0001');
  });

  it('allows Principal/Admin with Finance.Reports.View to view receivables while disabling statement drill-down', async () => {
    const wrapper = mountView({
      roles: ['PRINCIPAL'],
      permissions: ['Permissions.Finance.Reports.View']
    });
    await flushPromises();

    expect(wrapper.find('[data-testid="kpi-total-billed"]').text()).toContain('5.000.000');
    expect(wrapper.find('[data-testid="statement-permission-hint"]').exists()).toBe(true);

    const stmtBtn = wrapper.find(`[data-testid="view-statement-btn-${STUDENT_SITI_ID}"]`);
    expect(stmtBtn.exists()).toBe(true);
    expect((stmtBtn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('switches to Daily Cashier Summary tab, displays KPIs & receipt register, and reloads on date change', async () => {
    const queriedDates: string[] = [];
    setupDefaultMsw({
      onDailyCashierRequest: (date) => queriedDates.push(date)
    });

    const wrapper = mountView();
    await flushPromises();

    await wrapper.find('[data-testid="tab-daily-cashier"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="daily-cashier-tab"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="kpi-cash-collections"]').text()).toContain('350.000');

    // Change date to 2026-07-05
    const dateInput = wrapper.find('[data-testid="cashier-date-picker"]');
    await dateInput.setValue('2026-07-05');
    await flushPromises();

    expect(queriedDates).toContain('2026-07-05');
    expect(wrapper.find('[data-testid="kpi-cash-collections"]').text()).toContain('450.000');
    expect(wrapper.find('[data-testid="kpi-bank-collections"]').text()).toContain('700.000');
    expect(wrapper.find('[data-testid="kpi-deposits-held"]').text()).toContain('50.000');

    const tableText = wrapper.find('[data-testid="daily-cashier-table"]').text();
    expect(tableText).toContain('KWT-202607-0001');
    expect(tableText).toContain('Dewi Cashier');
    expect(tableText).toContain('KWT-202607-0002');
    expect(tableText).toContain('TRF-BCA-8821');

    // Filter receipt register by search text
    await wrapper.find('[data-testid="cashier-search-input"]').setValue('TRF-BCA-8821');
    await flushPromises();

    const filteredText = wrapper.find('[data-testid="daily-cashier-table"]').text();
    expect(filteredText).toContain('KWT-202607-0002');
    expect(filteredText).not.toContain('KWT-202607-0001');
  });
});
