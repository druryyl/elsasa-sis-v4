<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  FileText,
  History,
  Loader2,
  MessageCircle,
  Phone,
  PiggyBank,
  Printer,
  Receipt,
  Search,
  UserCheck,
  Wallet
} from 'lucide-vue-next';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { PaymentReceiptDto, StudentBillingContactDto } from '../api/payments.api';
import {
  useBillingLookupQuery,
  usePaymentReceiptDetailQuery,
  useStudentStatementQuery
} from '../api/usePaymentQueries';
import DepositAllocationModal from '../components/DepositAllocationModal.vue';
import PaymentReceiptPrintView from '../components/PaymentReceiptPrintView.vue';
import RecordPaymentModal from '../components/RecordPaymentModal.vue';
import StudentStatementDrawer from '../components/StudentStatementDrawer.vue';
import {
  buildWhatsAppHref,
  formatBillingMonth,
  formatIdr,
  formatPaymentMethod,
  getChargeStatusIntent
} from '../utils/formatters';

const PERMISSION_PAYMENTS_RECORD = 'Permissions.Finance.Payments.Record';
const PERMISSION_DEPOSITS_ALLOCATE = 'Permissions.Finance.Deposits.Allocate';
const PERMISSION_STATEMENTS_VIEW = 'Permissions.Finance.Statements.View';

const { hasPermission } = useCurrentSession();
const { selectedAcademicYearId, activeAcademicYearId, activeAcademicYearName } =
  useAppContextBridge();

const canRecordPayment = computed(() => hasPermission(PERMISSION_PAYMENTS_RECORD));
const canAllocateDeposit = computed(() => hasPermission(PERMISSION_DEPOSITS_ALLOCATE));
const canViewStatements = computed(() => hasPermission(PERMISSION_STATEMENTS_VIEW));
const hasAnyAccess = computed(
  () => canRecordPayment.value || canAllocateDeposit.value || canViewStatements.value
);

const academicYearsQuery = useQuery({
  ...academicYearsLookupQuery(),
  enabled: () => hasAnyAccess.value
});

const effectiveAcademicYearId = computed(() => {
  if (selectedAcademicYearId.value) return selectedAcademicYearId.value;
  if (activeAcademicYearId.value) return activeAcademicYearId.value;
  const years = academicYearsQuery.data.value ?? [];
  return years.find((y) => y.status === 'ACTIVE')?.id ?? years[0]?.id ?? '';
});

const effectiveAcademicYearName = computed(() => {
  if (activeAcademicYearName.value) return activeAcademicYearName.value;
  const years = academicYearsQuery.data.value ?? [];
  return years.find((y) => y.id === effectiveAcademicYearId.value)?.name ?? null;
});

const searchInput = ref('');
const debouncedSearch = ref('');
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

watch(searchInput, (nextValue) => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer);
  }
  searchDebounceTimer = setTimeout(() => {
    debouncedSearch.value = nextValue.trim();
  }, 200);
});

onBeforeUnmount(() => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer);
  }
});

const billingLookupQuery = useBillingLookupQuery(debouncedSearch, {
  enabled: () => hasAnyAccess.value
});

const billingContacts = computed(() => billingLookupQuery.data.value ?? []);
const selectedStudent = ref<StudentBillingContactDto | null>(null);

watch(billingContacts, (contacts) => {
  if (!selectedStudent.value && contacts.length > 0 && !debouncedSearch.value) {
    return;
  }
  if (selectedStudent.value?.studentId) {
    const refreshed = contacts.find((c) => c.studentId === selectedStudent.value?.studentId);
    if (refreshed) {
      selectedStudent.value = refreshed;
    }
  }
});

const selectedStudentId = computed(() => selectedStudent.value?.studentId ?? null);

const statementQuery = useStudentStatementQuery(selectedStudentId, {
  enabled: () => hasAnyAccess.value && Boolean(selectedStudentId.value)
});

const statement = computed(() => statementQuery.data.value ?? null);

