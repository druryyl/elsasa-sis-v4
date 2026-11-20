<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Skeleton from 'primevue/skeleton';
import { CheckCircle2, Coins, Lock, Pencil, Plus, Search } from 'lucide-vue-next';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import type { ChargeTypeDto } from '../api/feePolicy.api';
import { useChargeTypesQuery } from '../api/useFeePolicyQueries';
import { useActivateChargeTypeMutation } from '../api/useFeePolicyMutations';
import { CHARGE_FREQUENCIES } from '../schemas/feePolicy.schema';
import { formatCohortScope, formatFrequencyLabel, formatIdr } from '../utils/formatters';

const props = withDefaults(
  defineProps<{
    academicYearId?: string | null;
    canManage: boolean;
  }>(),
  {
    academicYearId: null
  }
);

const emit = defineEmits<{
  create: [];
  edit: [chargeType: ChargeTypeDto];
}>();

const searchTerm = ref('');
const statusFilter = ref<'ALL' | 'DRAFT' | 'ACTIVE'>('ALL');
const frequencyFilter = ref<'ALL' | (typeof CHARGE_FREQUENCIES)[number]>('ALL');
const activationTarget = ref<ChargeTypeDto | null>(null);

const chargeTypesQuery = useChargeTypesQuery(
  () => ({
    academicYearId: props.academicYearId ?? undefined
  }),
  { enabled: () => props.canManage }
);

const activateMutation = useActivateChargeTypeMutation();

const allChargeTypes = computed(() => chargeTypesQuery.data.value ?? []);

const filteredChargeTypes = computed(() => {
  const term = searchTerm.value.trim().toLowerCase();
  return allChargeTypes.value.filter((item) => {
    if (statusFilter.value !== 'ALL' && item.status !== statusFilter.value) {
      return false;
    }
    if (frequencyFilter.value !== 'ALL' && item.frequency !== frequencyFilter.value) {
      return false;
    }
    if (!term) return true;

    const scopeText = formatCohortScope(item).toLowerCase();
    return (
      (item.name ?? '').toLowerCase().includes(term) ||
      (item.academicYearName ?? '').toLowerCase().includes(term) ||
      scopeText.includes(term)
    );
  });
});

const totalCount = computed(() => allChargeTypes.value.length);
const activeCount = computed(
  () => allChargeTypes.value.filter((item) => item.status === 'ACTIVE').length
);
const draftCount = computed(
  () => allChargeTypes.value.filter((item) => item.status === 'DRAFT').length
);

const frequencyFilterOptions = [
  { label: 'All Frequencies', value: 'ALL' },
  ...CHARGE_FREQUENCIES.map((freq) => ({
    label: formatFrequencyLabel(freq),
    value: freq
  }))
];

const statusFilterOptions = [
  { label: 'All Statuses', value: 'ALL' },
  { label: 'Draft', value: 'DRAFT' },
  { label: 'Active', value: 'ACTIVE' }
];

function requestActivate(chargeType: ChargeTypeDto): void {
  activationTarget.value = chargeType;
}

async function confirmActivate(): Promise<void> {
  if (!activationTarget.value?.id) return;
  try {
    await activateMutation.mutateAsync(activationTarget.value.id);
    activationTarget.value = null;
  } catch {
    // Toast handled by mutation hook
  }
}
</script>

