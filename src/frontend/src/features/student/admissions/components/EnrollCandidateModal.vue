<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import { AlertCircle, AlertTriangle, GraduationCap, Loader2, RefreshCw } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import { admissionsApi, type CandidateDto, type CandidateSummaryDto, type EnrolledStudentResponse } from '../api/admissions.api';
import { useEnrollCandidateMutation } from '../api/useAdmissionsMutations';

const props = defineProps<{
  visible: boolean;
  candidate: CandidateSummaryDto | CandidateDto | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  enrolled: [student: EnrolledStudentResponse];
}>();

const enrollMutation = useEnrollCandidateMutation();
const isSubmitting = computed(() => enrollMutation.isPending.value);

const isLoadingNis = ref(false);
const nis = ref('');
const nisn = ref('');
const enrollmentDate = ref('');
const conflictBanner = ref<string | null>(null);

function getTodayIsoDate(): string {
  return new Date().toISOString().split('T')[0] ?? '';
}

async function fetchSuggestedNis(): Promise<void> {
  if (!props.candidate?.id) return;
  isLoadingNis.value = true;
  conflictBanner.value = null;

  try {
    const res = await admissionsApi.getNextNis(props.candidate.id, enrollmentDate.value || undefined);
    if (res.suggestedNis) {
      nis.value = res.suggestedNis;
    }
  } catch (error) {
    if (error instanceof ApiError) {
      conflictBanner.value = error.detail || error.title || 'Unable to generate sequential NIS suggestion.';
    } else {
      conflictBanner.value = 'Network error fetching sequential NIS.';
    }
  } finally {
    isLoadingNis.value = false;
  }
}

watch(
  () => [props.visible, props.candidate?.id] as const,
  async ([isOpen, id]) => {
    if (isOpen && id) {
      conflictBanner.value = null;
      nis.value = '';
      nisn.value = '';
      enrollmentDate.value = getTodayIsoDate();
      await fetchSuggestedNis();
    }
  },
  { immediate: true }
);

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

async function handleEnroll(): Promise<void> {
  if (!props.candidate?.id || !nis.value.trim() || !enrollmentDate.value) {
    return;
  }

  conflictBanner.value = null;

  try {
    const result = await enrollMutation.mutateAsync({
      id: props.candidate.id,
      payload: {
        nis: nis.value.trim(),
        nisn: nisn.value.trim() || null,
        enrollmentDate: enrollmentDate.value
      }
    });

    emit('enrolled', result);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 409) {
        // Clean 409 Conflict banner per completion criterion 3
        conflictBanner.value =
          error.detail || `Conflict: NIS ${nis.value} is already assigned to another student. Please enter a different NIS override.`;
        return;
      }

      if (error.status === 422 && error.errors) {
        const firstError = Object.values(error.errors)[0]?.[0];
        conflictBanner.value = firstError || error.detail || 'Validation failed. Please verify the enrollment inputs.';
        return;
      }

      conflictBanner.value = error.detail || error.title || 'Enrollment failed.';
      return;
    }

    conflictBanner.value = 'An unexpected error occurred during candidate enrollment.';
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
    data-testid="enroll-candidate-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
          <GraduationCap class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Enroll Candidate</h3>
          <p class="text-xs text-slate-500">Tier 3 Pre-Flight Impact Confirmation</p>
        </div>
      </div>
    </template>

    <div class="space-y-4 pt-1">
      <!-- 409 Conflict / Error Banner -->
      <div
        v-if="conflictBanner"
        role="alert"
        data-testid="enroll-conflict-alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <div>
          <p class="font-semibold text-rose-950">Enrollment Conflict</p>
          <p class="mt-0.5 text-xs text-rose-800 leading-relaxed">{{ conflictBanner }}</p>
        </div>
      </div>

      <!-- Tier 3 Pre-Flight Impact Notice -->
      <div
        data-testid="enroll-impact-notice"
        class="rounded-lg border border-amber-200 bg-amber-50/70 p-3.5 text-xs leading-relaxed text-amber-900"
      >
        <div class="flex items-center gap-1.5 font-semibold text-amber-950">
          <AlertTriangle class="h-4 w-4 text-amber-600" />
          <span>Tier 3 Pre-Flight Operational Impact</span>
        </div>
        <ul class="mt-2 list-disc pl-5 space-y-1 text-amber-800">
          <li>Converts prospective applicant into official active student (status <strong>ACTIVE</strong>).</li>
          <li>Clones guardian contacts to official student guardian records.</li>
          <li>Assigns official Student Identification Number (NIS) and marks admission as <strong>ENROLLED</strong>.</li>
        </ul>
      </div>

      <!-- Candidate Target Summary -->
      <div class="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
        <div class="font-medium text-slate-900" data-testid="enroll-candidate-name">
          {{ candidate?.fullName ?? 'Unknown Candidate' }}
        </div>
        <div class="mt-1 flex flex-wrap gap-x-4 text-xs text-slate-600">
          <span>Target: <strong class="font-semibold text-slate-700">{{ candidate?.targetSchoolLevel }} Grade {{ candidate?.targetGrade }}</strong></span>
          <span v-if="candidate?.admissionYearName">Year: {{ candidate.admissionYearName }}</span>
        </div>
      </div>

      <!-- Form Inputs: NIS, NISN, Enrollment Date -->
      <div class="space-y-3.5">
        <FormField
          label="Student Identification Number (NIS)"
          field-id="enroll-nis"
          required
          hint="Pre-filled sequential NIS. You may override if institutional scheme differs."
        >
          <div class="relative flex items-center">
            <InputText
              id="enroll-nis"
              v-model="nis"
              data-testid="enroll-nis-input"
              placeholder="e.g. 20260001"
              class="w-full pr-10"
              :disabled="isSubmitting || isLoadingNis"
            />
            <button
              type="button"
              title="Recalculate suggested NIS"
              data-testid="refresh-nis-button"
              class="absolute right-2 p-1 text-slate-400 hover:text-slate-600 transition-colors"
              :disabled="isSubmitting || isLoadingNis"
              @click="fetchSuggestedNis"
            >
              <Loader2 v-if="isLoadingNis" class="h-4 w-4 animate-spin text-brand-accent-600" />
              <RefreshCw v-else class="h-4 w-4" />
            </button>
          </div>
        </FormField>

        <FormField
          label="National Student ID (NISN)"
          field-id="enroll-nisn"
          hint="Optional 10-digit national identifier (Kemendikbudristek)."
        >
          <InputText
            id="enroll-nisn"
            v-model="nisn"
            data-testid="enroll-nisn-input"
            placeholder="e.g. 0012345678"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>

        <FormField
          label="Official Enrollment Date"
          field-id="enroll-date"
          required
        >
          <InputText
            id="enroll-date"
            v-model="enrollmentDate"
            data-testid="enroll-date-input"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
          />
        </FormField>
      </div>
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
          severity="success"
          label="Confirm & Enroll Student"
          :loading="isSubmitting"
          :disabled="isSubmitting || isLoadingNis || !nis.trim() || !enrollmentDate"
          data-testid="enroll-submit-button"
          @click="handleEnroll"
        />
      </div>
    </template>
  </Dialog>
</template>
