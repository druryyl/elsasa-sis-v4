import { describe, expect, it } from 'vitest';
import { chargeTypeSchema, grantDiscountSchema } from '../feePolicy.schema';

describe('feePolicy.schema', () => {
  describe('chargeTypeSchema', () => {
    it('accepts a valid fee structure payload', () => {
      const result = chargeTypeSchema.safeParse({
        academicYearId: '01900000-0000-7000-8000-000000000100',
        name: 'SPP Bulanan SMP',
        frequency: 'MONTHLY',
        amount: 350000,
        schoolLevel: 'SMP',
        grade: '7',
        studyGroupId: ''
      });

      expect(result.success).toBe(true);
    });

    it('rejects negative amount and overly long fee names', () => {
      const result = chargeTypeSchema.safeParse({
        academicYearId: '01900000-0000-7000-8000-000000000100',
        name: 'A'.repeat(101),
        frequency: 'MONTHLY',
        amount: -1000
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.issues.map((i) => i.path[0]);
        expect(paths).toContain('name');
        expect(paths).toContain('amount');
      }
    });
  });

  describe('grantDiscountSchema', () => {
    it('accepts valid percentage and fixed amount discounts', () => {
      const pct = grantDiscountSchema.safeParse({
        studentId: '01900000-0000-7000-8000-000000000401',
        chargeTypeId: '01900000-0000-7000-8000-000000000501',
        discountType: 'PERCENTAGE',
        discountValue: 20,
        reason: 'Sibling discount 20%'
      });
      const fixed = grantDiscountSchema.safeParse({
        studentId: '01900000-0000-7000-8000-000000000401',
        chargeTypeId: '01900000-0000-7000-8000-000000000501',
        discountType: 'FIXED_AMOUNT',
        discountValue: 100000,
        reason: 'Academic merit scholarship'
      });

      expect(pct.success).toBe(true);
      expect(fixed.success).toBe(true);
    });

    it('rejects percentage discounts exceeding 100% and empty reasons', () => {
      const result = grantDiscountSchema.safeParse({
        studentId: '01900000-0000-7000-8000-000000000401',
        chargeTypeId: '01900000-0000-7000-8000-000000000501',
        discountType: 'PERCENTAGE',
        discountValue: 120,
        reason: '   '
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        const paths = result.error.issues.map((i) => i.path[0]);
        expect(paths).toContain('discountValue');
        expect(paths).toContain('reason');
      }
    });

    it('rejects non-positive discount values', () => {
      const result = grantDiscountSchema.safeParse({
        studentId: '01900000-0000-7000-8000-000000000401',
        chargeTypeId: '01900000-0000-7000-8000-000000000501',
        discountType: 'FIXED_AMOUNT',
        discountValue: 0,
        reason: 'Scholarship'
      });

      expect(result.success).toBe(false);
    });
  });
});
