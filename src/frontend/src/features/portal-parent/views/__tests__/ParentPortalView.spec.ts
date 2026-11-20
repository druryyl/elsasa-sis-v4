import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import ParentPortalView from '../ParentPortalView.vue';

const CHILD_1_ID = '01a1f11a-7010-7010-8000-000000000010';
const CHILD_2_ID = '01a1f11a-7011-7011-8000-000000000011';

const MOCK_CHILDREN = [
  {
    studentId: CHILD_1_ID,
    nis: '2026001',
    nisn: '0012345678',
    fullName: 'Ahmad Dahlan',
    nickname: 'Ahmad',
    dateOfBirth: '2010-05-15T00:00:00Z',
    gender: 'MALE',
    studyGroupId: 'sg-10-tkj-1',
    studyGroupName: 'X TKJ 1',
    schoolLevel: 'SMK',
    grade: 'Kelas 10'
  },
  {
    studentId: CHILD_2_ID,
    nis: '2026002',
    nisn: '0012345679',
    fullName: 'Siti Nurhaliza',
    nickname: 'Siti',
    dateOfBirth: '2012-08-20T00:00:00Z',
    gender: 'FEMALE',
    studyGroupId: 'sg-8-smp-1',
    studyGroupName: 'VIII A',
    schoolLevel: 'SMP',
    grade: 'Kelas 8'
  }
];

const MOCK_REPORT_CARDS_CHILD_1 = [
  {
    id: 'rc-101',
    studentId: CHILD_1_ID,
    studentName: 'Ahmad Dahlan',
    academicYearName: '2026/2027',
    operationalPeriodName: 'Semester Ganjil',
    studyGroupName: 'X TKJ 1',
    status: 'FINALIZED',
    finalizedAt: '2026-11-15T10:00:00Z'
  }
];

const MOCK_REPORT_CARD_DETAIL_1 = {
  id: 'rc-101',
  studentId: CHILD_1_ID,
  studentName: 'Ahmad Dahlan',
  academicYearName: '2026/2027',
  operationalPeriodName: 'Semester Ganjil',
  studyGroupName: 'X TKJ 1',
  homeroomTeacherName: 'Budi Santoso, S.Pd',
  status: 'FINALIZED',
  generalRemarks: 'Ahmad menunjukkan ketertarikan tinggi pada instalasi jaringan komputer.',
  finalizedAt: '2026-11-15T10:00:00Z',
  averageScore: 90.0,
  totalSubjects: 2,
  entries: [
    {
      id: 'rce-1',
      subjectId: 'sub-mat',
      subjectName: 'Matematika Terapan',
      numericScore: 88,
      letterGrade: 'A',
      teacherNotes: 'Sangat baik dalam aljabar boolean.'
    },
    {
      id: 'rce-2',
      subjectId: 'sub-jrg',
      subjectName: 'Dasar Komputer dan Jaringan',
      numericScore: 92,
      letterGrade: 'A',
      teacherNotes: 'Penguasaan routing mikrotik sangat memuaskan.'
    }
  ],
  attendance: {
    presentCount: 78,
    absentCount: 0,
    sickCount: 2,
    lateCount: 1,
    excusedCount: 0,
    totalRecordedDays: 80
  }
};

const MOCK_REPORT_CARDS_CHILD_2 = [
  {
    id: 'rc-201',
    studentId: CHILD_2_ID,
    studentName: 'Siti Nurhaliza',
    academicYearName: '2026/2027',
    operationalPeriodName: 'Semester Ganjil',
    studyGroupName: 'VIII A',
    status: 'FINALIZED',
    finalizedAt: '2026-11-16T09:00:00Z'
  }
];

const MOCK_REPORT_CARD_DETAIL_2 = {
  id: 'rc-201',
  studentId: CHILD_2_ID,
  studentName: 'Siti Nurhaliza',
  academicYearName: '2026/2027',
  operationalPeriodName: 'Semester Ganjil',
  studyGroupName: 'VIII A',
  homeroomTeacherName: 'Dewi Lestari, M.Pd',
  status: 'FINALIZED',
  generalRemarks: 'Siti berprestasi sangat baik dan aktif dalam kegiatan sains.',
  finalizedAt: '2026-11-16T09:00:00Z',
  averageScore: 95.0,
  totalSubjects: 1,
  entries: [
    {
      id: 'rce-21',
      subjectId: 'sub-ipa',
      subjectName: 'Ilmu Pengetahuan Alam',
      numericScore: 95,
      letterGrade: 'A',
      teacherNotes: 'Memahami konsep biologi dengan luar biasa.'
    }
  ],
  attendance: {
    presentCount: 79,
    absentCount: 0,
    sickCount: 1,
    lateCount: 0,
    excusedCount: 0,
    totalRecordedDays: 80
  }
};

