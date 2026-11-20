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
import { AlertCircle, BadgePercent, Check, Search, UserCheck } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import type { ChargeTypeDto, StudentDiscountDto, StudentLookupItem } from '../api/feePolicy.api';
import { useDiscountStudentSearchQuery } from '../api/useFeePolicyQueries';
import { useCreateStudentDiscountMutation } from '../api/useFeePolicyMutations';
import {
  DISCOUNT_TYPES,
  grantDiscountSchema,
  type GrantDiscountFormValues
} from '../schemas/feePolicy.schema';
import { resolveFormSubmissionError } from '../utils/formErrors';
import { formatFrequencyLabel, formatIdr } from '../utils/formatters';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    academicYearId?: string | null;
    chargeTypes?: ChargeTypeDto[];
  }>(),
  {
    academicYearId: null,
    chargeTypes: () => []
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  granted: [discount: StudentDiscountDto];
}>();

const studentSearchText = ref('');
const selectedStudent = ref<StudentLookupItem | null>(null);
const errorBanner = ref<string | null>(null);

const studentSearchQuery = useDiscountStudentSearchQuery(
  () => ({
    academicYearId: props.academicYearId ?? undefined,
    search: studentSearchText.value
  }),
  { enabled: () => props.visible }
);

const students = computed(() => studentSearchQuery.data.value ?? []);

const chargeTypeOptions = computed(() =>
  (props.chargeTypes ?? [])
    .filter((ct) => Boolean(ct.id))
    .map((ct) => ({
      label: `${ct.name} (${formatFrequencyLabel(ct.frequency)} • ${formatIdr(ct.amount)})`,
      value: ct.id!,
      status: ct.status ?? 'DRAFT',
      academicYearId: ct.academicYearId,
      academicYearName: ct.academicYearName
    }))
);

const discountTypeOptions = [
  { label: 'Percentage (%)', value: DISCOUNT_TYPES[0] },
  { label: 'Fixed Amount (IDR)', value: DISCOUNT_TYPES[1] }
];

const createDiscountMutation = useCreateStudentDiscountMutation();
const isSubmitting = computed(() => createDiscountMutation.isPending.value);

function buildInitialValues(): GrantDiscountFormValues {
  return {
    studentId: '',
    chargeTypeId: '',
    discountType: 'PERCENTAGE',
    discountValue: 10,
    reason: ''
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors, setFieldValue } =
  useForm<GrantDiscountFormValues>({
    validationSchema: toTypedSchema(grantDiscountSchema),
    initialValues: buildInitialValues()
  });

const [studentId] = defineField('studentId');
const [chargeTypeId] = defineField('chargeTypeId');
const [discountType] = defineField('discountType');
const [discountValue] = defineField('discountValue');
const [reason, reasonAttrs] = defineField('reason');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      errorBanner.value = null;
      studentSearchText.value = '';
      selectedStudent.value = null;
      resetForm({ values: buildInitialValues() });
    }
  }
);

