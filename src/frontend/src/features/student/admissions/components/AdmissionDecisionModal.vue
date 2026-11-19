<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { AlertCircle, AlertTriangle, CheckCircle2, UserCheck, XCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import type { CandidateDto, CandidateSummaryDto } from '../api/admissions.api';
import { useRecordAdmissionDecisionMutation } from '../api/useAdmissionsMutations';

const props = defineProps<{
  visible: boolean;
  candidate: CandidateSummaryDto | CandidateDto | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  decided: [candidate: CandidateDto];
}>();

const decisionMutation = useRecordAdmissionDecisionMutation();
const isSubmitting = computed(() => decisionMutation.isPending.value);

const selectedDecision = ref<'ACCEPTED' | 'REJECTED'>('ACCEPTED');
const decisionNotes = ref('');
const conflictBanner = ref<string | null>(null);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      selectedDecision.value = 'ACCEPTED';
      decisionNotes.value = '';
      conflictBanner.value = null;
    }
  },
  { immediate: true }
);

const isReject = computed(() => selectedDecision.value === 'REJECTED');

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

async function handleConfirm(): Promise<void> {
  if (!props.candidate?.id) return;
  conflictBanner.value = null;

  try {
    const updated = await decisionMutation.mutateAsync({
      id: props.candidate.id,
      payload: {
        decision: selectedDecision.value,
        decisionNotes: decisionNotes.value.trim() || null
      }
    });

    emit('decided', updated);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      conflictBanner.value = error.detail || error.title || 'Failed to record admission decision.';
      return;
    }
    conflictBanner.value = 'An unexpected error occurred while saving decision.';
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :closable="!isSubmitting"
    :draggable="false"
    class="w-full max-w-lg"
    data-testid="admission-decision-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full"
          :class="isReject ? 'bg-rose-100 text-rose-600' : 'bg-brand-accent-50 text-brand-accent-600'"
        >
          <UserCheck v-if="!isReject" class="h-5 w-5" />
          <AlertTriangle v-else class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Record Admission Decision</h3>
          <p class="text-xs text-slate-500">1-Step Acceptance or Rejection Review</p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <!-- Error / Conflict Alert -->
      <div
        v-if="conflictBanner"
        role="alert"
        data-testid="decision-conflict-alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <span>{{ conflictBanner }}</span>
      </div>

      <!-- Candidate Information Callout -->
      <div class="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
        <div class="font-medium text-slate-900" data-testid="decision-candidate-name">
          {{ candidate?.fullName ?? 'Unknown Candidate' }}
        </div>
        <div class="mt-1 flex flex-wrap gap-x-4 text-xs text-slate-600">
          <span>Target: <strong class="font-semibold text-slate-700">{{ candidate?.targetSchoolLevel }} Grade {{ candidate?.targetGrade }}</strong></span>
          <span v-if="candidate?.admissionYearName">Year: {{ candidate.admissionYearName }}</span>
        </div>
      </div>

      <!-- Decision Selection Segmented Radio -->
      <div class="space-y-2">
        <label class="block text-sm font-medium text-slate-700">Admission Decision</label>
        <div class="grid grid-cols-2 gap-3">
          <button
            type="button"
            data-testid="decision-accept-btn"
            class="flex items-center justify-center gap-2 rounded-lg border px-3 py-2.5 text-sm font-medium transition-all"
            :class="
              selectedDecision === 'ACCEPTED'
                ? 'border-emerald-600 bg-emerald-50/70 text-emerald-800 ring-2 ring-emerald-500/20'
                : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
            "
            :disabled="isSubmitting"
            @click="selectedDecision = 'ACCEPTED'"
          >
            <CheckCircle2 class="h-4 w-4 text-emerald-600" />
            <span>Accept Candidate</span>
          </button>

          <button
            type="button"
            data-testid="decision-reject-btn"
            class="flex items-center justify-center gap-2 rounded-lg border px-3 py-2.5 text-sm font-medium transition-all"
            :class="
              selectedDecision === 'REJECTED'
                ? 'border-rose-600 bg-rose-50/70 text-rose-800 ring-2 ring-rose-500/20'
                : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
            "
            :disabled="isSubmitting"
            @click="selectedDecision = 'REJECTED'"
          >
            <XCircle class="h-4 w-4 text-rose-600" />
            <span>Reject Candidate</span>
          </button>
        </div>
      </div>

      <!-- Tier 2 Confirmation Tone on Reject -->
      <div
        v-if="isReject"
        role="alert"
        data-testid="decision-reject-warning"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-3 text-xs leading-relaxed text-amber-900"
      >
        <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div>
          <p class="font-semibold text-amber-950">Tier 2 Confirmation Required</p>
          <p class="mt-0.5">
            Rejecting this applicant is a terminal decision for this admission cycle. The candidate will not proceed to enrollment unless re-registered.
          </p>
        </div>
      </div>

      <!-- Decision Notes -->
      <FormField
        label="Decision Notes"
        field-id="decision-notes"
        :hint="`${decisionNotes.length}/500 characters`"
      >
        <Textarea
          id="decision-notes"
          v-model="decisionNotes"
          data-testid="decision-notes-input"
          rows="3"
          maxlength="500"
          placeholder="Add admission committee notes, rationale, or interview feedback..."
          class="w-full"
          :disabled="isSubmitting"
        />
      </FormField>
    </div>

    <template #footer>
      <div class="flex w-full items-center justify-end gap-2 border-t border-slate-200 pt-3">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeModal"
        />
        <Button
          type="button"
          :severity="isReject ? 'danger' : 'success'"
          :label="isReject ? 'Confirm Rejection' : 'Confirm Acceptance'"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="decision-submit-btn"
          @click="handleConfirm"
        />
      </div>
    </template>
  </Dialog>
</template>
