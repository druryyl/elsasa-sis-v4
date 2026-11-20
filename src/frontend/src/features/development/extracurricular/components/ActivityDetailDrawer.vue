<script setup lang="ts">
import { computed, ref } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import {
  Award,
  Calendar,
  CheckCircle2,
  ClipboardList,
  Pencil,
  ShieldCheck,
  UserCheck,
  UserPlus,
  Users,
  XCircle
} from 'lucide-vue-next';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  ExtracurricularActivityDto,
  ExtracurricularParticipationDto
} from '../api/extracurricular.api';
import { useExtracurricularParticipantsQuery } from '../api/useExtracurricularQueries';
import {
  useCompleteParticipantMutation,
  useWithdrawParticipantMutation
} from '../api/useExtracurricularMutations';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    activity: ExtracurricularActivityDto | null;
    canManage: boolean;
    canLogSession: boolean;
    isAssignedSupervisor: boolean;
  }>(),
  {
    canManage: false,
    canLogSession: false,
    isAssignedSupervisor: false
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  editActivity: [activity: ExtracurricularActivityDto];
  openEnroll: [activity: ExtracurricularActivityDto];
  openSessionLog: [participation: ExtracurricularParticipationDto];
}>();

const statusFilter = ref<'ALL' | 'ACTIVE' | 'COMPLETED' | 'WITHDRAWN'>('ALL');
const confirmTarget = ref<{
  action: 'withdraw' | 'complete';
  participation: ExtracurricularParticipationDto;
} | null>(null);

const participantsQuery = useExtracurricularParticipantsQuery(
  () => props.activity?.id,
  {},
  { enabled: () => props.visible && Boolean(props.activity?.id) }
);

const withdrawMutation = useWithdrawParticipantMutation();
const completeMutation = useCompleteParticipantMutation();

const participants = computed(() => participantsQuery.data.value ?? []);

const filteredParticipants = computed(() => {
  if (statusFilter.value === 'ALL') {
    return participants.value;
  }
  return participants.value.filter((p) => p.status === statusFilter.value);
});

const activeParticipantCount = computed(
  () => participants.value.filter((p) => p.status === 'ACTIVE').length
);

const recentSessionLogs = computed(() =>
  participants.value
    .filter((p) => Boolean(p.lastSessionLog))
    .slice()
    .sort((a, b) => (b.lastSessionDate ?? '').localeCompare(a.lastSessionDate ?? ''))
);

const canRecordLogs = computed(() => props.canLogSession && props.isAssignedSupervisor);

const isConfirmPending = computed(
  () => withdrawMutation.isPending.value || completeMutation.isPending.value
);

function closeDrawer(): void {
  emit('update:visible', false);
}

function requestParticipantAction(
  action: 'withdraw' | 'complete',
  participation: ExtracurricularParticipationDto
): void {
  confirmTarget.value = { action, participation };
}

