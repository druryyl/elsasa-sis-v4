<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useQuery } from '@tanstack/vue-query';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import FormField from '@/shared/components/forms/FormField.vue';
import {
  academicYearsLookupQuery,
  studyGroupsLookupQuery
} from '@/shared/services/lookups.api';
import {
  ALL_GRADES,
  GRADES_BY_SCHOOL_LEVEL,
  SCHOOL_LEVEL_OPTIONS,
  batchBillingSchema,
  extractSubmissionError,
  getCurrentBillingMonth
} from '../schemas/billing.schema';
import type { BatchBillingPreviewResponse } from '../api/billing.api';
import {
  useGenerateBatchBillingMutation,
  usePreviewBatchBillingMutation
} from '../api/useBillingMutations';
import PreFlightSummaryModal from './PreFlightSummaryModal.vue';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    defaultAcademicYearId?: string | null;
    defaultBillingMonth?: string | null;
  }>(),
  {
    defaultAcademicYearId: null,
    defaultBillingMonth: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  completed: [];
}>();

const serverError = ref<string | null>(null);
const commitError = ref<string | null>(null);
const showPreFlight = ref(false);
const previewData = ref<BatchBillingPreviewResponse | null>(null);

const previewMutation = usePreviewBatchBillingMutation();
const generateMutation = useGenerateBatchBillingMutation();

const academicYearsQuery = useQuery(academicYearsLookupQuery());

const { handleSubmit, errors, resetForm, defineField, setFieldValue, setErrors } = useForm({
  validationSchema: toTypedSchema(batchBillingSchema),
  initialValues: {
    academicYearId: props.defaultAcademicYearId ?? '',
    billingMonth: props.defaultBillingMonth || getCurrentBillingMonth(),
    schoolLevel: null as string | null,
    grade: null as string | null,
    studyGroupId: null as string | null,
    dueDate: ''
  }
});

const [academicYearId, academicYearIdAttrs] = defineField('academicYearId');
const [billingMonth, billingMonthAttrs] = defineField('billingMonth');
const [schoolLevel, schoolLevelAttrs] = defineField('schoolLevel');
const [grade, gradeAttrs] = defineField('grade');
const [studyGroupId, studyGroupIdAttrs] = defineField('studyGroupId');
const [dueDate, dueDateAttrs] = defineField('dueDate');

const studyGroupsQuery = useQuery(
  computed(() => ({
    ...studyGroupsLookupQuery({
      academicYearId: academicYearId.value || undefined,
      schoolLevel: schoolLevel.value || undefined,
      grade: grade.value || undefined
    }),
    enabled: props.visible
  }))
);

const academicYearOptions = computed(() =>
  (academicYearsQuery.data.value ?? []).map((year) => ({
    label: year.status === 'ACTIVE' ? `${year.name} (Active)` : (year.name ?? 'Unnamed Year'),
    value: year.id ?? ''
  }))
);

const gradeOptions = computed(() => {
  const list = schoolLevel.value
    ? (GRADES_BY_SCHOOL_LEVEL[schoolLevel.value] ?? ALL_GRADES)
    : ALL_GRADES;
  return list.map((g) => ({ label: `Grade ${g}`, value: g }));
});

const studyGroupOptions = computed(() =>
  (studyGroupsQuery.data.value ?? []).map((group) => ({
    label: `${group.name ?? 'Group'} (${group.schoolLevel ?? ''} ${group.grade ?? ''})`.trim(),
    value: group.id ?? ''
  }))
);

const selectedAcademicYearName = computed(
  () => academicYearsQuery.data.value?.find((y) => y.id === academicYearId.value)?.name ?? ''
);

const scopeSummary = computed(() => {
  const parts: string[] = [];
  if (schoolLevel.value) parts.push(schoolLevel.value);
  if (grade.value) parts.push(`Grade ${grade.value}`);
  if (studyGroupId.value) {
    const group = studyGroupsQuery.data.value?.find((g) => g.id === studyGroupId.value);
    parts.push(group?.name ? `Class ${group.name}` : 'Selected Study Group');
  }
  return parts.length > 0 ? parts.join(' • ') : 'All Active Students';
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      commitError.value = null;
      showPreFlight.value = false;
      previewData.value = null;
      const activeYear =
        props.defaultAcademicYearId ||
        academicYearsQuery.data.value?.find((y) => y.status === 'ACTIVE')?.id ||
        academicYearsQuery.data.value?.[0]?.id ||
        '';
      resetForm({
        values: {
          academicYearId: activeYear,
          billingMonth: props.defaultBillingMonth || getCurrentBillingMonth(),
          schoolLevel: null,
          grade: null,
          studyGroupId: null,
          dueDate: ''
        }
      });
    }
  }
);

watch(schoolLevel, (nextLevel) => {
  if (nextLevel && grade.value) {
    const allowed = GRADES_BY_SCHOOL_LEVEL[nextLevel] ?? [];
    if (!allowed.includes(grade.value)) {
      setFieldValue('grade', null);
    }
  }
  setFieldValue('studyGroupId', null);
});

watch(grade, () => {
  setFieldValue('studyGroupId', null);
});

watch(academicYearId, () => {
  setFieldValue('studyGroupId', null);
});

