<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import {
  CreditCard,
  DollarSign,
  FileCheck2,
  FileSpreadsheet,
  PiggyBank,
  Printer,
  Receipt,
  TrendingDown
} from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import PaymentReceiptPrintView from '@/shared/components/print/PaymentReceiptPrintView.vue';
import { formatIdr, formatPaymentMethod } from '@/shared/utils/formatters';
import { useChildStatementQuery } from '../api/useParentPortalQueries';
import { parentPortalApi } from '../api/parentPortal.api';
import type {
  PaymentReceiptDetailDto,
  StudentStatementChargeDto,
  StudentStatementReceiptDto
} from '../api/parentPortal.api';

const props = defineProps<{
  studentId: string;
}>();

const {
  data: statement,
  isLoading,
  isError,
  refetch
} = useChildStatementQuery(() => props.studentId);

const charges = computed<StudentStatementChargeDto[]>(() => statement.value?.charges ?? []);
const receipts = computed<StudentStatementReceiptDto[]>(() => statement.value?.receipts ?? []);

const totalDepositBalance = computed(() => statement.value?.totalDepositBalance ?? 0);
const totalOutstanding = computed(() => statement.value?.totalOutstanding ?? 0);
const totalPaid = computed(() => statement.value?.totalPaid ?? 0);
const totalBilled = computed(() => statement.value?.totalBilled ?? 0);

// Printable receipt modal state
const showReceiptModal = ref(false);
const activeReceipt = ref<PaymentReceiptDetailDto | null>(null);
const isLoadingReceiptDetail = ref(false);

async function handlePrintReceipt(receipt: StudentStatementReceiptDto): Promise<void> {
  if (!receipt.id) return;
  isLoadingReceiptDetail.value = true;
  showReceiptModal.value = true;

  try {
    const detail = await parentPortalApi.getPaymentReceiptDetail(receipt.id);
    activeReceipt.value = detail;
  } catch {
    // Fallback to minimal receipt detail from statement projection
    activeReceipt.value = {
      id: receipt.id,
      receiptNumber: receipt.receiptNumber ?? 'RCP-UNASSIGNED',
      studentId: statement.value?.studentId,
      studentName: statement.value?.studentName,
      studentNis: statement.value?.studentNis,
      studyGroupName: statement.value?.studyGroupName,
      paymentDate: receipt.paymentDate,
      paymentMethod: receipt.paymentMethod,
      totalAmount: receipt.totalAmount,
      allocatedAmount: receipt.allocatedAmount,
      unallocatedAmount: receipt.unallocatedAmount,
      notes: receipt.notes,
      allocations: receipt.allocations?.map((a) => ({
        id: a.id,
        studentChargeId: a.studentChargeId,
        chargeTypeName: a.chargeTypeName,
        billingMonth: a.billingMonth,
        allocatedAmount: a.allocatedAmount
      }))
    };
  } finally {
    isLoadingReceiptDetail.value = false;
    window.print();
  }
}

function getStatusBadge(status?: string | null): { label: string; class: string } {
  switch (status?.toUpperCase()) {
    case 'PAID':
      return {
        label: 'LUNAS',
        class: 'bg-emerald-100 text-emerald-800 border-emerald-200'
      };
    case 'PARTIALLY_PAID':
      return {
        label: 'SEBAGIAN',
        class: 'bg-amber-100 text-amber-800 border-amber-200'
      };
    case 'UNPAID':
    default:
      return {
        label: 'BELUM LUNAS',
        class: 'bg-rose-100 text-rose-800 border-rose-200'
      };
  }
}

defineExpose({
  handlePrintReceipt,
  refetch
});
</script>

