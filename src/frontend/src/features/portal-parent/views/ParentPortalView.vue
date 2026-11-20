<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import {
  AlertTriangle,
  Award,
  CreditCard,
  Megaphone,
  RefreshCw,
  Users
} from 'lucide-vue-next';
import ChildSelectorTabs from '../components/ChildSelectorTabs.vue';
import ReportCardsTab from '../components/ReportCardsTab.vue';
import BillingAndReceiptsTab from '../components/BillingAndReceiptsTab.vue';
import SchoolInfoTab from '../components/SchoolInfoTab.vue';
import { useMyChildrenQuery } from '../api/useParentPortalQueries';

type PortalTab = 'report-cards' | 'billing' | 'school-info';

const activeTab = ref<PortalTab>('report-cards');
const selectedChildId = ref<string | null>(null);
const isRefreshing = ref(false);

const {
  data: childrenData,
  isLoading: isLoadingChildren,
  isError: isErrorChildren,
  error: childrenError,
  refetch: refetchChildren
} = useMyChildrenQuery();

const children = computed(() => childrenData.value ?? []);

// Automatically select first child when children list loads
watch(
  () => children.value,
  (list) => {
    if (list && list.length > 0) {
      if (!selectedChildId.value || !list.some((c) => c.studentId === selectedChildId.value)) {
        selectedChildId.value = list[0]?.studentId ?? null;
      }
    } else {
      selectedChildId.value = null;
    }
  },
  { immediate: true }
);

async function handleRefresh(): Promise<void> {
  isRefreshing.value = true;
  try {
    await refetchChildren();
  } finally {
    setTimeout(() => {
      isRefreshing.value = false;
    }, 400);
  }
}

function handleSelectChildId(childId: string): void {
  selectedChildId.value = childId;
}
</script>

