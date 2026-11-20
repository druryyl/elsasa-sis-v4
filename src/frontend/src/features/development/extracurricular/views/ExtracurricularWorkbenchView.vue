<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Skeleton from 'primevue/skeleton';
import { Award, FolderOpen, Pencil, Plus, Search, ShieldCheck, UserCheck, Users } from 'lucide-vue-next';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import type {
  ExtracurricularActivityDto,
  ExtracurricularParticipationDto
} from '../api/extracurricular.api';
import {
  useExtracurricularActivitiesQuery,
  useExtracurricularParticipantsQuery
} from '../api/useExtracurricularQueries';
import ActivityDetailDrawer from '../components/ActivityDetailDrawer.vue';
import ActivityEditorModal from '../components/ActivityEditorModal.vue';
import EnrollmentModal from '../components/EnrollmentModal.vue';
import SessionLogModal from '../components/SessionLogModal.vue';
import { isActivitySupervisor } from '../utils/supervisorAuth';

const { currentContextLabel } = useAppContextBridge();
const { user, teacherId, hasPermission } = useCurrentSession();

const canManage = computed(() =>
  hasPermission('Permissions.Development.Extracurricular.Manage')
);
const canLogSession = computed(() =>
  hasPermission('Permissions.Development.Extracurricular.Log')
);

const searchTerm = ref('');
const statusFilter = ref<'ALL' | 'ACTIVE' | 'INACTIVE'>('ALL');
const onlyMySupervised = ref(false);

const selectedActivityId = ref<string | null>(null);
const isDetailDrawerOpen = ref(false);
const isEditorModalOpen = ref(false);
const editingActivity = ref<ExtracurricularActivityDto | null>(null);
const isEnrollmentModalOpen = ref(false);
const isSessionLogModalOpen = ref(false);
const activeParticipation = ref<ExtracurricularParticipationDto | null>(null);

const activitiesQuery = useExtracurricularActivitiesQuery(
  () => ({
    status: statusFilter.value === 'ALL' ? undefined : statusFilter.value
  }),
  { enabled: () => canManage.value || canLogSession.value }
);

const allActivities = computed(() => activitiesQuery.data.value ?? []);

const filteredActivities = computed(() => {
  const term = searchTerm.value.trim().toLowerCase();
  return allActivities.value.filter((club) => {
    if (onlyMySupervised.value && !checkIsSupervisor(club)) {
      return false;
    }
    if (!term) {
      return true;
    }
    return (
      (club.name ?? '').toLowerCase().includes(term) ||
      (club.description ?? '').toLowerCase().includes(term) ||
      (club.supervisorName ?? '').toLowerCase().includes(term)
    );
  });
});

const selectedActivity = computed<ExtracurricularActivityDto | null>(
  () => allActivities.value.find((a) => a.id === selectedActivityId.value) ?? null
);

const selectedParticipantsQuery = useExtracurricularParticipantsQuery(
  () => selectedActivity.value?.id,
  {},
  { enabled: () => isEnrollmentModalOpen.value && Boolean(selectedActivity.value?.id) }
);

const existingActiveStudentIds = computed(() =>
  (selectedParticipantsQuery.data.value ?? [])
    .filter((p) => p.status === 'ACTIVE' && Boolean(p.studentId))
    .map((p) => p.studentId!)
);

const totalClubsCount = computed(() => allActivities.value.length);
const activeClubsCount = computed(
  () => allActivities.value.filter((a) => a.status === 'ACTIVE').length
);
const totalActiveMembers = computed(() =>
  allActivities.value.reduce((acc, a) => acc + (a.activeParticipantCount ?? 0), 0)
);

function checkIsSupervisor(activity: ExtracurricularActivityDto | null): boolean {
  return isActivitySupervisor(activity, user.value?.id, teacherId.value);
}

function openCreateClub(): void {
  editingActivity.value = null;
  isEditorModalOpen.value = true;
}

function openEditClub(activity: ExtracurricularActivityDto): void {
  editingActivity.value = activity;
  isEditorModalOpen.value = true;
}

function openDetailDrawer(activity: ExtracurricularActivityDto): void {
  if (!activity.id) return;
  selectedActivityId.value = activity.id;
  isDetailDrawerOpen.value = true;
}

