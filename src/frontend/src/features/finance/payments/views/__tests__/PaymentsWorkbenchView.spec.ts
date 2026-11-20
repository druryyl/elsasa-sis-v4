import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
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
import type {
  PaymentReceiptDetailDto,
  PaymentReceiptDto,
  RecordPaymentReceiptRequest,
  AllocateAdvanceDepositRequest,
  StudentBillingContactDto,
  StudentStatementDto
} from '../../api/payments.api';
import PaymentsWorkbenchView from '../PaymentsWorkbenchView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000901';
const STUDENT_1_ID = '01900000-0000-7000-8000-000000000001';
const STUDENT_2_ID = '01900000-0000-7000-8000-000000000002';
const CHARGE_1_ID = '01900000-0000-7000-8000-000000001001';
const CHARGE_2_ID = '01900000-0000-7000-8000-000000001002';
const CHARGE_PAID_ID = '01900000-0000-7000-8000-000000001003';
const RECEIPT_1_ID = '01900000-0000-7000-8000-000000002001';
const RECEIPT_2_ID = '01900000-0000-7000-8000-000000002002';

const FINANCE_PERMISSIONS = [
  'Permissions.Finance.Payments.Record',
  'Permissions.Finance.Deposits.Allocate',
  'Permissions.Finance.Statements.View'
];

function createSampleBillingContacts(): StudentBillingContactDto[] {
  return [
    {
      studentId: STUDENT_1_ID,
      studentNis: '20260001',
      studentName: 'Ahmad Dahlan',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: '01900000-0000-7000-8000-000000000301',
      studyGroupName: '7A',
      primaryGuardianName: 'Haji Agus Salim',
      phoneNumber: '081234567890',
      relationship: 'FATHER',
      whatsAppUrl: 'https://wa.me/6281234567890'
    },
    {
      studentId: STUDENT_2_ID,
      studentNis: '20260002',
      studentName: 'Siti Nurhaliza',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: '01900000-0000-7000-8000-000000000301',
      studyGroupName: '7A',
      primaryGuardianName: 'Rahmawati',
      phoneNumber: '081298765432',
      relationship: 'MOTHER',
      whatsAppUrl: 'https://wa.me/6281298765432'
    }
  ];
}

function createSampleStatement(): StudentStatementDto {
  return {
    studentId: STUDENT_1_ID,
    studentName: 'Ahmad Dahlan',
    studentNis: '20260001',
    studyGroupId: '01900000-0000-7000-8000-000000000301',
    studyGroupName: '7A',
    totalBilled: 850000,
    totalPaid: 350000,
    totalOutstanding: 500000,
    totalDepositBalance: 100000,
    charges: [
      {
        id: CHARGE_1_ID,
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        billingMonth: '2026-08',
        chargeTypeId: '01900000-0000-7000-8000-000000003001',
        chargeTypeName: 'SPP Bulanan SMP',
        chargeFrequency: 'MONTHLY',
        description: 'August 2026 Tuition',
        unitAmount: 350000,
        quantity: 1,
        grossAmount: 350000,
        discountAmount: 100000,
        discountReason: 'Scholarship',
        netAmount: 250000,
        paidAmount: 0,
        outstandingAmount: 250000,
        status: 'UNPAID',
        dueDate: '2026-08-10',
        createdAt: '2026-08-01T00:00:00Z'
      },
      {
        id: CHARGE_2_ID,
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        billingMonth: null,
        chargeTypeId: '01900000-0000-7000-8000-000000003002',
        chargeTypeName: 'Seragam Sekolah',
        chargeFrequency: 'ONE_TIME',
        description: 'School Uniform Package',
        unitAmount: 250000,
        quantity: 1,
        grossAmount: 250000,
        discountAmount: 0,
        discountReason: null,
        netAmount: 250000,
        paidAmount: 0,
        outstandingAmount: 250000,
        status: 'UNPAID',
        dueDate: '2026-08-15',
        createdAt: '2026-08-02T00:00:00Z'
      },
      {
        id: CHARGE_PAID_ID,
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        billingMonth: '2026-07',
        chargeTypeId: '01900000-0000-7000-8000-000000003001',
        chargeTypeName: 'SPP Bulanan SMP',
        chargeFrequency: 'MONTHLY',
        description: 'July 2026 Tuition',
        unitAmount: 350000,
        quantity: 1,
        grossAmount: 350000,
        discountAmount: 0,
        discountReason: null,
        netAmount: 350000,
        paidAmount: 350000,
        outstandingAmount: 0,
        status: 'PAID',
        dueDate: '2026-07-10',
        createdAt: '2026-07-01T00:00:00Z'
      }
    ],
    receipts: [
      {
        id: RECEIPT_1_ID,
        receiptNumber: 'KWT-202607-0001',
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        paymentDate: '2026-07-05',
        paymentMethod: 'CASH',
        bankReference: null,
        totalAmount: 450000,
        allocatedAmount: 350000,
        unallocatedAmount: 100000,
        receivedById: '01900000-0000-7000-8000-000000009999',
        receivedByName: 'Finance Officer',
        notes: 'July SPP + advance deposit',
        createdAt: '2026-07-05T09:00:00Z',
        allocations: [
          {
            id: '01900000-0000-7000-8000-000000004001',
            studentChargeId: CHARGE_PAID_ID,
            chargeTypeName: 'SPP Bulanan SMP',
            billingMonth: '2026-07',
            allocatedAmount: 350000,
            allocationDate: '2026-07-05',
            createdAt: '2026-07-05T09:00:00Z'
          }
        ]
      }
    ]
  };
}

