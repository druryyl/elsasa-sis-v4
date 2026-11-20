import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureAppContextReader } from '@/shared/services/appContextBridge';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import { configureHttpClient } from '@/shared/services/http';
import BillingWorkbenchView from '../BillingWorkbenchView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000901';
const STUDENT_1_ID = '01900000-0000-7000-8000-000000000001';
const STUDENT_2_ID = '01900000-0000-7000-8000-000000000002';
const CHARGE_TYPE_SPP_ID = '01900000-0000-7000-8000-000000000201';
const CHARGE_TYPE_UNIFORM_ID = '01900000-0000-7000-8000-000000000202';
const CHARGE_UNPAID_ID = '01900000-0000-7000-8000-000000000301';
const CHARGE_PARTIAL_ID = '01900000-0000-7000-8000-000000000302';
const CHARGE_PAID_ID = '01900000-0000-7000-8000-000000000303';

function createAcademicYearsPayload() {
  return {
    items: [
      {
        id: ACADEMIC_YEAR_ID,
        name: '2026/2027',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        status: 'ACTIVE',
        periods: []
      }
    ],
    page: 1,
    pageSize: 100,
    totalCount: 1,
    totalPages: 1
  };
}

function createSampleCharges() {
  return [
    {
      id: CHARGE_UNPAID_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      billingMonth: '2026-07',
      chargeTypeId: CHARGE_TYPE_UNIFORM_ID,
      chargeTypeName: 'Seragam Sekolah',
      chargeFrequency: 'ONE_TIME',
      studentId: STUDENT_1_ID,
      studentName: 'Ahmad Dahlan',
      studentNis: '20260001',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupName: '7A',
      description: 'Seragam Sekolah Paket Lengkap',
      unitAmount: 250000,
      quantity: 1,
      grossAmount: 250000,
      discountAmount: 0,
      discountReason: null,
      netAmount: 250000,
      paidAmount: 0,
      outstandingAmount: 250000,
      status: 'UNPAID',
      dueDate: '2026-07-20'
    },
    {
      id: CHARGE_PARTIAL_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      billingMonth: '2026-07',
      chargeTypeId: CHARGE_TYPE_SPP_ID,
      chargeTypeName: 'SPP Bulanan SMP',
      chargeFrequency: 'MONTHLY',
      studentId: STUDENT_2_ID,
      studentName: 'Siti Nurhaliza',
      studentNis: '20260002',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupName: '7A',
      description: 'SPP Juli 2026',
      unitAmount: 350000,
      quantity: 1,
      grossAmount: 350000,
      discountAmount: 70000,
      discountReason: 'Sibling Discount 20%',
      netAmount: 280000,
      paidAmount: 150000,
      outstandingAmount: 130000,
      status: 'PARTIALLY_PAID',
      dueDate: '2026-07-10'
    },
    {
      id: CHARGE_PAID_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      billingMonth: '2026-07',
      chargeTypeId: CHARGE_TYPE_SPP_ID,
      chargeTypeName: 'SPP Bulanan SMP',
      chargeFrequency: 'MONTHLY',
      studentId: STUDENT_1_ID,
      studentName: 'Ahmad Dahlan',
      studentNis: '20260001',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupName: '7A',
      description: 'SPP Juli 2026',
      unitAmount: 350000,
      quantity: 1,
      grossAmount: 350000,
      discountAmount: 100000,
      discountReason: 'Scholarship',
      netAmount: 250000,
      paidAmount: 250000,
      outstandingAmount: 0,
      status: 'PAID',
      dueDate: '2026-07-10'
    }
  ];
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

async function mountBillingView() {
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

  const wrapper = mount(BillingWorkbenchView, {
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

  await flushPromises();
  return wrapper;
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

describe('BillingWorkbenchView.vue (SCR-FIN-BILLING)', () => {
  it('renders charges table, KPI summary, and disables cancellation with warning tooltip when paidAmount > 0', async () => {
    configurePermissions([
      'Permissions.Finance.Billing.Generate',
      'Permissions.Finance.Billing.ManageAdHoc',
      'Permissions.Finance.Billing.Cancel'
    ]);

    server.use(
      http.get('/api/opr/academic-years', () => HttpResponse.json(createAcademicYearsPayload())),
      http.get('/api/fin/charges', () => HttpResponse.json(createSampleCharges()))
    );

    const wrapper = await mountBillingView();

    expect(wrapper.text()).toContain('Tuition Billing');
    expect(wrapper.find('[data-testid="billing-kpi-count"]').text()).toContain('3');
    expect(wrapper.find('[data-testid="billing-kpi-net"]').text()).toContain('780.000');
    expect(wrapper.find('[data-testid="billing-kpi-paid"]').text()).toContain('400.000');
    expect(wrapper.find('[data-testid="billing-kpi-outstanding"]').text()).toContain('380.000');

    const tableText = wrapper.find('[data-testid="billing-charges-table"]').text();
    expect(tableText).toContain('Ahmad Dahlan');
    expect(tableText).toContain('Siti Nurhaliza');
    expect(tableText).toContain('Sibling Discount 20%');
    expect(tableText).toContain('Unpaid');
    expect(tableText).toContain('Partially Paid');
    expect(tableText).toContain('Paid');

    const cancelButtons = wrapper.findAll('[data-testid="cancel-charge-button"]');
    expect(cancelButtons).toHaveLength(3);

    // Row 0: UNPAID (paidAmount = 0) -> enabled
    expect((cancelButtons[0]!.element as HTMLButtonElement).disabled).toBe(false);

    // Row 1: PARTIALLY_PAID (paidAmount = 150000) -> disabled with warning tooltip
    expect((cancelButtons[1]!.element as HTMLButtonElement).disabled).toBe(true);
    expect(cancelButtons[1]!.element.parentElement?.getAttribute('title')).toContain(
      'Cannot cancel charge with existing payments'
    );

    // Row 2: PAID (paidAmount = 250000) -> disabled with warning tooltip
    expect((cancelButtons[2]!.element as HTMLButtonElement).disabled).toBe(true);
    expect(cancelButtons[2]!.element.parentElement?.getAttribute('title')).toContain(
      'Cannot cancel charge with existing payments'
    );
  });

  it('cancels an unpaid charge with mandatory audit reason and updates the table', async () => {
    configurePermissions([
      'Permissions.Finance.Billing.Generate',
      'Permissions.Finance.Billing.Cancel'
    ]);

    const charges = createSampleCharges();
    let cancelPayload: { cancellationReason?: string } | null = null;

    server.use(
      http.get('/api/opr/academic-years', () => HttpResponse.json(createAcademicYearsPayload())),
      http.get('/api/fin/charges', () => HttpResponse.json(charges)),
      http.post(`/api/fin/charges/${CHARGE_UNPAID_ID}/cancel`, async ({ request }) => {
        cancelPayload = (await request.json()) as { cancellationReason?: string };
        charges[0] = {
          ...charges[0]!,
          status: 'CANCELLED',
          outstandingAmount: 0
        };
        return HttpResponse.json(charges[0]);
      })
    );

    const wrapper = await mountBillingView();

    const cancelButtons = wrapper.findAll('[data-testid="cancel-charge-button"]');
    await cancelButtons[0]!.trigger('click');
    await flushPromises();

    const modal = wrapper.find('[data-testid="charge-cancellation-modal"]');
    expect(modal.exists()).toBe(true);
    expect(modal.text()).toContain('Seragam Sekolah');

    // Empty submit should show validation error and NOT post
    await modal.find('form').trigger('submit');
    await vi.waitFor(() => {
      expect(modal.text()).toContain('Cancellation reason is required.');
    });
    expect(cancelPayload).toBeNull();

    // Enter mandatory audit reason and confirm
    await modal.find('#cancellationReason').setValue('Student already owns uniform');
    await modal.find('form').trigger('submit');
    await vi.waitFor(() => {
      expect(cancelPayload).toEqual({ cancellationReason: 'Student already owns uniform' });
    });
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="billing-charges-table"]').text()).toContain('Cancelled');
    });
  });

  it('executes monthly batch billing with Tier 3 Pre-Flight Impact Summary Modal before commit', async () => {
    configurePermissions(['Permissions.Finance.Billing.Generate']);

    const charges = createSampleCharges();
    let previewRequestBody: Record<string, unknown> | null = null;
    let generateRequestBody: Record<string, unknown> | null = null;

    server.use(
      http.get('/api/opr/academic-years', () => HttpResponse.json(createAcademicYearsPayload())),
      http.get('/api/std/study-groups', () =>
        HttpResponse.json({
          items: [
            {
              id: '01900000-0000-7000-8000-000000000801',
              name: '7A',
              schoolLevel: 'SMP',
              grade: '7'
            }
          ],
          page: 1,
          pageSize: 100,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.get('/api/fin/charges', () => HttpResponse.json(charges)),
      http.post('/api/fin/charges/batch-preview', async ({ request }) => {
        previewRequestBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          studentCount: 2,
          chargeCount: 2,
          skippedCount: 1,
          totalGrossAmount: 700000,
          totalDiscountAmount: 170000,
          totalNetAmount: 530000,
          items: [
            {
              studentId: STUDENT_1_ID,
              studentName: 'Ahmad Dahlan',
              studentNis: '20260001',
              studyGroupName: '7A',
              chargeTypeId: CHARGE_TYPE_SPP_ID,
              chargeTypeName: 'SPP Bulanan SMP',
              grossAmount: 350000,
              discountAmount: 100000,
              discountReason: 'Scholarship',
              netAmount: 250000,
              isAlreadyBilled: false
            },
            {
              studentId: STUDENT_2_ID,
              studentName: 'Siti Nurhaliza',
              studentNis: '20260002',
              studyGroupName: '7A',
              chargeTypeId: CHARGE_TYPE_SPP_ID,
              chargeTypeName: 'SPP Bulanan SMP',
              grossAmount: 350000,
              discountAmount: 70000,
              discountReason: 'Sibling Discount 20%',
              netAmount: 280000,
              isAlreadyBilled: false
            }
          ]
        });
      }),
      http.post('/api/fin/charges/generate-batch', async ({ request }) => {
        generateRequestBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ generatedCount: 2, skippedCount: 1 });
      })
    );

    const wrapper = await mountBillingView();

    await wrapper.find('[data-testid="open-batch-billing-button"]').trigger('click');
    await flushPromises();

    const batchModal = wrapper.find('[data-testid="batch-billing-modal"]');
    expect(batchModal.exists()).toBe(true);

    await batchModal.find('#batchBillingMonth').setValue('2026-08');
    await batchModal.find('#batchDueDate').setValue('2026-08-10');
    await batchModal.find('form').trigger('submit');

    await vi.waitFor(() => {
      expect(previewRequestBody).toMatchObject({
        academicYearId: ACADEMIC_YEAR_ID,
        billingMonth: '2026-08'
      });
    });
    expect(generateRequestBody).toBeNull();

    // Tier 3 Pre-Flight Impact Summary Modal should now be visible
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="preflight-summary-modal"]').exists()).toBe(true);
    });
    const preFlightModal = wrapper.find('[data-testid="preflight-summary-modal"]');
    expect(preFlightModal.find('[data-testid="preflight-kpi-students"]').text()).toContain('2');
    expect(preFlightModal.find('[data-testid="preflight-kpi-gross"]').text()).toContain('700.000');
    expect(preFlightModal.find('[data-testid="preflight-kpi-discounts"]').text()).toContain(
      '170.000'
    );
    expect(preFlightModal.find('[data-testid="preflight-kpi-net"]').text()).toContain('530.000');
    expect(preFlightModal.find('[data-testid="preflight-preview-table"]').text()).toContain(
      'Ahmad Dahlan'
    );

    await preFlightModal.find('[data-testid="preflight-confirm-button"]').trigger('click');

    await vi.waitFor(() => {
      expect(generateRequestBody).toMatchObject({
        academicYearId: ACADEMIC_YEAR_ID,
        billingMonth: '2026-08',
        dueDate: '2026-08-10'
      });
    });
  });

  it('creates an ad-hoc invoice charge and enforces permission gating', async () => {
    configurePermissions([
      'Permissions.Finance.Billing.Generate',
      'Permissions.Finance.Billing.ManageAdHoc'
    ]);

    const charges = createSampleCharges();
    let adHocPayload: Record<string, unknown> | null = null;

    server.use(
      http.get('/api/opr/academic-years', () => HttpResponse.json(createAcademicYearsPayload())),
      http.get('/api/fin/charges', () => HttpResponse.json(charges)),
      http.get('/api/fin/charge-types', () =>
        HttpResponse.json([
          {
            id: CHARGE_TYPE_UNIFORM_ID,
            academicYearId: ACADEMIC_YEAR_ID,
            name: 'Seragam Sekolah',
            frequency: 'ONE_TIME',
            amount: 250000,
            status: 'ACTIVE'
          }
        ])
      ),
      http.get('/api/std/students', () =>
        HttpResponse.json({
          items: [
            {
              id: STUDENT_1_ID,
              nis: '20260001',
              fullName: 'Ahmad Dahlan',
              schoolLevel: 'SMP',
              grade: '7',
              studyGroupName: '7A',
              status: 'ACTIVE'
            }
          ],
          page: 1,
          pageSize: 50,
          totalCount: 1,
          totalPages: 1
        })
      ),
      http.post('/api/fin/charges/adhoc', async ({ request }) => {
        adHocPayload = (await request.json()) as Record<string, unknown>;
        const created = {
          ...charges[0]!,
          id: '01900000-0000-7000-8000-000000000999',
          description: String(adHocPayload.description ?? ''),
          quantity: Number(adHocPayload.quantity ?? 1),
          netAmount: 500000,
          outstandingAmount: 500000
        };
        charges.push(created);
        return HttpResponse.json(created, { status: 201 });
      })
    );

    const wrapper = await mountBillingView();

    // Cancel permission was not granted -> Cancel Charge buttons hidden
    expect(wrapper.findAll('[data-testid="cancel-charge-button"]')).toHaveLength(0);

    await wrapper.find('[data-testid="open-adhoc-charge-button"]').trigger('click');
    await flushPromises();

    const adHocModal = wrapper.find('[data-testid="adhoc-charge-modal"]');
    expect(adHocModal.exists()).toBe(true);

    // Wait for active charge types & students queries to resolve
    await vi.waitFor(() => {
      const selects = adHocModal.findAllComponents({ name: 'Select' });
      expect((selects[1]!.props('options') as unknown[]).length).toBeGreaterThan(0);
      expect((selects[2]!.props('options') as unknown[]).length).toBeGreaterThan(0);
    });

    const selects = adHocModal.findAllComponents({ name: 'Select' });
    // selects: [0] AcademicYear, [1] Student, [2] ChargeType
    selects[1]!.vm.$emit('update:modelValue', STUDENT_1_ID);
    selects[2]!.vm.$emit('update:modelValue', CHARGE_TYPE_UNIFORM_ID);
    await flushPromises();

    await adHocModal.find('#adhocDescription').setValue('Extra PE Uniform Set');
    const numberInputs = adHocModal.findAllComponents({ name: 'InputNumber' });
    numberInputs[1]!.vm.$emit('update:modelValue', 2);
    await flushPromises();

    await adHocModal.find('form').trigger('submit');

    await vi.waitFor(() => {
      expect(adHocPayload).toMatchObject({
        academicYearId: ACADEMIC_YEAR_ID,
        studentId: STUDENT_1_ID,
        chargeTypeId: CHARGE_TYPE_UNIFORM_ID,
        description: 'Extra PE Uniform Set',
        unitAmount: 250000,
        quantity: 2
      });
    });
  });

  it('shows access restricted state when user lacks billing permissions', async () => {
    configurePermissions(['Permissions.Students.Directory.View'], ['TEACHER']);

    server.use(
      http.get('/api/opr/academic-years', () => HttpResponse.json(createAcademicYearsPayload()))
    );

    const wrapper = await mountBillingView();
    expect(wrapper.text()).toContain('Access Restricted');
    expect(wrapper.find('[data-testid="open-batch-billing-button"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="open-adhoc-charge-button"]').exists()).toBe(false);
  });
});
