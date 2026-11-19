import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import WorkbenchLayout from '../WorkbenchLayout.vue';

describe('WorkbenchLayout.vue', () => {
  it('renders header, left worklist pane, and right detail workbench slots', () => {
    const wrapper = mount(WorkbenchLayout, {
      props: {
        title: 'Admissions Processing Queue',
        subtitle: 'Review and enroll prospective students'
      },
      slots: {
        actions: '<button type="button">Register Candidate</button>',
        'worklist-header': '<input aria-label="Search candidates" />',
        worklist: '<div>Candidate Row 1</div>',
        'worklist-footer': '<div>Total: 1</div>',
        'workbench-header': '<div>Candidate Summary</div>',
        workbench: '<div>Candidate Details</div>'
      }
    });

    expect(wrapper.text()).toContain('Admissions Processing Queue');
    expect(wrapper.text()).toContain('Register Candidate');
    expect(wrapper.get('[data-testid="workbench-worklist-pane"]').text()).toContain(
      'Candidate Row 1'
    );
    expect(wrapper.get('[data-testid="workbench-worklist-pane"]').text()).toContain('Total: 1');
    expect(wrapper.get('[data-testid="workbench-detail-pane"]').text()).toContain(
      'Candidate Summary'
    );
    expect(wrapper.get('[data-testid="workbench-detail-pane"]').text()).toContain(
      'Candidate Details'
    );
  });

  it('toggles responsive mobile panes via hasSelection and emits back when Back button is clicked', async () => {
    const wrapper = mount(WorkbenchLayout, {
      props: {
        hasSelection: false,
        backLabel: 'Back to queue'
      },
      slots: {
        worklist: '<div>Queue List</div>',
        workbench: '<div>Detail View</div>'
      }
    });

    expect(wrapper.get('[data-testid="workbench-worklist-pane"]').classes()).toContain('flex');
    expect(wrapper.get('[data-testid="workbench-worklist-pane"]').classes()).not.toContain(
      'hidden'
    );
    expect(wrapper.get('[data-testid="workbench-detail-pane"]').classes()).toContain('hidden');
    expect(wrapper.get('[data-testid="workbench-detail-pane"]').classes()).not.toContain('flex');
    expect(wrapper.find('[data-testid="workbench-back-button"]').exists()).toBe(false);

    await wrapper.setProps({ hasSelection: true });

    expect(wrapper.get('[data-testid="workbench-worklist-pane"]').classes()).toContain('hidden');
    expect(wrapper.get('[data-testid="workbench-worklist-pane"]').classes()).not.toContain('flex');
    expect(wrapper.get('[data-testid="workbench-detail-pane"]').classes()).toContain('flex');
    expect(wrapper.get('[data-testid="workbench-detail-pane"]').classes()).not.toContain('hidden');

    const backButton = wrapper.get('[data-testid="workbench-back-button"]');
    expect(backButton.text()).toContain('Back to queue');
    await backButton.trigger('click');

    expect(wrapper.emitted('back')).toHaveLength(1);
  });
});
