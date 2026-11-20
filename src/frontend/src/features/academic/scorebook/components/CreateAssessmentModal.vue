<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { AlertCircle, FileSpreadsheet, Plus } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import {
  createAssessmentSchema,
  type CreateAssessmentFormValues
} from '../schemas/scorebook.schema';
import {
  scorebookApi,
  type AssignmentDto
} from '../api/scorebook.api';

const props = defineProps<{
  visible: boolean;
  subjectStageId: string;
  subjectId: string;
  studyGroupId: string;
  operationalPeriodId: string;
  learningUnits?: Array<{ id?: string; name?: string | null }>;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  created: [assignment: AssignmentDto];
}>();

const isSubmitting = ref(false);
const serverError = ref<string | null>(null);

function getTodayString(): string {
  const today = new Date();
  const y = today.getFullYear();
  const m = String(today.getMonth() + 1).padStart(2, '0');
  const d = String(today.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<CreateAssessmentFormValues>({
    validationSchema: toTypedSchema(createAssessmentSchema),
    initialValues: {
      subjectStageId: props.subjectStageId,
      subjectId: props.subjectId,
      studyGroupId: props.studyGroupId,
      operationalPeriodId: props.operationalPeriodId,
      learningUnitId: null,
      title: '',
      assignmentType: 'FORMATIVE',
      maxScore: 100,
      weight: null,
      assignedDate: getTodayString(),
      dueDate: ''
    }
  });

const [title] = defineField('title');
const [assignmentType] = defineField('assignmentType');
const [maxScore] = defineField('maxScore');
const [weight] = defineField('weight');
const [assignedDate] = defineField('assignedDate');
const [dueDate] = defineField('dueDate');
const [learningUnitId] = defineField('learningUnitId');

const assignmentTypeOptions = [
  { label: 'Formative', value: 'FORMATIVE' },
  { label: 'Summative', value: 'SUMMATIVE' },
  { label: 'Exam', value: 'EXAM' }
];

const unitOptions = computed(() => {
  return (props.learningUnits ?? [])
    .filter((u) => u.id && u.name)
    .map((u) => ({ label: u.name as string, value: u.id as string }));
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      resetForm({
        values: {
          subjectStageId: props.subjectStageId,
          subjectId: props.subjectId,
          studyGroupId: props.studyGroupId,
          operationalPeriodId: props.operationalPeriodId,
          learningUnitId: null,
          title: '',
          assignmentType: 'FORMATIVE',
          maxScore: 100,
          weight: null,
          assignedDate: getTodayString(),
          dueDate: ''
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
  serverError.value = null;
  isSubmitting.value = true;

  try {
    const res = await scorebookApi.createAssignment({
      subjectStageId: props.subjectStageId,
      subjectId: props.subjectId,
      studyGroupId: props.studyGroupId,
      operationalPeriodId: props.operationalPeriodId,
      learningUnitId: values.learningUnitId || null,
      title: values.title.trim(),
      assignmentType: values.assignmentType,
      maxScore: Number(values.maxScore),
      weight: values.weight !== null && values.weight !== undefined && String(values.weight) !== ''
        ? Number(values.weight)
        : null,
      assignedDate: values.assignedDate,
      dueDate: values.dueDate && values.dueDate.trim() !== '' ? values.dueDate : null
    });

    emit('created', res);
    closeModal();
  } catch (err) {
    if (err instanceof ApiError) {
      serverError.value = err.message;
      if (err.errors) {
        const mappedErrors: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(err.errors)) {
          if (msgs && msgs[0]) {
            const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
            mappedErrors[camelKey] = msgs[0];
          }
        }
        setErrors(mappedErrors);
      }
    } else {
      serverError.value = 'An unexpected error occurred while creating assessment.';
    }
  } finally {
    isSubmitting.value = false;
  }
});
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :closable="!isSubmitting"
    :style="{ width: '512px' }"
    class="max-w-lg"
    data-testid="create-assessment-modal"
    @update:visible="closeModal"
  >
    <template #header>
      <div class="space-y-1">
        <div class="flex items-center gap-2">
          <FileSpreadsheet class="h-5 w-5 text-brand-accent-600" />
          <h2 class="text-lg font-semibold text-slate-900" data-testid="create-assessment-title">
            Create Assessment
          </h2>
        </div>
        <p class="text-xs text-slate-500">
          Add an assessment column for direct spreadsheet score recording.
        </p>
      </div>
    </template>

    <form class="space-y-4 py-2" @submit.prevent="onSubmit">
      <!-- Server Error Alert -->
      <div
        v-if="serverError"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-3 text-xs text-rose-900"
        role="alert"
        data-testid="create-assessment-error"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <div class="space-y-0.5">
          <p class="font-medium">Creation Failed</p>
          <p>{{ serverError }}</p>
        </div>
      </div>

      <!-- Assessment Title -->
      <FormField
        label="Assessment Title"
        field-id="assessmentTitle"
        :required="true"
        :error="errors.title"
      >
        <InputText
          id="assessmentTitle"
          v-model="title"
          placeholder="e.g. Tugas 1 - Aljabar Linier"
          class="w-full text-sm"
          :invalid="Boolean(errors.title)"
          data-testid="assessment-title-input"
        />
      </FormField>

      <!-- Assignment Type & Max Score -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Assessment Type"
          field-id="assignmentType"
          :required="true"
          :error="errors.assignmentType"
        >
          <Select
            id="assignmentType"
            v-model="assignmentType"
            :options="assignmentTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Type"
            class="w-full text-sm"
            :invalid="Boolean(errors.assignmentType)"
            data-testid="assessment-type-select"
          />
        </FormField>

        <FormField
          label="Max Score (Points)"
          field-id="maxScore"
          :required="true"
          :error="errors.maxScore"
        >
          <InputText
            id="maxScore"
            :model-value="maxScore !== undefined && maxScore !== null ? String(maxScore) : ''"
            type="number"
            min="1"
            placeholder="100"
            class="w-full text-sm font-mono"
            :invalid="Boolean(errors.maxScore)"
            data-testid="assessment-max-score-input"
            @update:model-value="maxScore = $event ? Number($event) : (undefined as any)"
          />
        </FormField>
      </div>

      <!-- Weight & Learning Unit -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Weight (%)"
          field-id="weight"
          :error="errors.weight"
          hint="Leave empty if weights are not used."
        >
          <InputText
            id="weight"
            :model-value="weight !== undefined && weight !== null ? String(weight) : ''"
            type="number"
            min="0"
            max="100"
            step="0.01"
            placeholder="e.g. 20 (optional)"
            class="w-full text-sm font-mono"
            :invalid="Boolean(errors.weight)"
            data-testid="assessment-weight-input"
            @update:model-value="weight = $event ? Number($event) : null"
          />
        </FormField>

        <FormField
          v-if="unitOptions.length > 0"
          label="Learning Unit (Optional)"
          field-id="learningUnit"
        >
          <Select
            id="learningUnit"
            v-model="learningUnitId"
            :options="unitOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Modular Unit"
            class="w-full text-sm"
            show-clear
            data-testid="assessment-unit-select"
          />
        </FormField>
      </div>

      <!-- Assigned Date & Due Date -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Assigned Date"
          field-id="assignedDate"
          :required="true"
          :error="errors.assignedDate"
        >
          <InputText
            id="assignedDate"
            v-model="assignedDate"
            type="date"
            class="w-full text-sm"
            :invalid="Boolean(errors.assignedDate)"
            data-testid="assessment-assigned-date-input"
          />
        </FormField>

        <FormField
          label="Due Date (Optional)"
          field-id="dueDate"
          :error="errors.dueDate"
        >
          <InputText
            id="dueDate"
            v-model="dueDate"
            type="date"
            class="w-full text-sm"
            :invalid="Boolean(errors.dueDate)"
            data-testid="assessment-due-date-input"
          />
        </FormField>
      </div>

      <!-- Modal Footer -->
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          size="small"
          :disabled="isSubmitting"
          data-testid="create-assessment-cancel-btn"
          @click="closeModal"
        />
        <Button
          type="submit"
          label="Create Assessment"
          severity="primary"
          size="small"
          :loading="isSubmitting"
          data-testid="create-assessment-submit-btn"
        >
          <template #icon>
            <Plus class="h-4 w-4 mr-1" />
          </template>
        </Button>
      </div>
    </form>
  </Dialog>
</template>
