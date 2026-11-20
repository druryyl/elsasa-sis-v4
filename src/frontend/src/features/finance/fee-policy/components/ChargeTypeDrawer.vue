<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Select from 'primevue/select';
import { AlertCircle, Coins, Lock } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { academicYearsLookupQuery, studyGroupsLookupQuery } from '@/shared/services/lookups.api';
import type { ChargeTypeDto } from '../api/feePolicy.api';
import {
  useCreateChargeTypeMutation,
  useUpdateChargeTypeMutation
} from '../api/useFeePolicyMutations';
import {
  CHARGE_FREQUENCIES,
  SCHOOL_LEVEL_OPTIONS,
  chargeTypeSchema,
  type ChargeTypeFormValues
} from '../schemas/feePolicy.schema';
import { resolveFormSubmissionError } from '../utils/formErrors';
import { formatFrequencyLabel } from '../utils/formatters';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    chargeType?: ChargeTypeDto | null;
    defaultAcademicYearId?: string | null;
  }>(),
  {
    chargeType: null,
    defaultAcademicYearId: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [chargeType: ChargeTypeDto];
}>();

const isEditMode = computed(() => Boolean(props.chargeType?.id));
const isLockedActive = computed(() => props.chargeType?.status === 'ACTIVE');

const drawerTitle = computed(() => {
  if (isLockedActive.value) return 'View Active Fee Structure';
  return isEditMode.value ? 'Edit Fee Structure' : 'New Fee Structure';
});

const academicYearsQuery = useQuery({
  ...academicYearsLookupQuery(),
  enabled: () => props.visible
});

const academicYearOptions = computed(() =>
  (academicYearsQuery.data.value ?? [])
    .filter((y) => Boolean(y.id))
    .map((y) => ({
      label: y.status === 'ACTIVE' ? `${y.name} (Active)` : (y.name ?? 'Unnamed Academic Year'),
      value: y.id!,
      name: y.name ?? ''
    }))
);

const frequencyOptions = CHARGE_FREQUENCIES.map((freq) => ({
  label: formatFrequencyLabel(freq),
  value: freq
}));

const schoolLevelOptions = [
  { label: 'All Levels', value: '' },
  ...SCHOOL_LEVEL_OPTIONS.map((level) => ({ label: level, value: level }))
];

const createMutation = useCreateChargeTypeMutation();
const updateMutation = useUpdateChargeTypeMutation();
const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const errorBanner = ref<string | null>(null);

function resolveInitialAcademicYearId(chargeType?: ChargeTypeDto | null): string {
  if (chargeType?.academicYearId) {
    return chargeType.academicYearId;
  }
  if (props.defaultAcademicYearId) {
    return props.defaultAcademicYearId;
  }
  const active = academicYearsQuery.data.value?.find((y) => y.status === 'ACTIVE');
  return active?.id ?? academicYearsQuery.data.value?.[0]?.id ?? '';
}