const onPreviewSubmit = handleSubmit(async (values) => {
  serverError.value = null;
  commitError.value = null;
  try {
    const preview = await previewMutation.mutateAsync({
      academicYearId: values.academicYearId,
      billingMonth: values.billingMonth.trim(),
      schoolLevel: values.schoolLevel || null,
      grade: values.grade || null,
      studyGroupId: values.studyGroupId || null
    });
    previewData.value = preview;
    showPreFlight.value = true;
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Unable to compute batch billing preview.');
    serverError.value = parsed.banner;
    if (Object.keys(parsed.fieldErrors).length > 0) {
      setErrors(parsed.fieldErrors);
    }
  }
});

async function handleConfirmGenerate(): Promise<void> {
  commitError.value = null;
  try {
    await generateMutation.mutateAsync({
      academicYearId: academicYearId.value,
      billingMonth: (billingMonth.value ?? '').trim(),
      schoolLevel: schoolLevel.value || null,
      grade: grade.value || null,
      studyGroupId: studyGroupId.value || null,
      dueDate: dueDate.value ? dueDate.value : null
    });
    showPreFlight.value = false;
    emit('update:visible', false);
    emit('completed');
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Failed to generate monthly tuition billing.');
    commitError.value = parsed.banner;
  }
}

function handleBackFromPreFlight(): void {
  showPreFlight.value = false;
  commitError.value = null;
}

function handleCancelAll(): void {
  showPreFlight.value = false;
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    :visible="visible && !showPreFlight"
    :modal="true"
    :closable="!previewMutation.isPending.value"
    :draggable="false"
    header="Generate Monthly Tuition Billing"
    class="w-full max-w-lg"
    data-testid="batch-billing-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4" @submit.prevent="onPreviewSubmit">
      <div
        class="rounded-lg border border-sky-200 bg-sky-50 px-3.5 py-2.5 text-xs leading-relaxed text-sky-800"
      >
        Select the target month and optional cohort filters. You will review a
        <strong>Tier 3 Pre-Flight Impact Summary</strong> with student counts, discounts, and net
        receivable totals before any charges are posted.
      </div>

      <div
        v-if="serverError"
        role="alert"
        class="rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-2.5 text-sm text-rose-700"
      >
        {{ serverError }}
      </div>

      <FormField
        label="Academic Year"
        required
        field-id="batchAcademicYearId"
        :error="errors.academicYearId"
      >
        <Select
          id="batchAcademicYearId"
          v-model="academicYearId"
          v-bind="academicYearIdAttrs"
          :options="academicYearOptions"
          option-label="label"
          option-value="value"
          placeholder="Select academic year"
          class="w-full"
          :disabled="previewMutation.isPending.value"
        />
      </FormField>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Billing Month (YYYY-MM)"
          required
          field-id="batchBillingMonth"
          :error="errors.billingMonth"
        >
          <InputText
            id="batchBillingMonth"
            v-model="billingMonth"
            v-bind="billingMonthAttrs"
            type="month"
            placeholder="2026-07"
            class="w-full"
            :disabled="previewMutation.isPending.value"
          />
        </FormField>

        <FormField label="Due Date (Optional)" field-id="batchDueDate" :error="errors.dueDate">
          <InputText
            id="batchDueDate"
            v-model="dueDate"
            v-bind="dueDateAttrs"
            type="date"
            class="w-full"
            :disabled="previewMutation.isPending.value"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="School Level (Optional)"
          field-id="batchSchoolLevel"
          :error="errors.schoolLevel"
        >
          <Select
            id="batchSchoolLevel"
            v-model="schoolLevel"
            v-bind="schoolLevelAttrs"
            :options="[...SCHOOL_LEVEL_OPTIONS]"
            option-label="label"
            option-value="value"
            placeholder="All Levels"
            show-clear
            class="w-full"
            :disabled="previewMutation.isPending.value"
          />
        </FormField>

        <FormField label="Grade (Optional)" field-id="batchGrade" :error="errors.grade">
          <Select
            id="batchGrade"
            v-model="grade"
            v-bind="gradeAttrs"
            :options="gradeOptions"
            option-label="label"
            option-value="value"
            placeholder="All Grades"
            show-clear
            class="w-full"
            :disabled="previewMutation.isPending.value"
          />
        </FormField>
      </div>

      <FormField
        label="Study Group (Optional)"
        field-id="batchStudyGroupId"
        :error="errors.studyGroupId"
      >
        <Select
          id="batchStudyGroupId"
          v-model="studyGroupId"
          v-bind="studyGroupIdAttrs"
          :options="studyGroupOptions"
          option-label="label"
          option-value="value"
          placeholder="All Study Groups"
          show-clear
          filter
          class="w-full"
          :loading="studyGroupsQuery.isLoading.value"
          :disabled="previewMutation.isPending.value"
        />
      </FormField>

      <div class="flex justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="previewMutation.isPending.value"
          @click="emit('update:visible', false)"
        />
        <Button
          type="submit"
          label="Preview Billing Impact"
          :loading="previewMutation.isPending.value"
          data-testid="batch-preview-submit"
        />
      </div>
    </form>
  </Dialog>

  <PreFlightSummaryModal
    :visible="visible && showPreFlight"
    :preview="previewData"
    :academic-year-name="selectedAcademicYearName"
    :billing-month="billingMonth || ''"
    :scope-summary="scopeSummary"
    :due-date="dueDate || null"
    :loading="generateMutation.isPending.value"
    :error-message="commitError"
    @update:visible="(val) => !val && handleCancelAll()"
    @back="handleBackFromPreFlight"
    @cancel="handleCancelAll"
    @confirm="handleConfirmGenerate"
  />
</template>
