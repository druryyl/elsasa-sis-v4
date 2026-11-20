<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { AlertCircle, ClipboardList } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { formatBackendDateOnly, formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  ExtracurricularActivityDto,
  ExtracurricularParticipationDto
} from '../api/extracurricular.api';
import { useRecordSessionLogMutation } from '../api/useExtracurricularMutations';
import {
  sessionLogSchema,
  type SessionLogFormValues
} from '../schemas/extracurricular.schema';
import { resolveFormSubmissionError } from '../utils/formErrors';

const props = defineProps<{
  visible: boolean;
  activity: ExtracurricularActivityDto | null;
  participation: ExtracurricularParticipationDto | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  recorded: [participation: ExtracurricularParticipationDto];
}>();

const recordMutation = useRecordSessionLogMutation();
const isSubmitting = computed(() => recordMutation.isPending.value);
const errorBanner = ref<string | null>(null);

function buildInitialValues(): SessionLogFormValues {
  return {
    sessionDate: formatBackendDateOnly(new Date()),
    log: ''
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<SessionLogFormValues>({
  validationSchema: toTypedSchema(sessionLogSchema),
  initialValues: buildInitialValues()
});

const [sessionDate, sessionDateAttrs] = defineField('sessionDate');
const [log, logAttrs] = defineField('log');

watch(
  () => [props.visible, props.participation?.id] as const,
  ([isOpen]) => {
    if (isOpen) {
      errorBanner.value = null;
      resetForm({
        values: buildInitialValues()
      });
    }
  }
);

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onSubmit = handleSubmit(async (values) => {
  if (!props.activity?.id || !props.participation?.id) return;
  errorBanner.value = null;

  try {
    const updated = await recordMutation.mutateAsync({
      activityId: props.activity.id,
      participationId: props.participation.id,
      payload: {
        sessionDate: values.sessionDate,
        log: values.log.trim()
      }
    });

    emit('recorded', updated);
    emit('update:visible', false);
  } catch (error) {
    errorBanner.value = resolveFormSubmissionError(
      error,
      setErrors,
      'Failed to record session activity log.'
    );
  }
});
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :draggable="false"
    :closable="!isSubmitting"
    class="w-full max-w-lg"
    data-testid="session-log-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-accent-50 text-brand-accent-600"
        >
          <ClipboardList class="h-4 w-4" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Record Session Activity Log</h3>
          <p class="text-xs text-slate-500">
            {{ activity?.name ?? 'Club' }} • {{ participation?.studentName ?? 'Participant' }}
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="errorBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="flex-1">{{ errorBanner }}</div>
      </div>

      <!-- Student Summary Banner -->
      <div
        v-if="participation"
        class="rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs text-slate-700"
      >
        <div class="flex items-center justify-between gap-2">
          <span class="font-semibold text-slate-900">{{ participation.studentName }}</span>
          <span class="font-mono text-slate-500">{{ participation.studentNis }}</span>
        </div>
        <div v-if="participation.lastSessionLog" class="mt-1.5 border-t border-slate-200 pt-1.5">
          <span class="font-medium text-slate-600">
            Previous log ({{ formatDisplayDate(participation.lastSessionDate, '—') }}):
          </span>
          <span class="ml-1 text-slate-600">{{ participation.lastSessionLog }}</span>
        </div>
      </div>

      <FormField
        label="Session Date"
        field-id="session-log-date"
        required
        :error="errors.sessionDate"
      >
        <InputText
          id="session-log-date"
          v-model="sessionDate"
          v-bind="sessionDateAttrs"
          type="date"
          class="w-full"
          :invalid="Boolean(errors.sessionDate)"
          data-testid="session-log-date"
        />
      </FormField>

      <FormField
        label="Meeting Topics & Participant Notes"
        field-id="session-log-content"
        required
        :error="errors.log"
        :hint="`${log?.length ?? 0} / 1000 characters`"
      >
        <Textarea
          id="session-log-content"
          v-model="log"
          v-bind="logAttrs"
          rows="4"
          placeholder="Record meeting topics covered, attendance remarks, and student engagement notes..."
          class="w-full"
          :invalid="Boolean(errors.log)"
          data-testid="session-log-content"
        />
      </FormField>

      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeModal"
        />
        <Button
          type="submit"
          label="Save Session Log"
          :loading="isSubmitting"
          data-testid="submit-session-log-button"
        />
      </div>
    </form>
  </Dialog>
</template>
