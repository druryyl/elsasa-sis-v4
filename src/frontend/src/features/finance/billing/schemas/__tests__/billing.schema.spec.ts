import { describe, expect, it } from 'vitest';
import {
  adHocChargeSchema,
  batchBillingSchema,
  cancelChargeSchema
} from '../billing.schema';

describe('billing.schema', () => {
  it('validates batchBillingSchema with valid YYYY-MM month and optional cohort filters', () => {
    const valid = batchBillingSchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000901',
      billingMonth: '2026-07',
      schoolLevel: 'SMP',
      grade: '7',
      studyGroupId: null,
      dueDate: '2026-07-10'
    });
    expect(valid.success).toBe(true);

    const invalidMonth = batchBillingSchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000901',
      billingMonth: '2026-13'
    });
    expect(invalidMonth.success).toBe(false);
    if (!invalidMonth.success) {
      expect(invalidMonth.error.issues[0]?.message).toContain('YYYY-MM');
    }
  });

  it('validates adHocChargeSchema constraints matching backend FluentValidation', () => {
    const valid = adHocChargeSchema.safeParse({
      academicYearId: '01900000-0000-7000-8000-000000000901',
      studentId: '01900000-0000-7000-8000-000000000001',
      chargeTypeId: '01900000-0000-7000-8000-000000000101',
      description: 'School uniform package',
      unitAmount: 250000,
      quantity: 2,
      billingMonth: '',
      dueDate: ''
    });
    expect(valid.success).toBe(true);

    const invalid = adHocChargeSchema.safeParse({
      academicYearId: '',
      studentId: '',
      chargeTypeId: '',
      description: '   ',
      unitAmount: -100,
      quantity: 0,
      billingMonth: 'bad-month'
    });
    expect(invalid.success).toBe(false);
  });

  it('requires non-empty cancellation reason up to 500 chars', () => {
    expect(cancelChargeSchema.safeParse({ cancellationReason: '   ' }).success).toBe(false);
    expect(
      cancelChargeSchema.safeParse({ cancellationReason: 'a'.repeat(501) }).success
    ).toBe(false);
    expect(
      cancelChargeSchema.safeParse({ cancellationReason: 'Duplicate billing entry' }).success
    ).toBe(true);
  });
});
