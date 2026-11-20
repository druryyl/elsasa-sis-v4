<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import { Loader2, Printer, X } from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { formatBillingMonth, formatIdr, formatPaymentMethod } from '@/shared/utils/formatters';
import type { components } from '@/shared/types/api/schema';

export type PaymentReceiptDetailDto = components['schemas']['PaymentReceiptDetailDto'];

const props = withDefaults(
  defineProps<{
    receipt: PaymentReceiptDetailDto | null;
    remainingOutstandingBalance?: number | null;
    loading?: boolean;
  }>(),
  {
    remainingOutstandingBalance: null,
    loading: false
  }
);

const emit = defineEmits<{
  close: [];
}>();

const allocations = computed(() => props.receipt?.allocations ?? []);

const formattedPaymentDate = computed(() =>
  props.receipt?.paymentDate ? formatDisplayDate(props.receipt.paymentDate) : '-'
);

function handlePrint(): void {
  window.print();
}
</script>

<template>
  <div
    class="payment-receipt-print-overlay fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 p-4 sm:p-6 print:static print:inset-auto print:overflow-visible print:bg-white print:p-0"
    data-testid="payment-receipt-print-view"
  >
    <!-- Non-printable Print Preview Toolbar -->
    <div
      class="no-print mx-auto mb-4 flex max-w-[210mm] items-center justify-between rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-md"
    >
      <div>
        <h2 class="text-sm font-semibold text-slate-900">
          Payment Receipt Print Preview (A4 Portrait)
        </h2>
        <p class="text-xs text-slate-500">
          Review the official tuition payment receipt before printing.
        </p>
      </div>
      <div class="flex items-center gap-2">
        <Button
          type="button"
          label="Print Receipt"
          size="small"
          :disabled="loading || !receipt"
          data-testid="receipt-print-trigger-btn"
          @click="handlePrint"
        >
          <template #icon>
            <Printer class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
        <Button
          type="button"
          label="Close"
          severity="secondary"
          outlined
          size="small"
          data-testid="receipt-print-close-btn"
          @click="emit('close')"
        >
          <template #icon>
            <X class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="loading"
      class="no-print mx-auto flex max-w-[210mm] items-center justify-center rounded-xl bg-white py-20 shadow-lg"
    >
      <Loader2 class="h-6 w-6 animate-spin text-teal-600" />
      <span class="ml-2 text-sm text-slate-600">Loading official payment receipt...</span>
    </div>

    <!-- Printable Receipt Sheet -->
    <article
      v-else-if="receipt"
      class="payment-receipt-print-sheet mx-auto max-w-[210mm] rounded-lg bg-white p-8 text-slate-900 shadow-lg print:max-w-none print:rounded-none print:p-0 print:shadow-none"
    >
      <!-- Official School Header -->
      <header class="border-b-2 border-slate-900 pb-4">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-xs font-bold uppercase tracking-widest text-slate-500">
              Elsasa School Information System
            </p>
            <h1 class="mt-0.5 text-xl font-bold uppercase tracking-wide text-slate-900">
              Official Payment Receipt
            </h1>
            <p class="text-xs text-slate-600">Kuitansi Pembayaran Resmi Siswa</p>
          </div>
          <div class="text-right">
            <p class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
              Receipt Number
            </p>
            <p class="text-lg font-bold tracking-wide text-slate-900" data-testid="print-receipt-number">
              {{ receipt.receiptNumber || '-' }}
            </p>
          </div>
        </div>
      </header>

      <!-- Student & Receipt Metadata -->
      <section class="mt-4 grid grid-cols-2 gap-x-8 gap-y-2 border-b border-slate-300 pb-4 text-xs">
        <div class="space-y-1.5">
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Student Name</span>
            <span class="font-bold text-slate-900">: {{ receipt.studentName || '-' }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">NIS</span>
            <span class="text-slate-900">: {{ receipt.studentNis || '-' }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Study Group</span>
            <span class="font-semibold text-slate-900">: {{ receipt.studyGroupName || '-' }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Academic Year</span>
            <span class="text-slate-900">: {{ receipt.academicYearName || '-' }}</span>
          </div>
        </div>

        <div class="space-y-1.5">
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Payment Date</span>
            <span class="font-semibold text-slate-900">: {{ formattedPaymentDate }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Payment Method</span>
            <span class="text-slate-900">
              : {{ formatPaymentMethod(receipt.paymentMethod) }}
            </span>
          </div>
          <div v-if="receipt.bankReference" class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Bank Reference</span>
            <span class="text-slate-900">: {{ receipt.bankReference }}</span>
          </div>
          <div class="flex">
            <span class="w-32 shrink-0 font-semibold text-slate-600">Received By</span>
            <span class="text-slate-900">: {{ receipt.receivedByName || '-' }}</span>
          </div>
        </div>
      </section>

      <!-- Itemized Settled Charges -->
      <section class="mt-5">
        <h2 class="mb-2 text-xs font-bold uppercase tracking-wider text-slate-800">
          Itemized Settled Charges
        </h2>
        <table class="w-full border-collapse border border-slate-400 text-xs">
          <thead>
            <tr class="bg-slate-100 text-slate-800">
              <th class="border border-slate-400 px-3 py-1.5 text-left font-bold">No</th>
              <th class="border border-slate-400 px-3 py-1.5 text-left font-bold">
                Charge Description
              </th>
              <th class="border border-slate-400 px-3 py-1.5 text-center font-bold">
                Billing Month
              </th>
              <th class="border border-slate-400 px-3 py-1.5 text-center font-bold">
                Allocation Date
              </th>
              <th class="border border-slate-400 px-3 py-1.5 text-right font-bold">
                Amount Settled
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="allocations.length === 0">
              <td
                colspan="5"
                class="border border-slate-400 px-3 py-4 text-center italic text-slate-500"
              >
                No charge allocations yet — full amount held as student advance deposit.
              </td>
            </tr>
            <tr v-for="(alloc, idx) in allocations" :key="alloc.id ?? idx">
              <td class="border border-slate-400 px-3 py-1.5 text-center">{{ idx + 1 }}</td>
              <td class="border border-slate-400 px-3 py-1.5 font-medium text-slate-900">
                {{ alloc.chargeTypeName || '-' }}
              </td>
              <td class="border border-slate-400 px-3 py-1.5 text-center text-slate-700">
                {{ formatBillingMonth(alloc.billingMonth) }}
              </td>
              <td class="border border-slate-400 px-3 py-1.5 text-center text-slate-700">
                {{ alloc.allocationDate ? formatDisplayDate(alloc.allocationDate) : '-' }}
              </td>
              <td class="border border-slate-400 px-3 py-1.5 text-right font-semibold">
                {{ formatIdr(alloc.allocatedAmount) }}
              </td>
            </tr>
          </tbody>
        </table>
      </section>

      <!-- Totals & Balance Summary -->
      <section class="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div class="rounded border border-slate-300 bg-slate-50/50 p-3 text-xs">
          <p class="font-bold uppercase tracking-wider text-slate-700">Cashier Remarks</p>
          <p class="mt-1 text-slate-700">
            {{ receipt.notes || 'Thank you for your payment.' }}
          </p>
        </div>

        <div class="space-y-1.5 rounded border border-slate-400 p-3 text-xs">
          <div class="flex items-center justify-between">
            <span class="font-medium text-slate-600">Total Amount Received</span>
            <span class="text-sm font-bold text-slate-900">
              {{ formatIdr(receipt.totalAmount) }}
            </span>
          </div>
          <div class="flex items-center justify-between">
            <span class="font-medium text-slate-600">Allocated to Charges</span>
            <span class="font-semibold text-emerald-700">
              {{ formatIdr(receipt.allocatedAmount) }}
            </span>
          </div>
          <div class="flex items-center justify-between">
            <span class="font-medium text-slate-600">Advance Deposit Held</span>
            <span class="font-semibold text-amber-700">
              {{ formatIdr(receipt.unallocatedAmount) }}
            </span>
          </div>
          <div
            v-if="remainingOutstandingBalance !== null"
            class="flex items-center justify-between border-t border-slate-300 pt-1.5"
          >
            <span class="font-semibold text-slate-700">Remaining Student Balance</span>
            <span class="font-bold text-slate-900">
              {{ formatIdr(remainingOutstandingBalance) }}
            </span>
          </div>
        </div>
      </section>

      <!-- Signature Blocks -->
      <footer class="mt-8 break-inside-avoid">
        <div class="grid grid-cols-2 gap-12 text-center text-xs">
          <div>
            <p class="font-semibold text-slate-700">Payer / Parent / Guardian</p>
            <div class="mt-14 border-t border-slate-500 pt-1 text-slate-600">
              ( ........................................ )
            </div>
          </div>
          <div>
            <p class="font-semibold text-slate-700">Cashier / Finance Officer</p>
            <div class="mt-14 border-t border-slate-500 pt-1 font-semibold text-slate-900">
              {{ receipt.receivedByName || '........................................' }}
            </div>
          </div>
        </div>
      </footer>
    </article>
  </div>
</template>

<style>
@media print {
  @page {
    size: A4 portrait;
    margin: 12mm;
  }

  body {
    background: #ffffff !important;
  }

  /* Hide app shell navigation, top header, breadcrumbs, and preview toolbar when printing */
  #app > div > header,
  #app > div > div > aside,
  nav[aria-label='Breadcrumb'],
  [data-testid='historical-period-banner'],
  .no-print {
    display: none !important;
  }

  main {
    padding: 0 !important;
    margin: 0 !important;
  }

  .payment-receipt-print-overlay {
    position: static !important;
    background: transparent !important;
    padding: 0 !important;
    overflow: visible !important;
  }

  .payment-receipt-print-sheet {
    box-shadow: none !important;
    padding: 0 !important;
    max-width: 100% !important;
  }
}
</style>
