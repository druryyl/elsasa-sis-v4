<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import { Trophy } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useRecordAchievementMutation } from '../api/usePastoralMutations';
import type { AchievementDto } from '../api/pastoral.api';
import {
  ACHIEVEMENT_TYPE_OPTIONS,
  RECOGNITION_LEVEL_OPTIONS,
  achievementAwardSchema,
  extractSubmissionError,
  getTodayIsoDate,
  type AchievementAwardFormValues,
  type PastoralStudentTarget
} from '../schemas/pastoral.schema';
import StudentContextBanner from './StudentContextBanner.vue';

const props = defineProps<{
  visible: boolean;
  student: PastoralStudentTarget | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  recorded: [achievement: AchievementDto];
}>();

const mutation = useRecordAchievementMutation();
const isSubmitting = computed(() => mutation.isPending.value);
const serverError = ref<string | null>(null);

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<AchievementAwardFormValues>({
    validationSchema: toTypedSchema(achievementAwardSchema),
    initialValues: {
      eventName: '',
      achievementType: 'SCIENCE',
      recognitionLevel: 'SCHOOL',
      achievementDate: getTodayIsoDate(),
      description: ''
    }
  });

const [eventName] = defineField('eventName');
const [achievementType] = defineField('achievementType');
const [recognitionLevel] = defineField('recognitionLevel');
const [achievementDate] = defineField('achievementDate');
const [description] = defineField('description');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      resetForm({
        values: {
          eventName: '',
          achievementType: 'SCIENCE',
          recognitionLevel: 'SCHOOL',
          achievementDate: getTodayIsoDate(),
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
      eventName: values.eventName.trim(),
      achievementType: values.achievementType,
      recognitionLevel: values.recognitionLevel,
      achievementDate: values.achievementDate,
      description: values.description?.trim() || null
    });

    emit('recorded', created);
    emit('update:visible', false);
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Failed to record achievement award.');
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
    data-testid="achievement-award-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-amber-50 text-amber-600"
        >
          <Trophy class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Record Achievement Award</h3>
          <p class="text-xs text-slate-500">Recognize academic, sports, arts, or community honors</p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <StudentContextBanner :student="student" :server-error="serverError" />

      <FormField label="Event / Competition Name" field-id="achievement-event-name" required :error="errors.eventName">
        <InputText
          id="achievement-event-name"
          v-model="eventName"
          class="w-full"
          placeholder="e.g., Olimpiade Sains Nasional Matematika"
          :disabled="isSubmitting"
          data-testid="achievement-event-name"
        />
      </FormField>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Achievement Category"
          field-id="achievement-type"
          required
          :error="errors.achievementType"
        >
          <Select
            v-model="achievementType"
            input-id="achievement-type"
            :options="ACHIEVEMENT_TYPE_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
            data-testid="achievement-type-select"
          />
        </FormField>

        <FormField
          label="Recognition Level"
          field-id="achievement-level"
          required
          :error="errors.recognitionLevel"
        >
          <Select
            v-model="recognitionLevel"
            input-id="achievement-level"
            :options="RECOGNITION_LEVEL_OPTIONS"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isSubmitting"
            data-testid="achievement-level-select"
          />
        </FormField>
      </div>

      <FormField
        label="Achievement Date"
        field-id="achievement-date"
        required
        :error="errors.achievementDate"
      >
        <InputText
          id="achievement-date"
          v-model="achievementDate"
          type="date"
          class="w-full"
          :disabled="isSubmitting"
          data-testid="achievement-date"
        />
      </FormField>

      <FormField
        label="Notes & Placement (Optional)"
        field-id="achievement-description"
        hint="Max 500 characters"
        :error="errors.description"
      >
        <Textarea
          id="achievement-description"
          v-model="description"
          rows="3"
          class="w-full"
          placeholder="e.g., 1st Place Gold Medal, team captain..."
          :disabled="isSubmitting"
          data-testid="achievement-description"
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
          label="Save Award"
          :loading="isSubmitting"
          :disabled="isSubmitting || !student"
          data-testid="achievement-submit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
