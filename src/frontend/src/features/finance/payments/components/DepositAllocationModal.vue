<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import InputNumber from 'primevue/inputnumber';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { AlertCircle, PiggyBank } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatBackendDateOnly, formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  PaymentReceiptDto,
  StudentBillingContactDto,
  StudentStatementChargeDto,
  StudentStatementReceiptDto
} from '../api/payments.api';
import { useAllocateDepositMutation } from '../api/usePaymentMutations';
import {
  allocateDepositSchema,
  extractApiErrorMessage,
  extractApiFieldErrors,
  type AllocateDepositFormValues
} from '../schemas/payments.schema';
import { formatBillingMonth, formatIdr, getChargeStatusIntent } from '../utils/formatters';

interface DepositRowState {
  checked: boolean;
  amount: number;
}

const props = withDefaults(
  defineProps<{
    visible: boolean;
    student: StudentBillingContactDto | null;
    receiptsWithDeposit?: StudentStatementReceiptDto[];
    openCharges?: StudentStatementChargeDto[];
    totalDepositBalance?: number;
  }>(),
  {
    receiptsWithDeposit: () => [],
    openCharges: () => [],
    totalDepositBalance: 0
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  allocated: [receipt: PaymentReceiptDto];
}>();

const mutation = useAllocateDepositMutation();
const isSubmitting = computed(() => mutation.isPending.value);
const serverError = ref<string | null>(null);
const allocationError = ref<string | null>(null);
const allocationMap = reactive<Record<string, DepositRowState>>({});

function getTodayDateString(): string {
  return formatBackendDateOnly(new Date());
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<AllocateDepositFormValues>({
    validationSchema: toTypedSchema(allocateDepositSchema),
    initialValues: {
      sourceReceiptId: '',
      allocationDate: getTodayDateString()
    }
  });

const [sourceReceiptId] = defineField('sourceReceiptId');
const [allocationDate] = defineField('allocationDate');

const receiptOptions = computed(() =>
  (props.receiptsWithDeposit ?? [])
    .filter((r) => Boolean(r.id) && Number(r.unallocatedAmount ?? 0) > 0)
    .map((r) => ({
      label: `${r.receiptNumber ?? 'Receipt'} (${formatDisplayDate(r.paymentDate)}) — Available: ${formatIdr(r.unallocatedAmount)}`,
      value: r.id!,
      available: Number(r.unallocatedAmount ?? 0)
    }))
);

const selectedSourceAvailable = computed(() => {
  const matched = receiptOptions.value.find((opt) => opt.value === sourceReceiptId.value);
  return matched?.available ?? 0;
});

function initAllocationState(): void {
  for (const key of Object.keys(allocationMap)) {
    delete allocationMap[key];
  }
  for (const charge of props.openCharges ?? []) {
    if (!charge.id) continue;
    allocationMap[charge.id] = { checked: false, amount: 0 };
  }
}

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      allocationError.value = null;
      const firstReceiptId = receiptOptions.value[0]?.value ?? '';
      resetForm({
        values: {
          sourceReceiptId: firstReceiptId,
          allocationDate: getTodayDateString()
        }
      });
      initAllocationState();
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

const exceedsAvailableDeposit = computed(() => allocatedSum.value > selectedSourceAvailable.value);

function toggleCharge(charge: StudentStatementChargeDto, checked: boolean): void {
  if (!charge.id) return;
  allocationError.value = null;
  const outstanding = Number(charge.outstandingAmount ?? 0);

  if (checked) {
    const remainingDeposit = Math.max(0, selectedSourceAvailable.value - allocatedSum.value);
    const suggested = remainingDeposit > 0 ? Math.min(outstanding, remainingDeposit) : outstanding;
    allocationMap[charge.id] = { checked: true, amount: suggested };
  } else {
    allocationMap[charge.id] = { checked: false, amount: 0 };
  }
}

function updateChargeAmount(charge: StudentStatementChargeDto, nextAmount: number | null): void {
  if (!charge.id) return;
  allocationError.value = null;
  const current = allocationMap[charge.id] ?? { checked: true, amount: 0 };
  allocationMap[charge.id] = {
    checked: current.checked,
    amount: Math.max(0, Number(nextAmount ?? 0))
  };
}

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

function validateSelections(): boolean {
  allocationError.value = null;

  if (checkedItems.value.length === 0) {
    allocationError.value = 'Select at least one open charge to allocate deposit funds.';
    return false;
  }

  for (const item of checkedItems.value) {
    const outstanding = Number(item.charge.outstandingAmount ?? 0);
    if (item.amount <= 0) {
      allocationError.value = `Allocation amount for "${item.charge.chargeTypeName}" must be greater than zero.`;
      return false;
    }
    if (item.amount > outstanding) {
      allocationError.value = `Allocation for "${item.charge.chargeTypeName}" exceeds outstanding balance (${formatIdr(outstanding)}).`;
      return false;
    }
  }

  if (exceedsAvailableDeposit.value) {
    allocationError.value = `Total allocation exceeds available deposit (${formatIdr(selectedSourceAvailable.value)}) on the selected receipt.`;
    return false;
  }

  return true;
}

const onSubmit = handleSubmit(async (values) => {
  if (!props.student?.studentId) return;
  if (!validateSelections()) return;

  serverError.value = null;

  try {
    const updatedReceipt = await mutation.mutateAsync({
      studentId: props.student.studentId,
      sourceReceiptId: values.sourceReceiptId,
      allocationDate: values.allocationDate,
      allocations: checkedItems.value.map((item) => ({
        chargeId: item.chargeId,
        amount: item.amount
      }))
    });

    emit('allocated', updatedReceipt);
    emit('update:visible', false);
  } catch (error) {
    const fieldErrors = extractApiFieldErrors(error);
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors);
    }
    const { detail } = extractApiErrorMessage(error, 'Failed to allocate advance deposit.');
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
    class="w-full max-w-2xl"
    data-testid="deposit-allocation-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-amber-50 text-amber-600"
        >
          <PiggyBank class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Apply Advance Deposit</h3>
          <p class="text-xs text-slate-500">
            {{ student?.studentName ?? 'Student' }} • Total Held Deposit:
            <span class="font-semibold text-amber-700">{{ formatIdr(totalDepositBalance) }}</span>
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4 pt-1" @submit.prevent="onSubmit">
      <div
        v-if="serverError"
        role="alert"
        class="flex items-start gap-2 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2.5 text-xs text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0" />
        <span>{{ serverError }}</span>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div class="sm:col-span-2">
          <FormField
            label="Source Receipt with Deposit"
            field-id="source-receipt-select"
            required
            :error="errors.sourceReceiptId"
          >
            <Select
              v-model="sourceReceiptId"
              input-id="source-receipt-select"
              :options="receiptOptions"
              option-label="label"
              option-value="value"
              placeholder="Select source receipt..."
              class="w-full"
              data-testid="deposit-source-select"
            />
          </FormField>
        </div>

        <FormField
          label="Allocation Date"
          field-id="allocation-date-input"
          required
          :error="errors.allocationDate"
        >
          <InputText
            id="allocation-date-input"
            v-model="allocationDate"
            type="date"
            class="w-full"
            data-testid="deposit-date-input"
          />
        </FormField>
      </div>

      <div
        v-if="allocationError"
        role="alert"
        class="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs font-medium text-rose-700"
        data-testid="deposit-allocation-error"
      >
        {{ allocationError }}
      </div>

      <div
        v-if="openCharges.length === 0"
        class="rounded-lg border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-center text-xs text-slate-500"
      >
        This student currently has no open unpaid charges to allocate deposit funds to.
      </div>

      <div v-else class="max-h-56 overflow-y-auto rounded-lg border border-slate-200">
        <table class="min-w-full divide-y divide-slate-200 text-xs">
          <thead class="sticky top-0 bg-slate-50 text-left font-semibold text-slate-600">
            <tr>
              <th class="w-10 px-3 py-2">Apply</th>
              <th class="px-3 py-2">Open Charge</th>
              <th class="px-3 py-2">Month</th>
              <th class="px-3 py-2 text-right">Outstanding</th>
              <th class="w-40 px-3 py-2 text-right">Allocate (IDR)</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 bg-white">
            <tr
              v-for="charge in openCharges"
              :key="charge.id"
              :class="allocationMap[charge.id!]?.checked ? 'bg-amber-50/40' : ''"
            >
              <td class="px-3 py-2">
                <Checkbox
                  :model-value="Boolean(allocationMap[charge.id!]?.checked)"
                  :binary="true"
                  :data-testid="`deposit-charge-checkbox-${charge.id}`"
                  @update:model-value="toggleCharge(charge, Boolean($event))"
                />
              </td>
              <td class="px-3 py-2">
                <div class="font-medium text-slate-900">{{ charge.chargeTypeName }}</div>
                <div class="mt-0.5">
                  <StatusPill
                    :status="charge.status ?? 'UNPAID'"
                    :intent="getChargeStatusIntent(charge.status)"
                  />
                </div>
              </td>
              <td class="px-3 py-2 text-slate-600">
                {{ formatBillingMonth(charge.billingMonth) }}
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
                  :data-testid="`deposit-charge-amount-${charge.id}`"
                  @update:model-value="updateChargeAmount(charge, $event)"
                />
                <span v-else class="text-slate-400">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="grid grid-cols-3 gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs">
        <div>
          <span class="block text-slate-500">Available on Receipt</span>
          <span class="mt-0.5 block text-sm font-bold text-slate-900">
            {{ formatIdr(selectedSourceAvailable) }}
          </span>
        </div>
        <div>
          <span class="block text-slate-500">Allocating Now</span>
          <span
            class="mt-0.5 block text-sm font-bold"
            :class="exceedsAvailableDeposit ? 'text-rose-600' : 'text-teal-700'"
            data-testid="deposit-summary-allocating"
          >
            {{ formatIdr(allocatedSum) }}
          </span>
        </div>
        <div>
          <span class="block text-slate-500">Remaining on Receipt</span>
          <span class="mt-0.5 block text-sm font-bold text-slate-700">
            {{ formatIdr(Math.max(0, selectedSourceAvailable - allocatedSum)) }}
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
          label="Apply Deposit"
          :loading="isSubmitting"
          :disabled="
            isSubmitting ||
            checkedItems.length === 0 ||
            exceedsAvailableDeposit ||
            openCharges.length === 0
          "
          data-testid="submit-deposit-btn"
        />
      </div>
    </form>
  </Dialog>
</template>
