<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { AlertCircle, Award } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { activeTeachersLookupQuery } from '@/shared/services/lookups.api';
import type { ExtracurricularActivityDto } from '../api/extracurricular.api';
import {
  useCreateExtracurricularMutation,
  useUpdateExtracurricularMutation
} from '../api/useExtracurricularMutations';
import {
  activityEditorSchema,
  type ActivityEditorFormValues
} from '../schemas/extracurricular.schema';
import { resolveFormSubmissionError } from '../utils/formErrors';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    activity?: ExtracurricularActivityDto | null;
  }>(),
  {
    activity: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [activity: ExtracurricularActivityDto];
}>();

const isEditMode = computed(() => Boolean(props.activity?.id));
const dialogTitle = computed(() =>
  isEditMode.value ? 'Edit Extracurricular Club' : 'New Extracurricular Club'
);

const teachersQuery = useQuery({
  ...activeTeachersLookupQuery(),
  enabled: () => props.visible
});

const teacherOptions = computed(() =>
  (teachersQuery.data.value ?? [])
    .filter((t) => Boolean(t.id))
    .map((t) => ({
      label: t.nip ? `${t.fullName} (NIP: ${t.nip})` : (t.fullName ?? 'Unnamed Teacher'),
      value: t.id!
    }))
);

const statusOptions = [
  { label: 'Active', value: 'ACTIVE' },
  { label: 'Inactive', value: 'INACTIVE' }
];

const createMutation = useCreateExtracurricularMutation();
const updateMutation = useUpdateExtracurricularMutation();
const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const errorBanner = ref<string | null>(null);

function buildInitialValues(
  activity?: ExtracurricularActivityDto | null
): ActivityEditorFormValues {
  return {
    name: activity?.name ?? '',
    description: activity?.description ?? '',
    supervisorId: activity?.supervisorId ?? '',
    status: activity?.status === 'INACTIVE' ? 'INACTIVE' : 'ACTIVE'
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<ActivityEditorFormValues>({
    validationSchema: toTypedSchema(activityEditorSchema),
    initialValues: buildInitialValues(props.activity)
  });

const [name, nameAttrs] = defineField('name');
const [description, descriptionAttrs] = defineField('description');
const [supervisorId] = defineField('supervisorId');
const [status] = defineField('status');

watch(
  () => [props.visible, props.activity] as const,
  ([isOpen, currentActivity]) => {
    if (isOpen) {
      errorBanner.value = null;
      resetForm({
        values: buildInitialValues(currentActivity)
      });
    }
  }
);

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onSubmit = handleSubmit(async (values) => {
  errorBanner.value = null;

  try {
    let saved: ExtracurricularActivityDto;
    if (isEditMode.value && props.activity?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.activity.id,
        payload: {
          name: values.name.trim(),
          description: values.description?.trim() || null,
          supervisorId: values.supervisorId,
          status: values.status
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        name: values.name.trim(),
        description: values.description?.trim() || null,
        supervisorId: values.supervisorId
      });
    }

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    errorBanner.value = resolveFormSubmissionError(
      error,
      setErrors,
      'Failed to save extracurricular club.'
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
    data-testid="activity-editor-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-accent-50 text-brand-accent-600"
        >
          <Award class="h-4 w-4" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">{{ dialogTitle }}</h3>
          <p class="text-xs text-slate-500">
            Configure club details and assign a supervising teacher.
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

      <FormField
        label="Club Name"
        field-id="club-name-input"
        required
        :error="errors.name"
      >
        <InputText
          id="club-name-input"
          v-model="name"
          v-bind="nameAttrs"
          type="text"
          placeholder="e.g. Pramuka, Robotics Club, English Debate"
          class="w-full"
          :invalid="Boolean(errors.name)"
          data-testid="club-name-input"
        />
      </FormField>

      <FormField
        label="Assigned Supervisor"
        field-id="club-supervisor-select"
        required
        :error="errors.supervisorId"
        hint="Only the assigned supervisor can record session activity logs."
      >
        <Select
          id="club-supervisor-select"
          v-model="supervisorId"
          :options="teacherOptions"
          option-label="label"
          option-value="value"
          placeholder="Select a supervising teacher..."
          filter
          :loading="teachersQuery.isLoading.value"
          class="w-full"
          :invalid="Boolean(errors.supervisorId)"
          data-testid="club-supervisor-select"
        />
      </FormField>

      <FormField
        v-if="isEditMode"
        label="Club Status"
        field-id="club-status-select"
        required
        :error="errors.status"
      >
        <Select
          id="club-status-select"
          v-model="status"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          class="w-full"
          data-testid="club-status-select"
        />
      </FormField>

      <FormField
        label="Description"
        field-id="club-description-input"
        :error="errors.description"
        :hint="`${description?.length ?? 0} / 500 characters`"
      >
        <Textarea
          id="club-description-input"
          v-model="description"
          v-bind="descriptionAttrs"
          rows="3"
          placeholder="Describe club schedule, objectives, and activities..."
          class="w-full"
          :invalid="Boolean(errors.description)"
          data-testid="club-description-input"
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
          :label="isEditMode ? 'Save Changes' : 'Create Club'"
          :loading="isSubmitting"
          data-testid="submit-club-button"
        />
      </div>
    </form>
  </Dialog>
</template>