<template>
  <div
    class="space-y-6"
    data-testid="billing-and-receipts-tab"
  >
    <!-- Loading skeleton -->
    <div
      v-if="isLoading"
      class="space-y-4"
      data-testid="billing-skeleton"
    >
      <div class="h-20 w-full animate-pulse rounded-xl bg-slate-200" />
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div v-for="i in 3" :key="i" class="h-20 animate-pulse rounded-xl bg-slate-200" />
      </div>
      <div class="h-64 w-full animate-pulse rounded-xl bg-slate-200" />
    </div>

    <!-- Error state -->
    <div
      v-else-if="isError"
      class="rounded-xl border border-red-200 bg-red-50 p-6 text-center text-red-700"
      data-testid="billing-error"
    >
      <p class="font-medium">Gagal memuat data tagihan dan kuitansi.</p>
      <Button
        label="Coba Lagi"
        severity="danger"
        text
        size="small"
        class="mt-2"
        @click="() => refetch()"
      />
    </div>

    <!-- Main Content -->
    <div
      v-else
      class="space-y-6"
    >
      <!-- Advance Deposit Banner -->
      <div
        class="relative overflow-hidden rounded-2xl border border-brand-primary-200 bg-gradient-to-r from-brand-primary-900 to-brand-primary-800 p-5 text-white shadow-sm sm:p-6"
        data-testid="advance-deposit-banner"
      >
        <div class="relative z-10 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div class="flex items-center gap-3.5">
            <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-white/10 text-brand-accent-300 backdrop-blur-xs">
              <PiggyBank class="h-6 w-6" />
            </div>
            <div>
              <p class="text-xs font-semibold uppercase tracking-wider text-brand-primary-200">
                Saldo Deposit Uang Muka (Advance Deposit)
              </p>
              <p class="text-2xl font-bold tracking-tight text-white sm:text-3xl" data-testid="deposit-balance-value">
                {{ formatIdr(totalDepositBalance) }}
              </p>
            </div>
          </div>
          <div class="rounded-xl bg-white/10 px-3.5 py-2 text-xs text-brand-primary-100 backdrop-blur-xs">
            Saldo deposit dapat dialokasikan langsung untuk tagihan SPP berikutnya di loket kasir sekolah.
          </div>
        </div>
      </div>

      <!-- Financial Metric Cards -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <!-- Total Outstanding -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-rose-50 text-rose-700">
              <DollarSign class="h-5 w-5" />
            </div>
            <div>
              <p class="text-xs font-medium text-slate-500">Sisa Tunggakan</p>
              <p class="text-xl font-bold text-rose-700" data-testid="total-outstanding-value">
                {{ formatIdr(totalOutstanding) }}
              </p>
            </div>
          </div>
        </div>

        <!-- Total Paid -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
              <FileCheck2 class="h-5 w-5" />
            </div>
            <div>
              <p class="text-xs font-medium text-slate-500">Total Telah Dibayar</p>
              <p class="text-xl font-bold text-emerald-700" data-testid="total-paid-value">
                {{ formatIdr(totalPaid) }}
              </p>
            </div>
          </div>
        </div>

        <!-- Total Billed -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 text-blue-700">
              <CreditCard class="h-5 w-5" />
            </div>
            <div>
              <p class="text-xs font-medium text-slate-500">Total Seluruh Tagihan</p>
              <p class="text-xl font-bold text-slate-900" data-testid="total-billed-value">
                {{ formatIdr(totalBilled) }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- Section 1: Monthly Tuition Billing Ledger -->
      <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
        <div class="border-b border-slate-200 bg-slate-50/75 px-4 py-3 sm:px-6">
          <div class="flex items-center gap-2">
            <FileSpreadsheet class="h-4 w-4 text-brand-primary-600" />
            <h4 class="text-sm font-semibold text-slate-900">
              Buku Besar Tagihan SPP & Biaya Sekolah
            </h4>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm" data-testid="charges-table">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase text-slate-500">
              <tr>
                <th scope="col" class="px-4 py-3 sm:px-6">Tagihan / Bulan</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Jumlah Bruto</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Potongan</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Dibayar</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Sisa Tagihan</th>
                <th scope="col" class="px-4 py-3 text-center sm:px-6">Status</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr
                v-for="charge in charges"
                :key="charge.id"
                class="hover:bg-slate-50/50"
                :data-testid="`charge-row-${charge.id}`"
              >
                <td class="px-4 py-3.5 font-medium text-slate-900 sm:px-6">
                  <div>
                    <span class="block text-slate-900">{{ charge.chargeTypeName || 'SPP Bulanan' }}</span>
                    <span v-if="charge.billingMonth" class="text-xs text-slate-500">
                      Bulan: {{ charge.billingMonth }}
                    </span>
                    <span v-if="charge.dueDate" class="block text-[11px] text-slate-400">
                      Jatuh tempo: {{ formatDisplayDate(charge.dueDate) }}
                    </span>
                  </div>
                </td>
                <td class="px-4 py-3.5 text-slate-600 sm:px-6">
                  {{ formatIdr(charge.grossAmount) }}
                </td>
                <td class="px-4 py-3.5 text-emerald-700 sm:px-6">
                  <span v-if="charge.discountAmount && charge.discountAmount > 0" class="inline-flex items-center gap-0.5">
                    <TrendingDown class="h-3.5 w-3.5" />
                    {{ formatIdr(charge.discountAmount) }}
                  </span>
                  <span v-else class="text-slate-400">—</span>
                </td>
                <td class="px-4 py-3.5 text-slate-700 sm:px-6">
                  {{ formatIdr(charge.paidAmount) }}
                </td>
                <td class="px-4 py-3.5 font-semibold text-slate-900 sm:px-6">
                  {{ formatIdr(charge.outstandingAmount) }}
                </td>
                <td class="px-4 py-3.5 text-center sm:px-6">
                  <span
                    class="inline-block rounded-md border px-2 py-0.5 text-xs font-semibold"
                    :class="getStatusBadge(charge.status).class"
                  >
                    {{ getStatusBadge(charge.status).label }}
                  </span>
                </td>
              </tr>

              <tr v-if="charges.length === 0">
                <td colspan="6" class="py-8 text-center text-xs text-slate-400" data-testid="empty-charges-row">
                  Tidak ada data tagihan sekolah untuk siswa ini.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Section 2: Historical Payment Receipts -->
      <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xs">
        <div class="border-b border-slate-200 bg-slate-50/75 px-4 py-3 sm:px-6">
          <div class="flex items-center gap-2">
            <Receipt class="h-4 w-4 text-brand-primary-600" />
            <h4 class="text-sm font-semibold text-slate-900">
              Riwayat Pembayaran & Kuitansi Resmi
            </h4>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm" data-testid="receipts-table">
            <thead class="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase text-slate-500">
              <tr>
                <th scope="col" class="px-4 py-3 sm:px-6">Nomor Kuitansi</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Tanggal Bayar</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Metode</th>
                <th scope="col" class="px-4 py-3 sm:px-6">Jumlah Bayar</th>
                <th scope="col" class="px-4 py-3 text-right sm:px-6">Aksi</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr
                v-for="receipt in receipts"
                :key="receipt.id"
                class="hover:bg-slate-50/50"
                :data-testid="`receipt-row-${receipt.id}`"
              >
                <td class="px-4 py-3.5 font-semibold text-brand-primary-950 sm:px-6">
                  {{ receipt.receiptNumber }}
                </td>
                <td class="px-4 py-3.5 text-slate-600 sm:px-6">
                  {{ formatDisplayDate(receipt.paymentDate) }}
                </td>
                <td class="px-4 py-3.5 text-slate-600 sm:px-6">
                  <span class="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                    {{ formatPaymentMethod(receipt.paymentMethod) }}
                  </span>
                </td>
                <td class="px-4 py-3.5 font-bold text-slate-900 sm:px-6">
                  {{ formatIdr(receipt.totalAmount) }}
                </td>
                <td class="px-4 py-3.5 text-right sm:px-6">
                  <Button
                    type="button"
                    label="Cetak Kuitansi"
                    size="small"
                    outlined
                    :data-testid="`print-receipt-btn-${receipt.id}`"
                    @click="handlePrintReceipt(receipt)"
                  >
                    <template #icon>
                      <Printer class="mr-1.5 h-3.5 w-3.5" />
                    </template>
                  </Button>
                </td>
              </tr>

              <tr v-if="receipts.length === 0">
                <td colspan="5" class="py-8 text-center text-xs text-slate-400" data-testid="empty-receipts-row">
                  Belum ada riwayat transaksi pembayaran.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Printable Receipt Modal / Overlay -->
    <PaymentReceiptPrintView
      v-if="showReceiptModal && activeReceipt"
      :receipt="activeReceipt"
      :remaining-outstanding-balance="totalOutstanding"
      :loading="isLoadingReceiptDetail"
      @close="showReceiptModal = false"
    />
  </div>
</template>
