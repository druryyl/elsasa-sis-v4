<script setup lang="ts">
import { computed, ref } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import { Receipt, ShieldAlert, TrendingDown } from 'lucide-vue-next';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import DailyCashierTab from '../components/DailyCashierTab.vue';
import ReceivablesTab from '../components/ReceivablesTab.vue';

const PERMISSION_REPORTS_VIEW = 'Permissions.Finance.Reports.View';
const PERMISSION_STATEMENTS_VIEW = 'Permissions.Finance.Statements.View';

const { selectedAcademicYearId, activeAcademicYearId, currentContextLabel } = useAppContextBridge();
const { hasPermission } = useCurrentSession();

const canViewReports = computed(() => hasPermission(PERMISSION_REPORTS_VIEW));
const canViewStatements = computed(() => hasPermission(PERMISSION_STATEMENTS_VIEW));

const activeTab = ref<'receivables' | 'daily-cashier'>('receivables');

const academicYearsQuery = useQuery({
  ...academicYearsLookupQuery(),
  enabled: () => canViewReports.value
});

const defaultAcademicYearId = computed(() => {
  if (selectedAcademicYearId.value) return selectedAcademicYearId.value;
  if (activeAcademicYearId.value) return activeAcademicYearId.value;
  const years = academicYearsQuery.data.value ?? [];
  return years.find((y) => y.status === 'ACTIVE')?.id ?? years[0]?.id ?? null;
});
</script>

<template>
  <div class="space-y-6 p-4 sm:p-6" data-testid="receivables-aging-view">
    <!-- Page Header -->
    <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
      <div class="space-y-1">
        <div class="flex flex-wrap items-center gap-2.5">
          <h1 class="text-xl font-semibold tracking-tight text-slate-900">
            Receivables &amp; Arrears
          </h1>
          <span
            class="inline-flex items-center rounded-full border border-slate-200 bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600"
          >
            {{ currentContextLabel }}
          </span>
        </div>
        <p class="text-sm text-slate-600">
          Monitor tuition receivables, aged student arrears buckets, student balance statements, and
          daily cashier collection reconciliation.
        </p>
      </div>
    </div>

    <EmptyState
      v-if="!canViewReports"
      title="Access Restricted"
      description="You do not have permission to view financial receivables or cashier reports."
      :icon="ShieldAlert"
    />

    <Tabs v-else v-model:value="activeTab" class="space-y-4">
      <TabList>
        <Tab value="receivables" data-testid="tab-receivables-arrears">
          <span class="inline-flex items-center gap-2">
            <TrendingDown class="h-4 w-4" />
            <span>Receivables &amp; Arrears</span>
          </span>
        </Tab>
        <Tab value="daily-cashier" data-testid="tab-daily-cashier">
          <span class="inline-flex items-center gap-2">
            <Receipt class="h-4 w-4" />
            <span>Daily Cashier Summary</span>
          </span>
        </Tab>
      </TabList>

      <TabPanels class="!bg-transparent !p-0">
        <TabPanel value="receivables" class="!p-0">
          <ReceivablesTab
            :academic-years="academicYearsQuery.data.value ?? []"
            :default-academic-year-id="defaultAcademicYearId"
            :can-view-statements="canViewStatements"
          />
        </TabPanel>

        <TabPanel value="daily-cashier" class="!p-0">
          <DailyCashierTab />
        </TabPanel>
      </TabPanels>
    </Tabs>
  </div>
</template>