<template>
  <div
    class="mx-auto max-w-5xl space-y-6 pb-12"
    data-testid="parent-portal-view"
  >
    <!-- Portal Header Bar -->
    <header class="flex flex-col gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <div class="flex items-center gap-2">
          <span class="inline-flex items-center rounded-md bg-brand-primary-100 px-2.5 py-0.5 text-xs font-semibold text-brand-primary-800">
            Portal Orang Tua & Wali
          </span>
        </div>
        <h1 class="mt-1.5 text-2xl font-bold tracking-tight text-slate-900 sm:text-3xl">
          Layanan Mandiri Keluarga
        </h1>
        <p class="mt-1 text-sm text-slate-500">
          Akses rapor akademik resmi, status pembayaran SPP, kuitansi digital, dan buletin sekolah.
        </p>
      </div>

      <!-- Action buttons -->
      <div class="flex items-center gap-2 shrink-0">
        <Button
          type="button"
          label="Segarkan Data"
          severity="secondary"
          outlined
          size="small"
          data-testid="portal-refresh-btn"
          :disabled="isRefreshing || isLoadingChildren"
          @click="handleRefresh"
        >
          <template #icon>
            <RefreshCw
              class="mr-1.5 h-4 w-4"
              :class="{ 'animate-spin': isRefreshing }"
            />
          </template>
        </Button>
      </div>
    </header>

    <!-- Error Alert Banner -->
    <div
      v-if="isErrorChildren"
      class="flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-red-800"
      data-testid="children-error-banner"
    >
      <AlertTriangle class="h-5 w-5 shrink-0 text-red-600" />
      <div class="min-w-0 flex-1">
        <h4 class="text-sm font-semibold">Gagal Memuat Profil Anak</h4>
        <p class="mt-0.5 text-xs text-red-700">
          {{ childrenError?.message || 'Terjadi kesalahan saat mengambil daftar anak yang terhubung.' }}
        </p>
        <Button
          label="Coba Lagi"
          severity="danger"
          size="small"
          text
          class="mt-2 p-0 text-xs font-semibold"
          @click="() => refetchChildren()"
        />
      </div>
    </div>

    <!-- Children Selector Tabs -->
    <section aria-label="Pemilihan Anak">
      <ChildSelectorTabs
        :children="children"
        :selected-child-id="selectedChildId"
        :loading="isLoadingChildren"
        @update:selected-child-id="handleSelectChildId"
      />
    </section>

    <!-- Empty State when no children linked -->
    <div
      v-if="!isLoadingChildren && children.length === 0 && !isErrorChildren"
      class="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 bg-white p-12 text-center shadow-xs"
      data-testid="empty-children-state"
    >
      <div class="flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100 text-slate-400">
        <Users class="h-8 w-8" />
      </div>
      <h3 class="mt-4 text-lg font-semibold text-slate-900">
        Tidak Ada Siswa Terhubung
      </h3>
      <p class="mt-1.5 max-w-md text-sm text-slate-500">
        Akun Anda belum dikaitkan dengan data siswa manapun. Silakan hubungi bagian tata usaha sekolah untuk menghubungkan akun wali murid.
      </p>
    </div>

    <!-- Main Feature Navigation Tabs & Content -->
    <div
      v-else-if="selectedChildId || activeTab === 'school-info'"
      class="space-y-6"
    >
      <!-- Horizontal Navigation Tabs -->
      <nav
        class="flex overflow-x-auto border-b border-slate-200 no-scrollbar"
        aria-label="Fitur Portal"
        role="tablist"
        data-testid="portal-feature-tabs"
      >
        <button
          type="button"
          role="tab"
          data-testid="tab-trigger-report-cards"
          :aria-selected="activeTab === 'report-cards'"
          class="flex min-h-[44px] shrink-0 items-center gap-2 border-b-2 px-4 py-3 text-sm font-medium transition-all"
          :class="[
            activeTab === 'report-cards'
              ? 'border-brand-primary-600 text-brand-primary-700 font-semibold'
              : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
          ]"
          @click="activeTab = 'report-cards'"
        >
          <Award class="h-4 w-4" />
          <span>Rapor Akademik</span>
        </button>

        <button
          type="button"
          role="tab"
          data-testid="tab-trigger-billing"
          :aria-selected="activeTab === 'billing'"
          class="flex min-h-[44px] shrink-0 items-center gap-2 border-b-2 px-4 py-3 text-sm font-medium transition-all"
          :class="[
            activeTab === 'billing'
              ? 'border-brand-primary-600 text-brand-primary-700 font-semibold'
              : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
          ]"
          @click="activeTab = 'billing'"
        >
          <CreditCard class="h-4 w-4" />
          <span>Tagihan & Kuitansi</span>
        </button>

        <button
          type="button"
          role="tab"
          data-testid="tab-trigger-school-info"
          :aria-selected="activeTab === 'school-info'"
          class="flex min-h-[44px] shrink-0 items-center gap-2 border-b-2 px-4 py-3 text-sm font-medium transition-all"
          :class="[
            activeTab === 'school-info'
              ? 'border-brand-primary-600 text-brand-primary-700 font-semibold'
              : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
          ]"
          @click="activeTab = 'school-info'"
        >
          <Megaphone class="h-4 w-4" />
          <span>Informasi Sekolah</span>
        </button>
      </nav>

      <!-- Active Tab Panel Content -->
      <main class="min-h-[400px]">
        <!-- Tab 1: Academic Report Cards -->
        <div
          v-if="activeTab === 'report-cards' && selectedChildId"
          data-testid="tab-content-report-cards"
        >
          <ReportCardsTab
            :key="`rc-${selectedChildId}`"
            :student-id="selectedChildId"
          />
        </div>

        <!-- Tab 2: Billing & Receipts -->
        <div
          v-else-if="activeTab === 'billing' && selectedChildId"
          data-testid="tab-content-billing"
        >
          <BillingAndReceiptsTab
            :key="`bill-${selectedChildId}`"
            :student-id="selectedChildId"
          />
        </div>

        <!-- Tab 3: School Calendar & Bulletins -->
        <div
          v-else-if="activeTab === 'school-info'"
          data-testid="tab-content-school-info"
        >
          <SchoolInfoTab />
        </div>
      </main>
    </div>
  </div>
</template>
