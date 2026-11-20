<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Select from 'primevue/select';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import { BadgePercent, Coins, ShieldAlert } from 'lucide-vue-next';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import type { ChargeTypeDto } from '../api/feePolicy.api';
import { useChargeTypesQuery } from '../api/useFeePolicyQueries';
import ChargeTypeDrawer from '../components/ChargeTypeDrawer.vue';
import FeeCatalogTab from '../components/FeeCatalogTab.vue';
import StudentDiscountsTab from '../components/StudentDiscountsTab.vue';

const PERMISSION_FEE_POLICY_MANAGE = 'Permissions.Finance.FeePolicy.Manage';
const PERMISSION_DISCOUNTS_MANAGE = 'Permissions.Finance.Discounts.Manage';

const { selectedAcademicYearId, activeAcademicYearId, currentContextLabel } = useAppContextBridge();
const { hasPermission } = useCurrentSession();

const canManageFeePolicy = computed(() => hasPermission(PERMISSION_FEE_POLICY_MANAGE));
const canManageDiscounts = computed(() => hasPermission(PERMISSION_DISCOUNTS_MANAGE));
const hasAnyAccess = computed(() => canManageFeePolicy.value || canManageDiscounts.value);

const activeTab = ref<string>(canManageFeePolicy.value ? 'catalog' : 'discounts');

watch(
  [canManageFeePolicy, canManageDiscounts],
  ([canPolicy, canDiscounts]) => {
    if (activeTab.value === 'catalog' && !canPolicy && canDiscounts) {
      activeTab.value = 'discounts';
    } else if (activeTab.value === 'discounts' && !canDiscounts && canPolicy) {
      activeTab.value = 'catalog';
    }
  },
  { immediate: true }
);

const academicYearsQuery = useQuery({
  ...academicYearsLookupQuery(),
  enabled: () => hasAnyAccess.value
});

const selectedYearFilter = ref<string | null>(
  selectedAcademicYearId.value ?? activeAcademicYearId.value ?? null
);

watch(
  [selectedAcademicYearId, activeAcademicYearId, () => academicYearsQuery.data.value],
  ([contextYearId, activeYearId, years]) => {
    if (selectedYearFilter.value) return;
    if (contextYearId) {
      selectedYearFilter.value = contextYearId;
      return;
    }
    if (activeYearId) {
      selectedYearFilter.value = activeYearId;
      return;
    }
    const activeYear = years?.find((y) => y.status === 'ACTIVE') ?? years?.[0];
    if (activeYear?.id) {
      selectedYearFilter.value = activeYear.id;
    }
  },
  { immediate: true }
);

const academicYearOptions = computed(() => [
  { label: 'All Academic Years', value: '' },
  ...(academicYearsQuery.data.value ?? [])
    .filter((y) => Boolean(y.id))
    .map((y) => ({
      label: y.status === 'ACTIVE' ? `${y.name} (Active)` : (y.name ?? 'Unnamed Academic Year'),
      value: y.id!
    }))
]);

const effectiveYearId = computed(() => selectedYearFilter.value || null);

const chargeTypesForDiscountsQuery = useChargeTypesQuery(
  () => ({
    academicYearId: effectiveYearId.value ?? undefined
  }),
  { enabled: () => canManageFeePolicy.value && canManageDiscounts.value }
);

const isDrawerOpen = ref(false);
const editingChargeType = ref<ChargeTypeDto | null>(null);

function openCreateDrawer(): void {
  editingChargeType.value = null;
  isDrawerOpen.value = true;
}

function openEditDrawer(chargeType: ChargeTypeDto): void {
  editingChargeType.value = chargeType;
  isDrawerOpen.value = true;
}
</script>

<template>
  <div class="space-y-6 p-4 sm:p-6" data-testid="fee-policy-view">
    <!-- Page Header -->
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div class="space-y-1">
        <div class="flex flex-wrap items-center gap-2.5">
          <h1 class="text-xl font-semibold tracking-tight text-slate-900">
            Fee Policies & Discounts
          </h1>
          <span
            class="inline-flex items-center rounded-full border border-slate-200 bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600"
          >
            {{ currentContextLabel }}
          </span>
        </div>
        <p class="text-sm text-slate-600">
          Define institutional fee structures, activate billing-ready tariffs, and manage standing
          student scholarships or sibling discounts.
        </p>
      </div>

      <div v-if="hasAnyAccess" class="flex items-center gap-2">
        <Select
          :model-value="selectedYearFilter ?? ''"
          :options="academicYearOptions"
          option-label="label"
          option-value="value"
          placeholder="Academic Year"
          class="w-56"
          aria-label="Filter by academic year"
          data-testid="fee-policy-year-select"
          @update:model-value="selectedYearFilter = $event || null"
        />
      </div>
    </div>

    <EmptyState
      v-if="!hasAnyAccess"
      title="Access Restricted"
      description="You do not have permission to manage fee policies or student standing discounts."
      :icon="ShieldAlert"
    />

    <Tabs v-else v-model:value="activeTab" class="space-y-4">
      <TabList>
        <Tab v-if="canManageFeePolicy" value="catalog" data-testid="tab-fee-catalog">
          <span class="inline-flex items-center gap-2">
            <Coins class="h-4 w-4" />
            <span>Fee Catalog</span>
          </span>
        </Tab>
        <Tab v-if="canManageDiscounts" value="discounts" data-testid="tab-student-discounts">
          <span class="inline-flex items-center gap-2">
            <BadgePercent class="h-4 w-4" />
            <span>Student Discounts</span>
          </span>
        </Tab>
      </TabList>

      <TabPanels class="!p-0 !bg-transparent">
        <TabPanel v-if="canManageFeePolicy" value="catalog" class="!p-0">
          <FeeCatalogTab
            :academic-year-id="effectiveYearId"
            :can-manage="canManageFeePolicy"
            @create="openCreateDrawer"
            @edit="openEditDrawer"
          />
        </TabPanel>

        <TabPanel v-if="canManageDiscounts" value="discounts" class="!p-0">
          <StudentDiscountsTab
            :academic-year-id="effectiveYearId"
            :charge-types="chargeTypesForDiscountsQuery.data.value ?? []"
            :can-manage="canManageDiscounts"
          />
        </TabPanel>
      </TabPanels>
    </Tabs>

    <!-- Fee Structure Drawer -->
    <ChargeTypeDrawer
      v-model:visible="isDrawerOpen"
      :charge-type="editingChargeType"
      :default-academic-year-id="effectiveYearId"
    />
  </div>
</template>
