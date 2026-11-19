import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import StatusPill from '../StatusPill.vue';

describe('StatusPill.vue Stage 1 status taxonomy', () => {
  it.each([
    { status: 'SCHOOL_DAY', expectedClass: 'bg-emerald-50', expectedLabel: 'School Day' },
    { status: 'HOLIDAY', expectedClass: 'bg-amber-50', expectedLabel: 'Holiday' },
    { status: 'CLOSURE', expectedClass: 'bg-rose-50', expectedLabel: 'Closure' },
    { status: 'ASSIGNED', expectedClass: 'bg-emerald-50', expectedLabel: 'Assigned' }
  ])(
    'maps $status to semantic class $expectedClass',
    ({ status, expectedClass, expectedLabel }) => {
      const wrapper = mount(StatusPill, {
        props: { status }
      });

      const pill = wrapper.get('[data-testid="status-pill"]');
      expect(pill.classes()).toContain(expectedClass);
      expect(pill.text()).toBe(expectedLabel);
    }
  );
});
