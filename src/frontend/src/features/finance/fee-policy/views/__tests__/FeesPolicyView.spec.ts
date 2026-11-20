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
import type { ChargeTypeDto, StudentDiscountDto } from '../../api/feePolicy.api';
import FeesPolicyView from '../FeesPolicyView.vue';

const server = setupServer();

const ACADEMIC_YEAR_ID = '01900000-0000-7000-8000-000000000100';
const CHARGE_SPP_SMP_ID = '01900000-0000-7000-8000-000000000201';
const CHARGE_UNIFORM_DRAFT_ID = '01900000-0000-7000-8000-000000000202';
const STUDENT_AHMAD_ID = '01900000-0000-7000-8000-000000000301';
const STUDENT_SITI_ID = '01900000-0000-7000-8000-000000000302';
const DISCOUNT_1_ID = '01900000-0000-7000-8000-000000000401';

function createInitialChargeTypes(): ChargeTypeDto[] {
  return [
    {
      id: CHARGE_SPP_SMP_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      name: 'SPP Bulanan SMP',
      frequency: 'MONTHLY',
      amount: 350000,
      schoolLevel: 'SMP',
      grade: null,
      studyGroupId: null,
      studyGroupName: null,
      status: 'ACTIVE',
      createdAt: '2026-07-01T08:00:00Z',
      updatedAt: '2026-07-01T08:00:00Z'
    },
    {
      id: CHARGE_UNIFORM_DRAFT_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      name: 'Seragam Sekolah',
      frequency: 'ONE_TIME',
      amount: 250000,
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: null,
      studyGroupName: null,
      status: 'DRAFT',
      createdAt: '2026-07-02T08:00:00Z',
      updatedAt: '2026-07-02T08:00:00Z'
    }
  ];
}

function createInitialDiscounts(): StudentDiscountDto[] {
  return [
    {
      id: DISCOUNT_1_ID,
      academicYearId: ACADEMIC_YEAR_ID,
      academicYearName: '2026/2027',
      chargeTypeId: CHARGE_SPP_SMP_ID,
      chargeTypeName: 'SPP Bulanan SMP',
      studentId: STUDENT_SITI_ID,
      studentName: 'Siti Nurhaliza',
      studentNis: '20260002',
      discountType: 'PERCENTAGE',
      discountValue: 20,
      reason: 'Sibling discount 20%',
      isActive: true,
      createdAt: '2026-07-05T08:00:00Z',
      updatedAt: '2026-07-05T08:00:00Z'
    }
  ];
}

