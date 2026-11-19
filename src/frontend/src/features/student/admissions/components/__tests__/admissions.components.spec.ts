import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import type { CandidateSummaryDto } from '../../api/admissions.api';
import AdmissionDecisionModal from '../AdmissionDecisionModal.vue';
import EnrollCandidateModal from '../EnrollCandidateModal.vue';

const server = setupServer();

const CANDIDATE_ID = '01900000-0000-7000-8000-000000000301';

const sampleCandidate: CandidateSummaryDto = {
  id: CANDIDATE_ID,
  admissionYearId: '01900000-0000-7000-8000-000000000101',
  admissionYearName: '2025/2026',
  fullName: 'Muhammad Farhan',
  targetSchoolLevel: 'SMP',
  targetGrade: '7',
  admissionStatus: 'ACCEPTED'
};

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
});

afterAll(() => {
  server.close();
});

describe('AdmissionDecisionModal.vue', () => {
  it('renders 1-step Accept/Reject, displays Tier 2 warning on reject, and submits decision', async () => {
    let capturedPayload: unknown = null;
    server.use(
      http.post('/api/std/candidates/:id/decision', async ({ request }) => {
        capturedPayload = await request.json();
        return HttpResponse.json({
          ...sampleCandidate,
          admissionStatus: (capturedPayload as { decision: string }).decision
        });
      })
    );

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } }
    });

    const wrapper = mount(AdmissionDecisionModal, {
      props: {
        visible: true,
        candidate: { ...sampleCandidate, admissionStatus: 'REGISTERED' }
      },
      global: {
        plugins: [
          [VueQueryPlugin, { queryClient }],
          ToastService,
          [PrimeVue, { theme: { preset: Aura } }]
        ],
        stubs: { Teleport: true }
      }
    });

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="admission-decision-modal"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="decision-candidate-name"]').text()).toBe('Muhammad Farhan');
    });

    // Default is ACCEPTED
    expect(wrapper.find('[data-testid="decision-reject-warning"]').exists()).toBe(false);

    // Switch to REJECTED
    await wrapper.find('[data-testid="decision-reject-btn"]').trigger('click');
    expect(wrapper.find('[data-testid="decision-reject-warning"]').exists()).toBe(true);

    // Enter notes and submit
    await wrapper.find('[data-testid="decision-notes-input"]').setValue('Does not meet requirement');
    await wrapper.find('[data-testid="decision-submit-btn"]').trigger('click');

    await vi.waitFor(() => {
      expect(capturedPayload).toEqual({
        decision: 'REJECTED',
        decisionNotes: 'Does not meet requirement'
      });
    });
  });
});

describe('EnrollCandidateModal.vue', () => {
  it('fetches sequential NIS on open, allows override, and renders clean 409 conflict banner', async () => {
    let enrollPayload: unknown = null;
    let enrollAttempts = 0;

    server.use(
      http.get('/api/std/candidates/:id/next-nis', () =>
        HttpResponse.json({ suggestedNis: '20260010' })
      ),
      http.post('/api/std/candidates/:id/enroll', async ({ request }) => {
        enrollAttempts++;
        enrollPayload = await request.json();

        if ((enrollPayload as { nis: string }).nis === '20260010' && enrollAttempts === 1) {
          return HttpResponse.json(
            {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
              title: 'Conflict',
              status: 409,
              detail: 'Student Identification Number (NIS) 20260010 is already assigned.'
            },
            { status: 409, headers: { 'Content-Type': 'application/problem+json' } }
          );
        }

        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000000501',
            candidateId: CANDIDATE_ID,
            nis: (enrollPayload as { nis: string }).nis,
            fullName: 'Muhammad Farhan',
            status: 'ACTIVE',
            enrollmentDate: '2026-07-01'
          },
          { status: 201 }
        );
      })
    );

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } }
    });

    const wrapper = mount(EnrollCandidateModal, {
      props: {
        visible: true,
        candidate: sampleCandidate
      },
      global: {
        plugins: [
          [VueQueryPlugin, { queryClient }],
          ToastService,
          [PrimeVue, { theme: { preset: Aura } }]
        ],
        stubs: { Teleport: true }
      }
    });

    // Wait for suggested NIS to be fetched
    await vi.waitFor(() => {
      const input = wrapper.find<HTMLInputElement>('[data-testid="enroll-nis-input"]');
      expect(input.element.value).toBe('20260010');
    });

    // Tier 3 Pre-Flight Impact notice must be visible
    expect(wrapper.find('[data-testid="enroll-impact-notice"]').exists()).toBe(true);

    // Submit with 20260010 (triggers 409)
    await wrapper.find('[data-testid="enroll-submit-button"]').trigger('click');

    await vi.waitFor(() => {
      const conflictAlert = wrapper.find('[data-testid="enroll-conflict-alert"]');
      expect(conflictAlert.exists()).toBe(true);
      expect(conflictAlert.text()).toContain('20260010 is already assigned');
    });

    // Override NIS to 20260011 and optional NISN
    await wrapper.find('[data-testid="enroll-nis-input"]').setValue('20260011');
    await wrapper.find('[data-testid="enroll-nisn-input"]').setValue('0012345678');
    await wrapper.find('[data-testid="enroll-submit-button"]').trigger('click');

    await vi.waitFor(() => {
      expect(enrollPayload).toEqual({
        nis: '20260011',
        nisn: '0012345678',
        enrollmentDate: expect.any(String)
      });
      expect(wrapper.emitted('enrolled')).toBeTruthy();
      const emitted = wrapper.emitted('enrolled');
      expect(emitted?.[0]?.[0]).toMatchObject({
        id: '01900000-0000-7000-8000-000000000501',
        nis: '20260011'
      });
    });
  });
});
