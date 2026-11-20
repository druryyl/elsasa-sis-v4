<script setup lang="ts">
import { computed } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import { Loader2, MessageCircle, Printer, Receipt } from 'lucide-vue-next';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { StudentBillingContactDto, StudentStatementDto } from '../api/payments.api';
import {
  buildWhatsAppHref,
  formatBillingMonth,
  formatIdr,
  formatPaymentMethod,
  getChargeStatusIntent
} from '../utils/formatters';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    statement: StudentStatementDto | null;
    student?: StudentBillingContactDto | null;
    loading?: boolean;
  }>(),
  {
    student: null,
    loading: false
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  'print-receipt': [receiptId: string];
}>();

const charges = computed(() => props.statement?.charges ?? []);
const receipts = computed(() => props.statement?.receipts ?? []);

const whatsAppHref = computed(() =>
  buildWhatsAppHref(props.student?.whatsAppUrl, props.student?.phoneNumber)
);
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="w-full max-w-4xl"
    data-testid="student-statement-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div>
        <h2 class="text-lg font-semibold text-slate-900">Student Financial Ledger Statement</h2>
        <p class="text-xs text-slate-500">
          Complete billing charges, discounts, payments, and advance deposit history
        </p>
      </div>
    </template>

    <div v-if="loading" class="flex items-center justify-center py-16">
      <Loader2 class="h-6 w-6 animate-spin text-teal-600" />
      <span class="ml-2 text-sm text-slate-600">Loading student ledger statement...</span>
    </div>

    <div v-else-if="statement" class="space-y-6 pb-6">
      <!-- Student & Guardian Context -->
      <section
        class="flex flex-wrap items-start justify-between gap-4 rounded-xl border border-slate-200 bg-slate-50 p-4"
      >
        <div>
          <h3 class="text-base font-bold text-slate-900">
            {{ statement.studentName || student?.studentName || '-' }}
          </h3>
          <p class="mt-0.5 text-xs text-slate-600">
            NIS: <span class="font-medium">{{ statement.studentNis || student?.studentNis || '-' }}</span>
            • Class:
            <span class="font-medium">{{
              statement.studyGroupName || student?.studyGroupName || 'Unplaced'
            }}</span>
          </p>
        </div>

        <div v-if="student?.primaryGuardianName || student?.phoneNumber" class="text-right text-xs">
          <div class="font-medium text-slate-800">
            Guardian: {{ student?.primaryGuardianName || '-' }}
            <span v-if="student?.relationship" class="text-slate-500">
              ({{ student.relationship }})
            </span>
          </div>
          <div class="mt-1 flex items-center justify-end gap-2">
            <span class="text-slate-600">{{ student?.phoneNumber || 'No phone' }}</span>
            <a
              v-if="whatsAppHref"
              :href="whatsAppHref"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1 rounded-md border border-emerald-200 bg-emerald-50 px-2 py-0.5 font-medium text-emerald-700 hover:bg-emerald-100"
            >
              <MessageCircle class="h-3.5 w-3.5" />
              <span>WhatsApp</span>
            </a>
          </div>
        </div>
      </section>

      <!-- Financial KPI Summary -->
      <section class="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <p class="text-xs font-medium text-slate-500">Total Billed</p>
          <p class="mt-1 text-base font-bold text-slate-900" data-testid="drawer-total-billed">
            {{ formatIdr(statement.totalBilled) }}
          </p>
        </div>
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <p class="text-xs font-medium text-slate-500">Total Paid</p>
          <p class="mt-1 text-base font-bold text-emerald-700" data-testid="drawer-total-paid">
            {{ formatIdr(statement.totalPaid) }}
          </p>
        </div>
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <p class="text-xs font-medium text-slate-500">Outstanding</p>
          <p class="mt-1 text-base font-bold text-rose-700" data-testid="drawer-total-outstanding">
            {{ formatIdr(statement.totalOutstanding) }}
          </p>
        </div>
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <p class="text-xs font-medium text-slate-500">Deposit Held</p>
          <p class="mt-1 text-base font-bold text-amber-700" data-testid="drawer-total-deposit">
            {{ formatIdr(statement.totalDepositBalance) }}
          </p>
        </div>
      </section>

      <!-- Charges Breakdown -->
      <section class="space-y-2">
        <h4 class="text-sm font-semibold text-slate-900">
          All Charges ({{ charges.length }})
        </h4>
        <div
          v-if="charges.length === 0"
          class="rounded-lg border border-dashed border-slate-200 py-6 text-center text-xs text-slate-500"
        >
          No charges recorded for this student.
        </div>
        <div v-else class="overflow-x-auto rounded-lg border border-slate-200">
          <table class="min-w-full divide-y divide-slate-200 text-xs">
            <thead class="bg-slate-50 text-left font-semibold text-slate-600">
              <tr>
                <th class="px-3 py-2">Charge</th>
                <th class="px-3 py-2">Month</th>
                <th class="px-3 py-2 text-right">Net</th>
                <th class="px-3 py-2 text-right">Paid</th>
                <th class="px-3 py-2 text-right">Outstanding</th>
                <th class="px-3 py-2">Status</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 bg-white">
              <tr v-for="charge in charges" :key="charge.id">
                <td class="px-3 py-2">
                  <div class="font-medium text-slate-900">{{ charge.chargeTypeName }}</div>
                  <div v-if="charge.description" class="text-[11px] text-slate-500">
                    {{ charge.description }}
                  </div>
                  <div v-if="(charge.discountAmount ?? 0) > 0" class="text-[11px] text-emerald-700">
                    Discount: -{{ formatIdr(charge.discountAmount) }}
                    <span v-if="charge.discountReason">({{ charge.discountReason }})</span>
                  </div>
                </td>
                <td class="px-3 py-2 text-slate-600">
                  <div>{{ formatBillingMonth(charge.billingMonth) }}</div>
                  <div v-if="charge.dueDate" class="text-[11px] text-slate-400">
                    Due {{ formatDisplayDate(charge.dueDate) }}
                  </div>
                </td>
                <td class="px-3 py-2 text-right text-slate-700">
                  {{ formatIdr(charge.netAmount) }}
                </td>
                <td class="px-3 py-2 text-right text-emerald-700">
                  {{ formatIdr(charge.paidAmount) }}
                </td>
                <td class="px-3 py-2 text-right font-semibold text-slate-900">
                  {{ formatIdr(charge.outstandingAmount) }}
                </td>
                <td class="px-3 py-2">
                  <StatusPill
                    :status="charge.status ?? 'UNPAID'"
                    :intent="getChargeStatusIntent(charge.status)"
                  />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- Payment Receipts Breakdown -->
      <section class="space-y-2">
        <h4 class="text-sm font-semibold text-slate-900">
          Payment Receipts ({{ receipts.length }})
        </h4>
        <div
          v-if="receipts.length === 0"
          class="rounded-lg border border-dashed border-slate-200 py-6 text-center text-xs text-slate-500"
        >
          No payment receipts recorded for this student.
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="receipt in receipts"
            :key="receipt.id"
            class="rounded-lg border border-slate-200 bg-white p-3.5 text-xs"
          >
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <Receipt class="h-4 w-4 text-teal-600" />
                <span class="font-bold text-slate-900">{{ receipt.receiptNumber }}</span>
                <span class="text-slate-500">• {{ formatDisplayDate(receipt.paymentDate) }}</span>
                <span
                  class="rounded bg-slate-100 px-1.5 py-0.5 font-medium text-slate-700"
                >
                  {{ formatPaymentMethod(receipt.paymentMethod) }}
                </span>
                <span v-if="receipt.bankReference" class="text-slate-500">
                  Ref: {{ receipt.bankReference }}
                </span>
              </div>

              <div class="flex items-center gap-3">
                <span class="font-bold text-slate-900">{{ formatIdr(receipt.totalAmount) }}</span>
                <Button
                  v-if="receipt.id"
                  type="button"
                  label="Print"
                  size="small"
                  severity="secondary"
                  outlined
                  @click="emit('print-receipt', receipt.id!)"
                >
                  <template #icon>
                    <Printer class="mr-1 h-3.5 w-3.5" />
                  </template>
                </Button>
              </div>
            </div>

            <div class="mt-2 flex flex-wrap gap-4 text-[11px] text-slate-600">
              <span>Allocated: {{ formatIdr(receipt.allocatedAmount) }}</span>
              <span
                :class="
                  (receipt.unallocatedAmount ?? 0) > 0
                    ? 'font-semibold text-amber-700'
                    : 'text-slate-500'
                "
              >
                Deposit Held: {{ formatIdr(receipt.unallocatedAmount) }}
              </span>
              <span>Cashier: {{ receipt.receivedByName || '-' }}</span>
            </div>

            <div
              v-if="receipt.allocations && receipt.allocations.length > 0"
              class="mt-2.5 border-t border-slate-100 pt-2"
            >
              <p class="mb-1 text-[11px] font-semibold uppercase tracking-wide text-slate-500">
                Allocations
              </p>
              <ul class="space-y-1">
                <li
                  v-for="alloc in receipt.allocations"
                  :key="alloc.id"
                  class="flex items-center justify-between text-[11px] text-slate-700"
                >
                  <span>
                    {{ alloc.chargeTypeName }}
                    <span v-if="alloc.billingMonth" class="text-slate-500">
                      ({{ formatBillingMonth(alloc.billingMonth) }})
                    </span>
                  </span>
                  <span class="font-medium">{{ formatIdr(alloc.allocatedAmount) }}</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>
    </div>
  </Drawer>
</template>