function buildInitialValues(chargeType?: ChargeTypeDto | null): ChargeTypeFormValues {
  const validFreq = CHARGE_FREQUENCIES.includes(
    (chargeType?.frequency ?? '') as (typeof CHARGE_FREQUENCIES)[number]
  )
    ? (chargeType!.frequency as (typeof CHARGE_FREQUENCIES)[number])
    : 'MONTHLY';

  return {
    academicYearId: resolveInitialAcademicYearId(chargeType),
    name: chargeType?.name ?? '',
    frequency: validFreq,
    amount: typeof chargeType?.amount === 'number' ? chargeType.amount : 0,
    schoolLevel: chargeType?.schoolLevel ?? '',
    grade: chargeType?.grade ?? '',
    studyGroupId: chargeType?.studyGroupId ?? ''
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors, setFieldValue } =
  useForm<ChargeTypeFormValues>({
    validationSchema: toTypedSchema(chargeTypeSchema),
    initialValues: buildInitialValues(props.chargeType)
  });

const [academicYearId] = defineField('academicYearId');
const [name, nameAttrs] = defineField('name');
const [frequency] = defineField('frequency');
const [amount] = defineField('amount');
const [schoolLevel] = defineField('schoolLevel');
const [grade, gradeAttrs] = defineField('grade');
const [studyGroupId] = defineField('studyGroupId');

const studyGroupsQuery = useQuery({
  ...studyGroupsLookupQuery(),
  enabled: () => props.visible
});

const studyGroupOptions = computed(() => {
  const groups = studyGroupsQuery.data.value ?? [];
  const selectedYear = academicYearId.value;
  const selectedLevel = schoolLevel.value?.trim() || '';
  const selectedGrade = grade.value?.trim() || '';

  const filtered = groups.filter((sg) => {
    if (selectedYear && sg.academicYearId && sg.academicYearId !== selectedYear) return false;
    if (selectedLevel && sg.schoolLevel && sg.schoolLevel !== selectedLevel) return false;
    if (selectedGrade && sg.grade && sg.grade !== selectedGrade) return false;
    return Boolean(sg.id);
  });

  return [
    { label: 'All Classes (No Study Group Restriction)', value: '' },
    ...filtered.map((sg) => ({
      label: `${sg.name} (${sg.schoolLevel ?? ''} Grade ${sg.grade ?? ''})`.trim(),
      value: sg.id!,
      name: sg.name ?? ''
    }))
  ];
});

watch(
  () => [props.visible, props.chargeType] as const,
  ([isOpen, current]) => {
    if (isOpen) {
      errorBanner.value = null;
      resetForm({
        values: buildInitialValues(current)
      });
    }
  }
);

watch(
  () => academicYearsQuery.data.value,
  (years) => {
    if (props.visible && !academicYearId.value && years && years.length > 0) {
      const active = years.find((y) => y.status === 'ACTIVE') ?? years[0];
      if (active?.id) {
        setFieldValue('academicYearId', active.id);
      }
    }
  }
);

function closeDrawer(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onSubmit = handleSubmit(async (values) => {
  if (isLockedActive.value) return;
  errorBanner.value = null;

  const selectedYearOption = academicYearOptions.value.find(
    (opt) => opt.value === values.academicYearId
  );
  const selectedGroupOption = studyGroupOptions.value.find(
    (opt) => opt.value && opt.value === values.studyGroupId
  );

  const normalizedSchoolLevel = values.schoolLevel?.trim() || null;
  const normalizedGrade = values.grade?.trim() || null;
  const normalizedStudyGroupId = values.studyGroupId?.trim() || null;
  const normalizedStudyGroupName =
    normalizedStudyGroupId && selectedGroupOption && 'name' in selectedGroupOption
      ? selectedGroupOption.name
      : null;

  try {
    let saved: ChargeTypeDto;
    if (isEditMode.value && props.chargeType?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.chargeType.id,
        payload: {
          name: values.name.trim(),
          frequency: values.frequency,
          amount: Number(values.amount),
          schoolLevel: normalizedSchoolLevel,
          grade: normalizedGrade,
          studyGroupId: normalizedStudyGroupId,
          studyGroupName: normalizedStudyGroupName
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        academicYearId: values.academicYearId,
        academicYearName: selectedYearOption?.name || null,
        name: values.name.trim(),
        frequency: values.frequency,
        amount: Number(values.amount),
        schoolLevel: normalizedSchoolLevel,
        grade: normalizedGrade,
        studyGroupId: normalizedStudyGroupId,
        studyGroupName: normalizedStudyGroupName
      });
    }

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    errorBanner.value = resolveFormSubmissionError(
      error,
      setErrors,
      'Failed to save fee structure.'
    );
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    :modal="true"
    :dismissable="!isSubmitting"
    class="w-full sm:max-w-lg"
    data-testid="charge-type-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-accent-50 text-brand-accent-600"
        >
          <Coins class="h-4 w-4" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">{{ drawerTitle }}</h3>
          <p class="text-xs text-slate-500">
            Configure fee amount, billing frequency, and optional cohort scope.
          </p>
        </div>
      </div>
    </template>

    <form id="charge-type-form" class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="isLockedActive"
        role="status"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800"
        data-testid="charge-type-locked-banner"
      >
        <Lock class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div>
          This fee structure is <strong>ACTIVE</strong> and locked from modifications to protect
          issued student charges.
        </div>
      </div>

      <div
        v-if="errorBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800"
        data-testid="charge-type-error-banner"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <div class="flex-1">{{ errorBanner }}</div>
      </div>

      <FormField
        label="Academic Year"
        field-id="charge-type-year"
        required
        :error="errors.academicYearId"
      >
        <Select
          v-model="academicYearId"
          input-id="charge-type-year"
          :options="academicYearOptions"
          option-label="label"
          option-value="value"
          placeholder="Select academic year"
          class="w-full"
          :disabled="isEditMode || isLockedActive"
          :invalid="Boolean(errors.academicYearId)"
          data-testid="charge-type-year-select"
        />
      </FormField>

      <FormField label="Fee Name" field-id="charge-type-name" required :error="errors.name">
        <InputText
          id="charge-type-name"
          v-model="name"
          v-bind="nameAttrs"
          type="text"
          placeholder="e.g. SPP Bulanan SMP"
          class="w-full"
          :disabled="isLockedActive"
          :invalid="Boolean(errors.name)"
          data-testid="charge-type-name-input"
        />
      </FormField>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Recurrence Frequency"
          field-id="charge-type-frequency"
          required
          :error="errors.frequency"
        >
          <Select
            v-model="frequency"
            input-id="charge-type-frequency"
            :options="frequencyOptions"
            option-label="label"
            option-value="value"
            placeholder="Select frequency"
            class="w-full"
            :disabled="isLockedActive"
            :invalid="Boolean(errors.frequency)"
            data-testid="charge-type-frequency-select"
          />
        </FormField>

        <FormField
          label="Standard Amount (IDR)"
          field-id="charge-type-amount"
          required
          :error="errors.amount"
        >
          <InputNumber
            v-model="amount"
            input-id="charge-type-amount"
            mode="decimal"
            :min="0"
            :max-fraction-digits="0"
            placeholder="350000"
            class="w-full"
            :disabled="isLockedActive"
            :invalid="Boolean(errors.amount)"
            data-testid="charge-type-amount-input"
          />
        </FormField>
      </div>

      <div class="rounded-lg border border-slate-200 bg-slate-50/70 p-3.5 space-y-3">
        <div>
          <h4 class="text-xs font-semibold uppercase tracking-wide text-slate-600">
            Target Cohort Scope (Optional)
          </h4>
          <p class="mt-0.5 text-xs text-slate-500">
            Leave blank to apply across all active students, or narrow by level, grade, or class.
          </p>
        </div>

        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormField label="School Level" field-id="charge-type-level" :error="errors.schoolLevel">
            <Select
              v-model="schoolLevel"
              input-id="charge-type-level"
              :options="schoolLevelOptions"
              option-label="label"
              option-value="value"
              class="w-full"
              :disabled="isLockedActive"
              data-testid="charge-type-level-select"
            />
          </FormField>

          <FormField label="Grade" field-id="charge-type-grade" :error="errors.grade">
            <InputText
              id="charge-type-grade"
              v-model="grade"
              v-bind="gradeAttrs"
              type="text"
              placeholder="e.g. 7 or 10"
              class="w-full"
              :disabled="isLockedActive"
              :invalid="Boolean(errors.grade)"
              data-testid="charge-type-grade-input"
            />
          </FormField>
        </div>

        <FormField
          label="Specific Study Group"
          field-id="charge-type-study-group"
          :error="errors.studyGroupId"
        >
          <Select
            v-model="studyGroupId"
            input-id="charge-type-study-group"
            :options="studyGroupOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :disabled="isLockedActive"
            data-testid="charge-type-study-group-select"
          />
        </FormField>
      </div>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-2">
        <Button
          type="button"
          :label="isLockedActive ? 'Close' : 'Cancel'"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeDrawer"
        />
        <Button
          v-if="!isLockedActive"
          type="submit"
          form="charge-type-form"
          :label="isEditMode ? 'Save Changes' : 'Save as Draft'"
          :loading="isSubmitting"
          data-testid="charge-type-submit-button"
        />
      </div>
    </template>
  </Drawer>
</template>