function openEnrollModal(activity: ExtracurricularActivityDto): void {
  if (!activity.id) return;
  selectedActivityId.value = activity.id;
  isEnrollmentModalOpen.value = true;
}

function openSessionLogModal(participation: ExtracurricularParticipationDto): void {
  if (!canLogSession.value || !checkIsSupervisor(selectedActivity.value)) {
    return;
  }
  activeParticipation.value = participation;
  isSessionLogModalOpen.value = true;
}
</script>

<template>
  <div class="space-y-6 p-4 sm:p-6" data-testid="extracurricular-workbench">
    <!-- Page Header -->
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div class="space-y-1">
        <div class="flex flex-wrap items-center gap-2.5">
          <h1 class="text-xl font-semibold tracking-tight text-slate-900">
            Extracurricular Clubs
          </h1>
          <span
            class="inline-flex items-center rounded-full border border-slate-200 bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600"
          >
            {{ currentContextLabel }}
          </span>
        </div>
        <p class="text-sm text-slate-600">
          Manage the extracurricular catalog, supervisor assignments, student enrollments, and session logs.
        </p>
      </div>

      <Button
        v-if="canManage"
        label="New Club"
        data-testid="new-club-button"
        @click="openCreateClub"
      >
        <template #icon>
          <Plus class="mr-1.5 h-4 w-4" />
        </template>
      </Button>
    </div>

    <!-- Summary KPI Cards -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-slate-500">Total Clubs</span>
          <Award class="h-4 w-4 text-slate-400" />
        </div>
        <p class="mt-2 text-2xl font-semibold text-slate-900">{{ totalClubsCount }}</p>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-slate-500">Active Clubs</span>
          <ShieldCheck class="h-4 w-4 text-emerald-500" />
        </div>
        <p class="mt-2 text-2xl font-semibold text-slate-900">{{ activeClubsCount }}</p>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-4">
        <div class="flex items-center justify-between">
          <span class="text-xs font-medium text-slate-500">Active Memberships</span>
          <Users class="h-4 w-4 text-brand-accent-500" />
        </div>
        <p class="mt-2 text-2xl font-semibold text-slate-900">{{ totalActiveMembers }}</p>
      </div>
    </div>

    <!-- Search & Filter Toolbar -->
    <div
      class="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <div class="relative flex-1 sm:max-w-sm">
        <Search
          class="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
        />
        <InputText
          v-model="searchTerm"
          type="text"
          placeholder="Search clubs or supervisors..."
          class="w-full pl-9"
          data-testid="club-search-input"
        />
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <div class="flex items-center gap-1 rounded-lg border border-slate-200 bg-slate-50 p-1">
          <button
            v-for="status in ['ALL', 'ACTIVE', 'INACTIVE'] as const"
            :key="status"
            type="button"
            class="rounded-md px-3 py-1 text-xs font-medium transition-colors"
            :class="
              statusFilter === status
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            "
            :data-testid="`status-filter-${status.toLowerCase()}`"
            @click="statusFilter = status"
          >
            {{ status === 'ALL' ? 'All' : status === 'ACTIVE' ? 'Active' : 'Inactive' }}
          </button>
        </div>

        <button
          v-if="teacherId || canLogSession"
          type="button"
          class="inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs font-medium transition-colors"
          :class="
            onlyMySupervised
              ? 'border-brand-accent-600 bg-brand-accent-50 text-brand-accent-700'
              : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
          "
          data-testid="my-supervised-toggle"
          @click="onlyMySupervised = !onlyMySupervised"
        >
          <UserCheck class="h-3.5 w-3.5" />
          <span>My Supervised Clubs</span>
        </button>
      </div>
    </div>

    <!-- Loading Skeletons -->
    <div
      v-if="activitiesQuery.isLoading.value"
      class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3"
    >
      <div
        v-for="idx in 6"
        :key="idx"
        class="space-y-3 rounded-xl border border-slate-200 bg-white p-5"
      >
        <Skeleton width="60%" height="1.25rem" />
        <Skeleton width="100%" height="2.5rem" />
        <Skeleton width="45%" height="1rem" />
      </div>
    </div>

    <!-- Error State -->
    <EmptyState
      v-else-if="activitiesQuery.isError.value"
      title="Failed to load extracurricular clubs"
      description="There was a problem loading the activity catalog. Please try again."
      variant="error"
    >
      <Button label="Retry" size="small" @click="activitiesQuery.refetch()" />
    </EmptyState>

    <!-- Empty Catalog State -->
    <EmptyState
      v-else-if="filteredActivities.length === 0"
      :title="
        allActivities.length === 0
          ? 'No extracurricular clubs yet'
          : 'No clubs match your filter'
      "
      :description="
        allActivities.length === 0
          ? 'Create your first extracurricular activity and assign a teacher supervisor.'
          : 'Try clearing your search term or switching the status filter.'
      "
      :variant="allActivities.length === 0 ? 'empty' : 'no-results'"
    >
      <Button
        v-if="allActivities.length === 0 && canManage"
        label="New Club"
        size="small"
        @click="openCreateClub"
      />
    </EmptyState>

    <!-- Activity Catalog Grid -->
    <div
      v-else
      class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3"
      data-testid="activity-catalog-grid"
    >
      <article
        v-for="activity in filteredActivities"
        :key="activity.id"
        :data-testid="`activity-card-${activity.id}`"
        class="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 transition-shadow hover:shadow-md"
      >
        <div class="space-y-3">
          <div class="flex items-start justify-between gap-2">
            <div class="min-w-0 space-y-1">
              <h2 class="truncate text-base font-semibold text-slate-900">
                {{ activity.name }}
              </h2>
              <div class="flex flex-wrap items-center gap-1.5">
                <StatusPill v-if="activity.status" :status="activity.status" />
                <span
                  v-if="checkIsSupervisor(activity)"
                  class="inline-flex items-center gap-1 rounded-full border border-sky-200 bg-sky-50 px-2 py-0.5 text-[11px] font-medium text-sky-700"
                >
                  Supervised by You
                </span>
              </div>
            </div>

            <Button
              v-if="canManage"
              title="Edit Club"
              size="small"
              severity="secondary"
              text
              rounded
              aria-label="Edit Club"
              :data-testid="`edit-club-button-${activity.id}`"
              @click="openEditClub(activity)"
            >
              <template #icon>
                <Pencil class="h-4 w-4" />
              </template>
            </Button>
          </div>

          <p class="line-clamp-2 min-h-10 text-sm text-slate-600">
            {{ activity.description || 'No description provided.' }}
          </p>
        </div>

        <div class="mt-4 space-y-3 border-t border-slate-100 pt-3">
          <div class="flex items-center justify-between text-xs text-slate-600">
            <span class="inline-flex items-center gap-1.5 truncate">
              <UserCheck class="h-3.5 w-3.5 shrink-0 text-slate-400" />
              <span class="truncate">{{ activity.supervisorName ?? 'Unassigned' }}</span>
            </span>

            <span class="inline-flex shrink-0 items-center gap-1 font-medium text-slate-700">
              <Users class="h-3.5 w-3.5 text-slate-400" />
              <span>{{ activity.activeParticipantCount ?? 0 }} active</span>
            </span>
          </div>

          <Button
            size="small"
            severity="secondary"
            outlined
            class="w-full justify-center"
            label="View Roster & Logs"
            :data-testid="`view-roster-button-${activity.id}`"
            @click="openDetailDrawer(activity)"
          >
            <template #icon>
              <FolderOpen class="mr-1.5 h-3.5 w-3.5" />
            </template>
          </Button>
        </div>
      </article>
    </div>

    <!-- Detail Drawer -->
    <ActivityDetailDrawer
      v-model:visible="isDetailDrawerOpen"
      :activity="selectedActivity"
      :can-manage="canManage"
      :can-log-session="canLogSession"
      :is-assigned-supervisor="checkIsSupervisor(selectedActivity)"
      @edit-activity="openEditClub"
      @open-enroll="openEnrollModal"
      @open-session-log="openSessionLogModal"
    />

    <!-- Create / Edit Activity Modal -->
    <ActivityEditorModal
      v-model:visible="isEditorModalOpen"
      :activity="editingActivity"
    />

    <!-- Student Enrollment Modal -->
    <EnrollmentModal
      v-model:visible="isEnrollmentModalOpen"
      :activity="selectedActivity"
      :existing-student-ids="existingActiveStudentIds"
    />

    <!-- Supervisor Session Log Modal -->
    <SessionLogModal
      v-model:visible="isSessionLogModalOpen"
      :activity="selectedActivity"
      :participation="activeParticipation"
    />
  </div>
</template>
