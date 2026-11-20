<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { AlertTriangle, ShieldBan } from 'lucide-vue-next';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import FormField from '@/shared/components/forms/FormField.vue';
import { useRecordEscalatedSanctionMutation } from '../api/usePastoralMutations';
import type { DisciplineRecordDto } from '../api/pastoral.api';
import {
  ESCALATED_SANCTION_OPTIONS,
  escalatedSanctionSchema,
  extractSubmissionError,
  getTodayIsoDate,
  type EscalatedSanctionFormValues,
  type PastoralStudentTarget
} from '../schemas/pastoral.schema';
import StudentContextBanner from './StudentContextBanner.vue';

const props = defineProps<{
  visible: boolean;
  student: PastoralStudentTarget | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  recorded: [record: DisciplineRecordDto];
}>();

const mutation = useRecordEscalatedSanctionMutation();
const isSubmitting = computed(() => mutation.isPending.value);
const serverError = ref<string | null>(null);
const confirmVisible = ref(false);
const pendingValues = ref<EscalatedSanctionFormValues | null>(null);

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<EscalatedSanctionFormValues>({
    validationSchema: toTypedSchema(escalatedSanctionSchema),
    initialValues: {
      incidentDate: getTodayIsoDate(),
      violationType: '',
      sanction: 'PARENT_MEETING',
      description: ''
    }
  });

const [incidentDate] = defineField('incidentDate');
const [violationType] = defineField('violationType');
const [sanction] = defineField('sanction');
const [description] = defineField('description');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      confirmVisible.value = false;
      pendingValues.value = null;
      resetForm({
        values: {
          incidentDate: getTodayIsoDate(),
          violationType: '',
          sanction: 'PARENT_MEETING',
          description: ''
        }
      });
    }
  }
);

const pendingSanctionLabel = computed(() => {
  const found = ESCALATED_SANCTION_OPTIONS.find((opt) => opt.value === pendingValues.value?.sanction);
  return found?.label ?? pendingValues.value?.sanction ?? '';
});

const confirmationMessage = computed(() => {
  if (!props.student || !pendingValues.value) return '';
  return `You are about to issue a formal ${pendingSanctionLabel.value} sanction for ${props.student.fullName} (NIS: ${props.student.nis}) regarding "${pendingValues.value.violationType}". This high-impact disciplinary record will be logged permanently in the student dossier.`;
});

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onRequestEscalation = handleSubmit((values) => {
  if (!props.student) return;
  serverError.value = null;
  pendingValues.value = { ...values };
  confirmVisible.value = true;
});

async function handleConfirmEscalation(): Promise<void> {
  if (!props.student || !pendingValues.value) return;
  serverError.value = null;

  try {
    const created = await mutation.mutateAsync({
      studentId: props.student.id,
      studentName: props.student.fullName,
      studentNis: props.student.nis,
      academicYearId: props.student.academicYearId ?? null,
      academicYearName: props.student.academicYearName ?? null,
      incidentDate: pendingValues.value.incidentDate,
      violationType: pendingValues.value.violationType.trim(),
      sanction: pendingValues.value.sanction,
      description: pendingValues.value.description.trim()
    });

    confirmVisible.value = false;
    pendingValues.value = null;
    emit('recorded', created);
    emit('update:visible', false);
  } catch (error) {
    confirmVisible.value = false;
    const parsed = extractSubmissionError(error, 'Failed to issue escalated sanction.');
    serverError.value = parsed.banner;
    if (Object.keys(parsed.fieldErrors).length > 0) {
      setErrors(parsed.fieldErrors);
    }
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
    data-testid="escalated-sanction-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-rose-100 text-rose-600"
        >
          <ShieldBan class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Issue Escalated Sanction</h3>
          <p class="text-xs text-slate-500">
            Principal-level disciplinary sanction (Parent Meeting or Suspension)
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onRequestEscalation">
      <StudentContextBanner :student="student" :server-error="serverError" />

      <div
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-xs text-amber-800"
      >
        <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <span>
          Escalated sanctions are formal institutional actions restricted to the Principal and require
          confirmation before saving.
        </span>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Incident Date"
          field-id="escalated-incident-date"
          required
          :error="errors.incidentDate"
        >
          <InputText
            id="escalated-incident-date"
            v-model="incidentDate"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
            data-testid="escalated-incident-date"
          />
        </FormField>

        <FormField label="Escalated Sanction" field-id="escalated-sanction" required :error="errors.sanction">
          <Select
            v-model="sanction"
            input-id="escalated-sanction"
            :options="ESCALATED_SANCTION_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
            data-testid="escalated-sanction-select"
          />
        </FormField>
      </div>

      <FormField
        label="Violation Category"
        field-id="escalated-violation-type"
        required
        :error="errors.violationType"
      >
        <InputText
          id="escalated-violation-type"
          v-model="violationType"
          class="w-full"
          placeholder="e.g., Repeated Truancy, Severe Misconduct"
          :disabled="isSubmitting"
          data-testid="escalated-violation-type"
        />
      </FormField>

      <FormField
        label="Official Sanction Justification"
        field-id="escalated-description"
        required
        :error="errors.description"
      >
        <Textarea
          id="escalated-description"
          v-model="description"
          rows="4"
          class="w-full"
          placeholder="Provide complete justification, parent notification expectations, or suspension terms..."
          :disabled="isSubmitting"
          data-testid="escalated-description"
        />
      </FormField>

      <div class="flex items-center justify-end gap-2 border-t border-slate-100 pt-4">
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
          label="Review & Issue Sanction"
          severity="danger"
          :loading="isSubmitting"
          :disabled="isSubmitting || !student"
          data-testid="escalated-submit-btn"
        />
      </div>
    </form>

    <ConfirmActionDialog
      v-model:visible="confirmVisible"
      title="Confirm Escalated Sanction"
      :message="confirmationMessage"
      confirm-label="Confirm & Issue Sanction"
      cancel-label="Back to Form"
      severity="danger"
      :loading="isSubmitting"
      data-testid="escalated-confirm-dialog"
      @confirm="handleConfirmEscalation"
    />
  </Dialog>
</template>