function setupDefaultMsw(options?: {
  onCreateChargeType?: (body: Record<string, unknown>) => void;
  onUpdateChargeType?: (id: string, body: Record<string, unknown>) => void;
  onActivateChargeType?: (id: string) => void;
  onCreateDiscount?: (body: Record<string, unknown>) => void;
  onRevokeDiscount?: (id: string) => void;
  conflictOnCreateDiscount?: boolean;
}) {
  const chargeTypes = createInitialChargeTypes();
  const discounts = createInitialDiscounts();

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
    http.get('/api/std/study-groups', () =>
      HttpResponse.json({
        items: [
          {
            id: '01900000-0000-7000-8000-000000000601',
            academicYearId: ACADEMIC_YEAR_ID,
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
    http.get('/api/std/students', ({ request }) => {
      const url = new URL(request.url);
      const search = (url.searchParams.get('Search') ?? '').toLowerCase();
      const items = [
        {
          id: STUDENT_AHMAD_ID,
          nis: '20260001',
          fullName: 'Ahmad Dahlan',
          studyGroupName: '7A',
          grade: '7',
          status: 'ACTIVE'
        },
        {
          id: STUDENT_SITI_ID,
          nis: '20260002',
          fullName: 'Siti Nurhaliza',
          studyGroupName: '7A',
          grade: '7',
          status: 'ACTIVE'
        }
      ].filter(
        (s) =>
          !search ||
          s.fullName.toLowerCase().includes(search) ||
          s.nis.toLowerCase().includes(search)
      );

      return HttpResponse.json({
        items,
        page: 1,
        pageSize: 50,
        totalCount: items.length,
        totalPages: 1
      });
    }),
    http.get('/api/fin/charge-types', () => HttpResponse.json(chargeTypes)),
    http.post('/api/fin/charge-types', async ({ request }) => {
      const body = (await request.json()) as Record<string, unknown>;
      options?.onCreateChargeType?.(body);
      const created: ChargeTypeDto = {
        id: '01900000-0000-7000-8000-000000000299',
        academicYearId: String(body.academicYearId ?? ACADEMIC_YEAR_ID),
        academicYearName: '2026/2027',
        name: String(body.name ?? ''),
        frequency: String(body.frequency ?? 'MONTHLY'),
        amount: Number(body.amount ?? 0),
        schoolLevel: (body.schoolLevel as string | null) ?? null,
        grade: (body.grade as string | null) ?? null,
        studyGroupId: (body.studyGroupId as string | null) ?? null,
        studyGroupName: (body.studyGroupName as string | null) ?? null,
        status: 'DRAFT',
        createdAt: '2026-11-20T08:00:00Z',
        updatedAt: '2026-11-20T08:00:00Z'
      };
      chargeTypes.push(created);
      return HttpResponse.json(created, { status: 201 });
    }),
    http.put('/api/fin/charge-types/:id', async ({ params, request }) => {
      const id = String(params.id);
      const body = (await request.json()) as Record<string, unknown>;
      options?.onUpdateChargeType?.(id, body);
      const existing = chargeTypes.find((c) => c.id === id);
      if (existing) {
        existing.name = String(body.name ?? existing.name);
        existing.frequency = String(body.frequency ?? existing.frequency);
        existing.amount = Number(body.amount ?? existing.amount);
      }
      return HttpResponse.json(existing);
    }),
    http.post('/api/fin/charge-types/:id/activate', ({ params }) => {
      const id = String(params.id);
      options?.onActivateChargeType?.(id);
      const existing = chargeTypes.find((c) => c.id === id);
      if (existing) {
        existing.status = 'ACTIVE';
      }
      return HttpResponse.json(existing);
    }),
    http.get('/api/fin/discounts', () => HttpResponse.json(discounts)),
    http.post('/api/fin/discounts', async ({ request }) => {
      if (options?.conflictOnCreateDiscount) {
        return HttpResponse.json(
          {
            title: 'Duplicate Student Discount',
            status: 409,
            detail: 'An active discount already exists for this student and fee structure.'
          },
          { status: 409 }
        );
      }
      const body = (await request.json()) as Record<string, unknown>;
      options?.onCreateDiscount?.(body);
      const created: StudentDiscountDto = {
        id: '01900000-0000-7000-8000-000000000499',
        academicYearId: ACADEMIC_YEAR_ID,
        academicYearName: '2026/2027',
        chargeTypeId: String(body.chargeTypeId ?? CHARGE_SPP_SMP_ID),
        chargeTypeName: 'SPP Bulanan SMP',
        studentId: String(body.studentId ?? STUDENT_AHMAD_ID),
        studentName: String(body.studentName ?? 'Ahmad Dahlan'),
        studentNis: String(body.studentNis ?? '20260001'),
        discountType: String(body.discountType ?? 'FIXED_AMOUNT'),
        discountValue: Number(body.discountValue ?? 100000),
        reason: String(body.reason ?? ''),
        isActive: true,
        createdAt: '2026-11-20T08:00:00Z',
        updatedAt: '2026-11-20T08:00:00Z'
      };
      discounts.push(created);
      return HttpResponse.json(created, { status: 201 });
    }),
    http.post('/api/fin/discounts/:id/revoke', ({ params }) => {
      const id = String(params.id);
      options?.onRevokeDiscount?.(id);
      const existing = discounts.find((d) => d.id === id);
      if (existing) {
        existing.isActive = false;
      }
      return HttpResponse.json(existing);
    })
  );
}

function mountView(options?: { permissions?: string[] }) {
  const permissions = options?.permissions ?? [
    'Permissions.Finance.FeePolicy.Manage',
    'Permissions.Finance.Discounts.Manage'
  ];

  configureAuthSessionReader({
    getUser: () => ({
      id: 'usr-fin-1',
      username: 'finance.officer',
      displayName: 'Finance Officer',
      roles: ['FINANCE_OFFICER'],
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

  return mount(FeesPolicyView, {
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
  if (typeof globalThis.ResizeObserver === 'undefined') {
    globalThis.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;
  }
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

describe('FeesPolicyView.vue', () => {
  it('renders Fee Catalog tab with fee structures, frequencies, amounts, and DRAFT/ACTIVE badges', async () => {
    setupDefaultMsw();
    const wrapper = mountView();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="fee-catalog-table"]').exists()).toBe(true);
    });

    expect(wrapper.text()).toContain('SPP Bulanan SMP');
    expect(wrapper.text()).toContain('Monthly');
    expect(wrapper.text()).toContain('350.000');
    expect(wrapper.text()).toContain('Seragam Sekolah');
    expect(wrapper.text()).toContain('250.000');
    expect(wrapper.find(`[data-testid="locked-fee-badge-${CHARGE_SPP_SMP_ID}"]`).exists()).toBe(
      true
    );
    expect(wrapper.find(`[data-testid="edit-fee-button-${CHARGE_SPP_SMP_ID}"]`).exists()).toBe(
      false
    );
    expect(
      wrapper.find(`[data-testid="edit-fee-button-${CHARGE_UNIFORM_DRAFT_ID}"]`).exists()
    ).toBe(true);
    expect(
      wrapper.find(`[data-testid="activate-fee-button-${CHARGE_UNIFORM_DRAFT_ID}"]`).exists()
    ).toBe(true);
  });

  it('creates a new fee structure through ChargeTypeDrawer', async () => {
    const onCreateChargeType = vi.fn();
    setupDefaultMsw({ onCreateChargeType });
    const wrapper = mountView();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="new-fee-structure-button"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="new-fee-structure-button"]').trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="charge-type-name-input"]').setValue('SPP Bulanan SMA');
    const amountInput = wrapper.find('#charge-type-amount');
    await amountInput.setValue('450000');
    await amountInput.trigger('blur');

    await wrapper.find('#charge-type-form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(onCreateChargeType).toHaveBeenCalledTimes(1);
    });

    expect(onCreateChargeType).toHaveBeenCalledWith(
      expect.objectContaining({
        academicYearId: ACADEMIC_YEAR_ID,
        name: 'SPP Bulanan SMA',
        frequency: 'MONTHLY',
        amount: 450000
      })
    );
  });

  it('edits a DRAFT fee structure via ChargeTypeDrawer', async () => {
    const onUpdateChargeType = vi.fn();
    setupDefaultMsw({ onUpdateChargeType });
    const wrapper = mountView();

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="edit-fee-button-${CHARGE_UNIFORM_DRAFT_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper
      .find(`[data-testid="edit-fee-button-${CHARGE_UNIFORM_DRAFT_ID}"]`)
      .trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="charge-type-name-input"]').setValue('Seragam Olahraga');
    await wrapper.find('#charge-type-form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(onUpdateChargeType).toHaveBeenCalledWith(
        CHARGE_UNIFORM_DRAFT_ID,
        expect.objectContaining({
          name: 'Seragam Olahraga'
        })
      );
    });
  });

  it('activates a DRAFT fee structure with Tier 2 confirmation warning', async () => {
    const onActivateChargeType = vi.fn();
    setupDefaultMsw({ onActivateChargeType });
    const wrapper = mountView();

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="activate-fee-button-${CHARGE_UNIFORM_DRAFT_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper
      .find(`[data-testid="activate-fee-button-${CHARGE_UNIFORM_DRAFT_ID}"]`)
      .trigger('click');
    await flushPromises();

    expect(wrapper.text()).toContain('Activate Fee Structure');
    expect(wrapper.text()).toContain('cannot be modified');

    const confirmButton = wrapper
      .findAll('button')
      .find((btn) => btn.text().includes('Activate & Lock Fee'));
    expect(confirmButton).toBeDefined();
    await confirmButton!.trigger('click');
    await flushPromises();

    await vi.waitFor(() => {
      expect(onActivateChargeType).toHaveBeenCalledWith(CHARGE_UNIFORM_DRAFT_ID);
    });
  });

  it('switches to Student Discounts tab, grants a new discount, and revokes an existing discount', async () => {
    const onCreateDiscount = vi.fn();
    const onRevokeDiscount = vi.fn();
    setupDefaultMsw({ onCreateDiscount, onRevokeDiscount });
    const wrapper = mountView();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="tab-student-discounts"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="tab-student-discounts"]').trigger('click');
    await flushPromises();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="student-discounts-table"]').exists()).toBe(true);
    });

    expect(wrapper.text()).toContain('Siti Nurhaliza');
    expect(wrapper.text()).toContain('20%');
    expect(wrapper.text()).toContain('Sibling discount 20%');

    // Grant new discount
    await wrapper.find('[data-testid="grant-discount-button"]').trigger('click');
    await flushPromises();

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="discount-student-option-${STUDENT_AHMAD_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper
      .find(`[data-testid="discount-student-option-${STUDENT_AHMAD_ID}"]`)
      .trigger('click');

    // Select fee structure via PrimeVue Select
    const feeSelect = wrapper
      .findAllComponents(Select)
      .find((s) => s.props('inputId') === 'discount-charge-type');
    expect(feeSelect).toBeDefined();
    feeSelect!.vm.$emit('update:modelValue', CHARGE_SPP_SMP_ID);
    await flushPromises();

    await wrapper.find('[data-testid="discount-reason-input"]').setValue('Merit Scholarship');
    await wrapper.find('#grant-discount-form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(onCreateDiscount).toHaveBeenCalledWith(
        expect.objectContaining({
          studentId: STUDENT_AHMAD_ID,
          chargeTypeId: CHARGE_SPP_SMP_ID,
          discountType: 'PERCENTAGE',
          discountValue: 10,
          reason: 'Merit Scholarship'
        })
      );
    });

    // Revoke existing discount
    await wrapper.find(`[data-testid="revoke-discount-button-${DISCOUNT_1_ID}"]`).trigger('click');
    await flushPromises();

    expect(wrapper.text()).toContain('Revoke Standing Discount');
    const revokeConfirmBtn = wrapper
      .findAll('button')
      .find((btn) => btn.text().includes('Revoke Discount'));
    expect(revokeConfirmBtn).toBeDefined();
    await revokeConfirmBtn!.trigger('click');
    await flushPromises();

    await vi.waitFor(() => {
      expect(onRevokeDiscount).toHaveBeenCalledWith(DISCOUNT_1_ID);
    });
  });

  it('displays conflict alert banner when granting duplicate standing discount returns 409', async () => {
    setupDefaultMsw({ conflictOnCreateDiscount: true });
    const wrapper = mountView();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="tab-student-discounts"]').exists()).toBe(true);
    });

    await wrapper.find('[data-testid="tab-student-discounts"]').trigger('click');
    await flushPromises();

    await wrapper.find('[data-testid="grant-discount-button"]').trigger('click');
    await flushPromises();

    await vi.waitFor(() => {
      expect(
        wrapper.find(`[data-testid="discount-student-option-${STUDENT_SITI_ID}"]`).exists()
      ).toBe(true);
    });

    await wrapper
      .find(`[data-testid="discount-student-option-${STUDENT_SITI_ID}"]`)
      .trigger('click');
    const feeSelect = wrapper
      .findAllComponents(Select)
      .find((s) => s.props('inputId') === 'discount-charge-type');
    expect(feeSelect).toBeDefined();
    feeSelect!.vm.$emit('update:modelValue', CHARGE_SPP_SMP_ID);
    await wrapper.find('[data-testid="discount-reason-input"]').setValue('Duplicate test');
    await wrapper.find('#grant-discount-form').trigger('submit');
    await flushPromises();

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="grant-discount-error-banner"]').exists()).toBe(true);
    });

    expect(wrapper.find('[data-testid="grant-discount-error-banner"]').text()).toContain(
      'already exists'
    );
  });

  it('shows Access Restricted state when user lacks finance policy and discount permissions', async () => {
    setupDefaultMsw();
    const wrapper = mountView({ permissions: [] });
    await flushPromises();

    expect(wrapper.text()).toContain('Access Restricted');
    expect(wrapper.find('[data-testid="fee-catalog-tab"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="student-discounts-tab"]').exists()).toBe(false);
  });
});
