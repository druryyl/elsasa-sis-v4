<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import { ShieldAlert, Users, FileCheck2, Coins, ArrowLeft } from 'lucide-vue-next';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { BatchBillingPreviewResponse } from '../api/billing.api';
import { formatBillingMonth, formatIdr } from '../utils/formatters';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    preview: BatchBillingPreviewResponse | null;
    academicYearName?: string;
    billingMonth: string;
    scopeSummary: string;
    dueDate?: string | null;
    loading?: boolean;
    errorMessage?: string | null;
  }>(),
  {
    academicYearName: '',
    dueDate: null,
    loading: false,
    errorMessage: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  confirm: [];
  back: [];
  cancel: [];
}>();

const previewItems = computed(() => props.preview?.items ?? []);
const prospectiveChargeCount = computed(() => props.preview?.chargeCount ?? 0);
const skippedCount = computed(() => props.preview?.skippedCount ?? 0);
const isConfirmDisabled = computed(() => props.loading || prospectiveChargeCount.value === 0);

function handleClose(): void {
  if (props.loading) return;
  emit('update:visible', false);
  emit('cancel');
}
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :closable="!loading"
    :draggable="false"
    class="w-full max-w-4xl"
    data-testid="preflight-summary-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-700"
        >
          <ShieldAlert class="h-5 w-5" />
        </div>
        <div>
          <span
            class="inline-flex items-center rounded bg-amber-50 px-2 py-0.5 text-[11px] font-semibold tracking-wide text-amber-700 uppercase"
          >
            Tier 3 Pre-Flight Impact Summary
          </span>
          <h3 class="mt-0.5 text-base font-semibold text-slate-900">
            Confirm Monthly Batch Tuition Billing
          </h3>
        </div>
      </div>
    </template>

    <div class="space-y-4">
      <div
        v-if="errorMessage"
        role="alert"
        class="rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-2.5 text-sm text-rose-700"
      >
        {{ errorMessage }}
      </div>

      <!-- Target Cohort Context -->
      <div
        class="grid grid-cols-1 gap-2 rounded-xl border border-slate-200 bg-slate-50/80 p-3.5 text-xs sm:grid-cols-4"
      >
        <div>
          <span class="font-medium text-slate-500">Academic Year</span>
          <p class="mt-0.5 font-semibold text-slate-900">{{ academicYearName || 'Selected Year' }}</p>
        </div>
        <div>
          <span class="font-medium text-slate-500">Billing Month</span>
          <p class="mt-0.5 font-semibold text-slate-900">
            {{ formatBillingMonth(billingMonth) }} ({{ billingMonth }})
          </p>
        </div>
        <div>
          <span class="font-medium text-slate-500">Target Cohort</span>
          <p class="mt-0.5 font-semibold text-slate-900">{{ scopeSummary }}</p>
        </div>
        <div>
          <span class="font-medium text-slate-500">Due Date</span>
          <p class="mt-0.5 font-semibold text-slate-900">
            {{ dueDate ? formatDisplayDate(dueDate, dueDate) : 'Not specified' }}
          </p>
        </div>
      </div>

      <!-- Impact KPI Summary Cards -->
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div
          class="rounded-xl border border-slate-200 bg-white p-3.5"
          data-testid="preflight-kpi-students"
        >
          <div class="flex items-center justify-between text-xs font-medium text-slate-500">
            <span>Student Cohort Size</span>
            <Users class="h-4 w-4 text-slate-400" />
          </div>
          <p class="mt-1.5 text-2xl font-bold text-slate-900">
            {{ preview?.studentCount ?? 0 }}
          </p>
          <p class="mt-0.5 text-xs text-slate-500">
            {{ prospectiveChargeCount }} new charge{{ prospectiveChargeCount === 1 ? '' : 's' }}
            <span v-if="skippedCount > 0"> • {{ skippedCount }} skipped</span>
          </p>
        </div>

        <div
          class="rounded-xl border border-slate-200 bg-white p-3.5"
          data-testid="preflight-kpi-gross"
        >
          <div class="flex items-center justify-between text-xs font-medium text-slate-500">
            <span>Total Gross Amount</span>
            <FileCheck2 class="h-4 w-4 text-slate-400" />
          </div>
          <p class="mt-1.5 text-xl font-bold text-slate-900">
            {{ formatIdr(preview?.totalGrossAmount) }}
          </p>
          <p class="mt-0.5 text-xs text-slate-500">Before standing discounts</p>
        </div>

        <div
          class="rounded-xl border border-slate-200 bg-white p-3.5"
          data-testid="preflight-kpi-discounts"
        >
          <div class="flex items-center justify-between text-xs font-medium text-slate-500">
            <span>Total Discounts</span>
            <Coins class="h-4 w-4 text-amber-500" />
          </div>
          <p class="mt-1.5 text-xl font-bold text-amber-700">
            -{{ formatIdr(preview?.totalDiscountAmount) }}
          </p>
          <p class="mt-0.5 text-xs text-slate-500">Scholarships & sibling waivers</p>
        </div>

        <div
          class="rounded-xl border border-emerald-200 bg-emerald-50/60 p-3.5"
          data-testid="preflight-kpi-net"
        >
          <div class="flex items-center justify-between text-xs font-semibold text-emerald-800">
            <span>Prospective Net Total</span>
            <Coins class="h-4 w-4 text-emerald-600" />
          </div>
          <p class="mt-1.5 text-xl font-bold text-emerald-900">
            {{ formatIdr(preview?.totalNetAmount) }}
          </p>
          <p class="mt-0.5 text-xs text-emerald-700">Receivable balance to be posted</p>
        </div>
      </div>

      <div
        v-if="prospectiveChargeCount === 0"
        class="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
      >
        No new monthly charges will be generated for this scope. Either all eligible students have
        already been billed for <strong>{{ billingMonth }}</strong> or no active monthly fee
        structures match the selected cohort.
      </div>

      <!-- Itemized Preview Table -->
      <div class="overflow-hidden rounded-xl border border-slate-200">
        <div class="border-b border-slate-200 bg-slate-50 px-3.5 py-2 text-xs font-semibold text-slate-700">
          Preview Breakdown ({{ previewItems.length }} line item{{ previewItems.length === 1 ? '' : 's' }})
        </div>
        <DataTable
          :value="previewItems"
          :paginator="previewItems.length > 8"
          :rows="8"
          scrollable
          scroll-height="260px"
          data-testid="preflight-preview-table"
        >
          <Column header="Student" style="min-width: 12rem">
            <template #body="{ data }">
              <div>
                <p class="text-sm font-medium text-slate-900">{{ data.studentName || '—' }}</p>
                <p class="text-xs text-slate-500">
                  NIS: {{ data.studentNis || '—' }}
                  <span v-if="data.studyGroupName"> • {{ data.studyGroupName }}</span>
                </p>
              </div>
            </template>
          </Column>

          <Column field="chargeTypeName" header="Fee Structure" style="min-width: 10rem">
            <template #body="{ data }">
              <span class="text-sm text-slate-800">{{ data.chargeTypeName || '—' }}</span>
            </template>
          </Column>

          <Column header="Gross" style="min-width: 7.5rem">
            <template #body="{ data }">
              <span class="text-sm text-slate-700">{{ formatIdr(data.grossAmount) }}</span>
            </template>
          </Column>

          <Column header="Discount" style="min-width: 8.5rem">
            <template #body="{ data }">
              <div>
                <span
                  class="text-sm"
                  :class="(data.discountAmount ?? 0) > 0 ? 'font-medium text-amber-700' : 'text-slate-400'"
                >
                  {{ (data.discountAmount ?? 0) > 0 ? `-${formatIdr(data.discountAmount)}` : '—' }}
                </span>
                <p v-if="data.discountReason" class="text-[11px] text-slate-500">
                  {{ data.discountReason }}
                </p>
              </div>
            </template>
          </Column>

          <Column header="Net Amount" style="min-width: 7.5rem">
            <template #body="{ data }">
              <span class="text-sm font-semibold text-slate-900">{{ formatIdr(data.netAmount) }}</span>
            </template>
          </Column>

          <Column header="Action" style="min-width: 8rem">
            <template #body="{ data }">
              <StatusPill
                :status="data.isAlreadyBilled ? 'SKIP' : 'GENERATE'"
                :label="data.isAlreadyBilled ? 'Already Billed' : 'Will Generate'"
                :intent="data.isAlreadyBilled ? 'neutral' : 'success'"
              />
            </template>
          </Column>

          <template #empty>
            <div class="py-6 text-center text-sm text-slate-500">
              No active monthly fee structures or placed students matched this filter.
            </div>
          </template>
        </DataTable>
      </div>
    </div>

    <template #footer>
      <div class="flex w-full flex-wrap items-center justify-between gap-2">
        <Button
          type="button"
          label="Back to Filters"
          severity="secondary"
          outlined
          :disabled="loading"
          data-testid="preflight-back-button"
          @click="emit('back')"
        >
          <template #icon>
            <ArrowLeft class="mr-1.5 h-4 w-4" />
          </template>
        </Button>

        <div class="flex items-center gap-2">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            text
            :disabled="loading"
            @click="handleClose"
          />
          <Button
            type="button"
            label="Confirm & Generate Billing"
            :loading="loading"
            :disabled="isConfirmDisabled"
            data-testid="preflight-confirm-button"
            @click="emit('confirm')"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>
