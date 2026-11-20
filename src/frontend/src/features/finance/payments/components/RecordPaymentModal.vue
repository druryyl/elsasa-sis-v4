<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { AlertCircle, Banknote, Building2, Wallet } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatBackendDateOnly, formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  PaymentReceiptDto,
  StudentBillingContactDto,
  StudentStatementChargeDto
} from '../api/payments.api';
import { useRecordPaymentMutation } from '../api/usePaymentMutations';
import {
  extractApiErrorMessage,
  extractApiFieldErrors,
  PAYMENT_METHOD_OPTIONS,
  recordPaymentSchema,
  type PaymentMethod,
  type RecordPaymentFormValues
} from '../schemas/payments.schema';
import { formatBillingMonth, formatIdr, getChargeStatusIntent } from '../utils/formatters';

interface ChargeAllocationRowState {
  checked: boolean;
  amount: number;
}

const props = withDefaults(
  defineProps<{
    visible: boolean;
    student: StudentBillingContactDto | null;
    academicYearId: string;
    academicYearName?: string | null;
    openCharges?: StudentStatementChargeDto[];
    preselectedChargeId?: string | null;
  }>(),
  {
    academicYearName: null,
    openCharges: () => [],
    preselectedChargeId: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  recorded: [receipt: PaymentReceiptDto];
}>();

const mutation = useRecordPaymentMutation();
const isSubmitting = computed(() => mutation.isPending.value);
const serverError = ref<string | null>(null);
const allocationError = ref<string | null>(null);
const hasManualTotal = ref(false);
const allocationMap = reactive<Record<string, ChargeAllocationRowState>>({});

function getTodayDateString(): string {
  return formatBackendDateOnly(new Date());
}

const { defineField, errors, handleSubmit, resetForm, setErrors, setFieldValue } =
  useForm<RecordPaymentFormValues>({
    validationSchema: toTypedSchema(recordPaymentSchema),
    initialValues: {
      paymentDate: getTodayDateString(),
      paymentMethod: 'CASH',
      bankReference: '',
      totalAmount: 0,
      notes: ''
    }
  });

const [paymentDate] = defineField('paymentDate');
const [paymentMethod] = defineField('paymentMethod');
const [bankReference] = defineField('bankReference');
const [totalAmount] = defineField('totalAmount');
const [notes] = defineField('notes');

function resetAllocations(): void {
  for (const key of Object.keys(allocationMap)) {
    delete allocationMap[key];
  }

  let preselectedAmount = 0;
  for (const charge of props.openCharges ?? []) {
    if (!charge.id) continue;
    const outstanding = Number(charge.outstandingAmount ?? 0);
    const shouldCheck = Boolean(props.preselectedChargeId && charge.id === props.preselectedChargeId);
    allocationMap[charge.id] = {
      checked: shouldCheck,
      amount: shouldCheck ? outstanding : 0
    };
    if (shouldCheck) {
      preselectedAmount += outstanding;
    }
  }

  hasManualTotal.value = false;
  if (preselectedAmount > 0) {
    setFieldValue('totalAmount', preselectedAmount);
  }
}

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      allocationError.value = null;
      resetForm({
        values: {
          paymentDate: getTodayDateString(),
          paymentMethod: 'CASH',
          bankReference: '',
          totalAmount: 0,
          notes: ''
        }
      });
      resetAllocations();
    }
  }
);

const checkedItems = computed(() => {
  const items: { chargeId: string; charge: StudentStatementChargeDto; amount: number }[] = [];
  for (const charge of props.openCharges ?? []) {
    if (!charge.id) continue;
    const row = allocationMap[charge.id];
    if (row?.checked) {
      items.push({
        chargeId: charge.id,
        charge,
        amount: Number(row.amount || 0)
      });
    }
  }
  return items;
});

const allocatedSum = computed(() =>
  checkedItems.value.reduce((acc, item) => acc + (Number.isFinite(item.amount) ? item.amount : 0), 0)
);

const numericTotalAmount = computed(() => {
  const val = Number(totalAmount.value ?? 0);
  return Number.isFinite(val) ? val : 0;
});

const advanceDepositHeld = computed(() =>
  Math.max(0, numericTotalAmount.value - allocatedSum.value)
);

const exceedsTotalAmount = computed(() => allocatedSum.value > numericTotalAmount.value);

