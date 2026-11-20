<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Skeleton from 'primevue/skeleton';
import {
  AlertCircle,
  Banknote,
  Building2,
  Calendar,
  PiggyBank,
  Receipt,
  RefreshCw,
  Search
} from 'lucide-vue-next';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import { formatBackendDateOnly, formatDisplayDate, formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { ApiError } from '@/shared/types/common';
import type { DailyCashierReceiptItemDto } from '../api/receivables.api';
import { useDailyCashierSummaryQuery } from '../api/useReceivableQueries';
import { formatIdr, formatPaymentMethod } from '../utils/formatters';

const todayDate = () => formatBackendDateOnly(new Date());

const selectedDate = ref<string>(todayDate());
const searchText = ref<string>('');

const queryParams = computed(() => ({
  date: selectedDate.value || todayDate()
}));

const dailyCashierQuery = useDailyCashierSummaryQuery(queryParams);

const summary = computed(() => dailyCashierQuery.data.value);

const cashSummary = computed(() =>
  (summary.value?.paymentMethodSummaries ?? []).find(
    (m) => m.paymentMethod?.trim().toUpperCase() === 'CASH'
  )
);

const bankTransferSummary = computed(() =>
  (summary.value?.paymentMethodSummaries ?? []).find(
    (m) => m.paymentMethod?.trim().toUpperCase() === 'BANK_TRANSFER'
  )
);

const filteredReceipts = computed<DailyCashierReceiptItemDto[]>(() => {
  const rows = summary.value?.receipts ?? [];
  const term = searchText.value.trim().toLowerCase();
  if (!term) return rows;

  return rows.filter((row) => {
    const haystack = [
      row.receiptNumber,
      row.studentName,
      row.studentNis,
      row.studyGroupName,
      row.receivedByName,
      row.bankReference,
      row.paymentMethod
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();
    return haystack.includes(term);
  });
});

const errorMessage = computed<string | null>(() => {
  const err = dailyCashierQuery.error.value;
  if (!err) return null;
  if (err instanceof ApiError) {
    return err.detail || err.title || 'Failed to load daily cashier summary.';
  }
  return err instanceof Error ? err.message : 'Failed to load daily cashier summary.';
});

function selectToday(): void {
  selectedDate.value = todayDate();
}
</script>

<template>
  <div class="space-y-5" data-testid="daily-cashier-tab">
    <!-- Filter Bar -->
    <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div class="flex flex-wrap items-end gap-3">
          <div class="space-y-1">
            <label
              for="cashier-summary-date"
              class="flex items-center gap-1.5 text-xs font-medium text-slate-600"
            >
              <Calendar class="h-3.5 w-3.5 text-slate-400" />
              <span>Collection Date</span>
            </label>
            <input
              id="cashier-summary-date"
              v-model="selectedDate"
              type="date"
              class="h-[38px] rounded-md border border-slate-300 bg-white px-3 text-sm text-slate-800 shadow-xs focus:border-teal-600 focus:outline-hidden focus:ring-1 focus:ring-teal-600"
              data-testid="cashier-date-picker"
            />
          </div>

          <Button
            type="button"
            label="Today"
            severity="secondary"
            outlined
            size="small"
            data-testid="cashier-today-btn"
            @click="selectToday"
          />

          <Button
            type="button"
            label="Refresh"
            severity="secondary"
            outlined
            size="small"
            :loading="dailyCashierQuery.isFetching.value"
            data-testid="cashier-refresh-btn"
            @click="dailyCashierQuery.refetch()"
          >
            <template #icon>
              <RefreshCw class="mr-1.5 h-3.5 w-3.5" />
            </template>
          </Button>
        </div>

        <div class="relative w-full sm:w-72">
          <Search
            class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
          />
          <InputText
            v-model="searchText"
            placeholder="Search receipt, student, cashier..."
            class="w-full pl-9"
            data-testid="cashier-search-input"
          />
        </div>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="errorMessage"
      class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800"
      data-testid="cashier-error-banner"
    >
      <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
      <div>
        <p class="font-semibold">Unable to load daily cashier report</p>
        <p class="mt-0.5 text-xs text-rose-700">{{ errorMessage }}</p>
      </div>
    </div>

    <!-- Summary KPI Cards -->
    <div v-if="dailyCashierQuery.isLoading.value" class="grid grid-cols-1 gap-4 md:grid-cols-3">
      <Skeleton v-for="i in 3" :key="i" height="6.5rem" border-radius="0.75rem" />
    </div>

    <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-3">
      <div
        class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
        data-testid="kpi-cash-collections"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total Cash Collections
          </span>
          <Banknote class="h-4 w-4 text-emerald-600" />
        </div>
        <p class="mt-2 text-xl font-bold text-emerald-700">
          {{ formatIdr(summary?.totalCashCollections) }}
        </p>
        <p class="mt-1 text-xs text-slate-500">
          {{ cashSummary?.receiptCount ?? 0 }} cash receipt(s)
        </p>
      </div>

      <div
        class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
        data-testid="kpi-bank-collections"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total Bank Transfer Collections
          </span>
          <Building2 class="h-4 w-4 text-sky-600" />
        </div>
        <p class="mt-2 text-xl font-bold text-sky-700">
          {{ formatIdr(summary?.totalBankTransferCollections) }}
        </p>
        <p class="mt-1 text-xs text-slate-500">
          {{ bankTransferSummary?.receiptCount ?? 0 }} bank transfer receipt(s)
        </p>
      </div>

      <div
        class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
        data-testid="kpi-deposits-held"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-slate-500">
            Total Advance Deposits Held
          </span>
          <PiggyBank class="h-4 w-4 text-amber-600" />
        </div>
        <p class="mt-2 text-xl font-bold text-amber-700">
          {{ formatIdr(summary?.totalDepositsHeld) }}
        </p>
        <p class="mt-1 text-xs text-slate-500">
          Allocated to charges:
          <span class="font-semibold text-slate-700">{{ formatIdr(summary?.totalAllocated) }}</span>
        </p>
      </div>
    </div>

    <!-- Reconciliation Banner -->
    <div
      v-if="!dailyCashierQuery.isLoading.value && summary"
      class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-teal-200 bg-teal-50/50 px-4 py-3 text-xs text-slate-700"
      data-testid="cashier-reconciliation-bar"
    >
      <div class="flex items-center gap-2">
        <Receipt class="h-4 w-4 text-teal-700" />
        <span>
          Collection Register for
          <strong class="text-slate-900">{{ formatDisplayDate(summary.date ?? selectedDate) }}</strong>
        </span>
      </div>
      <div class="flex flex-wrap items-center gap-4">
        <span>
          Total Receipts: <strong class="text-slate-900">{{ summary.receiptCount ?? 0 }}</strong>
        </span>
        <span>
          Gross Collections:
          <strong class="text-teal-800">{{ formatIdr(summary.totalCollections) }}</strong>
        </span>
      </div>
    </div>

    <!-- Itemized Daily Receipt Register Table -->
    <div class="rounded-xl border border-slate-200 bg-white shadow-xs">
      <div class="border-b border-slate-200 px-4 py-3">
        <h2 class="text-sm font-semibold text-slate-900">Daily Receipt Register</h2>
        <p class="text-xs text-slate-500">
          Showing {{ filteredReceipts.length }} receipt(s) recorded on
          {{ formatDisplayDate(summary?.date ?? selectedDate) }}
        </p>
      </div>

      <AppDataTable
        :value="filteredReceipts"
        :loading="dailyCashierQuery.isLoading.value"
        :paginator="filteredReceipts.length > 10"
        :rows="10"
        data-key="id"
        striped-rows
        empty-title="No receipts collected"
        empty-description="No payment receipts were recorded on the selected date."
        data-testid="daily-cashier-table"
      >
        <Column field="receiptNumber" header="Receipt Number" sortable style="min-width: 10rem">
          <template #body="{ data }">
            <div>
              <div class="font-mono text-xs font-bold text-slate-900">
                {{ data.receiptNumber || '—' }}
              </div>
              <div class="text-[11px] text-slate-400">
                {{ formatDisplayDateTime(data.createdAt) }}
              </div>
            </div>
          </template>
        </Column>

        <Column field="studentName" header="Student" sortable style="min-width: 12rem">
          <template #body="{ data }">
            <div>
              <div class="font-semibold text-slate-900">{{ data.studentName || '—' }}</div>
              <div class="text-xs text-slate-500">
                NIS: {{ data.studentNis || '—' }}
                <span v-if="data.studyGroupName">• {{ data.studyGroupName }}</span>
              </div>
            </div>
          </template>
        </Column>

        <Column field="paymentMethod" header="Method" sortable style="min-width: 9rem">
          <template #body="{ data }">
            <div>
              <span
                class="inline-flex rounded-md px-2 py-0.5 text-xs font-medium"
                :class="
                  data.paymentMethod?.toUpperCase() === 'CASH'
                    ? 'bg-emerald-50 text-emerald-700'
                    : 'bg-sky-50 text-sky-700'
                "
              >
                {{ formatPaymentMethod(data.paymentMethod) }}
              </span>
              <div v-if="data.bankReference" class="mt-0.5 text-[11px] text-slate-500">
                Ref: {{ data.bankReference }}
              </div>
            </div>
          </template>
        </Column>

        <Column field="totalAmount" header="Amount" sortable style="min-width: 8.5rem">
          <template #body="{ data }">
            <span class="font-bold text-slate-900">{{ formatIdr(data.totalAmount) }}</span>
          </template>
        </Column>

        <Column field="allocatedAmount" header="Allocated / Deposit" style="min-width: 10rem">
          <template #body="{ data }">
            <div class="text-xs">
              <div class="text-slate-700">Allocated: {{ formatIdr(data.allocatedAmount) }}</div>
              <div
                :class="
                  (data.unallocatedAmount ?? 0) > 0
                    ? 'font-semibold text-amber-700'
                    : 'text-slate-400'
                "
              >
                Deposit: {{ formatIdr(data.unallocatedAmount) }}
              </div>
            </div>
          </template>
        </Column>

        <Column field="receivedByName" header="Cashier" sortable style="min-width: 9.5rem">
          <template #body="{ data }">
            <div class="text-xs">
              <div class="font-medium text-slate-800">{{ data.receivedByName || '—' }}</div>
              <div v-if="data.notes" class="text-[11px] text-slate-500">
                {{ data.notes }}
              </div>
            </div>
          </template>
        </Column>
      </AppDataTable>
    </div>
  </div>
</template>