function selectStudent(student: StudentLookupItem): void {
  if (!student.id) return;
  selectedStudent.value = student;
  setFieldValue('studentId', student.id);
}

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onSubmit = handleSubmit(async (values) => {
  errorBanner.value = null;

  const chosenChargeType = chargeTypeOptions.value.find((ct) => ct.value === values.chargeTypeId);

  try {
    const created = await createDiscountMutation.mutateAsync({
      studentId: values.studentId,
      studentName: selectedStudent.value?.fullName ?? null,
      studentNis: selectedStudent.value?.nis ?? null,
      chargeTypeId: values.chargeTypeId,
      academicYearId: chosenChargeType?.academicYearId ?? props.academicYearId ?? null,
      academicYearName: chosenChargeType?.academicYearName ?? null,
      discountType: values.discountType,
      discountValue: Number(values.discountValue),
      reason: values.reason.trim()
    });

    emit('granted', created);
    emit('update:visible', false);
  } catch (error) {
    errorBanner.value = resolveFormSubmissionError(
      error,
      setErrors,
      'Failed to grant standing discount.'
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
    class="w-full max-w-xl"
    data-testid="grant-discount-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700"
        >
          <BadgePercent class="h-4 w-4" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Grant Student Standing Discount</h3>
          <p class="text-xs text-slate-500">
            Assign a recurring sibling discount or scholarship deduction for automated billing.
          </p>
        </div>
      </div>
    </template>

    <form id="grant-discount-form" class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="errorBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800"
        data-testid="grant-discount-error-banner"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <div class="flex-1">{{ errorBanner }}</div>
      </div>

      <!-- Student Search & Selection -->
      <FormField
        label="Student"
        field-id="discount-student-search"
        required
        :error="errors.studentId"
      >
        <div class="space-y-2">
          <div class="relative">
            <Search
              class="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              id="discount-student-search"
              v-model="studentSearchText"
              type="search"
              placeholder="Search active student by name or NIS..."
              class="w-full pl-9"
              data-testid="discount-student-search-input"
            />
          </div>

          <div
            v-if="selectedStudent"
            class="flex items-center justify-between rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-900"
            data-testid="selected-discount-student"
          >
            <div class="flex items-center gap-2">
              <UserCheck class="h-4 w-4 text-emerald-700" />
              <span class="font-semibold">{{ selectedStudent.fullName }}</span>
              <span class="text-emerald-700">NIS: {{ selectedStudent.nis }}</span>
              <span v-if="selectedStudent.studyGroupName" class="text-emerald-700">
                • {{ selectedStudent.studyGroupName }}
              </span>
            </div>
            <button
              type="button"
              class="font-medium text-emerald-800 underline hover:text-emerald-950"
              @click="
                selectedStudent = null;
                setFieldValue('studentId', '');
              "
            >
              Change
            </button>
          </div>

          <div
            v-else
            class="max-h-40 overflow-y-auto rounded-lg border border-slate-200 divide-y divide-slate-100"
            data-testid="discount-student-results"
          >
            <div v-if="studentSearchQuery.isPending.value" class="p-3 text-xs text-slate-500">
              Searching active students...
            </div>
            <div v-else-if="students.length === 0" class="p-3 text-xs text-slate-500">
              No matching active students found.
            </div>
            <button
              v-for="student in students"
              :key="student.id"
              type="button"
              class="flex w-full items-center justify-between px-3 py-2 text-left text-xs transition-colors hover:bg-slate-50"
              :class="studentId === student.id ? 'bg-brand-accent-50/70' : ''"
              :data-testid="`discount-student-option-${student.id}`"
              @click="selectStudent(student)"
            >
              <div>
                <div class="font-medium text-slate-900">{{ student.fullName }}</div>
                <div class="text-slate-500">
                  NIS: {{ student.nis ?? '—' }}
                  <span v-if="student.studyGroupName"> • Class {{ student.studyGroupName }}</span>
                </div>
              </div>
              <Check v-if="studentId === student.id" class="h-4 w-4 text-brand-accent-600" />
            </button>
          </div>
        </div>
      </FormField>

      <!-- Fee Structure Selection -->
      <FormField
        label="Target Fee Structure"
        field-id="discount-charge-type"
        required
        :error="errors.chargeTypeId"
      >
        <Select
          v-model="chargeTypeId"
          input-id="discount-charge-type"
          :options="chargeTypeOptions"
          option-label="label"
          option-value="value"
          placeholder="Select fee structure"
          class="w-full"
          :invalid="Boolean(errors.chargeTypeId)"
          data-testid="discount-charge-type-select"
        />
      </FormField>

      <!-- Discount Type & Value -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Discount Type"
          field-id="discount-type"
          required
          :error="errors.discountType"
        >
          <Select
            v-model="discountType"
            input-id="discount-type"
            :options="discountTypeOptions"
            option-label="label"
            option-value="value"
            class="w-full"
            :invalid="Boolean(errors.discountType)"
            data-testid="discount-type-select"
          />
        </FormField>

        <FormField
          :label="discountType === 'PERCENTAGE' ? 'Percentage (%)' : 'Fixed Deduction (IDR)'"
          field-id="discount-value"
          required
          :error="errors.discountValue"
        >
          <InputNumber
            v-model="discountValue"
            input-id="discount-value"
            mode="decimal"
            :min="1"
            :max="discountType === 'PERCENTAGE' ? 100 : undefined"
            :max-fraction-digits="discountType === 'PERCENTAGE' ? 2 : 0"
            class="w-full"
            :invalid="Boolean(errors.discountValue)"
            data-testid="discount-value-input"
          />
        </FormField>
      </div>

      <!-- Reason -->
      <FormField
        label="Reason / Scholarship Program"
        field-id="discount-reason"
        required
        :error="errors.reason"
      >
        <Textarea
          id="discount-reason"
          v-model="reason"
          v-bind="reasonAttrs"
          rows="2"
          placeholder="e.g. Sibling Discount (2nd child) or Academic Merit Scholarship"
          class="w-full"
          :invalid="Boolean(errors.reason)"
          data-testid="discount-reason-input"
        />
      </FormField>
    </form>

    <template #footer>
      <div class="flex items-center justify-end gap-2">
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
          form="grant-discount-form"
          label="Grant Discount"
          :loading="isSubmitting"
          data-testid="grant-discount-submit-button"
        />
      </div>
    </template>
  </Dialog>
</template>
