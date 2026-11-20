<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import type { SubjectStageResponse } from '../api/curriculum.api';
import {
  useCreateSubjectStageMutation,
  useUpdateSubjectStageMutation
} from '../api/useCurriculumMutations';
import { subjectStageSchema, type SubjectStageFormValues } from '../schemas/curriculum.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    subjectId: string;
    subjectName?: string;
    stage?: SubjectStageResponse | null;
    defaultSequenceOrder?: number;
  }>(),
  {
    subjectName: '',
    stage: null,
    defaultSequenceOrder: 1
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [stage: SubjectStageResponse];
}>();

const isEditMode = computed(() => Boolean(props.stage?.id));
const dialogTitle = computed(() =>
  isEditMode.value ? `Edit Subject Stage` : `Add Stage to ${props.subjectName || 'Subject'}`
);

const createMutation = useCreateSubjectStageMutation();
const updateMutation = useUpdateSubjectStageMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function buildInitialValues(st?: SubjectStageResponse | null): SubjectStageFormValues {
  return {
    name: st?.name ?? '',
    sequenceOrder: st?.sequenceOrder ?? props.defaultSequenceOrder ?? 1,
    description: st?.description ?? ''
  };
}

const { handleSubmit, resetForm, defineField, setErrors, errors } = useForm<SubjectStageFormValues>({
  validationSchema: toTypedSchema(subjectStageSchema),
  initialValues: buildInitialValues(props.stage)
});

const [name, nameAttrs] = defineField('name');
const [sequenceOrder, sequenceOrderAttrs] = defineField('sequenceOrder');
const [description, descriptionAttrs] = defineField('description');

watch(
  () => [props.visible, props.stage] as const,
  ([isOpen, currentStage]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(currentStage)
      });
    }
  }
);

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

function mapValidationErrors(fieldErrors: Record<string, string[]>): Record<string, string> {
  const mapped: Record<string, string> = {};
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const first = messages[0];
    if (first) {
      const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
      mapped[normalizedKey] = first;
    }
  }
  return mapped;
}

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    let saved: SubjectStageResponse;
    if (isEditMode.value && props.stage?.id) {
      saved = await updateMutation.mutateAsync({
        subjectId: props.subjectId,
        stageId: props.stage.id,
        payload: {
          name: values.name.trim(),
          sequenceOrder: Number(values.sequenceOrder),
          description: values.description?.trim() || null
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        subjectId: props.subjectId,
        payload: {
          name: values.name.trim(),
          sequenceOrder: Number(values.sequenceOrder),
          description: values.description?.trim() || null
        }
      });
    }

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 409) {
        conflictBanner.value =
          error.detail || 'A stage with this sequence order or name already exists in this subject.';
        return;
      }
      if (error.status === 422 && error.errors) {
        setErrors(mapValidationErrors(error.errors));
        return;
      }
    }
    conflictBanner.value =
      error instanceof Error ? error.message : 'An unexpected error occurred.';
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
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <h3 class="text-base font-semibold text-slate-900">{{ dialogTitle }}</h3>
      </div>
    </template>

    <form id="subject-stage-form" class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="conflictBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="flex-1">{{ conflictBanner }}</div>
      </div>

      <FormField
        label="Stage Name"
        field-id="stage-name"
        required
        :error="errors.name"
      >
        <InputText
          id="stage-name"
          v-model="name"
          v-bind="nameAttrs"
          type="text"
          placeholder="e.g. Fase D (Kelas 7)"
          class="w-full"
          :invalid="Boolean(errors.name)"
          data-testid="stage-name-input"
        />
      </FormField>

      <FormField
        label="Sequence Order"
        field-id="stage-seq"
        required
        :error="errors.sequenceOrder"
      >
        <InputNumber
          id="stage-seq"
          v-model="sequenceOrder"
          v-bind="sequenceOrderAttrs"
          :min="1"
          class="w-full"
          :invalid="Boolean(errors.sequenceOrder)"
          data-testid="stage-sequence-input"
        />
      </FormField>

      <FormField
        label="Description (Optional)"
        field-id="stage-desc"
        :error="errors.description"
      >
        <Textarea
          id="stage-desc"
          v-model="description"
          v-bind="descriptionAttrs"
          rows="3"
          placeholder="Describe the learning objectives or scope of this stage..."
          class="w-full"
          :invalid="Boolean(errors.description)"
          data-testid="stage-description-input"
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
          :label="isEditMode ? 'Save Changes' : 'Create Stage'"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Dialog>
</template>