function createSampleReceiptDetail(): PaymentReceiptDetailDto {
  return {
    id: RECEIPT_1_ID,
    receiptNumber: 'KWT-202607-0001',
    academicYearId: ACADEMIC_YEAR_ID,
    academicYearName: '2026/2027',
    studentId: STUDENT_1_ID,
    studentName: 'Ahmad Dahlan',
    studentNis: '20260001',
    studyGroupName: '7A',
    paymentDate: '2026-07-05',
    paymentMethod: 'CASH',
    bankReference: null,
    totalAmount: 450000,
    allocatedAmount: 350000,
    unallocatedAmount: 100000,
    receivedById: '01900000-0000-7000-8000-000000009999',
    receivedByName: 'Finance Officer',
    notes: 'July SPP + advance deposit',
    createdAt: '2026-07-05T09:00:00Z',
    allocations: [
      {
        id: '01900000-0000-7000-8000-000000004001',
        studentChargeId: CHARGE_PAID_ID,
        chargeTypeName: 'SPP Bulanan SMP',
        billingMonth: '2026-07',
        allocatedAmount: 350000,
        allocationDate: '2026-07-05',
        createdAt: '2026-07-05T09:00:00Z'
      }
    ]
  };
}

function configurePermissions(perms: string[], roles: string[] = ['FINANCE_OFFICER']): void {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000009999',
      username: 'finance.officer',
      displayName: 'Finance Officer',
      roles,
      permissions: perms
    }),
    getRoles: () => roles,
    getPermissions: () => perms,
    hasPermission: (perm: string) => perms.includes(perm),
    hasAnyRole: (required: string[]) => required.some((r) => roles.includes(r))
  });
}

async function mountPaymentsView() {
  configureHttpClient({
    getAccessToken: () => 'test-token',
    getContextHeaders: () => ({}),
    refreshAccessToken: async () => null,
    onSessionExpired: () => {}
  });

  configureAppContextReader(() => ({
    activeAcademicYearId: ACADEMIC_YEAR_ID,
    selectedAcademicYearId: ACADEMIC_YEAR_ID,
    activeAcademicYearName: '2026/2027'
  }));

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/finance/payments',
        name: 'finance.payments',
        component: PaymentsWorkbenchView
      }
    ]
  });

  await router.push('/finance/payments');
  await router.isReady();

  const wrapper = mount(PaymentsWorkbenchView, {
    global: {
      plugins: [
        router,
        [VueQueryPlugin, { queryClient }],
        ToastService,
        [PrimeVue, { theme: { preset: Aura } }]
      ],
      stubs: {
        Teleport: true
      }
    }
  });

  await flushPromises();
  return wrapper;
}

function registerDefaultHandlers(): void {
  const contacts = createSampleBillingContacts();
  const statement = createSampleStatement();
  const receiptDetail = createSampleReceiptDetail();

  server.use(
    http.get('/api/opr/academic-years', () =>
      HttpResponse.json({
        items: [{ id: ACADEMIC_YEAR_ID, name: '2026/2027', status: 'ACTIVE' }],
        page: 1,
        pageSize: 100,
        totalCount: 1
      })
    ),
    http.get('/api/std/billing-lookup', ({ request }) => {
      const url = new URL(request.url);
      const search = (url.searchParams.get('Search') ?? '').trim().toLowerCase();
      if (!search) {
        return HttpResponse.json(contacts);
      }
      const filtered = contacts.filter(
        (c) =>
          c.studentName?.toLowerCase().includes(search) ||
          c.studentNis?.toLowerCase().includes(search) ||
          c.primaryGuardianName?.toLowerCase().includes(search)
      );
      return HttpResponse.json(filtered);
    }),
    http.get(`/api/fin/students/${STUDENT_1_ID}/statement`, () => HttpResponse.json(statement)),
    http.get(`/api/fin/payments/${RECEIPT_1_ID}`, () => HttpResponse.json(receiptDetail))
  );
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
  configureAppContextReader(null);
});