function toggleChargeSelection(charge: StudentStatementChargeDto, checked: boolean): void {
  if (!charge.id) return;
  allocationError.value = null;

  const existing = allocationMap[charge.id] ?? { checked: false, amount: 0 };
  const outstanding = Number(charge.outstandingAmount ?? 0);

  if (checked) {
    let defaultAlloc = outstanding;
    if (hasManualTotal.value && numericTotalAmount.value > 0) {
      const otherAllocated = allocatedSum.value - (existing.checked ? existing.amount : 0);
      const remaining = Math.max(0, numericTotalAmount.value - otherAllocated);
      defaultAlloc = remaining > 0 ? Math.min(outstanding, remaining) : outstanding;
    }
    allocationMap[charge.id] = { checked: true, amount: defaultAlloc };
  } else {
    allocationMap[charge.id] = { checked: false, amount: 0 };
  }

  if (!hasManualTotal.value) {
    setFieldValue('totalAmount', allocatedSum.value);
  }
}

function updateChargeAllocationAmount(charge: StudentStatementChargeDto, nextAmount: number | null): void {
  if (!charge.id) return;
  allocationError.value = null;
  const current = allocationMap[charge.id] ?? { checked: true, amount: 0 };
  const sanitized = Math.max(0, Number(nextAmount ?? 0));
  allocationMap[charge.id] = {
    checked: current.checked,
    amount: sanitized
  };

  if (!hasManualTotal.value) {
    setFieldValue('totalAmount', allocatedSum.value);
  }
}

function handleTotalAmountUpdate(value: number | null): void {
  hasManualTotal.value = true;
  allocationError.value = null;
  setFieldValue('totalAmount', Number(value ?? 0));
}

function autoAllocateFromTotal(): void {
  allocationError.value = null;
  let remaining = numericTotalAmount.value;
  if (remaining <= 0) return;

  for (const charge of props.openCharges ?? []) {
    if (!charge.id) continue;
    const outstanding = Number(charge.outstandingAmount ?? 0);
    if (remaining > 0 && outstanding > 0) {
      const alloc = Math.min(outstanding, remaining);
      allocationMap[charge.id] = { checked: true, amount: alloc };
      remaining -= alloc;
    } else {
      allocationMap[charge.id] = { checked: false, amount: 0 };
    }
  }
}

function syncTotalToSelected(): void {
  hasManualTotal.value = false;
  allocationError.value = null;
  setFieldValue('totalAmount', allocatedSum.value);
}

function selectPaymentMethod(method: PaymentMethod): void {
  setFieldValue('paymentMethod', method);
  if (method === 'CASH') {
    setFieldValue('bankReference', '');
  }
}

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

function validateAllocations(): boolean {
  allocationError.value = null;

  for (const item of checkedItems.value) {
    const outstanding = Number(item.charge.outstandingAmount ?? 0);
    if (item.amount <= 0) {
      allocationError.value = `Allocated amount for "${item.charge.chargeTypeName}" must be greater than zero.`;
      return false;
    }
    if (item.amount > outstanding) {
      allocationError.value = `Allocated amount for "${item.charge.chargeTypeName}" cannot exceed outstanding balance (${formatIdr(outstanding)}).`;
      return false;
    }
  }

  if (exceedsTotalAmount.value) {
    allocationError.value = 'Total allocated amount cannot exceed total payment received.';
    return false;
  }

  return true;
}

