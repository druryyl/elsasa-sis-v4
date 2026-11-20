<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { HeartPulse, ThumbsDown, ThumbsUp } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useRecordBehaviourNoteMutation } from '../api/usePastoralMutations';
import type { BehaviourNoteDto } from '../api/pastoral.api';
import {
  BEHAVIOUR_TYPE_OPTIONS,
  behaviourNoteSchema,
  extractSubmissionError,
  getTodayIsoDate,
  type BehaviourNoteFormValues,
  type PastoralStudentTarget
} from '../schemas/pastoral.schema';
import StudentContextBanner from './StudentContextBanner.vue';

const props = defineProps<{
  visible: boolean;
  student: PastoralStudentTarget | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  recorded: [note: BehaviourNoteDto];
}>();

const mutation = useRecordBehaviourNoteMutation();
const isSubmitting = computed(() => mutation.isPending.value);
const serverError = ref<string | null>(null);

const { defineField, errors, handleSubmit, resetForm, setErrors, setFieldValue } =
  useForm<BehaviourNoteFormValues>({
    validationSchema: toTypedSchema(behaviourNoteSchema),
    initialValues: {
      behaviourType: 'POSITIVE',
      observedDate: getTodayIsoDate(),
      description: ''
    }
  });

const [behaviourType] = defineField('behaviourType');
const [observedDate] = defineField('observedDate');
const [description] = defineField('description');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      resetForm({
        values: {
          behaviourType: 'POSITIVE',
          observedDate: getTodayIsoDate(),
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
      behaviourType: values.behaviourType,
      observedDate: values.observedDate,
      description: values.description.trim()
    });

    emit('recorded', created);
    emit('update:visible', false);
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Failed to record behaviour note.');
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
    data-testid="behaviour-note-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-emerald-50 text-emerald-600"
        >
          <HeartPulse class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Record Behaviour Observation</h3>
          <p class="text-xs text-slate-500">Log positive conduct or pastoral concern note</p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <StudentContextBanner :student="student" :server-error="serverError" />

      <FormField label="Observation Type" required :error="errors.behaviourType">
        <div class="grid grid-cols-2 gap-2.5">
          <button
            v-for="option in BEHAVIOUR_TYPE_OPTIONS"
            :key="option.value"
            type="button"
            :data-testid="`behaviour-type-${option.value}`"
            class="flex items-center justify-center gap-2 rounded-lg border px-3 py-2.5 text-sm font-medium transition-colors"
            :class="
              behaviourType === option.value
                ? option.value === 'POSITIVE'
                  ? 'border-emerald-500 bg-emerald-50 text-emerald-700'
                  : 'border-rose-500 bg-rose-50 text-rose-700'
                : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
            "
            @click="setFieldValue('behaviourType', option.value)"
          >
            <ThumbsUp v-if="option.value === 'POSITIVE'" class="h-4 w-4" />
            <ThumbsDown v-else class="h-4 w-4" />
            <span>{{ option.label }}</span>
          </button>
        </div>
      </FormField>

      <FormField label="Observation Date" field-id="behaviour-observed-date" required :error="errors.observedDate">
        <InputText
          id="behaviour-observed-date"
          v-model="observedDate"
          type="date"
          class="w-full"
          :disabled="isSubmitting"
          data-testid="behaviour-observed-date"
        />
      </FormField>

      <FormField
        label="Observation Description"
        field-id="behaviour-description"
        required
        hint="Max 1000 characters"
        :error="errors.description"
      >
        <Textarea
          id="behaviour-description"
          v-model="description"
          rows="4"
          class="w-full"
          placeholder="Describe observed student behaviour or pastoral context..."
          :disabled="isSubmitting"
          data-testid="behaviour-description"
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
          label="Save Observation"
          :loading="isSubmitting"
          :disabled="isSubmitting || !student"
          data-testid="behaviour-submit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
