<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useQuery } from '@tanstack/vue-query';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  ExternalLink,
  GraduationCap,
  Mail,
  MapPin,
  Phone,
  Search,
  UserCheck,
  UserPlus,
  Users
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import WorkbenchLayout from '@/shared/components/layouts/WorkbenchLayout.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import type { CandidateSummaryDto } from '../api/admissions.api';
import { useCandidateDetailQuery, useCandidatesQuery } from '../api/useAdmissionsQueries';
import AdmissionDecisionModal from '../components/AdmissionDecisionModal.vue';
import EnrollCandidateModal from '../components/EnrollCandidateModal.vue';
import RegisterCandidateDrawer from '../components/RegisterCandidateDrawer.vue';

const route = useRoute();
const router = useRouter();
const contextBridge = useAppContextBridge();
const session = useCurrentSession();

// Permissions gating
const canRegister = computed(() =>
  session.hasPermission('Permissions.Students.Admissions.Register')
);
const canDecideOrApprove = computed(
  () =>
    session.hasPermission('Permissions.Students.Admissions.Decide') ||
    session.hasPermission('Permissions.Students.Admissions.Approve')
);
const canEnroll = computed(() =>
  session.hasPermission('Permissions.Students.Admissions.Enroll')
);

// Status tabs
const STATUS_TABS = ['REGISTERED', 'ACCEPTED', 'REJECTED', 'ENROLLED'] as const;
type AdmissionStatusTab = (typeof STATUS_TABS)[number];

const currentStatus = ref<AdmissionStatusTab>(
  (route.query.status as AdmissionStatusTab) &&
    STATUS_TABS.includes(route.query.status as AdmissionStatusTab)
    ? (route.query.status as AdmissionStatusTab)
    : 'REGISTERED'
);

const selectedCandidateId = ref<string | null>(
  (route.query.candidateId as string) || null
);

const searchInput = ref('');
const selectedYearId = ref<string>(
  contextBridge.activeAcademicYearId.value ?? ''
);

// Watch for bridge active year becoming available
watch(
  () => contextBridge.activeAcademicYearId.value,
  (activeId) => {
    if (activeId && !selectedYearId.value) {
      selectedYearId.value = activeId;
    }
  },
  { immediate: true }
);

// Load academic years lookup
const { data: academicYears = [] } = useQuery(academicYearsLookupQuery());

// Sync URL Query
watch([currentStatus, selectedCandidateId], ([status, candidateId]) => {
  const newQuery: Record<string, string> = {
    ...route.query,
    status
  };
  if (candidateId) {
    newQuery.candidateId = candidateId;
  } else {
    delete newQuery.candidateId;
  }

  // Avoid redundant navigation
  if (
    route.query.status !== newQuery.status ||
    route.query.candidateId !== newQuery.candidateId
  ) {
    void router.replace({ query: newQuery });
  }
});

// Watch route changes from browser back/forward
watch(
  () => route.query,
  (q) => {
    if (q.status && STATUS_TABS.includes(q.status as AdmissionStatusTab)) {
      currentStatus.value = q.status as AdmissionStatusTab;
    }
    selectedCandidateId.value = (q.candidateId as string) || null;
  }
);

// Query candidates
const candidatesQuery = useCandidatesQuery(
  computed(() => ({
    status: currentStatus.value,
    admissionYearId: selectedYearId.value || undefined,
    search: searchInput.value.trim() || undefined,
    pageSize: 100
  }))
);

const candidateList = computed<CandidateSummaryDto[]>(
  () => candidatesQuery.data.value?.items ?? []
);

// Query selected candidate detail
const candidateDetailQuery = useCandidateDetailQuery(selectedCandidateId);
const selectedCandidate = computed(
  () => candidateDetailQuery.data.value ?? null
);

// Fallback candidate from list while detail is loading
const currentCandidateSummary = computed<CandidateSummaryDto | null>(() => {
  if (!selectedCandidateId.value) return null;
  return (
    candidateList.value.find((c) => c.id === selectedCandidateId.value) ?? null
  );
});

// Card selection
function selectCandidate(candidate: CandidateSummaryDto): void {
  selectedCandidateId.value = candidate.id ?? null;
}

function clearSelection(): void {
  selectedCandidateId.value = null;
}

function setStatusTab(tab: AdmissionStatusTab): void {
  currentStatus.value = tab;
  // If the currently selected candidate is not in the new tab's list, clear selection
  if (
    selectedCandidate.value &&
    selectedCandidate.value.admissionStatus !== tab
  ) {
    selectedCandidateId.value = null;
  }
}

