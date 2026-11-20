<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Skeleton from 'primevue/skeleton';
import { BadgePercent, Plus, Search, ShieldOff } from 'lucide-vue-next';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import type { ChargeTypeDto, StudentDiscountDto } from '../api/feePolicy.api';
import { useStudentDiscountsQuery } from '../api/useFeePolicyQueries';
import { useRevokeStudentDiscountMutation } from '../api/useFeePolicyMutations';
import { formatDiscountValue } from '../utils/formatters';
import GrantDiscountModal from './GrantDiscountModal.vue';

const props = withDefaults(
  defineProps<{
    academicYearId?: string | null;
    chargeTypes?: ChargeTypeDto[];
    canManage: boolean;
  }>(),
  {
    academicYearId: null,
    chargeTypes: () => []
  }
);

const searchTerm = ref('');
const statusFilter = ref<'ACTIVE' | 'REVOKED' | 'ALL'>('ACTIVE');
const isGrantModalOpen = ref(false);
const revokeTarget = ref<StudentDiscountDto | null>(null);

const discountsQuery = useStudentDiscountsQuery(
  () => ({
    academicYearId: props.academicYearId ?? undefined
  }),
  { enabled: () => props.canManage }
);

const revokeMutation = useRevokeStudentDiscountMutation();

const allDiscounts = computed(() => discountsQuery.data.value ?? []);

const filteredDiscounts = computed(() => {
  const term = searchTerm.value.trim().toLowerCase();
  return allDiscounts.value.filter((item) => {
    if (statusFilter.value === 'ACTIVE' && item.isActive === false) return false;
    if (statusFilter.value === 'REVOKED' && item.isActive !== false) return false;
    if (!term) return true;

    return (
      (item.studentName ?? '').toLowerCase().includes(term) ||
      (item.studentNis ?? '').toLowerCase().includes(term) ||
      (item.chargeTypeName ?? '').toLowerCase().includes(term) ||
      (item.reason ?? '').toLowerCase().includes(term)
    );
  });
});

const activeDiscountsCount = computed(
  () => allDiscounts.value.filter((d) => d.isActive !== false).length
);
const percentageDiscountsCount = computed(
  () =>
    allDiscounts.value.filter((d) => d.isActive !== false && d.discountType === 'PERCENTAGE').length
);
const fixedDiscountsCount = computed(
  () =>
    allDiscounts.value.filter((d) => d.isActive !== false && d.discountType === 'FIXED_AMOUNT')
      .length
);

const statusFilterOptions = [
  { label: 'Active Discounts', value: 'ACTIVE' },
  { label: 'Revoked Discounts', value: 'REVOKED' },
  { label: 'All Discounts', value: 'ALL' }
];

function requestRevoke(discount: StudentDiscountDto): void {
  revokeTarget.value = discount;
}

async function confirmRevoke(): Promise<void> {
  if (!revokeTarget.value?.id) return;
  try {
    await revokeMutation.mutateAsync(revokeTarget.value.id);
    revokeTarget.value = null;
  } catch {
    // Toast handled by mutation hook
  }
}
</script>

