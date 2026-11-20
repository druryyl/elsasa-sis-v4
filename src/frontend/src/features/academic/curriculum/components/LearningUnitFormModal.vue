<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import type { LearningUnitResponse } from '../api/curriculum.api';
import {
  useCreateLearningUnitMutation,
  useUpdateLearningUnitMutation
} from '../api/useCurriculumMutations';
import {
  learningUnitSchema,
  UNIT_TYPES,
  type LearningUnitFormValues,
  type UnitType
} from '../schemas/curriculum.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    subjectId: string;
    stageId: string;
    stageName?: string;
    unit?: LearningUnitResponse | null;
    defaultSequenceOrder?: number;
  }>(),
  {
    stageName: '',
    unit: null,
    defaultSequenceOrder: 1
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [unit: LearningUnitResponse];
}>();

const UNIT_TYPE_OPTIONS = UNIT_TYPES.map((type) => ({
  label: type.charAt(0) + type.slice(1).toLowerCase(),
  value: type
}));

const isEditMode = computed(() => Boolean(props.unit?.id));
const dialogTitle = computed(() =>
  isEditMode.value ? 'Edit Learning Unit' : `Add Learning Unit to ${props.stageName || 'Stage'}`
);

const createMutation = useCreateLearningUnitMutation();
const updateMutation = useUpdateLearningUnitMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function buildInitialValues(u?: LearningUnitResponse | null): LearningUnitFormValues {
  return {
    name: u?.name ?? '',
    unitType: (u?.unitType as UnitType) ?? 'MODULE',
    sequenceOrder: u?.sequenceOrder ?? props.defaultSequenceOrder ?? 1,
    description: u?.description ?? ''
  };
}

const { handleSubmit, resetForm, defineField, setErrors, errors } = useForm<LearningUnitFormValues>({
  validationSchema: toTypedSchema(learningUnitSchema),
  initialValues: buildInitialValues(props.unit)
});

const [name, nameAttrs] = defineField('name');
const [unitType, unitTypeAttrs] = defineField('unitType');
const [sequenceOrder, sequenceOrderAttrs] = defineField('sequenceOrder');
const [description, descriptionAttrs] = defineField('description');

watch(
  () => [props.visible, props.unit] as const,
  ([isOpen, currentUnit]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(currentUnit)
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
    let saved: LearningUnitResponse;
    if (isEditMode.value && props.unit?.id) {
      saved = await updateMutation.mutateAsync({
        subjectId: props.subjectId,
        stageId: props.stageId,
        unitId: props.unit.id,
        payload: {
          name: values.name.trim(),
          unitType: values.unitType,
          sequenceOrder: Number(values.sequenceOrder),
          description: values.description?.trim() || null
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        subjectId: props.subjectId,
        stageId: props.stageId,
        payload: {
          name: values.name.trim(),
          unitType: values.unitType,
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
          error.detail || 'A unit with this sequence order or name already exists in this stage.';
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

    <form id="learning-unit-form" class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="conflictBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="flex-1">{{ conflictBanner }}</div>
      </div>

      <FormField
        label="Unit Name"
        field-id="unit-name"
        required
        :error="errors.name"
      >
        <InputText
          id="unit-name"
          v-model="name"
          v-bind="nameAttrs"
          type="text"
          placeholder="e.g. Aljabar Linear"
          class="w-full"
          :invalid="Boolean(errors.name)"
          data-testid="unit-name-input"
        />
      </FormField>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Unit Type"
          field-id="unit-type"
          required
          :error="errors.unitType"
        >
          <Select
            id="unit-type"
            v-model="unitType"
            v-bind="unitTypeAttrs"
            :options="UNIT_TYPE_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            :invalid="Boolean(errors.unitType)"
            data-testid="unit-type-select"
          />
        </FormField>

        <FormField
          label="Sequence Order"
          field-id="unit-seq"
          required
          :error="errors.sequenceOrder"
        >
          <InputNumber
            id="unit-seq"
            v-model="sequenceOrder"
            v-bind="sequenceOrderAttrs"
            :min="1"
            class="w-full"
            :invalid="Boolean(errors.sequenceOrder)"
            data-testid="unit-sequence-input"
          />
        </FormField>
      </div>

      <FormField
        label="Description (Optional)"
        field-id="unit-desc"
        :error="errors.description"
      >
        <Textarea
          id="unit-desc"
          v-model="description"
          v-bind="descriptionAttrs"
          rows="3"
          placeholder="Describe the unit modules, topics, or competencies..."
          class="w-full"
          :invalid="Boolean(errors.description)"
          data-testid="unit-description-input"
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
          :label="isEditMode ? 'Save Changes' : 'Create Unit'"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Dialog>
</template>
