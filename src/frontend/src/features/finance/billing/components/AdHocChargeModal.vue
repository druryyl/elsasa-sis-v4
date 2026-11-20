<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useQuery } from '@tanstack/vue-query';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Textarea from 'primevue/textarea';
import FormField from '@/shared/components/forms/FormField.vue';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import { adHocChargeSchema, extractSubmissionError } from '../schemas/billing.schema';
import {
  useActiveChargeTypesQuery,
  useBillingStudentSearchQuery
} from '../api/useBillingQueries';
import { useCreateAdHocChargeMutation } from '../api/useBillingMutations';
import { formatFrequencyLabel, formatIdr } from '../utils/formatters';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    defaultAcademicYearId?: string | null;
  }>(),
  {
    defaultAcademicYearId: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  created: [];
}>();

const serverError = ref<string | null>(null);
const createMutation = useCreateAdHocChargeMutation();
const academicYearsQuery = useQuery(academicYearsLookupQuery());

const { handleSubmit, errors, resetForm, defineField, setFieldValue, setErrors } = useForm({
  validationSchema: toTypedSchema(adHocChargeSchema),
  initialValues: {
    academicYearId: props.defaultAcademicYearId ?? '',
    studentId: '',
    chargeTypeId: '',
    description: '',
    unitAmount: 0,
    quantity: 1,
    billingMonth: '',
    dueDate: ''
  }
});

const [academicYearId, academicYearIdAttrs] = defineField('academicYearId');
const [studentId, studentIdAttrs] = defineField('studentId');
const [chargeTypeId, chargeTypeIdAttrs] = defineField('chargeTypeId');
const [description, descriptionAttrs] = defineField('description');
const [unitAmount, unitAmountAttrs] = defineField('unitAmount');
const [quantity, quantityAttrs] = defineField('quantity');
const [billingMonth, billingMonthAttrs] = defineField('billingMonth');
const [dueDate, dueDateAttrs] = defineField('dueDate');

const activeChargeTypesQuery = useActiveChargeTypesQuery(
  computed(() => academicYearId.value || undefined),
  { enabled: computed(() => props.visible && Boolean(academicYearId.value)) }
);

const studentsQuery = useBillingStudentSearchQuery(
  computed(() => ({ academicYearId: academicYearId.value || undefined })),
  { enabled: computed(() => props.visible) }
);

const academicYearOptions = computed(() =>
  (academicYearsQuery.data.value ?? []).map((year) => ({
    label: year.status === 'ACTIVE' ? `${year.name} (Active)` : (year.name ?? 'Unnamed Year'),
    value: year.id ?? ''
  }))
);

const studentOptions = computed(() =>
  (studentsQuery.data.value ?? []).map((student) => {
    const scope = student.studyGroupName
      ? ` • ${student.studyGroupName}`
      : student.schoolLevel && student.grade
        ? ` • ${student.schoolLevel} ${student.grade}`
        : '';
    return {
      label: `${student.fullName ?? 'Student'} (${student.nis ?? 'No NIS'})${scope}`,
      value: student.id ?? ''
    };
  })
);

const chargeTypeOptions = computed(() =>
  (activeChargeTypesQuery.data.value ?? []).map((ct) => ({
    label: `${ct.name ?? 'Fee'} (${formatFrequencyLabel(ct.frequency)}) — ${formatIdr(ct.amount)}`,
    value: ct.id ?? ''
  }))
);

const computedGrossPreview = computed(() => {
  const unit = typeof unitAmount.value === 'number' && Number.isFinite(unitAmount.value) ? unitAmount.value : 0;
  const qty = typeof quantity.value === 'number' && Number.isFinite(quantity.value) ? quantity.value : 0;
  return formatIdr(Math.max(0, unit * qty));
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      const activeYear =
        props.defaultAcademicYearId ||
        academicYearsQuery.data.value?.find((y) => y.status === 'ACTIVE')?.id ||
        academicYearsQuery.data.value?.[0]?.id ||
        '';
      resetForm({
        values: {
          academicYearId: activeYear,
          studentId: '',
          chargeTypeId: '',
          description: '',
          unitAmount: 0,
          quantity: 1,
          billingMonth: '',
          dueDate: ''
        }
      });
    }
  }
);

watch(chargeTypeId, (nextChargeTypeId) => {
  if (!nextChargeTypeId) return;
  const matched = activeChargeTypesQuery.data.value?.find((ct) => ct.id === nextChargeTypeId);
  if (matched && typeof matched.amount === 'number') {
    setFieldValue('unitAmount', matched.amount);
  }
});