<template>
  <div class="space-y-5" data-testid="student-discounts-tab">
    <!-- Summary KPI Strip -->
    <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="text-xs font-medium text-slate-500">Active Standing Discounts</div>
        <div
          class="mt-1 text-2xl font-semibold text-emerald-700"
          data-testid="kpi-active-discounts"
        >
          {{ activeDiscountsCount }}
        </div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="text-xs font-medium text-slate-500">Percentage Waivers</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900">
          {{ percentageDiscountsCount }}
        </div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="text-xs font-medium text-slate-500">Fixed-Amount Scholarships</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900">
          {{ fixedDiscountsCount }}
        </div>
      </div>
    </div>

    <!-- Filter & Action Toolbar -->
    <div
      class="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 lg:flex-row lg:items-center lg:justify-between"
    >
      <div class="flex flex-1 flex-wrap items-center gap-2.5">
        <div class="relative min-w-56 flex-1 sm:max-w-xs">
          <Search
            class="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
          />
          <InputText
            v-model="searchTerm"
            type="search"
            placeholder="Search student, NIS, fee, or reason..."
            class="w-full pl-9"
            data-testid="discounts-search-input"
          />
        </div>

        <Select
          v-model="statusFilter"
          :options="statusFilterOptions"
          option-label="label"
          option-value="value"
          class="w-44"
          aria-label="Filter discounts by status"
          data-testid="discounts-status-filter"
        />
      </div>

      <Button
        v-if="canManage"
        label="Grant Discount"
        data-testid="grant-discount-button"
        @click="isGrantModalOpen = true"
      >
        <template #icon>
          <Plus class="mr-1.5 h-4 w-4" />
        </template>
      </Button>
    </div>

    <!-- Loading Skeleton -->
    <div
      v-if="discountsQuery.isPending.value"
      class="space-y-3 rounded-xl border border-slate-200 bg-white p-4"
    >
      <Skeleton v-for="idx in 4" :key="idx" height="2.75rem" />
    </div>

    <!-- Empty State -->
    <EmptyState
      v-else-if="filteredDiscounts.length === 0"
      title="No standing discounts found"
      description="Grant sibling discounts or scholarships to automatically deduct amounts during tuition billing."
      :icon="BadgePercent"
    >
      <template v-if="canManage" #actions>
        <Button label="Grant Discount" size="small" @click="isGrantModalOpen = true" />
      </template>
    </EmptyState>

    <!-- Standing Discounts Table -->
    <div v-else class="overflow-hidden rounded-xl border border-slate-200 bg-white">
      <DataTable
        :value="filteredDiscounts"
        data-key="id"
        striped-rows
        responsive-layout="scroll"
        data-testid="student-discounts-table"
      >
        <Column field="studentName" header="Student">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="font-medium text-slate-900">{{ data.studentName }}</div>
              <div class="text-xs text-slate-500">NIS: {{ data.studentNis }}</div>
            </div>
          </template>
        </Column>

        <Column field="chargeTypeName" header="Fee Structure">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="text-sm font-medium text-slate-800">{{ data.chargeTypeName }}</div>
              <div class="text-xs text-slate-500">{{ data.academicYearName }}</div>
            </div>
          </template>
        </Column>

        <Column field="discountValue" header="Discount">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="font-semibold text-emerald-700 tabular-nums">
                {{ formatDiscountValue(data.discountType, data.discountValue) }}
              </div>
              <div class="text-xs text-slate-500">
                {{ data.discountType === 'PERCENTAGE' ? 'Percentage' : 'Fixed Amount' }}
              </div>
            </div>
          </template>
        </Column>

        <Column field="reason" header="Reason / Program">
          <template #body="{ data }">
            <span class="text-sm text-slate-700">{{ data.reason }}</span>
          </template>
        </Column>

        <Column field="isActive" header="Status">
          <template #body="{ data }">
            <StatusPill
              :status="data.isActive !== false ? 'ACTIVE' : 'INACTIVE'"
              :label="data.isActive !== false ? 'Active' : 'Revoked'"
            />
          </template>
        </Column>

        <Column v-if="canManage" header="Actions" class="w-32">
          <template #body="{ data }">
            <Button
              v-if="data.isActive !== false"
              type="button"
              label="Revoke"
              severity="danger"
              size="small"
              outlined
              :data-testid="`revoke-discount-button-${data.id}`"
              @click="requestRevoke(data)"
            >
              <template #icon>
                <ShieldOff class="mr-1 h-3.5 w-3.5" />
              </template>
            </Button>
            <span v-else class="text-xs text-slate-400">Revoked</span>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Grant Discount Modal -->
    <GrantDiscountModal
      v-model:visible="isGrantModalOpen"
      :academic-year-id="academicYearId"
      :charge-types="chargeTypes"
    />

    <!-- Revoke Confirmation Dialog -->
    <ConfirmActionDialog
      :visible="Boolean(revokeTarget)"
      title="Revoke Standing Discount"
      :message="`Are you sure you want to revoke the ${formatDiscountValue(revokeTarget?.discountType, revokeTarget?.discountValue)} discount on '${revokeTarget?.chargeTypeName ?? ''}' for ${revokeTarget?.studentName ?? ''}? Future tuition billing runs will no longer apply this deduction.`"
      confirm-label="Revoke Discount"
      cancel-label="Cancel"
      severity="danger"
      :loading="revokeMutation.isPending.value"
      data-testid="revoke-discount-confirm-dialog"
      @update:visible="!$event && (revokeTarget = null)"
      @confirm="confirmRevoke"
      @cancel="revokeTarget = null"
    />
  </div>
</template>