const MOCK_STATEMENT_CHILD_1 = {
  studentId: CHILD_1_ID,
  studentName: 'Ahmad Dahlan',
  studentNis: '2026001',
  studyGroupId: 'sg-10-tkj-1',
  studyGroupName: 'X TKJ 1',
  totalBilled: 1200000,
  totalPaid: 600000,
  totalOutstanding: 600000,
  totalDepositBalance: 250000,
  charges: [
    {
      id: 'chg-1',
      billingMonth: '2026-07',
      chargeTypeName: 'SPP Juli 2026',
      grossAmount: 600000,
      discountAmount: 0,
      netAmount: 600000,
      paidAmount: 600000,
      outstandingAmount: 0,
      status: 'PAID',
      dueDate: '2026-07-10'
    },
    {
      id: 'chg-2',
      billingMonth: '2026-08',
      chargeTypeName: 'SPP Agustus 2026',
      grossAmount: 600000,
      discountAmount: 0,
      netAmount: 600000,
      paidAmount: 0,
      outstandingAmount: 600000,
      status: 'UNPAID',
      dueDate: '2026-08-10'
    }
  ],
  receipts: [
    {
      id: 'rcp-1',
      receiptNumber: 'RCP-2026-0001',
      paymentDate: '2026-07-05',
      paymentMethod: 'BANK_TRANSFER',
      totalAmount: 600000,
      allocatedAmount: 600000,
      unallocatedAmount: 0,
      notes: 'Pembayaran SPP Juli via transfer BSI',
      allocations: [
        {
          id: 'alc-1',
          studentChargeId: 'chg-1',
          chargeTypeName: 'SPP Juli 2026',
          billingMonth: '2026-07',
          allocatedAmount: 600000
        }
      ]
    }
  ]
};

const MOCK_STATEMENT_CHILD_2 = {
  studentId: CHILD_2_ID,
  studentName: 'Siti Nurhaliza',
  studentNis: '2026002',
  studyGroupId: 'sg-8-smp-1',
  studyGroupName: 'VIII A',
  totalBilled: 500000,
  totalPaid: 500000,
  totalOutstanding: 0,
  totalDepositBalance: 500000,
  charges: [
    {
      id: 'chg-21',
      billingMonth: '2026-07',
      chargeTypeName: 'SPP Juli 2026',
      grossAmount: 500000,
      discountAmount: 0,
      netAmount: 500000,
      paidAmount: 500000,
      outstandingAmount: 0,
      status: 'PAID',
      dueDate: '2026-07-10'
    }
  ],
  receipts: [
    {
      id: 'rcp-21',
      receiptNumber: 'RCP-2026-0021',
      paymentDate: '2026-07-06',
      paymentMethod: 'CASH',
      totalAmount: 500000,
      allocatedAmount: 500000,
      unallocatedAmount: 0,
      notes: 'Pembayaran tunai kasir'
    }
  ]
};

const MOCK_ANNOUNCEMENTS = {
  items: [
    {
      id: 'ann-1',
      title: 'Pertemuan Orang Tua & Guru Semester Ganjil',
      content: 'Undangan silaturahmi dan koordinasi program belajar semester ganjil.',
      targetAudience: 'PARENTS',
      status: 'PUBLISHED',
      publishedAt: '2026-11-10T08:00:00Z',
      authorName: 'Humas Elsasa'
    }
  ],
  page: 1,
  pageSize: 50,
  totalCount: 1,
  totalPages: 1
};

const MOCK_CALENDAR = {
  items: [
    {
      id: 'cal-1',
      entryDate: '2026-11-25',
      description: 'Hari Guru Nasional',
      dayType: 'SCHOOL_DAY'
    },
    {
      id: 'cal-2',
      entryDate: '2026-12-25',
      description: 'Libur Akhir Semester Ganjil',
      dayType: 'HOLIDAY'
    }
  ],
  page: 1,
  pageSize: 100,
  totalCount: 2,
  totalPages: 1
};

const server = setupServer(
  http.get('/api/std/students/my-children', () => HttpResponse.json(MOCK_CHILDREN)),
  http.get('/api/acd/report-cards/student/:studentId', ({ params }) => {
    if (params.studentId === CHILD_2_ID) {
      return HttpResponse.json(MOCK_REPORT_CARDS_CHILD_2);
    }
    return HttpResponse.json(MOCK_REPORT_CARDS_CHILD_1);
  }),
  http.get('/api/acd/report-cards/:id', ({ params }) => {
    if (params.id === 'rc-201') {
      return HttpResponse.json(MOCK_REPORT_CARD_DETAIL_2);
    }
    return HttpResponse.json(MOCK_REPORT_CARD_DETAIL_1);
  }),
  http.get('/api/fin/students/:studentId/statement', ({ params }) => {
    if (params.studentId === CHILD_2_ID) {
      return HttpResponse.json(MOCK_STATEMENT_CHILD_2);
    }
    return HttpResponse.json(MOCK_STATEMENT_CHILD_1);
  }),
  http.get('/api/fin/payments/:receiptId', ({ params }) => {
    return HttpResponse.json({
      id: params.receiptId,
      receiptNumber: 'RCP-2026-0001',
      studentName: 'Ahmad Dahlan',
      paymentDate: '2026-07-05',
      paymentMethod: 'BANK_TRANSFER',
      totalAmount: 600000,
      allocatedAmount: 600000,
      unallocatedAmount: 0,
      allocations: []
    });
  }),
  http.get('/api/opr/announcements', () => HttpResponse.json(MOCK_ANNOUNCEMENTS)),
  http.get('/api/opr/calendar-entries', () => HttpResponse.json(MOCK_CALENDAR)),
  http.get('/api/opr/calendar/entries', () => HttpResponse.json(MOCK_CALENDAR))
);

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  vi.restoreAllMocks();
});