const onSubmit = handleSubmit(async (values) => {
  if (!props.student?.studentId || !props.academicYearId) {
    serverError.value = 'Student and active academic year context are required.';
    return;
  }

  if (!validateAllocations()) {
    return;
  }

  serverError.value = null;

  try {
    const receipt = await mutation.mutateAsync({
      studentId: props.student.studentId,
      academicYearId: props.academicYearId,
      academicYearName: props.academicYearName ?? null,
      studentName: props.student.studentName ?? null,
      studentNis: props.student.studentNis ?? null,
      studyGroupId: props.student.studyGroupId ?? null,
      studyGroupName: props.student.studyGroupName ?? null,
      paymentDate: values.paymentDate,
      paymentMethod: values.paymentMethod,
      bankReference:
        values.paymentMethod === 'BANK_TRANSFER' ? values.bankReference?.trim() || null : null,
      totalAmount: Number(values.totalAmount),
      notes: values.notes?.trim() || null,
      allocations: checkedItems.value.map((item) => ({
        chargeId: item.chargeId,
        amount: item.amount
      }))
    });

    emit('recorded', receipt);
    emit('update:visible', false);
  } catch (error) {
    const fieldErrors = extractApiFieldErrors(error);
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors);
    }
    const { detail } = extractApiErrorMessage(error, 'Failed to record payment.');
    serverError.value = detail;
  }
});
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :draggable="false"
    :closable="!isSubmitting"
    class="w-full max-w-3xl"
    data-testid="record-payment-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-teal-50 text-teal-600"
        >
          <Wallet class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Record Cashier Payment</h3>
          <p class="text-xs text-slate-500">
            {{ student?.studentName ?? 'Student' }} ({{ student?.studentNis ?? '-' }}) •
            {{ student?.studyGroupName ?? 'Unplaced' }}
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <div
        v-if="serverError"
        role="alert"
        class="flex items-start gap-2 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2.5 text-xs text-rose-800"
        data-testid="record-payment-server-error"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0" />
        <span>{{ serverError }}</span>
      </div>

      <!-- Payment Method & Date Row -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField label="Payment Method" required :error="errors.paymentMethod">
          <div class="grid grid-cols-2 gap-2">
            <button
              v-for="opt in PAYMENT_METHOD_OPTIONS"
              :key="opt.value"
              type="button"
              :data-testid="`payment-method-${opt.value}`"
              class="flex items-center justify-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium transition-colors"
              :class="
                paymentMethod === opt.value
                  ? 'border-teal-600 bg-teal-50 text-teal-800'
                  : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
              "
              @click="selectPaymentMethod(opt.value)"
            >
              <Banknote v-if="opt.value === 'CASH'" class="h-4 w-4" />
              <Building2 v-else class="h-4 w-4" />
              <span>{{ opt.label }}</span>
            </button>
          </div>
        </FormField>

        <FormField label="Payment Date" field-id="payment-date" required :error="errors.paymentDate">
          <InputText
            id="payment-date"
            v-model="paymentDate"
            type="date"
            class="w-full"
            data-testid="payment-date-input"
          />
        </FormField>
      </div>

      <!-- Total Amount & Bank Reference -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="Total Amount Received (IDR)"
          field-id="payment-total-amount"
          required
          :error="errors.totalAmount"
        >
          <div class="flex items-center gap-2">
            <InputNumber
              input-id="payment-total-amount"
              :model-value="totalAmount"
              mode="currency"
              currency="IDR"
              locale="id-ID"
              :min="0"
              :max-fraction-digits="0"
              class="w-full"
              data-testid="payment-amount-input"
              @update:model-value="handleTotalAmountUpdate"
            />
            <Button
              v-if="openCharges.length > 0 && numericTotalAmount > 0"
              type="button"
              label="Auto"
              size="small"
              severity="secondary"
              outlined
              title="Auto-allocate payment across open charges"
              data-testid="auto-allocate-btn"
              @click="autoAllocateFromTotal"
            />
          </div>
        </FormField>

        <FormField
          v-if="paymentMethod === 'BANK_TRANSFER'"
          label="Bank Reference / Transfer ID"
          field-id="bank-reference"
          :error="errors.bankReference"
        >
          <InputText
            id="bank-reference"
            v-model="bankReference"
            placeholder="e.g. BCA-TRF-20260715-01"
            class="w-full"
            data-testid="bank-reference-input"
          />
        </FormField>

        <FormField v-else label="Cashier Notes (Optional)" field-id="payment-notes" :error="errors.notes">
          <InputText
            id="payment-notes"
            v-model="notes"
            placeholder="Optional receipt remarks..."
            class="w-full"
            data-testid="payment-notes-input"
          />
        </FormField>
      </div>

      <FormField
        v-if="paymentMethod === 'BANK_TRANSFER'"
        label="Cashier Notes (Optional)"
        field-id="payment-notes-bank"
        :error="errors.notes"
      >
        <Textarea
          id="payment-notes-bank"
          v-model="notes"
          rows="2"
          placeholder="Optional receipt remarks..."
          class="w-full"
          data-testid="payment-notes-input"
        />
      </FormField>

      <!-- Open Charges Allocation Section -->
      <div class="space-y-2">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h4 class="text-sm font-semibold text-slate-900">Allocate to Open Charges</h4>
            <p class="text-xs text-slate-500">
              Select charges to settle. Any excess payment is held as an advance deposit.
            </p>
          </div>
          <Button
            v-if="hasManualTotal && allocatedSum > 0 && allocatedSum !== numericTotalAmount"
            type="button"
            label="Match Selected Total"
            size="small"
            text
            data-testid="sync-total-btn"
            @click="syncTotalToSelected"
          />
        </div>

        <div
          v-if="allocationError"
          role="alert"
          class="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs font-medium text-rose-700"
          data-testid="allocation-error"
        >
          {{ allocationError }}
        </div>

        <div
          v-if="openCharges.length === 0"
          class="rounded-lg border border-dashed border-slate-200 bg-slate-50 px-4 py-4 text-center text-xs text-slate-500"
        >
          No open unpaid charges for this student. The full payment amount will be held as an advance deposit.
        </div>

        <div v-else class="max-h-56 overflow-y-auto rounded-lg border border-slate-200">
          <table class="min-w-full divide-y divide-slate-200 text-xs">
            <thead class="sticky top-0 bg-slate-50 text-left font-semibold text-slate-600">
              <tr>
                <th class="w-10 px-3 py-2">Pay</th>
                <th class="px-3 py-2">Charge</th>
                <th class="px-3 py-2">Month / Due</th>
                <th class="px-3 py-2 text-right">Outstanding</th>
                <th class="w-40 px-3 py-2 text-right">Allocate (IDR)</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 bg-white">
              <tr
                v-for="charge in openCharges"
                :key="charge.id"
                :class="allocationMap[charge.id!]?.checked ? 'bg-teal-50/40' : ''"
              >
                <td class="px-3 py-2">
                  <Checkbox
                    :model-value="Boolean(allocationMap[charge.id!]?.checked)"
                    :binary="true"
                    :input-id="`alloc-chk-${charge.id}`"
                    :data-testid="`alloc-checkbox-${charge.id}`"
                    @update:model-value="toggleChargeSelection(charge, Boolean($event))"
                  />
                </td>
                <td class="px-3 py-2">
                  <div class="font-medium text-slate-900">{{ charge.chargeTypeName }}</div>
                  <div class="mt-0.5 flex items-center gap-1.5">
                    <StatusPill
                      :status="charge.status ?? 'UNPAID'"
                      :intent="getChargeStatusIntent(charge.status)"
                    />
                    <span v-if="charge.description" class="text-slate-500">
                      {{ charge.description }}
                    </span>
                  </div>
                </td>
                <td class="px-3 py-2 text-slate-600">
                  <div>{{ formatBillingMonth(charge.billingMonth) }}</div>
                  <div v-if="charge.dueDate" class="text-[11px] text-slate-400">
                    Due {{ formatDisplayDate(charge.dueDate) }}
                  </div>
                </td>
                <td class="px-3 py-2 text-right font-medium text-slate-800">
                  {{ formatIdr(charge.outstandingAmount) }}
                </td>
                <td class="px-3 py-2 text-right">
                  <InputNumber
                    v-if="allocationMap[charge.id!]?.checked"
                    :model-value="allocationMap[charge.id!]?.amount ?? 0"
                    :min="0"
                    :max="Number(charge.outstandingAmount ?? 0)"
                    :max-fraction-digits="0"
                    input-class="w-full text-right text-xs py-1 px-2"
                    class="w-full"
                    :data-testid="`alloc-amount-${charge.id}`"
                    @update:model-value="updateChargeAllocationAmount(charge, $event)"
                  />
                  <span v-else class="text-slate-400">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Allocation & Advance Deposit Summary -->
      <div class="grid grid-cols-3 gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs">
        <div>
          <span class="block text-slate-500">Payment Received</span>
          <span class="mt-0.5 block text-sm font-bold text-slate-900" data-testid="summary-received">
            {{ formatIdr(numericTotalAmount) }}
          </span>
        </div>
        <div>
          <span class="block text-slate-500">Allocated to Charges</span>
          <span
            class="mt-0.5 block text-sm font-bold"
            :class="exceedsTotalAmount ? 'text-rose-600' : 'text-teal-700'"
            data-testid="summary-allocated"
          >
            {{ formatIdr(allocatedSum) }}
          </span>
        </div>
        <div>
          <span class="block text-slate-500">Advance Deposit Held</span>
          <span
            class="mt-0.5 block text-sm font-bold"
            :class="advanceDepositHeld > 0 ? 'text-amber-700' : 'text-slate-700'"
            data-testid="summary-deposit"
          >
            {{ formatIdr(advanceDepositHeld) }}
          </span>
        </div>
      </div>

      <div class="flex items-center justify-end gap-2 border-t border-slate-200 pt-3">
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
          label="Post Payment Receipt"
          :loading="isSubmitting"
          :disabled="isSubmitting || numericTotalAmount <= 0 || exceedsTotalAmount"
          data-testid="submit-payment-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