<template>
  <div class="space-y-5" data-testid="fee-catalog-tab">
    <!-- Summary KPI Strip -->
    <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="text-xs font-medium text-slate-500">Total Fee Structures</div>
        <div class="mt-1 text-2xl font-semibold text-slate-900" data-testid="kpi-total-fees">
          {{ totalCount }}
        </div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="text-xs font-medium text-slate-500">Active (Billing-Ready)</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-700" data-testid="kpi-active-fees">
          {{ activeCount }}
        </div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="text-xs font-medium text-slate-500">Draft Policies</div>
        <div class="mt-1 text-2xl font-semibold text-slate-700" data-testid="kpi-draft-fees">
          {{ draftCount }}
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
            placeholder="Search fee name or scope..."
            class="w-full pl-9"
            data-testid="fee-catalog-search-input"
          />
        </div>

        <Select
          v-model="frequencyFilter"
          :options="frequencyFilterOptions"
          option-label="label"
          option-value="value"
          class="w-44"
          aria-label="Filter by frequency"
          data-testid="fee-catalog-frequency-filter"
        />

        <Select
          v-model="statusFilter"
          :options="statusFilterOptions"
          option-label="label"
          option-value="value"
          class="w-40"
          aria-label="Filter by status"
          data-testid="fee-catalog-status-filter"
        />
      </div>

      <Button
        v-if="canManage"
        label="New Fee Structure"
        data-testid="new-fee-structure-button"
        @click="emit('create')"
      >
        <template #icon>
          <Plus class="mr-1.5 h-4 w-4" />
        </template>
      </Button>
    </div>

    <!-- Loading Skeleton -->
    <div
      v-if="chargeTypesQuery.isPending.value"
      class="space-y-3 rounded-xl border border-slate-200 bg-white p-4"
    >
      <Skeleton v-for="idx in 4" :key="idx" height="2.75rem" />
    </div>

    <!-- Empty State -->
    <EmptyState
      v-else-if="filteredChargeTypes.length === 0"
      title="No fee structures found"
      description="Create a fee policy structure (such as monthly SPP tuition or uniforms) to get started."
      :icon="Coins"
    >
      <template v-if="canManage" #actions>
        <Button label="New Fee Structure" size="small" @click="emit('create')" />
      </template>
    </EmptyState>

    <!-- Data Table -->
    <div v-else class="overflow-hidden rounded-xl border border-slate-200 bg-white">
      <DataTable
        :value="filteredChargeTypes"
        data-key="id"
        striped-rows
        responsive-layout="scroll"
        data-testid="fee-catalog-table"
      >
        <Column field="name" header="Fee Structure">
          <template #body="{ data }">
            <div class="space-y-0.5">
              <div class="font-medium text-slate-900">{{ data.name }}</div>
              <div class="text-xs text-slate-500">
                {{ data.academicYearName }} • {{ formatCohortScope(data) }}
              </div>
            </div>
          </template>
        </Column>

        <Column field="frequency" header="Frequency">
          <template #body="{ data }">
            <span
              class="inline-flex items-center rounded-md border border-slate-200 bg-slate-50 px-2 py-0.5 text-xs font-medium text-slate-700"
            >
              {{ formatFrequencyLabel(data.frequency) }}
            </span>
          </template>
        </Column>

        <Column field="amount" header="Standard Amount">
          <template #body="{ data }">
            <span class="font-semibold text-slate-900 tabular-nums">
              {{ formatIdr(data.amount) }}
            </span>
          </template>
        </Column>

        <Column field="status" header="Status">
          <template #body="{ data }">
            <StatusPill :status="data.status ?? 'DRAFT'" />
          </template>
        </Column>

        <Column v-if="canManage" header="Actions" class="w-48">
          <template #body="{ data }">
            <div class="flex items-center gap-2">
              <Button
                v-if="data.status !== 'ACTIVE'"
                type="button"
                label="Edit"
                severity="secondary"
                size="small"
                outlined
                :data-testid="`edit-fee-button-${data.id}`"
                @click="emit('edit', data)"
              >
                <template #icon>
                  <Pencil class="mr-1 h-3.5 w-3.5" />
                </template>
              </Button>

              <Button
                v-if="data.status === 'DRAFT'"
                type="button"
                label="Activate"
                severity="success"
                size="small"
                :data-testid="`activate-fee-button-${data.id}`"
                @click="requestActivate(data)"
              >
                <template #icon>
                  <CheckCircle2 class="mr-1 h-3.5 w-3.5" />
                </template>
              </Button>

              <span
                v-else
                class="inline-flex items-center gap-1 text-xs font-medium text-slate-500"
                title="Active fee structures are immutable and cannot be modified."
                :data-testid="`locked-fee-badge-${data.id}`"
              >
                <Lock class="h-3.5 w-3.5" />
                Locked
              </span>
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Tier 2 Activation Confirmation Dialog -->
    <ConfirmActionDialog
      :visible="Boolean(activationTarget)"
      title="Activate Fee Structure"
      :message="`Activating '${activationTarget?.name ?? ''}' (${formatIdr(activationTarget?.amount)}) makes it available for student tuition billing. Warning: Once active, this fee structure cannot be modified.`"
      confirm-label="Activate & Lock Fee"
      cancel-label="Keep as Draft"
      severity="warn"
      :loading="activateMutation.isPending.value"
      data-testid="activate-fee-confirm-dialog"
      @update:visible="!$event && (activationTarget = null)"
      @confirm="confirmActivate"
      @cancel="activationTarget = null"
    />
  </div>
</template>