// Modals state
const showRegisterDrawer = ref(false);
const showDecisionModal = ref(false);
const showEnrollModal = ref(false);

function openRegisterDrawer(): void {
  showRegisterDrawer.value = true;
}

function openDecisionModal(): void {
  showDecisionModal.value = true;
}

function openEnrollModal(): void {
  showEnrollModal.value = true;
}

function onCandidateRegistered(newCandidate: { id?: string }): void {
  if (newCandidate.id) {
    currentStatus.value = 'REGISTERED';
    selectedCandidateId.value = newCandidate.id;
  }
}

function navigateToStudentDirectory(studentId?: string | null): void {
  if (!studentId) return;
  void router.push({
    name: 'students.directory',
    query: { studentId }
  });
}
</script>

<template>
  <AppErrorBoundary>
    <WorkbenchLayout
      title="Admissions Processing Queue"
      subtitle="Register prospective candidates, review admissions, and enroll accepted students."
      :has-selection="Boolean(selectedCandidateId)"
      @back="clearSelection"
    >
      <!-- Page Actions: Register Candidate -->
      <template #actions>
        <Button
          v-if="canRegister"
          type="button"
          data-testid="register-candidate-btn"
          class="inline-flex items-center gap-1.5"
          @click="openRegisterDrawer"
        >
          <UserPlus class="h-4 w-4" />
          <span>Register Candidate</span>
        </Button>
      </template>

      <!-- Left Worklist Pane Header: Tabs + Filters -->
      <template #worklist-header>
        <div class="space-y-3">
          <!-- Status Tabs -->
          <div
            role="tablist"
            data-testid="admissions-status-tabs"
            class="flex rounded-lg bg-slate-100 p-1 text-xs font-medium"
          >
            <button
              v-for="tab in STATUS_TABS"
              :key="tab"
              type="button"
              role="tab"
              :aria-selected="currentStatus === tab"
              :data-testid="`tab-${tab.toLowerCase()}`"
              class="flex-1 rounded-md py-1.5 text-center transition-all"
              :class="
                currentStatus === tab
                  ? 'bg-white font-semibold text-brand-accent-600 shadow-xs'
                  : 'text-slate-600 hover:text-slate-900'
              "
              @click="setStatusTab(tab)"
            >
              {{ tab }}
            </button>
          </div>

          <!-- Admission Year & Search Filter -->
          <div class="flex flex-col gap-2">
            <select
              v-model="selectedYearId"
              data-testid="queue-year-filter"
              class="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-xs text-slate-800 focus:border-brand-accent-600 focus:outline-none focus:ring-1 focus:ring-brand-accent-600"
            >
              <option value="">All Academic Years</option>
              <option
                v-for="ay in academicYears"
                :key="ay.id"
                :value="ay.id"
              >
                {{ ay.name }} ({{ ay.status }})
              </option>
            </select>

            <div class="relative">
              <Search
                class="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400"
              />
              <InputText
                v-model="searchInput"
                data-testid="queue-search-input"
                placeholder="Search candidate name..."
                class="w-full pl-8 text-xs py-1.5"
              />
            </div>
          </div>
        </div>
      </template>

      <!-- Left Worklist Cards (scannable <= 4 elements) -->
      <template #worklist>
        <div class="space-y-2">
          <!-- Loading state -->
          <div
            v-if="candidatesQuery.isLoading.value"
            class="p-4 text-center text-xs text-slate-500"
          >
            Loading admissions queue...
          </div>

          <!-- Empty list state -->
          <div
            v-else-if="candidateList.length === 0"
            data-testid="queue-empty-list"
            class="rounded-lg border border-dashed border-slate-200 p-6 text-center"
          >
            <p class="text-xs font-medium text-slate-600">No candidates found</p>
            <p class="mt-1 text-[11px] text-slate-400">
              No applications match status "{{ currentStatus }}"
            </p>
          </div>

          <!-- Scannable <= 4-element Cards -->
          <div
            v-for="c in candidateList"
            :key="c.id"
            role="button"
            tabindex="0"
            data-testid="candidate-card"
            :data-candidate-id="c.id"
            class="group cursor-pointer rounded-lg border p-3 transition-all"
            :class="
              selectedCandidateId === c.id
                ? 'border-brand-accent-600 bg-brand-accent-50/20 ring-1 ring-brand-accent-600'
                : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50/60'
            "
            @click="selectCandidate(c)"
            @keydown.enter="selectCandidate(c)"
            @keydown.space.prevent="selectCandidate(c)"
          >
            <div class="flex items-start justify-between gap-2">
              <!-- Element 1: Full Name + Nickname -->
              <div class="min-w-0">
                <h4
                  data-testid="candidate-card-name"
                  class="truncate text-sm font-semibold text-slate-900 group-hover:text-brand-accent-600"
                >
                  {{ c.fullName }}
                </h4>
                <p v-if="c.nickname" class="text-xs text-slate-500">
                  ({{ c.nickname }})
                </p>
              </div>

              <!-- Element 4: Status Pill -->
              <StatusPill
                v-if="c.admissionStatus"
                :status="c.admissionStatus"
                class="shrink-0 text-[10px]"
              />
            </div>

            <div class="mt-2.5 flex items-center justify-between text-xs text-slate-500">
              <!-- Element 2: Target Level & Grade Badge -->
              <span class="inline-flex items-center rounded-md bg-slate-100 px-2 py-0.5 font-medium text-slate-700">
                {{ c.targetSchoolLevel }} • Grade {{ c.targetGrade }}
              </span>

              <!-- Element 3: Registered Date -->
              <span class="text-[11px] text-slate-400">
                {{ c.registeredAt ? new Date(c.registeredAt).toLocaleDateString() : '' }}
              </span>
            </div>
          </div>
        </div>
      </template>

      <!-- Worklist Footer: Count -->
      <template #worklist-footer>
        <div class="flex items-center justify-between text-xs text-slate-500">
          <span>Queue Count:</span>
          <span class="font-medium text-slate-700" data-testid="queue-count">
            {{ candidateList.length }} candidate{{ candidateList.length === 1 ? '' : 's' }}
          </span>
        </div>
      </template>

      <!-- Right Workbench Detail Canvas -->
      <template #workbench-header>
        <div
          v-if="selectedCandidate || currentCandidateSummary"
          class="flex w-full flex-wrap items-center justify-between gap-3"
        >
          <div class="flex items-center gap-2.5">
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-700 font-semibold text-sm">
              {{ (selectedCandidate?.fullName || currentCandidateSummary?.fullName || 'C').charAt(0) }}
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h2
                  data-testid="workbench-candidate-name"
                  class="text-base font-semibold text-slate-900"
                >
                  {{ selectedCandidate?.fullName || currentCandidateSummary?.fullName }}
                </h2>
                <span
                  v-if="selectedCandidate?.nickname || currentCandidateSummary?.nickname"
                  class="text-xs text-slate-500"
                >
                  ({{ selectedCandidate?.nickname || currentCandidateSummary?.nickname }})
                </span>
                <StatusPill
                  v-if="selectedCandidate?.admissionStatus || currentCandidateSummary?.admissionStatus"
                  :status="selectedCandidate?.admissionStatus || currentCandidateSummary!.admissionStatus!"
                />
              </div>
              <p class="text-xs text-slate-500">
                Target: {{ selectedCandidate?.targetSchoolLevel || currentCandidateSummary?.targetSchoolLevel }}
                Grade {{ selectedCandidate?.targetGrade || currentCandidateSummary?.targetGrade }}
                <span v-if="selectedCandidate?.admissionYearName || currentCandidateSummary?.admissionYearName">
                  • {{ selectedCandidate?.admissionYearName || currentCandidateSummary?.admissionYearName }}
                </span>
              </p>
            </div>
          </div>

          <!-- Contextual Actions -->
          <div class="flex items-center gap-2">
            <!-- REGISTERED: Decision Review -->
            <Button
              v-if="
                (selectedCandidate?.admissionStatus || currentCandidateSummary?.admissionStatus) === 'REGISTERED' &&
                canDecideOrApprove
              "
              type="button"
              severity="secondary"
              size="small"
              data-testid="workbench-decision-btn"
              class="inline-flex items-center gap-1.5"
              @click="openDecisionModal"
            >
              <UserCheck class="h-4 w-4 text-emerald-600" />
              <span>Record Decision</span>
            </Button>

            <!-- ACCEPTED: Enroll Student -->
            <Button
              v-if="
                (selectedCandidate?.admissionStatus || currentCandidateSummary?.admissionStatus) === 'ACCEPTED' &&
                canEnroll
              "
              type="button"
              severity="success"
              size="small"
              data-testid="workbench-enroll-btn"
              class="inline-flex items-center gap-1.5"
              @click="openEnrollModal"
            >
              <GraduationCap class="h-4 w-4" />
              <span>Enroll Student</span>
            </Button>

            <!-- ENROLLED: Post-Enrollment Deep Link Button -->
            <Button
              v-if="
                (selectedCandidate?.admissionStatus || currentCandidateSummary?.admissionStatus) === 'ENROLLED' &&
                (selectedCandidate?.enrolledStudentId || currentCandidateSummary?.enrolledStudentId)
              "
              type="button"
              severity="primary"
              size="small"
              data-testid="workbench-view-directory-btn"
              class="inline-flex items-center gap-1.5"
              @click="
                navigateToStudentDirectory(
                  selectedCandidate?.enrolledStudentId || currentCandidateSummary?.enrolledStudentId
                )
              "
            >
              <ExternalLink class="h-4 w-4" />
              <span>View in Student Directory</span>
            </Button>
          </div>
        </div>
      </template>

      <!-- Right Workbench Body -->
      <div v-if="!selectedCandidateId" class="flex h-full items-center justify-center p-8">
        <EmptyState
          title="No Candidate Selected"
          description="Select a candidate from the admissions queue on the left to review demographics, inspect guardian contacts, record admission decisions, or enroll student."
        />
      </div>

      <div
        v-else-if="candidateDetailQuery.isLoading.value && !selectedCandidate"
        class="flex h-full items-center justify-center p-8 text-sm text-slate-500"
      >
        Loading candidate details...
      </div>

      <div v-else class="space-y-6 p-6 overflow-y-auto">
        <!-- Section 1: Demographics Card -->
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs space-y-4">
          <h3 class="text-sm font-semibold text-slate-900 border-b border-slate-100 pb-2.5">
            Personal Demographics
          </h3>

          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-3 text-xs">
            <div>
              <span class="text-slate-500">Date of Birth</span>
              <p class="mt-0.5 font-medium text-slate-800">
                {{ selectedCandidate?.dateOfBirth ? new Date(selectedCandidate.dateOfBirth).toLocaleDateString() : '-' }}
              </p>
            </div>

            <div>
              <span class="text-slate-500">Gender</span>
              <p class="mt-0.5 font-medium text-slate-800">
                {{ selectedCandidate?.gender === 'MALE' ? 'Male (Laki-laki)' : selectedCandidate?.gender === 'FEMALE' ? 'Female (Perempuan)' : '-' }}
              </p>
            </div>

            <div>
              <span class="text-slate-500">Phone</span>
              <p class="mt-0.5 flex items-center gap-1 font-medium text-slate-800">
                <Phone v-if="selectedCandidate?.phone" class="h-3 w-3 text-slate-400" />
                {{ selectedCandidate?.phone || '-' }}
              </p>
            </div>

            <div>
              <span class="text-slate-500">Email</span>
              <p class="mt-0.5 flex items-center gap-1 font-medium text-slate-800 truncate">
                <Mail v-if="selectedCandidate?.email" class="h-3 w-3 text-slate-400" />
                {{ selectedCandidate?.email || '-' }}
              </p>
            </div>

            <div>
              <span class="text-slate-500">Registered At</span>
              <p class="mt-0.5 font-medium text-slate-800">
                {{ selectedCandidate?.registeredAt ? new Date(selectedCandidate.registeredAt).toLocaleString() : '-' }}
              </p>
            </div>

            <div class="sm:col-span-2 md:col-span-3">
              <span class="text-slate-500">Address</span>
              <p class="mt-0.5 flex items-start gap-1 font-medium text-slate-800">
                <MapPin v-if="selectedCandidate?.address" class="mt-0.5 h-3 w-3 shrink-0 text-slate-400" />
                <span>{{ selectedCandidate?.address || '-' }}</span>
              </p>
            </div>
          </div>
        </div>

        <!-- Section 2: Guardian Contact Cards -->
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs space-y-4">
          <div class="flex items-center justify-between border-b border-slate-100 pb-2.5">
            <div class="flex items-center gap-2">
              <Users class="h-4 w-4 text-brand-accent-600" />
              <h3 class="text-sm font-semibold text-slate-900">Guardian Contacts</h3>
            </div>
            <span class="text-xs text-slate-500">
              {{ (selectedCandidate?.guardians?.length || 0) }} contact{{ (selectedCandidate?.guardians?.length || 0) === 1 ? '' : 's' }}
            </span>
          </div>

          <div
            v-if="!selectedCandidate?.guardians || selectedCandidate.guardians.length === 0"
            class="text-xs text-slate-400 italic"
          >
            No guardian contacts recorded.
          </div>

          <div v-else class="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div
              v-for="g in selectedCandidate.guardians"
              :key="g.id"
              data-testid="guardian-card"
              class="rounded-lg border border-slate-200 bg-slate-50/60 p-3.5 space-y-2 text-xs"
            >
              <div class="flex items-center justify-between">
                <h4 class="font-semibold text-slate-900">{{ g.fullName }}</h4>
                <span class="rounded-full bg-slate-200 px-2 py-0.5 text-[10px] font-medium text-slate-700">
                  {{ g.relationshipType }}
                </span>
              </div>

              <div class="space-y-1 text-slate-600">
                <p v-if="g.phone" class="flex items-center gap-1.5">
                  <Phone class="h-3 w-3 text-slate-400" />
                  <span>{{ g.phone }}</span>
                </p>
                <p v-if="g.email" class="flex items-center gap-1.5 truncate">
                  <Mail class="h-3 w-3 text-slate-400" />
                  <span>{{ g.email }}</span>
                </p>
                <p v-if="g.address" class="flex items-start gap-1.5">
                  <MapPin class="mt-0.5 h-3 w-3 shrink-0 text-slate-400" />
                  <span>{{ g.address }}</span>
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Section 3: Decision & Enrollment Audit -->
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs space-y-4">
          <h3 class="text-sm font-semibold text-slate-900 border-b border-slate-100 pb-2.5">
            Admission & Enrollment Audit
          </h3>

          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 text-xs">
            <div>
              <span class="text-slate-500">Admission Status</span>
              <div class="mt-1">
                <StatusPill
                  v-if="selectedCandidate?.admissionStatus"
                  :status="selectedCandidate.admissionStatus"
                />
              </div>
            </div>

            <div>
              <span class="text-slate-500">Decided At</span>
              <p class="mt-1 font-medium text-slate-800">
                {{ selectedCandidate?.decidedAt ? new Date(selectedCandidate.decidedAt).toLocaleString() : 'Pending Decision' }}
              </p>
            </div>

            <div v-if="selectedCandidate?.decisionNotes" class="sm:col-span-2">
              <span class="text-slate-500">Decision Notes</span>
              <div
                data-testid="workbench-decision-notes"
                class="mt-1 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs leading-relaxed text-slate-700 italic"
              >
                "{{ selectedCandidate.decisionNotes }}"
              </div>
            </div>

            <div
              v-if="selectedCandidate?.admissionStatus === 'ENROLLED'"
              class="sm:col-span-2 rounded-lg border border-emerald-200 bg-emerald-50/60 p-3.5 space-y-2"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <GraduationCap class="h-4 w-4 text-emerald-600" />
                  <span class="font-semibold text-emerald-950">Official Student Record Created</span>
                </div>
                <Button
                  v-if="selectedCandidate.enrolledStudentId"
                  severity="success"
                  size="small"
                  text
                  data-testid="audit-view-directory-btn"
                  @click="navigateToStudentDirectory(selectedCandidate.enrolledStudentId)"
                >
                  <ExternalLink class="h-3.5 w-3.5 mr-1" />
                  View Profile
                </Button>
              </div>
              <p class="text-emerald-800">
                Student ID: <code class="rounded bg-emerald-100 px-1.5 py-0.5 font-mono text-[11px] text-emerald-900">{{ selectedCandidate.enrolledStudentId }}</code>
              </p>
            </div>
          </div>
        </div>
      </div>
    </WorkbenchLayout>

    <!-- Modals & Drawers -->
    <RegisterCandidateDrawer
      v-model:visible="showRegisterDrawer"
      :default-admission-year-id="selectedYearId"
      @registered="onCandidateRegistered"
    />

    <AdmissionDecisionModal
      v-model:visible="showDecisionModal"
      :candidate="selectedCandidate || currentCandidateSummary"
    />

    <EnrollCandidateModal
      v-model:visible="showEnrollModal"
      :candidate="selectedCandidate || currentCandidateSummary"
      @enrolled="
        (res) => {
          if (res.id) {
            currentStatus = 'ENROLLED';
          }
        }
      "
    />
  </AppErrorBoundary>
</template>