async function handleConfirmAction(): Promise<void> {
  if (!confirmTarget.value || !props.activity?.id || !confirmTarget.value.participation.id) {
    return;
  }

  const { action, participation } = confirmTarget.value;
  if (action === 'withdraw') {
    await withdrawMutation.mutateAsync({
      activityId: props.activity.id,
      participationId: participation.id!
    });
  } else {
    await completeMutation.mutateAsync({
      activityId: props.activity.id,
      participationId: participation.id!
    });
  }
  confirmTarget.value = null;
}
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="!w-full sm:!w-[740px] md:!w-[880px]"
    data-testid="activity-detail-drawer"
    @update:visible="closeDrawer"
  >
    <template #header>
      <div class="flex min-w-0 flex-1 items-center justify-between gap-3 pr-2">
        <div class="flex min-w-0 items-center gap-2.5">
          <div
            class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-accent-50 text-brand-accent-600"
          >
            <Award class="h-5 w-5" />
          </div>
          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
              <h2 class="truncate text-base font-semibold text-slate-900">
                {{ activity?.name ?? 'Club Details' }}
              </h2>
              <StatusPill v-if="activity?.status" :status="activity.status" />
            </div>
            <p class="truncate text-xs text-slate-500">
              Supervisor: {{ activity?.supervisorName ?? 'Unassigned' }}
            </p>
          </div>
        </div>

        <Button
          v-if="canManage && activity"
          size="small"
          severity="secondary"
          outlined
          label="Edit Club"
          data-testid="drawer-edit-club-button"
          @click="emit('editActivity', activity)"
        >
          <template #icon>
            <Pencil class="mr-1.5 h-3.5 w-3.5" />
          </template>
        </Button>
      </div>
    </template>

    <div v-if="activity" class="space-y-5">
      <!-- Club Overview Summary -->
      <div class="rounded-xl border border-slate-200 bg-slate-50/70 p-4">
        <p class="text-sm text-slate-700">
          {{ activity.description || 'No description provided for this club.' }}
        </p>

        <div class="mt-3 flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-3">
          <div class="flex flex-wrap items-center gap-4 text-xs text-slate-600">
            <span class="inline-flex items-center gap-1.5 font-medium">
              <UserCheck class="h-3.5 w-3.5 text-slate-400" />
              Supervisor: <strong class="text-slate-800">{{ activity.supervisorName }}</strong>
            </span>
            <span class="inline-flex items-center gap-1.5 font-medium">
              <Users class="h-3.5 w-3.5 text-slate-400" />
              Active Members:
              <strong class="text-slate-800">
                {{ participantsQuery.isSuccess.value ? activeParticipantCount : activity.activeParticipantCount ?? 0 }}
              </strong>
            </span>
          </div>

          <Button
            v-if="canManage && activity.status === 'ACTIVE'"
            size="small"
            label="Enroll Student"
            data-testid="enroll-student-button"
            @click="emit('openEnroll', activity)"
          >
            <template #icon>
              <UserPlus class="mr-1.5 h-3.5 w-3.5" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Supervisor Session Logging Status Banner -->
      <div
        v-if="canRecordLogs"
        class="flex items-center gap-2.5 rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-2.5 text-xs text-emerald-800"
        data-testid="supervisor-access-banner"
      >
        <ShieldCheck class="h-4 w-4 shrink-0 text-emerald-600" />
        <span>
          You are the assigned supervisor for this club. You can record session activity logs for active members below.
        </span>
      </div>
      <div
        v-else-if="canLogSession"
        class="flex items-center gap-2.5 rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs text-slate-600"
        data-testid="non-supervisor-banner"
      >
        <ClipboardList class="h-4 w-4 shrink-0 text-slate-400" />
        <span>
          Session activity logs can only be recorded by the assigned supervisor ({{ activity.supervisorName }}).
        </span>
      </div>

      <!-- Enrolled Students Roster Section -->
      <div class="space-y-3">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <h3 class="text-sm font-semibold text-slate-900">Enrolled Students Roster</h3>

          <div class="flex flex-wrap items-center gap-1.5" role="group" aria-label="Participant status filter">
            <button
              v-for="filter in ['ALL', 'ACTIVE', 'COMPLETED', 'WITHDRAWN'] as const"
              :key="filter"
              type="button"
              class="rounded-full border px-2.5 py-1 text-xs font-medium transition-colors"
              :class="
                statusFilter === filter
                  ? 'border-brand-accent-600 bg-brand-accent-50 text-brand-accent-700'
                  : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
              "
              @click="statusFilter = filter"
            >
              {{ filter === 'ALL' ? 'All' : filter.charAt(0) + filter.slice(1).toLowerCase() }}
            </button>
          </div>
        </div>

        <DataTable
          :value="filteredParticipants"
          :loading="participantsQuery.isLoading.value"
          data-key="id"
          responsive-layout="scroll"
          striped-rows
          class="text-xs"
          data-testid="participants-table"
        >
          <template #empty>
            <div class="py-6">
              <EmptyState
                title="No participants found"
                description="Enroll students into this extracurricular club to begin tracking participation and session logs."
                variant="empty"
              />
            </div>
          </template>

          <Column header="Student" class="min-w-44">
            <template #body="{ data }">
              <div class="font-medium text-slate-900">{{ data.studentName }}</div>
              <div class="font-mono text-[11px] text-slate-500">{{ data.studentNis }}</div>
            </template>
          </Column>

          <Column header="Academic Year / Joined" class="min-w-36">
            <template #body="{ data }">
              <div class="text-slate-800">{{ data.academicYearName }}</div>
              <div class="text-[11px] text-slate-500">
                Joined {{ formatDisplayDate(data.joinedDate, '—') }}
              </div>
            </template>
          </Column>

          <Column header="Status" class="w-28">
            <template #body="{ data }">
              <StatusPill v-if="data.status" :status="data.status" />
            </template>
          </Column>

          <Column header="Latest Session Log" class="min-w-52">
            <template #body="{ data }">
              <div v-if="data.lastSessionLog" class="space-y-0.5">
                <div class="flex items-center gap-1 text-[11px] font-medium text-slate-500">
                  <Calendar class="h-3 w-3" />
                  <span>{{ formatDisplayDate(data.lastSessionDate, '—') }}</span>
                </div>
                <p class="line-clamp-2 text-slate-700">{{ data.lastSessionLog }}</p>
              </div>
              <span v-else class="text-slate-400 italic">No session logged yet</span>
            </template>
          </Column>

          <Column
            v-if="canRecordLogs || canManage"
            header="Actions"
            class="w-40 text-right"
          >
            <template #body="{ data }">
              <div class="flex items-center justify-end gap-1.5">
                <Button
                  v-if="canRecordLogs && data.status === 'ACTIVE'"
                  size="small"
                  severity="secondary"
                  outlined
                  label="Log Session"
                  :data-testid="`log-session-button-${data.id}`"
                  @click="emit('openSessionLog', data)"
                >
                  <template #icon>
                    <ClipboardList class="mr-1 h-3.5 w-3.5" />
                  </template>
                </Button>

                <Button
                  v-if="canManage && data.status === 'ACTIVE'"
                  title="Mark Completed"
                  size="small"
                  severity="success"
                  text
                  rounded
                  aria-label="Mark Completed"
                  :data-testid="`complete-participant-button-${data.id}`"
                  @click="requestParticipantAction('complete', data)"
                >
                  <template #icon>
                    <CheckCircle2 class="h-4 w-4" />
                  </template>
                </Button>

                <Button
                  v-if="canManage && data.status === 'ACTIVE'"
                  title="Withdraw Student"
                  size="small"
                  severity="danger"
                  text
                  rounded
                  aria-label="Withdraw Student"
                  :data-testid="`withdraw-participant-button-${data.id}`"
                  @click="requestParticipantAction('withdraw', data)"
                >
                  <template #icon>
                    <XCircle class="h-4 w-4" />
                  </template>
                </Button>
              </div>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Recent Session Activity Logs Summary -->
      <div class="space-y-2.5">
        <h3 class="text-sm font-semibold text-slate-900">Recent Session Activity Logs</h3>

        <div
          v-if="recentSessionLogs.length === 0"
          class="rounded-lg border border-dashed border-slate-200 bg-slate-50/50 px-4 py-5 text-center text-xs text-slate-500"
        >
          No meeting session logs have been recorded for this club yet.
        </div>

        <ul v-else class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
          <li
            v-for="entry in recentSessionLogs"
            :key="entry.id"
            class="flex items-start justify-between gap-3 px-4 py-3 text-xs"
            data-testid="recent-session-log-item"
          >
            <div class="min-w-0 space-y-1">
              <div class="flex items-center gap-2">
                <span class="font-semibold text-slate-900">{{ entry.studentName }}</span>
                <span class="font-mono text-[11px] text-slate-500">{{ entry.studentNis }}</span>
              </div>
              <p class="text-slate-700">{{ entry.lastSessionLog }}</p>
            </div>
            <span class="shrink-0 rounded-md bg-slate-100 px-2 py-1 text-[11px] font-medium text-slate-600">
              {{ formatDisplayDate(entry.lastSessionDate, '—') }}
            </span>
          </li>
        </ul>
      </div>
    </div>

    <ConfirmActionDialog
      :visible="Boolean(confirmTarget)"
      :title="
        confirmTarget?.action === 'withdraw'
          ? 'Withdraw Participant'
          : 'Complete Participation'
      "
      :message="
        confirmTarget?.action === 'withdraw'
          ? `Mark ${confirmTarget?.participation.studentName} as withdrawn from ${activity?.name}?`
          : `Mark ${confirmTarget?.participation.studentName} as having completed ${activity?.name}?`
      "
      :confirm-label="confirmTarget?.action === 'withdraw' ? 'Withdraw' : 'Mark Completed'"
      :severity="confirmTarget?.action === 'withdraw' ? 'danger' : 'primary'"
      :loading="isConfirmPending"
      @update:visible="(val) => { if (!val) confirmTarget = null; }"
      @confirm="handleConfirmAction"
    />
  </Drawer>
</template>