afterAll(() => {
  server.close();
});

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(ParentPortalView, {
    global: {
      plugins: [
        [VueQueryPlugin, { queryClient }],
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

describe('ParentPortalView.vue', () => {
  beforeEach(() => {
    vi.spyOn(window, 'print').mockImplementation(() => {});
  });

  it('renders child selector tabs and defaults to first child', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="parent-portal-view"]').exists()).toBe(true);
    expect(wrapper.find(`[data-testid="child-tab-${CHILD_1_ID}"]`).exists()).toBe(true);
    expect(wrapper.find(`[data-testid="child-tab-${CHILD_2_ID}"]`).exists()).toBe(true);

    // Ahmad Dahlan is first child and selected by default
    expect(wrapper.text()).toContain('Ahmad Dahlan');
    expect(wrapper.text()).toContain('Matematika Terapan');
  });

  it('switches active child and updates report cards and billing queries', async () => {
    let capturedReportCardStudentId = '';
    let capturedStatementStudentId = '';

    server.use(
      http.get('/api/acd/report-cards/student/:studentId', ({ params }) => {
        capturedReportCardStudentId = String(params.studentId);
        if (params.studentId === CHILD_2_ID) {
          return HttpResponse.json(MOCK_REPORT_CARDS_CHILD_2);
        }
        return HttpResponse.json(MOCK_REPORT_CARDS_CHILD_1);
      }),
      http.get('/api/fin/students/:studentId/statement', ({ params }) => {
        capturedStatementStudentId = String(params.studentId);
        if (params.studentId === CHILD_2_ID) {
          return HttpResponse.json(MOCK_STATEMENT_CHILD_2);
        }
        return HttpResponse.json(MOCK_STATEMENT_CHILD_1);
      })
    );

    const wrapper = createWrapper();
    await flushPromises();

    expect(capturedReportCardStudentId).toBe(CHILD_1_ID);

    // Click Siti Nurhaliza (Child 2) tab
    const child2Tab = wrapper.find(`[data-testid="child-tab-${CHILD_2_ID}"]`);
    expect(child2Tab.exists()).toBe(true);
    await child2Tab.trigger('click');
    await flushPromises();

    // Query switches to Child 2
    expect(capturedReportCardStudentId).toBe(CHILD_2_ID);
    expect(wrapper.text()).toContain('Ilmu Pengetahuan Alam');

    // Switch to Billing & Receipts tab
    const billingTabTrigger = wrapper.find('[data-testid="tab-trigger-billing"]');
    await billingTabTrigger.trigger('click');
    await flushPromises();

    // Statement query called for Child 2
    expect(capturedStatementStudentId).toBe(CHILD_2_ID);
    expect(wrapper.find('[data-testid="advance-deposit-banner"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('RCP-2026-0021');
  });

  it('triggers print dialog without throwing errors from report cards and receipts', async () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});

    const wrapper = createWrapper();
    await flushPromises();

    // Trigger report card print
    const printReportBtn = wrapper.find('[data-testid="print-report-card-btn"]');
    expect(printReportBtn.exists()).toBe(true);
    await printReportBtn.trigger('click');
    await flushPromises();

    expect(printSpy).toHaveBeenCalledTimes(1);

    // Switch to billing tab and trigger receipt print
    const billingTabTrigger = wrapper.find('[data-testid="tab-trigger-billing"]');
    await billingTabTrigger.trigger('click');
    await flushPromises();

    const printReceiptBtn = wrapper.find(`[data-testid="print-receipt-btn-rcp-1"]`);
    expect(printReceiptBtn.exists()).toBe(true);
    await printReceiptBtn.trigger('click');
    await flushPromises();

    expect(printSpy).toHaveBeenCalledTimes(2);
  });

  it('renders empty states correctly when no children or records exist', async () => {
    server.use(
      http.get('/api/std/students/my-children', () => HttpResponse.json([]))
    );

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="empty-children-state"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Tidak Ada Siswa Terhubung');
  });

  it('renders empty state when child has no finalized report cards', async () => {
    server.use(
      http.get('/api/acd/report-cards/student/:studentId', () => HttpResponse.json([]))
    );

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="empty-report-cards"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Belum Ada Rapor Final');
  });
});
