<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import type { SubjectResponse } from '../api/curriculum.api';
import {
  useCreateSubjectMutation,
  useUpdateSubjectMutation
} from '../api/useCurriculumMutations';
import { subjectSchema, type SubjectFormValues } from '../schemas/curriculum.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    subject?: SubjectResponse | null;
  }>(),
  {
    subject: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [subject: SubjectResponse];
}>();

const isEditMode = computed(() => Boolean(props.subject?.id));
const dialogTitle = computed(() => (isEditMode.value ? 'Edit Subject' : 'Add Subject'));

const createMutation = useCreateSubjectMutation();
const updateMutation = useUpdateSubjectMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function buildInitialValues(subj?: SubjectResponse | null): SubjectFormValues {
  return {
    name: subj?.name ?? '',
    code: subj?.code ?? ''
  };
}

const { handleSubmit, resetForm, defineField, setErrors, errors } = useForm<SubjectFormValues>({
  validationSchema: toTypedSchema(subjectSchema),
  initialValues: buildInitialValues(props.subject)
});

const [name, nameAttrs] = defineField('name');
const [code, codeAttrs] = defineField('code');

watch(
  () => [props.visible, props.subject] as const,
  ([isOpen, currentSubject]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(currentSubject)
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
    let saved: SubjectResponse;
    if (isEditMode.value && props.subject?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.subject.id,
        payload: {
          name: values.name.trim(),
          code: values.code.trim().toUpperCase()
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        name: values.name.trim(),
        code: values.code.trim().toUpperCase()
      });
    }

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 409) {
        conflictBanner.value =
          error.detail || 'A subject with this name or code already exists.';
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

    <form id="subject-form" class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="conflictBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="flex-1">{{ conflictBanner }}</div>
      </div>

      <FormField
        label="Subject Name"
        field-id="subject-name"
        required
        :error="errors.name"
      >
        <InputText
          id="subject-name"
          v-model="name"
          v-bind="nameAttrs"
          type="text"
          placeholder="e.g. Matematika"
          class="w-full"
          :invalid="Boolean(errors.name)"
          data-testid="subject-name-input"
        />
      </FormField>

      <FormField
        label="Subject Code"
        field-id="subject-code"
        required
        :error="errors.code"
      >
        <InputText
          id="subject-code"
          v-model="code"
          v-bind="codeAttrs"
          type="text"
          placeholder="e.g. MTK"
          class="w-full uppercase"
          :invalid="Boolean(errors.code)"
          data-testid="subject-code-input"
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
          :label="isEditMode ? 'Save Changes' : 'Create Subject'"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Dialog>
</template>
