<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { ShieldAlert } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useRecordDisciplineIncidentMutation } from '../api/usePastoralMutations';
import type { DisciplineRecordDto } from '../api/pastoral.api';
import {
  CLASSROOM_SANCTION_OPTIONS,
  COMMON_VIOLATION_TYPES,
  disciplineIncidentSchema,
  extractSubmissionError,
  getTodayIsoDate,
  type DisciplineIncidentFormValues,
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

const mutation = useRecordDisciplineIncidentMutation();
const isSubmitting = computed(() => mutation.isPending.value);
const serverError = ref<string | null>(null);

const { defineField, errors, handleSubmit, resetForm, setErrors, setFieldValue } =
  useForm<DisciplineIncidentFormValues>({
    validationSchema: toTypedSchema(disciplineIncidentSchema),
    initialValues: {
      incidentDate: getTodayIsoDate(),
      violationType: '',
      sanction: 'VERBAL_WARNING',
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
      resetForm({
        values: {
          incidentDate: getTodayIsoDate(),
          violationType: '',
          sanction: 'VERBAL_WARNING',
          description: ''
        }
      });
    }
  }
);

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onSubmit = handleSubmit(async (values) => {
  if (!props.student) return;
  serverError.value = null;

  try {
    const created = await mutation.mutateAsync({
      studentId: props.student.id,
      studentName: props.student.fullName,
      studentNis: props.student.nis,
      academicYearId: props.student.academicYearId ?? null,
      academicYearName: props.student.academicYearName ?? null,
      incidentDate: values.incidentDate,
      violationType: values.violationType.trim(),
      sanction: values.sanction,
      description: values.description.trim()
    });

    emit('recorded', created);
    emit('update:visible', false);
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Failed to record discipline incident.');
    serverError.value = parsed.banner;
    if (Object.keys(parsed.fieldErrors).length > 0) {
      setErrors(parsed.fieldErrors);
    }
  }
});
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :closable="!isSubmitting"
    :draggable="false"
    class="w-full max-w-lg"
    data-testid="discipline-incident-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-amber-50 text-amber-600"
        >
          <ShieldAlert class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Record Classroom Discipline</h3>
          <p class="text-xs text-slate-500">
            Log classroom infraction with verbal or written warning
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <StudentContextBanner :student="student" :server-error="serverError" />

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Incident Date"
          field-id="discipline-incident-date"
          required
          :error="errors.incidentDate"
        >
          <InputText
            id="discipline-incident-date"
            v-model="incidentDate"
            type="date"
            class="w-full"
            :disabled="isSubmitting"
            data-testid="discipline-incident-date"
          />
        </FormField>

        <FormField label="Sanction Tier" field-id="discipline-sanction" required :error="errors.sanction">
          <Select
            v-model="sanction"
            input-id="discipline-sanction"
            :options="CLASSROOM_SANCTION_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
            data-testid="discipline-sanction-select"
          />
        </FormField>
      </div>

      <FormField
        label="Violation Type"
        field-id="discipline-violation-type"
        required
        hint="Select a quick tag or enter up to 50 characters"
        :error="errors.violationType"
      >
        <div class="space-y-2">
          <InputText
            id="discipline-violation-type"
            v-model="violationType"
            class="w-full"
            placeholder="e.g., Late Arrival, Classroom Disruption"
            :disabled="isSubmitting"
            data-testid="discipline-violation-type"
          />
          <div class="flex flex-wrap gap-1.5">
            <button
              v-for="tag in COMMON_VIOLATION_TYPES"
              :key="tag"
              type="button"
              class="rounded-md border px-2 py-0.5 text-xs transition-colors"
              :class="
                violationType === tag
                  ? 'border-amber-400 bg-amber-50 font-medium text-amber-800'
                  : 'border-slate-200 bg-slate-50 text-slate-600 hover:bg-slate-100'
              "
              @click="setFieldValue('violationType', tag)"
            >
              {{ tag }}
            </button>
          </div>
        </div>
      </FormField>

      <FormField
        label="Incident Details"
        field-id="discipline-description"
        required
        :error="errors.description"
      >
        <Textarea
          id="discipline-description"
          v-model="description"
          rows="4"
          class="w-full"
          placeholder="Document the incident chronology and corrective guidance given..."
          :disabled="isSubmitting"
          data-testid="discipline-description"
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
          label="Record Incident"
          severity="warn"
          :loading="isSubmitting"
          :disabled="isSubmitting || !student"
          data-testid="discipline-submit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
