<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Skeleton from 'primevue/skeleton';
import { Ban, FilePlus2, Layers, RotateCcw, Search, X } from 'lucide-vue-next';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { StudentChargeDto } from '../api/billing.api';
import { useStudentChargesQuery } from '../api/useBillingQueries';
import AdHocChargeModal from '../components/AdHocChargeModal.vue';
import BatchBillingModal from '../components/BatchBillingModal.vue';
import ChargeCancellationModal from '../components/ChargeCancellationModal.vue';
import {
  ALL_GRADES,
  CHARGE_STATUS_OPTIONS,
  GRADES_BY_SCHOOL_LEVEL,
  SCHOOL_LEVEL_OPTIONS
} from '../schemas/billing.schema';
import {
  formatBillingMonth,
  formatFrequencyLabel,
  formatIdr,
  getChargeStatusIntent
} from '../utils/formatters';

const { selectedAcademicYearId, activeAcademicYearId } = useAppContextBridge();
const { hasPermission } = useCurrentSession();

const canGenerateBatch = computed(() => hasPermission('Permissions.Finance.Billing.Generate'));
const canCreateAdHoc = computed(() => hasPermission('Permissions.Finance.Billing.ManageAdHoc'));
const canCancelCharge = computed(() => hasPermission('Permissions.Finance.Billing.Cancel'));

const filterAcademicYearId = ref<string>('');
const filterBillingMonth = ref<string>('');
const filterSchoolLevel = ref<string | null>(null);
const filterGrade = ref<string | null>(null);
const filterStatus = ref<string | null>(null);
const searchText = ref<string>('');

const showBatchModal = ref(false);
const showAdHocModal = ref(false);
const showCancelModal = ref(false);
const chargeToCancel = ref<StudentChargeDto | null>(null);

const academicYearsQuery = useQuery(academicYearsLookupQuery());

watch(
  [() => selectedAcademicYearId.value, () => activeAcademicYearId.value, () => academicYearsQuery.data.value],
  ([selectedYear, activeYear, years]) => {
    if (filterAcademicYearId.value) return;
    if (selectedYear) {
      filterAcademicYearId.value = selectedYear;
      return;
    }
    if (activeYear) {
      filterAcademicYearId.value = activeYear;
      return;
    }
    const defaultYear = years?.find((y) => y.status === 'ACTIVE') ?? years?.[0];
    if (defaultYear?.id) {
      filterAcademicYearId.value = defaultYear.id;
    }
  },
  { immediate: true }
);

watch(filterSchoolLevel, (nextLevel) => {
  if (nextLevel && filterGrade.value) {
    const allowed = GRADES_BY_SCHOOL_LEVEL[nextLevel] ?? [];
    if (!allowed.includes(filterGrade.value)) {
      filterGrade.value = null;
    }
  }
});

const academicYearOptions = computed(() =>
  (academicYearsQuery.data.value ?? []).map((year) => ({
    label: year.status === 'ACTIVE' ? `${year.name} (Active)` : (year.name ?? 'Unnamed Year'),
    value: year.id ?? ''
  }))
);

const gradeOptions = computed(() => {
  const list = filterSchoolLevel.value
    ? (GRADES_BY_SCHOOL_LEVEL[filterSchoolLevel.value] ?? ALL_GRADES)
    : ALL_GRADES;
  return list.map((g) => ({ label: `Grade ${g}`, value: g }));
});

const queryParams = computed(() => ({
  academicYearId: filterAcademicYearId.value || undefined,
  billingMonth: filterBillingMonth.value.trim() || undefined,
  schoolLevel: filterSchoolLevel.value || undefined,
  grade: filterGrade.value || undefined,
  status: filterStatus.value || undefined
}));

const chargesQuery = useStudentChargesQuery(queryParams, {
  enabled: canGenerateBatch
});