const openCharges = computed(() =>
  (statement.value?.charges ?? []).filter(
    (c) => c.status === 'UNPAID' || c.status === 'PARTIALLY_PAID'
  )
);

const receipts = computed(() => statement.value?.receipts ?? []);

const receiptsWithDeposit = computed(() =>
  receipts.value.filter((r) => Number(r.unallocatedAmount ?? 0) > 0)
);

const totalDepositBalance = computed(() => Number(statement.value?.totalDepositBalance ?? 0));

const selectedStudentWhatsAppHref = computed(() =>
  buildWhatsAppHref(selectedStudent.value?.whatsAppUrl, selectedStudent.value?.phoneNumber)
);

const activeTab = ref<'charges' | 'receipts'>('charges');

const isRecordPaymentModalOpen = ref(false);
const preselectedChargeId = ref<string | null>(null);
const isDepositModalOpen = ref(false);
const isStatementDrawerOpen = ref(false);
const printingReceiptId = ref<string | null>(null);
const lastRecordedReceipt = ref<PaymentReceiptDto | null>(null);

const receiptDetailQuery = usePaymentReceiptDetailQuery(printingReceiptId, {
  enabled: () => Boolean(printingReceiptId.value)
});

function selectStudent(contact: StudentBillingContactDto): void {
  selectedStudent.value = contact;
  lastRecordedReceipt.value = null;
}

function openRecordPayment(chargeId: string | null = null): void {
  preselectedChargeId.value = chargeId;
  isRecordPaymentModalOpen.value = true;
}

function openDepositModal(): void {
  isDepositModalOpen.value = true;
}

function openStatementDrawer(): void {
  isStatementDrawerOpen.value = true;
}

function openReceiptPrint(receiptId: string): void {
  printingReceiptId.value = receiptId;
}

function closeReceiptPrint(): void {
  printingReceiptId.value = null;
}

function handlePaymentRecorded(receipt: PaymentReceiptDto): void {
  lastRecordedReceipt.value = receipt;
}
</script>