afterAll(() => {
  server.close();
});

describe('PaymentsWorkbenchView.vue (SCR-FIN-PAYMENTS)', () => {
  it('shows Access Restricted empty state when user lacks finance payment/statement permissions', async () => {
    configurePermissions(['Permissions.Academic.ReportCards.View'], ['TEACHER']);
    const wrapper = await mountPaymentsView();

    expect(wrapper.text()).toContain('Access Restricted');
    expect(wrapper.find('[data-testid="billing-lookup-input"]').exists()).toBe(false);
  });

  it('displays guardian billing contacts with WhatsApp links and loads student statement on selection', async () => {
    configurePermissions(FINANCE_PERMISSIONS);
    registerDefaultHandlers();

    const wrapper = await mountPaymentsView();

    expect(wrapper.text()).toContain('Ahmad Dahlan');
    expect(wrapper.text()).toContain('Haji Agus Salim');
    const waLink = wrapper.find(`[data-testid="whatsapp-link-${STUDENT_1_ID}"]`);
    expect(waLink.exists()).toBe(true);
    expect(waLink.attributes('href')).toBe('https://wa.me/6281234567890');

    await wrapper.find(`[data-testid="billing-contact-card-${STUDENT_1_ID}"]`).trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="selected-student-banner"]').text()).toContain('Ahmad Dahlan');
    expect(wrapper.find('[data-testid="selected-student-whatsapp"]').attributes('href')).toBe(
      'https://wa.me/6281234567890'
    );
    expect(wrapper.find('[data-testid="kpi-total-outstanding"]').text()).toContain('500.000');
    expect(wrapper.find('[data-testid="kpi-total-deposit"]').text()).toContain('100.000');
    expect(wrapper.find('[data-testid="open-charges-table"]').text()).toContain('SPP Bulanan SMP');
    expect(wrapper.find('[data-testid="open-charges-table"]').text()).toContain('Seragam Sekolah');
    expect(wrapper.find('[data-testid="open-charges-table"]').text()).not.toContain('July 2026 Tuition');
  });

  it('records a multi-charge payment with excess advance deposit held via RecordPaymentModal', async () => {
    configurePermissions(FINANCE_PERMISSIONS);
    registerDefaultHandlers();

    let capturedPayload: RecordPaymentReceiptRequest | null = null;
    server.use(
      http.post('/api/fin/payments', async ({ request }) => {
        capturedPayload = (await request.json()) as RecordPaymentReceiptRequest;
        const created: PaymentReceiptDto = {
          id: RECEIPT_2_ID,
          receiptNumber: 'KWT-202608-0002',
          academicYearId: ACADEMIC_YEAR_ID,
          academicYearName: '2026/2027',
          studentId: STUDENT_1_ID,
          studentName: 'Ahmad Dahlan',
          studentNis: '20260001',
          paymentDate: capturedPayload.paymentDate,
          paymentMethod: capturedPayload.paymentMethod,
          bankReference: capturedPayload.bankReference ?? null,
          totalAmount: capturedPayload.totalAmount,
          allocatedAmount: 250000,
          unallocatedAmount: 50000,
          receivedByName: 'Finance Officer'
        };
        return HttpResponse.json(created, { status: 201 });
      })
    );

    const wrapper = await mountPaymentsView();
    await wrapper.find(`[data-testid="billing-contact-card-${STUDENT_1_ID}"]`).trigger('click');
    await flushPromises();

    await wrapper.find(`[data-testid="pay-charge-btn-${CHARGE_1_ID}"]`).trigger('click');
    await flushPromises();

    const modal = wrapper.find('[data-testid="record-payment-modal"]');
    expect(modal.exists()).toBe(true);

    // Preselected charge allocates 250,000; switch to BANK_TRANSFER and increase total to 300,000
    await modal.find('[data-testid="payment-method-BANK_TRANSFER"]').trigger('click');
    await modal.find('[data-testid="bank-reference-input"]').setValue('BCA-TRF-8899');

    const amountInput = modal.find('#payment-total-amount');
    await amountInput.setValue('300000');
    await amountInput.trigger('blur');
    await flushPromises();

    expect(modal.find('[data-testid="summary-allocated"]').text()).toContain('250.000');
    expect(modal.find('[data-testid="summary-deposit"]').text()).toContain('50.000');

    await modal.find('form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(capturedPayload).not.toBeNull();
    });
    expect(capturedPayload!.paymentMethod).toBe('BANK_TRANSFER');
    expect(capturedPayload!.bankReference).toBe('BCA-TRF-8899');
    expect(capturedPayload!.totalAmount).toBe(300000);
    expect(capturedPayload!.allocations).toEqual([{ chargeId: CHARGE_1_ID, amount: 250000 }]);

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="latest-receipt-banner"]').text()).toContain(
        'KWT-202608-0002'
      );
    });
  });

  it('allocates existing advance deposit to an open charge via DepositAllocationModal', async () => {
    configurePermissions(FINANCE_PERMISSIONS);
    registerDefaultHandlers();

    let capturedDepositPayload: AllocateAdvanceDepositRequest | null = null;
    server.use(
      http.post('/api/fin/deposits/allocate', async ({ request }) => {
        capturedDepositPayload = (await request.json()) as AllocateAdvanceDepositRequest;
        return HttpResponse.json({
          id: RECEIPT_1_ID,
          receiptNumber: 'KWT-202607-0001',
          studentId: STUDENT_1_ID,
          totalAmount: 450000,
          allocatedAmount: 450000,
          unallocatedAmount: 0
        });
      })
    );

    const wrapper = await mountPaymentsView();
    await wrapper.find(`[data-testid="billing-contact-card-${STUDENT_1_ID}"]`).trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="apply-deposit-btn"]').trigger('click');
    await flushPromises();

    const modal = wrapper.find('[data-testid="deposit-allocation-modal"]');
    expect(modal.exists()).toBe(true);
    expect(modal.text()).toContain('100.000');

    const chk = modal.find(`[data-testid="deposit-charge-checkbox-${CHARGE_1_ID}"] input`);
    await chk.setValue(true);
    await flushPromises();

    expect(modal.find('[data-testid="deposit-summary-allocating"]').text()).toContain('100.000');

    await modal.find('form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(capturedDepositPayload).not.toBeNull();
    });
    expect(capturedDepositPayload!.studentId).toBe(STUDENT_1_ID);
    expect(capturedDepositPayload!.sourceReceiptId).toBe(RECEIPT_1_ID);
    expect(capturedDepositPayload!.allocations).toEqual([{ chargeId: CHARGE_1_ID, amount: 100000 }]);
  });

  it('opens StudentStatementDrawer and PaymentReceiptPrintView with @media print support', async () => {
    configurePermissions(['Permissions.Finance.Statements.View'], ['FINANCE_OFFICER']);
    registerDefaultHandlers();

    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});

    const wrapper = await mountPaymentsView();
    await wrapper.find(`[data-testid="billing-contact-card-${STUDENT_1_ID}"]`).trigger('click');
    await flushPromises();

    // User with only Statements.View does not see Record Payment or Apply Deposit buttons
    expect(wrapper.find('[data-testid="record-payment-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="apply-deposit-btn"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="view-statement-btn"]').exists()).toBe(true);

    // Open Student Statement Drawer
    await wrapper.find('[data-testid="view-statement-btn"]').trigger('click');
    await flushPromises();

    const drawer = wrapper.find('[data-testid="student-statement-drawer"]');
    expect(drawer.exists()).toBe(true);
    expect(drawer.find('[data-testid="drawer-total-billed"]').text()).toContain('850.000');
    expect(drawer.text()).toContain('KWT-202607-0001');
    expect(drawer.text()).toContain('July 2026 Tuition');

    // Switch to Payment Receipts tab and open browser print receipt preview
    const tabs = wrapper.findAll('[role="tab"]');
    const receiptsTab = tabs.find((tab) => tab.text().includes('Payment Receipts'));
    await receiptsTab?.trigger('click');
    await flushPromises();

    await wrapper.find(`[data-testid="print-receipt-btn-${RECEIPT_1_ID}"]`).trigger('click');
    await flushPromises();

    const printView = wrapper.find('[data-testid="payment-receipt-print-view"]');
    expect(printView.exists()).toBe(true);
    expect(printView.find('[data-testid="print-receipt-number"]').text()).toBe('KWT-202607-0001');
    expect(printView.text()).toContain('Ahmad Dahlan');
    expect(printView.text()).toContain('SPP Bulanan SMP');
    expect(printView.text()).toContain('Finance Officer');

    await printView.find('[data-testid="receipt-print-trigger-btn"]').trigger('click');
    expect(printSpy).toHaveBeenCalledTimes(1);
    printSpy.mockRestore();

    await printView.find('[data-testid="receipt-print-close-btn"]').trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="payment-receipt-print-view"]').exists()).toBe(false);
  });
});
