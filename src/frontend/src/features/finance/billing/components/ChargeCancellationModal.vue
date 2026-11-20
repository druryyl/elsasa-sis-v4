<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { AlertTriangle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import type { StudentChargeDto } from '../api/billing.api';
import { useCancelChargeMutation } from '../api/useBillingMutations';
import { cancelChargeSchema, extractSubmissionError } from '../schemas/billing.schema';
import { formatBillingMonth, formatIdr } from '../utils/formatters';

const props = defineProps<{
  visible: boolean;
  charge: StudentChargeDto | null;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  cancelled: [];
}>();

const serverError = ref<string | null>(null);
const cancelMutation = useCancelChargeMutation();

const { handleSubmit, errors, resetForm, defineField, setErrors } = useForm({
  validationSchema: toTypedSchema(cancelChargeSchema),
  initialValues: {
    cancellationReason: ''
  }
});

const [cancellationReason, cancellationReasonAttrs] = defineField('cancellationReason');

const hasRecordedPayments = computed(() => (props.charge?.paidAmount ?? 0) > 0);
const isAlreadyCancelled = computed(
  () => props.charge?.status?.trim().toUpperCase() === 'CANCELLED'
);
const isActionBlocked = computed(() => hasRecordedPayments.value || isAlreadyCancelled.value);
const reasonLength = computed(() => (cancellationReason.value ?? '').trim().length);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      serverError.value = null;
      resetForm({
        values: {
          cancellationReason: ''
        }
      });
    }
  }
);

const onSubmit = handleSubmit(async (values) => {
  if (!props.charge?.id || isActionBlocked.value) return;
  serverError.value = null;
  try {
    await cancelMutation.mutateAsync({
      id: props.charge.id,
      payload: {
        cancellationReason: values.cancellationReason.trim()
      }
    });
    emit('update:visible', false);
    emit('cancelled');
  } catch (error) {
    const parsed = extractSubmissionError(error, 'Failed to cancel student charge.');
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
    :closable="!cancelMutation.isPending.value"
    :draggable="false"
    class="w-full max-w-md"
    data-testid="charge-cancellation-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-rose-100 text-rose-600"
        >
          <AlertTriangle class="h-4 w-4" />
        </div>
        <div>
          <span class="text-[11px] font-semibold tracking-wide text-rose-700 uppercase">
            Tier 2 Audit Confirmation
          </span>
          <h3 class="text-base font-semibold text-slate-900">Cancel Student Charge</h3>
        </div>
      </div>
    </template>

    <form class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="serverError"
        role="alert"
        class="rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-2.5 text-sm text-rose-700"
      >
        {{ serverError }}
      </div>

      <div
        v-if="charge"
        class="space-y-1.5 rounded-xl border border-slate-200 bg-slate-50 p-3.5 text-xs"
      >
        <div class="flex items-center justify-between gap-2">
          <span class="text-slate-500">Student</span>
          <span class="font-semibold text-slate-900">
            {{ charge.studentName || '—' }} ({{ charge.studentNis || '—' }})
          </span>
        </div>
        <div class="flex items-center justify-between gap-2">
          <span class="text-slate-500">Fee Item</span>
          <span class="font-medium text-slate-800">
            {{ charge.chargeTypeName || '—' }}
            <span v-if="charge.billingMonth">({{ formatBillingMonth(charge.billingMonth) }})</span>
          </span>
        </div>
        <div class="flex items-center justify-between gap-2">
          <span class="text-slate-500">Net Amount</span>
          <span class="font-semibold text-slate-900">{{ formatIdr(charge.netAmount) }}</span>
        </div>
        <div class="flex items-center justify-between gap-2">
          <span class="text-slate-500">Paid Amount</span>
          <span
            class="font-semibold"
            :class="hasRecordedPayments ? 'text-rose-700' : 'text-slate-700'"
          >
            {{ formatIdr(charge.paidAmount) }}
          </span>
        </div>
      </div>

      <div
        v-if="hasRecordedPayments"
        role="alert"
        class="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-xs leading-relaxed text-amber-800"
      >
        This charge already has recorded payments ({{ formatIdr(charge?.paidAmount) }}) and cannot
        be cancelled.
      </div>
      <div
        v-else-if="isAlreadyCancelled"
        role="alert"
        class="rounded-lg border border-slate-200 bg-slate-100 px-3.5 py-2.5 text-xs text-slate-700"
      >
        This charge is already in <strong>CANCELLED</strong> status.
      </div>
      <p v-else class="text-xs leading-relaxed text-slate-600">
        Cancelling this charge transitions it to a terminal <strong>CANCELLED</strong> state and
        removes it from the student&apos;s outstanding receivable balance. An audit reason is
        required.
      </p>

      <FormField
        label="Cancellation Audit Reason"
        required
        field-id="cancellationReason"
        :error="errors.cancellationReason"
        :hint="`${reasonLength} / 500 characters`"
      >
        <Textarea
          id="cancellationReason"
          v-model="cancellationReason"
          v-bind="cancellationReasonAttrs"
          rows="3"
          placeholder="e.g., Duplicate billing entry or student transferred prior to billing month"
          class="w-full"
          :disabled="cancelMutation.isPending.value || isActionBlocked"
        />
      </FormField>

      <div class="flex justify-end gap-2 pt-1">
        <Button
          type="button"
          label="Keep Charge"
          severity="secondary"
          text
          :disabled="cancelMutation.isPending.value"
          @click="emit('update:visible', false)"
        />
        <Button
          type="submit"
          label="Confirm Cancellation"
          severity="danger"
          :loading="cancelMutation.isPending.value"
          :disabled="cancelMutation.isPending.value || isActionBlocked"
          data-testid="confirm-cancel-charge-button"
        />
      </div>
    </form>
  </Dialog>
</template>