<template>
  <div class="space-y-6 p-6" data-testid="payments-workbench-view">
    <!-- Page Header -->
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Cashier Counter</h1>
        <p class="mt-1 text-sm text-slate-600">
          Search student billing profiles, record tuition payments, allocate advance deposits, and print official receipts.
        </p>
      </div>

      <div v-if="selectedStudent && hasAnyAccess" class="flex flex-wrap items-center gap-2">
        <Button
          v-if="canRecordPayment"
          type="button"
          label="Record Payment"
          :disabled="!effectiveAcademicYearId"
          data-testid="record-payment-btn"
          @click="openRecordPayment(null)"
        >
          <template #icon>
            <Wallet class="mr-1.5 h-4 w-4" />
          </template>
        </Button>

        <Button
          v-if="canAllocateDeposit"
          type="button"
          label="Apply Deposit"
          severity="secondary"
          outlined
          :disabled="totalDepositBalance <= 0 || openCharges.length === 0"
          data-testid="apply-deposit-btn"
          @click="openDepositModal"
        >
          <template #icon>
            <PiggyBank class="mr-1.5 h-4 w-4" />
          </template>
        </Button>

        <Button
          v-if="canViewStatements"
          type="button"
          label="View Statement"
          severity="secondary"
          outlined
          data-testid="view-statement-btn"
          @click="openStatementDrawer"
        >
          <template #icon>
            <FileText class="mr-1.5 h-4 w-4" />
          </template>
        </Button>
      </div>
    </div>

    <!-- Unauthorized State -->
    <EmptyState
      v-if="!hasAnyAccess"
      variant="no-permission"
      title="Access Restricted"
      description="You do not have permission to access the Cashier Counter or student financial statements."
    />

    <template v-else>
      <!-- Student & Guardian Billing Contact Search (UC-FIN-10) -->
      <section class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-slate-900">
              Student &amp; Guardian Billing Lookup
            </h2>
            <p class="text-xs text-slate-500">
              Find a student by name, NIS, study group, guardian name, or phone number.
            </p>
          </div>

          <div class="relative w-full sm:w-96">
            <Search
              class="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              v-model="searchInput"
              placeholder="Search student name, NIS, guardian, phone..."
              class="w-full pl-9"
              data-testid="billing-lookup-input"
            />
          </div>
        </div>

        <div v-if="billingLookupQuery.isLoading.value" class="mt-4 flex items-center gap-2 text-xs text-slate-500">
          <Loader2 class="h-4 w-4 animate-spin text-teal-600" />
          <span>Searching student billing profiles...</span>
        </div>

        <div
          v-else-if="billingContacts.length === 0"
          class="mt-4 rounded-lg border border-dashed border-slate-200 py-6 text-center text-xs text-slate-500"
          data-testid="billing-lookup-empty"
        >
          No matching student billing profiles found.
        </div>

        <div
          v-else
          class="mt-4 grid max-h-64 grid-cols-1 gap-2.5 overflow-y-auto md:grid-cols-2 xl:grid-cols-3"
          data-testid="billing-lookup-results"
        >
          <div
            v-for="contact in billingContacts"
            :key="contact.studentId"
            role="button"
            tabindex="0"
            :data-testid="`billing-contact-card-${contact.studentId}`"
            class="flex cursor-pointer flex-col justify-between rounded-lg border p-3 text-left transition-colors"
            :class="
              selectedStudent?.studentId === contact.studentId
                ? 'border-teal-600 bg-teal-50/50 ring-1 ring-teal-600'
                : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50'
            "
            @click="selectStudent(contact)"
            @keydown.enter.prevent="selectStudent(contact)"
          >
            <div class="flex items-start justify-between gap-2">
              <div>
                <p class="text-sm font-semibold text-slate-900">{{ contact.studentName }}</p>
                <p class="text-xs text-slate-500">
                  NIS {{ contact.studentNis || '-' }} •
                  {{ contact.studyGroupName || `${contact.schoolLevel ?? ''} ${contact.grade ?? ''}`.trim() || 'Unplaced' }}
                </p>
              </div>
              <UserCheck
                v-if="selectedStudent?.studentId === contact.studentId"
                class="h-4 w-4 shrink-0 text-teal-600"
              />
            </div>

            <div class="mt-2.5 flex flex-wrap items-center justify-between gap-2 border-t border-slate-100 pt-2 text-xs">
              <div class="text-slate-600">
                <span class="font-medium text-slate-800">
                  {{ contact.primaryGuardianName || 'No guardian on file' }}
                </span>
                <span v-if="contact.relationship" class="text-slate-400">
                  ({{ contact.relationship }})
                </span>
                <div v-if="contact.phoneNumber" class="mt-0.5 flex items-center gap-1 text-slate-500">
                  <Phone class="h-3 w-3" />
                  <span>{{ contact.phoneNumber }}</span>
                </div>
              </div>

              <a
                v-if="buildWhatsAppHref(contact.whatsAppUrl, contact.phoneNumber)"
                :href="buildWhatsAppHref(contact.whatsAppUrl, contact.phoneNumber)!"
                target="_blank"
                rel="noopener noreferrer"
                class="inline-flex items-center gap-1 rounded-md border border-emerald-200 bg-emerald-50 px-2 py-1 text-[11px] font-medium text-emerald-700 hover:bg-emerald-100"
                :data-testid="`whatsapp-link-${contact.studentId}`"
                @click.stop
              >
                <MessageCircle class="h-3.5 w-3.5" />
                <span>WhatsApp</span>
              </a>
            </div>
          </div>
        </div>
      </section>

      <!-- Prompt to select a student -->
      <EmptyState
        v-if="!selectedStudent"
        title="Select a Student to Begin"
        description="Choose a student from the billing directory above to view open charges, guardian contact info, deposit balances, and payment receipts."
      />

      <!-- Selected Student Ledger Workspace -->
      <template v-else>
        <!-- Selected Student Profile & Guardian Contact Banner -->
        <section
          class="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
          data-testid="selected-student-banner"
        >
          <div class="space-y-1">
            <div class="flex flex-wrap items-center gap-2">
              <h2 class="text-lg font-bold text-slate-900">{{ selectedStudent.studentName }}</h2>
              <span class="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                NIS: {{ selectedStudent.studentNis || '-' }}
              </span>
              <span class="rounded bg-teal-50 px-2 py-0.5 text-xs font-medium text-teal-800">
                {{ selectedStudent.schoolLevel || '-' }} • Grade {{ selectedStudent.grade || '-' }} •
                {{ selectedStudent.studyGroupName || 'Unplaced' }}
              </span>
            </div>

            <div class="flex flex-wrap items-center gap-3 text-xs text-slate-600">
              <span>
                Primary Guardian:
                <strong class="text-slate-800">
                  {{ selectedStudent.primaryGuardianName || 'Not specified' }}
                </strong>
                <span v-if="selectedStudent.relationship">
                  ({{ selectedStudent.relationship }})
                </span>
              </span>
              <span v-if="selectedStudent.phoneNumber" class="inline-flex items-center gap-1">
                <Phone class="h-3.5 w-3.5 text-slate-400" />
                {{ selectedStudent.phoneNumber }}
              </span>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <a
              v-if="selectedStudentWhatsAppHref"
              :href="selectedStudentWhatsAppHref"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1.5 rounded-lg border border-emerald-300 bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-800 hover:bg-emerald-100"
              data-testid="selected-student-whatsapp"
            >
              <MessageCircle class="h-4 w-4" />
              <span>Contact Guardian via WhatsApp</span>
            </a>
          </div>
        </section>

        <!-- Latest Posted Receipt Banner -->
        <div
          v-if="lastRecordedReceipt && lastRecordedReceipt.studentId === selectedStudent.studentId"
          class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-xs text-emerald-900"
          data-testid="latest-receipt-banner"
        >
          <div class="flex items-center gap-2">
            <Receipt class="h-4 w-4 text-emerald-700" />
            <span>
              Receipt <strong>{{ lastRecordedReceipt.receiptNumber }}</strong> ({{
                formatIdr(lastRecordedReceipt.totalAmount)
              }}) was recorded successfully.
            </span>
          </div>
          <div class="flex items-center gap-2">
            <Button
              v-if="lastRecordedReceipt.id"
              type="button"
              label="Print Receipt"
              size="small"
              data-testid="print-latest-receipt-btn"
              @click="openReceiptPrint(lastRecordedReceipt.id!)"
            >
              <template #icon>
                <Printer class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>
            <Button
              type="button"
              label="Dismiss"
              size="small"
              severity="secondary"
              text
              @click="lastRecordedReceipt = null"
            />
          </div>
        </div>

        <!-- Loading Statement -->
        <div
          v-if="statementQuery.isLoading.value"
          class="flex items-center justify-center rounded-xl border border-slate-200 bg-white py-14"
        >
          <Loader2 class="h-6 w-6 animate-spin text-teal-600" />
          <span class="ml-2 text-sm text-slate-600">Loading student ledger statement...</span>
        </div>

        <!-- Statement Error -->
        <EmptyState
          v-else-if="statementQuery.isError.value"
          variant="error"
          title="Unable to Load Student Ledger"
          description="Could not retrieve the financial statement for this student. Ensure the student has an active placement or billing record."
        />

        <!-- Financial KPIs + Tabs -->
        <template v-else-if="statement">
          <section class="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
              <p class="text-xs font-medium text-slate-500">Total Billed</p>
              <p class="mt-1 text-xl font-bold text-slate-900" data-testid="kpi-total-billed">
                {{ formatIdr(statement.totalBilled) }}
              </p>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
              <p class="text-xs font-medium text-slate-500">Total Paid</p>
              <p class="mt-1 text-xl font-bold text-emerald-700" data-testid="kpi-total-paid">
                {{ formatIdr(statement.totalPaid) }}
              </p>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
              <p class="text-xs font-medium text-slate-500">Outstanding Balance</p>
              <p class="mt-1 text-xl font-bold text-rose-700" data-testid="kpi-total-outstanding">
                {{ formatIdr(statement.totalOutstanding) }}
              </p>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
              <p class="text-xs font-medium text-slate-500">Advance Deposit Balance</p>
              <p class="mt-1 text-xl font-bold text-amber-700" data-testid="kpi-total-deposit">
                {{ formatIdr(statement.totalDepositBalance) }}
              </p>
            </div>
          </section>

          <div class="rounded-xl border border-slate-200 bg-white shadow-xs">
            <Tabs v-model:value="activeTab">
              <TabList class="border-b border-slate-200 px-4">
                <Tab value="charges" class="flex items-center gap-2 text-sm">
                  <Wallet class="h-4 w-4" />
                  <span>Open Charges ({{ openCharges.length }})</span>
                </Tab>
                <Tab value="receipts" class="flex items-center gap-2 text-sm">
                  <History class="h-4 w-4" />
                  <span>Payment Receipts ({{ receipts.length }})</span>
                </Tab>
              </TabList>

              <TabPanels class="p-4">
                <!-- Open Charges Ledger Tab -->
                <TabPanel value="charges">
                  <EmptyState
                    v-if="openCharges.length === 0"
                    title="No Open Charges"
                    description="All billed charges for this student are settled."
                  />

                  <div v-else class="overflow-x-auto rounded-lg border border-slate-200">
                    <table
                      class="min-w-full divide-y divide-slate-200 text-sm"
                      data-testid="open-charges-table"
                    >
                      <thead class="bg-slate-50 text-left text-xs font-semibold text-slate-600">
                        <tr>
                          <th class="px-4 py-3">Charge Type</th>
                          <th class="px-4 py-3">Billing Month</th>
                          <th class="px-4 py-3">Due Date</th>
                          <th class="px-4 py-3 text-right">Net Amount</th>
                          <th class="px-4 py-3 text-right">Paid</th>
                          <th class="px-4 py-3 text-right">Outstanding</th>
                          <th class="px-4 py-3">Status</th>
                          <th v-if="canRecordPayment" class="px-4 py-3 text-right">Action</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-100 bg-white">
                        <tr v-for="charge in openCharges" :key="charge.id">
                          <td class="px-4 py-3">
                            <div class="font-medium text-slate-900">{{ charge.chargeTypeName }}</div>
                            <div v-if="charge.description" class="text-xs text-slate-500">
                              {{ charge.description }}
                            </div>
                          </td>
                          <td class="px-4 py-3 text-slate-600">
                            {{ formatBillingMonth(charge.billingMonth) }}
                          </td>
                          <td class="px-4 py-3 text-slate-600">
                            {{ charge.dueDate ? formatDisplayDate(charge.dueDate) : '—' }}
                          </td>
                          <td class="px-4 py-3 text-right text-slate-700">
                            {{ formatIdr(charge.netAmount) }}
                          </td>
                          <td class="px-4 py-3 text-right text-emerald-700">
                            {{ formatIdr(charge.paidAmount) }}
                          </td>
                          <td class="px-4 py-3 text-right font-semibold text-rose-700">
                            {{ formatIdr(charge.outstandingAmount) }}
                          </td>
                          <td class="px-4 py-3">
                            <StatusPill
                              :status="charge.status ?? 'UNPAID'"
                              :intent="getChargeStatusIntent(charge.status)"
                            />
                          </td>
                          <td v-if="canRecordPayment" class="px-4 py-3 text-right">
                            <Button
                              type="button"
                              label="Pay"
                              size="small"
                              outlined
                              :disabled="!effectiveAcademicYearId"
                              :data-testid="`pay-charge-btn-${charge.id}`"
                              @click="openRecordPayment(charge.id ?? null)"
                            />
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </TabPanel>

                <!-- Historical Payment Receipts Tab -->
                <TabPanel value="receipts">
                  <EmptyState
                    v-if="receipts.length === 0"
                    title="No Payment Receipts"
                    description="No payments have been recorded for this student yet."
                  />

                  <div v-else class="overflow-x-auto rounded-lg border border-slate-200">
                    <table
                      class="min-w-full divide-y divide-slate-200 text-sm"
                      data-testid="payment-receipts-table"
                    >
                      <thead class="bg-slate-50 text-left text-xs font-semibold text-slate-600">
                        <tr>
                          <th class="px-4 py-3">Receipt No.</th>
                          <th class="px-4 py-3">Payment Date</th>
                          <th class="px-4 py-3">Method</th>
                          <th class="px-4 py-3 text-right">Total Paid</th>
                          <th class="px-4 py-3 text-right">Allocated</th>
                          <th class="px-4 py-3 text-right">Deposit Held</th>
                          <th class="px-4 py-3">Cashier</th>
                          <th class="px-4 py-3 text-right">Action</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-100 bg-white">
                        <tr v-for="receipt in receipts" :key="receipt.id">
                          <td class="px-4 py-3 font-semibold text-slate-900">
                            {{ receipt.receiptNumber }}
                          </td>
                          <td class="px-4 py-3 text-slate-600">
                            {{ formatDisplayDate(receipt.paymentDate) }}
                          </td>
                          <td class="px-4 py-3">
                            <div class="text-slate-800">
                              {{ formatPaymentMethod(receipt.paymentMethod) }}
                            </div>
                            <div v-if="receipt.bankReference" class="text-xs text-slate-500">
                              Ref: {{ receipt.bankReference }}
                            </div>
                          </td>
                          <td class="px-4 py-3 text-right font-semibold text-slate-900">
                            {{ formatIdr(receipt.totalAmount) }}
                          </td>
                          <td class="px-4 py-3 text-right text-emerald-700">
                            {{ formatIdr(receipt.allocatedAmount) }}
                          </td>
                          <td
                            class="px-4 py-3 text-right font-medium"
                            :class="
                              (receipt.unallocatedAmount ?? 0) > 0
                                ? 'text-amber-700'
                                : 'text-slate-400'
                            "
                          >
                            {{ formatIdr(receipt.unallocatedAmount) }}
                          </td>
                          <td class="px-4 py-3 text-slate-600">
                            {{ receipt.receivedByName || '—' }}
                          </td>
                          <td class="px-4 py-3 text-right">
                            <Button
                              v-if="receipt.id"
                              type="button"
                              label="Print Receipt"
                              size="small"
                              severity="secondary"
                              outlined
                              :data-testid="`print-receipt-btn-${receipt.id}`"
                              @click="openReceiptPrint(receipt.id!)"
                            >
                              <template #icon>
                                <Printer class="mr-1.5 h-3.5 w-3.5" />
                              </template>
                            </Button>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </TabPanel>
              </TabPanels>
            </Tabs>
          </div>
        </template>
      </template>
    </template>

    <!-- Modals, Statement Drawer & Print Preview -->
    <RecordPaymentModal
      v-model:visible="isRecordPaymentModalOpen"
      :student="selectedStudent"
      :academic-year-id="effectiveAcademicYearId"
      :academic-year-name="effectiveAcademicYearName"
      :open-charges="openCharges"
      :preselected-charge-id="preselectedChargeId"
      @recorded="handlePaymentRecorded"
    />

    <DepositAllocationModal
      v-model:visible="isDepositModalOpen"
      :student="selectedStudent"
      :receipts-with-deposit="receiptsWithDeposit"
      :open-charges="openCharges"
      :total-deposit-balance="totalDepositBalance"
    />

    <StudentStatementDrawer
      v-model:visible="isStatementDrawerOpen"
      :statement="statement"
      :student="selectedStudent"
      :loading="statementQuery.isLoading.value"
      @print-receipt="openReceiptPrint"
    />

    <PaymentReceiptPrintView
      v-if="printingReceiptId"
      :receipt="receiptDetailQuery.data.value ?? null"
      :remaining-outstanding-balance="statement?.totalOutstanding ?? null"
      :loading="receiptDetailQuery.isLoading.value"
      @close="closeReceiptPrint"
    />
  </div>
</template>