const onSubmit = handleSubmit(async (values) => {
  serverError.value = null;
  try {
    await createMutation.mutateAsync({
      academicYearId: values.academicYearId,
      studentId: values.studentId,
      chargeTypeId: values.chargeTypeId,
      description: values.description.trim(),
      unitAmount: values.unitAmount,
      quantity: values.quantity,
      billingMonth: values.billingMonth?.trim() ? values.billingMonth.trim() : null,
      dueDate: values.dueDate ? values.dueDate : null
    });
    emit('update:visible', false);
    emit('created');
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Failed to create ad-hoc charge.');
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
    :closable="!createMutation.isPending.value"
    :draggable="false"
    header="Create Ad-Hoc Student Charge"
    class="w-full max-w-lg"
    data-testid="adhoc-charge-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <form class="space-y-4" @submit.prevent="onSubmit">
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
        field-id="adhocAcademicYearId"
        :error="errors.academicYearId"
      >
        <Select
          id="adhocAcademicYearId"
          v-model="academicYearId"
          v-bind="academicYearIdAttrs"
          :options="academicYearOptions"
          option-label="label"
          option-value="value"
          placeholder="Select academic year"
          class="w-full"
          :disabled="createMutation.isPending.value"
        />
      </FormField>

      <FormField label="Student" required field-id="adhocStudentId" :error="errors.studentId">
        <Select
          id="adhocStudentId"
          v-model="studentId"
          v-bind="studentIdAttrs"
          :options="studentOptions"
          option-label="label"
          option-value="value"
          placeholder="Select placed student"
          filter
          class="w-full"
          :loading="studentsQuery.isLoading.value"
          :disabled="createMutation.isPending.value"
        />
      </FormField>

      <FormField
        label="Fee / Charge Type"
        required
        field-id="adhocChargeTypeId"
        :error="errors.chargeTypeId"
      >
        <Select
          id="adhocChargeTypeId"
          v-model="chargeTypeId"
          v-bind="chargeTypeIdAttrs"
          :options="chargeTypeOptions"
          option-label="label"
          option-value="value"
          placeholder="Select active fee structure"
          filter
          class="w-full"
          :loading="activeChargeTypesQuery.isLoading.value"
          :disabled="createMutation.isPending.value"
        />
      </FormField>

      <FormField
        label="Charge Description"
        required
        field-id="adhocDescription"
        :error="errors.description"
      >
        <Textarea
          id="adhocDescription"
          v-model="description"
          v-bind="descriptionAttrs"
          rows="2"
          placeholder="e.g., School uniform package size L or replacement lab workbook"
          class="w-full"
          :disabled="createMutation.isPending.value"
        />
      </FormField>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Unit Price (IDR)"
          required
          field-id="adhocUnitAmount"
          :error="errors.unitAmount"
        >
          <InputNumber
            id="adhocUnitAmount"
            v-model="unitAmount"
            v-bind="unitAmountAttrs"
            :min="0"
            :use-grouping="true"
            input-class="w-full"
            class="w-full"
            :disabled="createMutation.isPending.value"
          />
        </FormField>

        <FormField label="Quantity" required field-id="adhocQuantity" :error="errors.quantity">
          <InputNumber
            id="adhocQuantity"
            v-model="quantity"
            v-bind="quantityAttrs"
            :min="1"
            :step="1"
            show-buttons
            input-class="w-full"
            class="w-full"
            :disabled="createMutation.isPending.value"
          />
        </FormField>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Billing Month (Optional)"
          field-id="adhocBillingMonth"
          :error="errors.billingMonth"
        >
          <InputText
            id="adhocBillingMonth"
            v-model="billingMonth"
            v-bind="billingMonthAttrs"
            type="month"
            placeholder="YYYY-MM"
            class="w-full"
            :disabled="createMutation.isPending.value"
          />
        </FormField>

        <FormField label="Due Date (Optional)" field-id="adhocDueDate" :error="errors.dueDate">
          <InputText
            id="adhocDueDate"
            v-model="dueDate"
            v-bind="dueDateAttrs"
            type="date"
            class="w-full"
            :disabled="createMutation.isPending.value"
          />
        </FormField>
      </div>

      <div
        class="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs"
      >
        <div>
          <span class="font-medium text-slate-700">Gross Invoice Amount</span>
          <p class="text-[11px] text-slate-500">
            Active student discounts for this fee type are auto-applied on submit.
          </p>
        </div>
        <span class="text-sm font-bold text-slate-900">{{ computedGrossPreview }}</span>
      </div>

      <div class="flex justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="createMutation.isPending.value"
          @click="emit('update:visible', false)"
        />
        <Button
          type="submit"
          label="Create Charge"
          :loading="createMutation.isPending.value"
          data-testid="adhoc-submit-button"
        />
      </div>
    </form>
  </Dialog>
</template>