const filteredCharges = computed<StudentChargeDto[]>(() => {
  const rows = chargesQuery.data.value ?? [];
  const term = searchText.value.trim().toLowerCase();
  if (!term) return rows;

  return rows.filter((row) => {
    const haystack = [
      row.studentName,
      row.studentNis,
      row.chargeTypeName,
      row.description,
      row.studyGroupName,
      row.billingMonth
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();
    return haystack.includes(term);
  });
});

const summaryMetrics = computed(() => {
  let activeCount = 0;
  let cancelledCount = 0;
  let totalNet = 0;
  let totalPaid = 0;
  let totalOutstanding = 0;

  for (const row of filteredCharges.value) {
    if (row.status?.trim().toUpperCase() === 'CANCELLED') {
      cancelledCount += 1;
      continue;
    }
    activeCount += 1;
    totalNet += row.netAmount ?? 0;
    totalPaid += row.paidAmount ?? 0;
    totalOutstanding += row.outstandingAmount ?? 0;
  }

  return {
    totalRows: filteredCharges.value.length,
    activeCount,
    cancelledCount,
    totalNet,
    totalPaid,
    totalOutstanding
  };
});

const hasActiveFilters = computed(
  () =>
    Boolean(filterBillingMonth.value) ||
    Boolean(filterSchoolLevel.value) ||
    Boolean(filterGrade.value) ||
    Boolean(filterStatus.value) ||
    Boolean(searchText.value.trim())
);

function resetFilters(): void {
  filterBillingMonth.value = '';
  filterSchoolLevel.value = null;
  filterGrade.value = null;
  filterStatus.value = null;
  searchText.value = '';
}

function isCancelDisabled(row: StudentChargeDto): boolean {
  const status = row.status?.trim().toUpperCase();
  return (row.paidAmount ?? 0) > 0 || status === 'CANCELLED' || status === 'PAID';
}

function getCancelActionTooltip(row: StudentChargeDto): string {
  const status = row.status?.trim().toUpperCase();
  if (status === 'CANCELLED') {
    return 'This charge has already been cancelled.';
  }
  if ((row.paidAmount ?? 0) > 0) {
    return `Cannot cancel charge with existing payments (${formatIdr(row.paidAmount)} recorded).`;
  }
  return 'Cancel unpaid charge with audit reason';
}

function openCancelModal(row: StudentChargeDto): void {
  if (isCancelDisabled(row)) return;
  chargeToCancel.value = row;
  showCancelModal.value = true;
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Tuition Billing</h1>
        <p class="mt-1 text-sm text-slate-600">
          Generate monthly batch tuition charges, create ad-hoc invoices, and cancel erroneous
          unpaid charges.
        </p>
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <Button
          v-if="canCreateAdHoc"
          type="button"
          label="Create Ad-Hoc Charge"
          severity="secondary"
          outlined
          data-testid="open-adhoc-charge-button"
          @click="showAdHocModal = true"
        >
          <template #icon>
            <FilePlus2 class="mr-1.5 h-4 w-4" />
          </template>
        </Button>

        <Button
          v-if="canGenerateBatch"
          type="button"
          label="Generate Monthly Billing"
          data-testid="open-batch-billing-button"
          @click="showBatchModal = true"
        >
          <template #icon>
            <Layers class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
      </div>
    </div>

    <EmptyState
      v-if="!canGenerateBatch && !canCreateAdHoc && !canCancelCharge"
      variant="no-permission"
      title="Access Restricted"
      description="You do not have billing permissions to view or manage student tuition charges."
    />

    <template v-else>
      <!-- Summary KPI Strip -->
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div
          class="rounded-xl border border-slate-200 bg-white p-4"
          data-testid="billing-kpi-count"
        >
          <p class="text-xs font-medium text-slate-500">Visible Charges</p>
          <p class="mt-1 text-2xl font-bold text-slate-900">{{ summaryMetrics.totalRows }}</p>
          <p class="mt-0.5 text-xs text-slate-500">
            {{ summaryMetrics.activeCount }} active • {{ summaryMetrics.cancelledCount }} cancelled
          </p>
        </div>

        <div
          class="rounded-xl border border-slate-200 bg-white p-4"
          data-testid="billing-kpi-net"
        >
          <p class="text-xs font-medium text-slate-500">Net Billed (Active)</p>
          <p class="mt-1 text-xl font-bold text-slate-900">
            {{ formatIdr(summaryMetrics.totalNet) }}
          </p>
          <p class="mt-0.5 text-xs text-slate-500">After student discounts</p>
        </div>

        <div
          class="rounded-xl border border-slate-200 bg-white p-4"
          data-testid="billing-kpi-paid"
        >
          <p class="text-xs font-medium text-slate-500">Collected Payments</p>
          <p class="mt-1 text-xl font-bold text-emerald-700">
            {{ formatIdr(summaryMetrics.totalPaid) }}
          </p>
          <p class="mt-0.5 text-xs text-slate-500">Allocated across active charges</p>
        </div>

        <div
          class="rounded-xl border border-slate-200 bg-white p-4"
          data-testid="billing-kpi-outstanding"
        >
          <p class="text-xs font-medium text-slate-500">Outstanding Balance</p>
          <p class="mt-1 text-xl font-bold text-amber-700">
            {{ formatIdr(summaryMetrics.totalOutstanding) }}
          </p>
          <p class="mt-0.5 text-xs text-slate-500">Remaining student receivables</p>
        </div>
      </div>

      <!-- Filter Toolbar -->
      <div
        class="rounded-xl border border-slate-200 bg-white p-4"
        data-testid="billing-filter-toolbar"
      >
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-6">
          <div>
            <label for="filterAcademicYear" class="mb-1 block text-xs font-medium text-slate-600">
              Academic Year
            </label>
            <Select
              id="filterAcademicYear"
              v-model="filterAcademicYearId"
              :options="academicYearOptions"
              option-label="label"
              option-value="value"
              placeholder="All Years"
              show-clear
              class="w-full"
            />
          </div>

          <div>
            <label for="filterBillingMonth" class="mb-1 block text-xs font-medium text-slate-600">
              Billing Month
            </label>
            <div class="flex items-center gap-1">
              <InputText
                id="filterBillingMonth"
                v-model="filterBillingMonth"
                type="month"
                placeholder="YYYY-MM"
                class="w-full"
              />
              <Button
                v-if="filterBillingMonth"
                type="button"
                severity="secondary"
                text
                rounded
                aria-label="Clear billing month"
                @click="filterBillingMonth = ''"
              >
                <template #icon>
                  <X class="h-4 w-4" />
                </template>
              </Button>
            </div>
          </div>

          <div>
            <label for="filterSchoolLevel" class="mb-1 block text-xs font-medium text-slate-600">
              School Level
            </label>
            <Select
              id="filterSchoolLevel"
              v-model="filterSchoolLevel"
              :options="[...SCHOOL_LEVEL_OPTIONS]"
              option-label="label"
              option-value="value"
              placeholder="All Levels"
              show-clear
              class="w-full"
            />
          </div>

          <div>
            <label for="filterGrade" class="mb-1 block text-xs font-medium text-slate-600">
              Grade
            </label>
            <Select
              id="filterGrade"
              v-model="filterGrade"
              :options="gradeOptions"
              option-label="label"
              option-value="value"
              placeholder="All Grades"
              show-clear
              class="w-full"
            />
          </div>

          <div>
            <label for="filterStatus" class="mb-1 block text-xs font-medium text-slate-600">
              Status
            </label>
            <Select
              id="filterStatus"
              v-model="filterStatus"
              :options="[...CHARGE_STATUS_OPTIONS]"
              option-label="label"
              option-value="value"
              placeholder="All Statuses"
              show-clear
              class="w-full"
            />
          </div>

          <div>
            <label for="filterChargeSearch" class="mb-1 block text-xs font-medium text-slate-600">
              Quick Search
            </label>
            <div class="relative">
              <Search
                class="pointer-events-none absolute top-1/2 left-2.5 h-3.5 w-3.5 -translate-y-1/2 text-slate-400"
              />
              <InputText
                id="filterChargeSearch"
                v-model="searchText"
                placeholder="Student, NIS, fee..."
                class="w-full pl-8"
              />
            </div>
          </div>
        </div>

        <div v-if="hasActiveFilters" class="mt-3 flex items-center justify-end">
          <Button
            type="button"
            label="Reset Filters"
            severity="secondary"
            text
            size="small"
            data-testid="reset-billing-filters-button"
            @click="resetFilters"
          >
            <template #icon>
              <RotateCcw class="mr-1.5 h-3.5 w-3.5" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Charges Data Table -->
      <div class="overflow-hidden rounded-xl border border-slate-200 bg-white">
        <div v-if="chargesQuery.isLoading.value" class="space-y-3 p-5">
          <Skeleton height="2.25rem" />
          <Skeleton height="2.25rem" />
          <Skeleton height="2.25rem" />
          <Skeleton height="2.25rem" />
        </div>

        <div v-else-if="chargesQuery.isError.value" class="p-6">
          <EmptyState
            variant="error"
            title="Unable to Load Student Charges"
            description="There was a problem fetching tuition billing records."
          >
            <Button
              type="button"
              label="Retry"
              severity="secondary"
              outlined
              @click="chargesQuery.refetch()"
            />
          </EmptyState>
        </div>

        <AppDataTable
          v-else
          :value="filteredCharges"
          :paginator="filteredCharges.length > 15"
          :rows="15"
          striped-rows
          empty-title="No tuition charges found"
          empty-description="Adjust the filters above or generate monthly tuition billing for this academic year."
          data-testid="billing-charges-table"
        >
          <Column header="Student" style="min-width: 13rem">
            <template #body="{ data }">
              <div>
                <p class="text-sm font-medium text-slate-900">{{ data.studentName || '—' }}</p>
                <p class="text-xs text-slate-500">
                  NIS: {{ data.studentNis || '—' }}
                  <span v-if="data.studyGroupName"> • {{ data.studyGroupName }}</span>
                  <span v-else-if="data.schoolLevel || data.grade">
                    • {{ [data.schoolLevel, data.grade ? `Grade ${data.grade}` : ''].filter(Boolean).join(' ') }}
                  </span>
                </p>
              </div>
            </template>
          </Column>

          <Column header="Charge & Period" style="min-width: 14rem">
            <template #body="{ data }">
              <div>
                <div class="flex items-center gap-1.5">
                  <span class="text-sm font-medium text-slate-900">
                    {{ data.chargeTypeName || '—' }}
                  </span>
                  <span
                    class="rounded bg-slate-100 px-1.5 py-0.5 text-[10px] font-semibold text-slate-600 uppercase"
                  >
                    {{ formatFrequencyLabel(data.chargeFrequency) }}
                  </span>
                </div>
                <p class="text-xs text-slate-500">
                  <span v-if="data.billingMonth">
                    {{ formatBillingMonth(data.billingMonth) }}
                  </span>
                  <span v-if="data.billingMonth && data.description"> • </span>
                  <span v-if="data.description">{{ data.description }}</span>
                </p>
              </div>
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

          <Column header="Net Amount" style="min-width: 8rem">
            <template #body="{ data }">
              <span class="text-sm font-semibold text-slate-900">
                {{ formatIdr(data.netAmount) }}
              </span>
            </template>
          </Column>

          <Column header="Paid" style="min-width: 7.5rem">
            <template #body="{ data }">
              <span class="text-sm text-emerald-700">{{ formatIdr(data.paidAmount) }}</span>
            </template>
          </Column>

          <Column header="Balance" style="min-width: 8rem">
            <template #body="{ data }">
              <span
                class="text-sm font-semibold"
                :class="(data.outstandingAmount ?? 0) > 0 ? 'text-slate-900' : 'text-slate-400'"
              >
                {{ formatIdr(data.outstandingAmount) }}
              </span>
            </template>
          </Column>

          <Column header="Due Date" style="min-width: 7.5rem">
            <template #body="{ data }">
              <span class="text-xs text-slate-600">
                {{ formatDisplayDate(data.dueDate, '—') }}
              </span>
            </template>
          </Column>

          <Column header="Status" style="min-width: 9rem">
            <template #body="{ data }">
              <div class="space-y-1">
                <StatusPill
                  :status="data.status || 'UNPAID'"
                  :intent="getChargeStatusIntent(data.status)"
                />
                <p
                  v-if="data.status === 'CANCELLED' && data.cancellationReason"
                  class="max-w-44 truncate text-[11px] text-slate-500"
                  :title="data.cancellationReason"
                >
                  {{ data.cancellationReason }}
                </p>
              </div>
            </template>
          </Column>

          <Column v-if="canCancelCharge" header="Actions" style="min-width: 8.5rem">
            <template #body="{ data }">
              <span class="inline-flex" :title="getCancelActionTooltip(data)">
                <Button
                  type="button"
                  label="Cancel Charge"
                  severity="danger"
                  outlined
                  size="small"
                  :disabled="isCancelDisabled(data)"
                  data-testid="cancel-charge-button"
                  @click="openCancelModal(data)"
                >
                  <template #icon>
                    <Ban class="mr-1.5 h-3.5 w-3.5" />
                  </template>
                </Button>
              </span>
            </template>
          </Column>
        </AppDataTable>
      </div>
    </template>

    <!-- Modals -->
    <BatchBillingModal
      v-model:visible="showBatchModal"
      :default-academic-year-id="filterAcademicYearId || null"
      :default-billing-month="filterBillingMonth || null"
      @completed="chargesQuery.refetch()"
    />

    <AdHocChargeModal
      v-model:visible="showAdHocModal"
      :default-academic-year-id="filterAcademicYearId || null"
      @created="chargesQuery.refetch()"
    />

    <ChargeCancellationModal
      v-model:visible="showCancelModal"
      :charge="chargeToCancel"
      @cancelled="chargesQuery.refetch()"
    />
  </div>
</template>
