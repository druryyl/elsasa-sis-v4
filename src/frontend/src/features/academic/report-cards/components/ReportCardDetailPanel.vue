<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import {
  AlertTriangle,
  CheckCircle2,
  Edit3,
  Loader2,
  Lock,
  Printer,
  Save,
  Unlock
} from 'lucide-vue-next';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatDisplayDateTime } from '@/shared/utils/dateUtils';
import type { ReportCardDetailDto, ReportCardEntryDetailDto } from '../api/reportCards.api';
import {
  useFinalizeReportCardMutation,
  useOverrideReportCardMutation,
  useProvideSubjectNotesMutation,
  useWriteHomeroomRemarksMutation
} from '../api/useReportCardMutations';

const props = withDefaults(
  defineProps<{
    reportCard: ReportCardDetailDto | null;
    loading?: boolean;
    canProvideNotes: boolean;
    canWriteRemarks: boolean;
    canFinalize: boolean;
    canOverride: boolean;
    isHistoricalPeriod?: boolean;
  }>(),
  {
    loading: false,
    isHistoricalPeriod: false
  }
);

const emit = defineEmits<{
  print: [reportCardId: string];
}>();

const provideSubjectNotesMutation = useProvideSubjectNotesMutation();
const writeHomeroomRemarksMutation = useWriteHomeroomRemarksMutation();
const finalizeMutation = useFinalizeReportCardMutation();
const overrideMutation = useOverrideReportCardMutation();

// Subject Note Editor State
const editingSubjectStageId = ref<string | null>(null);
const editingSubjectNote = ref<string>('');

// Homeroom Remarks Editor State
const homeroomRemarksDraft = ref<string>('');

// Confirmation Dialogs
const showFinalizeDialog = ref(false);
const showOverrideDialog = ref(false);

const isDraft = computed(() => props.reportCard?.status === 'DRAFT');
const isFinalized = computed(() => props.reportCard?.status === 'FINALIZED');

const canEditNotes = computed(
  () => props.canProvideNotes && isDraft.value && !props.isHistoricalPeriod
);
const canEditRemarks = computed(
  () => props.canWriteRemarks && isDraft.value && !props.isHistoricalPeriod
);
const canRunFinalize = computed(
  () => props.canFinalize && isDraft.value && !props.isHistoricalPeriod
);
const canRunOverride = computed(
  () => props.canOverride && isFinalized.value && !props.isHistoricalPeriod
);

watch(
  () => props.reportCard,
  (card) => {
    editingSubjectStageId.value = null;
    editingSubjectNote.value = '';
    homeroomRemarksDraft.value = card?.generalRemarks ?? '';
  },
  { immediate: true }
);

const isRemarksDirty = computed(
  () => homeroomRemarksDraft.value !== (props.reportCard?.generalRemarks ?? '')
);

const isRemarksValid = computed(() => homeroomRemarksDraft.value.length <= 1000);

const isSubjectNoteValid = computed(
  () =>
    editingSubjectNote.value.trim().length > 0 &&
    editingSubjectNote.value.trim().length <= 1000
);

function startEditingSubjectNote(entry: ReportCardEntryDetailDto): void {
  if (!canEditNotes.value || !entry.subjectStageId) return;
  editingSubjectStageId.value = entry.subjectStageId;
  editingSubjectNote.value = entry.teacherNotes ?? '';
}

function cancelEditingSubjectNote(): void {
  editingSubjectStageId.value = null;
  editingSubjectNote.value = '';
}

async function handleSaveSubjectNote(entry: ReportCardEntryDetailDto): Promise<void> {
  if (!props.reportCard?.id || !entry.subjectStageId || !isSubjectNoteValid.value) return;

  try {
    await provideSubjectNotesMutation.mutateAsync({
      id: props.reportCard.id,
      payload: {
        subjectStageId: entry.subjectStageId,
        teacherNotes: editingSubjectNote.value.trim()
      }
    });
    editingSubjectStageId.value = null;
    editingSubjectNote.value = '';
  } catch {
    // Error toast handled by mutation hook
  }
}

async function handleSaveHomeroomRemarks(): Promise<void> {
  if (!props.reportCard?.id || !canEditRemarks.value || !isRemarksValid.value) return;

  try {
    await writeHomeroomRemarksMutation.mutateAsync({
      id: props.reportCard.id,
      payload: {
        remarks: homeroomRemarksDraft.value.trim()
      }
    });
  } catch {
    // Error toast handled by mutation hook
  }
}

async function handleConfirmFinalize(): Promise<void> {
  if (!props.reportCard?.id) return;

  try {
    await finalizeMutation.mutateAsync(props.reportCard.id);
    showFinalizeDialog.value = false;
  } catch {
    // Error toast handled by mutation hook
  }
}

async function handleConfirmOverride(reason?: string): Promise<void> {
  if (!props.reportCard?.id || !reason?.trim()) return;

  try {
    await overrideMutation.mutateAsync({
      id: props.reportCard.id,
      payload: {
        overrideReason: reason.trim()
      }
    });
    showOverrideDialog.value = false;
  } catch {
    // Error toast handled by mutation hook
  }
}

function formatScore(score: number | undefined): string {
  if (score === undefined || score === null) return '0.00';
  return score.toFixed(2);
}

const completedNotesCount = computed(
  () =>
    (props.reportCard?.entries ?? []).filter(
      (e) => Boolean(e.teacherNotes && e.teacherNotes.trim().length > 0)
    ).length
);

const totalSubjectsCount = computed(
  () => props.reportCard?.totalSubjects ?? (props.reportCard?.entries?.length ?? 0)
);
</script>

<template>
  <div
    class="rounded-xl border border-slate-200 bg-white shadow-xs"
    data-testid="report-card-detail-panel"
  >
    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <Loader2 class="h-6 w-6 animate-spin text-brand-accent-600" />
      <span class="ml-2 text-sm text-slate-600">Loading report card details...</span>
    </div>

    <!-- Detail Content -->
    <template v-else-if="reportCard">
      <!-- Header -->
      <div
        class="flex flex-col gap-4 border-b border-slate-200 px-5 py-4 sm:flex-row sm:items-start sm:justify-between"
      >
        <div>
          <div class="flex flex-wrap items-center gap-2.5">
            <h2 class="text-lg font-semibold text-slate-900">
              {{ reportCard.studentName || 'Student Report Card' }}
            </h2>
            <StatusPill :status="reportCard.status ?? 'DRAFT'" />
          </div>
          <p class="mt-1 text-xs text-slate-500">
            NIS: <span class="font-medium text-slate-700">{{ reportCard.studentNis || '-' }}</span>
            <span v-if="reportCard.studentNisn">
              • NISN: <span class="font-medium text-slate-700">{{ reportCard.studentNisn }}</span>
            </span>
            • Study Group:
            <span class="font-medium text-slate-700">{{ reportCard.studyGroupName || '-' }}</span>
            • Homeroom:
            <span class="font-medium text-slate-700">{{
              reportCard.homeroomTeacherName || 'Unassigned'
            }}</span>
          </p>
          <p v-if="reportCard.finalizedAt" class="mt-1 text-xs text-emerald-700">
            Finalized on {{ formatDisplayDateTime(reportCard.finalizedAt) }}
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            label="Print Preview"
            severity="secondary"
            outlined
            size="small"
            data-testid="detail-print-btn"
            @click="reportCard.id && emit('print', reportCard.id)"
          >
            <template #icon>
              <Printer class="mr-1.5 h-4 w-4" />
            </template>
          </Button>

          <Button
            v-if="canRunFinalize"
            type="button"
            label="Finalize & Lock"
            size="small"
            :loading="finalizeMutation.isPending.value"
            data-testid="finalize-card-btn"
            @click="showFinalizeDialog = true"
          >
            <template #icon>
              <Lock class="mr-1.5 h-4 w-4" />
            </template>
          </Button>

          <Button
            v-if="canRunOverride"
            type="button"
            label="Unlock to Draft"
            severity="warn"
            outlined
            size="small"
            :loading="overrideMutation.isPending.value"
            data-testid="override-card-btn"
            @click="showOverrideDialog = true"
          >
            <template #icon>
              <Unlock class="mr-1.5 h-4 w-4" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Finalized Read-Only Lock Notice -->
      <div
        v-if="isFinalized"
        class="flex items-center gap-2.5 border-b border-emerald-200 bg-emerald-50 px-5 py-2.5 text-xs text-emerald-900"
        data-testid="finalized-lock-banner"
      >
        <CheckCircle2 class="h-4 w-4 shrink-0 text-emerald-600" />
        <span>
          This report card is <strong>FINALIZED</strong> and locked. Subject teacher notes and
          homeroom remarks cannot be modified unless unlocked by an administrator.
        </span>
      </div>

      <!-- Override Audit Reason Banner (when unlocked back to DRAFT) -->
      <div
        v-if="reportCard.overrideReason && isDraft"
        class="flex items-start gap-2.5 border-b border-amber-200 bg-amber-50 px-5 py-2.5 text-xs text-amber-900"
        data-testid="override-reason-banner"
      >
        <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div>
          <span class="font-semibold">Unlocked via Administrative Override:</span>
          {{ reportCard.overrideReason }}
        </div>
      </div>

      <!-- Body -->
      <div class="space-y-6 p-5">
        <!-- Key Academic & Attendance Metrics -->
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div class="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <p class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
              Semester Average
            </p>
            <p class="mt-1 font-mono text-xl font-bold text-slate-900">
              {{ formatScore(reportCard.averageScore) }}
            </p>
          </div>
          <div class="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <p class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
              Subject Notes
            </p>
            <p class="mt-1 text-xl font-bold text-slate-900">
              {{ completedNotesCount }} / {{ totalSubjectsCount }}
            </p>
          </div>
          <div class="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <p class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
              Present / Total Days
            </p>
            <p class="mt-1 text-xl font-bold text-emerald-700">
              {{ reportCard.attendance?.presentCount ?? 0 }}
              <span class="text-sm font-normal text-slate-500">
                / {{ reportCard.attendance?.totalRecordedDays ?? 0 }}
              </span>
            </p>
          </div>
          <div class="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <p class="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
              Sick / Excused / Absent
            </p>
            <p class="mt-1 text-sm font-semibold text-slate-800">
              S: {{ reportCard.attendance?.sickCount ?? 0 }} • E:
              {{ reportCard.attendance?.excusedCount ?? 0 }} • A:
              {{ reportCard.attendance?.absentCount ?? 0 }}
            </p>
            <p class="text-[11px] text-slate-500">
              Late: {{ reportCard.attendance?.lateCount ?? 0 }}
            </p>
          </div>
        </div>

        <!-- Subject Grades & Subject Teacher Feedback Table -->
        <section class="space-y-3">
          <div class="flex items-center justify-between">
            <h3 class="text-sm font-semibold text-slate-900">
              Subject Grades & Teacher Feedback
            </h3>
            <span class="text-xs text-slate-500">
              {{ totalSubjectsCount }} subject(s)
            </span>
          </div>

          <div class="overflow-x-auto rounded-lg border border-slate-200">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                <tr>
                  <th class="px-3.5 py-2.5">Subject</th>
                  <th class="px-3 py-2.5 text-center">Score</th>
                  <th class="px-3 py-2.5 text-center">Grade</th>
                  <th class="px-3.5 py-2.5">Subject Teacher Feedback</th>
                  <th v-if="canEditNotes" class="w-28 px-3 py-2.5 text-right">Action</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200 bg-white">
                <tr v-if="!reportCard.entries || reportCard.entries.length === 0">
                  <td
                    :colspan="canEditNotes ? 5 : 4"
                    class="px-4 py-6 text-center text-sm text-slate-500"
                  >
                    No subject entries found on this report card.
                  </td>
                </tr>

                <template v-for="entry in reportCard.entries ?? []" :key="entry.id">
                  <tr class="hover:bg-slate-50/70">
                    <td class="px-3.5 py-3 align-top">
                      <p class="font-medium text-slate-900">{{ entry.subjectName || '-' }}</p>
                      <p class="text-xs text-slate-500">
                        {{ entry.subjectCode ? `${entry.subjectCode} • ` : ''
                        }}{{ entry.subjectStageName || '-' }}
                      </p>
                    </td>
                    <td class="px-3 py-3 text-center align-top font-mono font-semibold text-slate-900">
                      {{ formatScore(entry.numericScore) }}
                    </td>
                    <td class="px-3 py-3 text-center align-top font-semibold text-slate-700">
                      {{ entry.letterGrade || '-' }}
                    </td>
                    <td class="px-3.5 py-3 align-top">
                      <template v-if="editingSubjectStageId === entry.subjectStageId">
                        <div class="space-y-2" data-testid="subject-note-editor">
                          <Textarea
                            v-model="editingSubjectNote"
                            rows="3"
                            placeholder="Enter qualitative subject feedback for the student..."
                            class="w-full text-sm"
                            :disabled="provideSubjectNotesMutation.isPending.value"
                            data-testid="subject-note-textarea"
                          />
                          <div class="flex items-center justify-between gap-2">
                            <span class="text-[11px] text-slate-400">
                              {{ editingSubjectNote.trim().length }}/1000 chars
                            </span>
                            <div class="flex items-center gap-2">
                              <Button
                                type="button"
                                label="Cancel"
                                severity="secondary"
                                text
                                size="small"
                                :disabled="provideSubjectNotesMutation.isPending.value"
                                data-testid="subject-note-cancel-btn"
                                @click="cancelEditingSubjectNote"
                              />
                              <Button
                                type="button"
                                label="Save Note"
                                size="small"
                                :loading="provideSubjectNotesMutation.isPending.value"
                                :disabled="!isSubjectNoteValid"
                                data-testid="subject-note-save-btn"
                                @click="handleSaveSubjectNote(entry)"
                              >
                                <template #icon>
                                  <Save class="mr-1.5 h-3.5 w-3.5" />
                                </template>
                              </Button>
                            </div>
                          </div>
                        </div>
                      </template>
                      <template v-else>
                        <p
                          v-if="entry.teacherNotes"
                          class="text-sm leading-relaxed text-slate-800"
                        >
                          {{ entry.teacherNotes }}
                        </p>
                        <p v-else class="text-xs italic text-slate-400">
                          No feedback note recorded yet.
                        </p>
                        <p v-if="entry.teacherName" class="mt-1 text-xs text-slate-500">
                          Teacher: {{ entry.teacherName }}
                        </p>
                      </template>
                    </td>
                    <td v-if="canEditNotes" class="px-3 py-3 text-right align-top">
                      <Button
                        v-if="editingSubjectStageId !== entry.subjectStageId"
                        type="button"
                        :label="entry.teacherNotes ? 'Edit' : 'Add Note'"
                        severity="secondary"
                        outlined
                        size="small"
                        :data-testid="`edit-subject-note-btn-${entry.subjectStageId}`"
                        @click="startEditingSubjectNote(entry)"
                      >
                        <template #icon>
                          <Edit3 class="mr-1 h-3.5 w-3.5" />
                        </template>
                      </Button>
                    </td>
                  </tr>
                </template>
              </tbody>
            </table>
          </div>
        </section>

        <!-- Homeroom Teacher Narrative Remarks -->
        <section class="space-y-2.5 rounded-lg border border-slate-200 bg-slate-50/60 p-4">
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-sm font-semibold text-slate-900">
                Homeroom Teacher Holistic Remarks
              </h3>
              <p class="text-xs text-slate-500">
                Overall narrative evaluation of student growth, character, and semester progress.
              </p>
            </div>
            <span
              v-if="reportCard.generalRemarks && reportCard.generalRemarks.trim().length > 0"
              class="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-700"
            >
              Recorded
            </span>
          </div>

          <div v-if="canEditRemarks" class="space-y-2">
            <Textarea
              v-model="homeroomRemarksDraft"
              rows="3"
              placeholder="Write holistic homeroom teacher remarks for this student..."
              class="w-full bg-white text-sm"
              :disabled="writeHomeroomRemarksMutation.isPending.value"
              data-testid="homeroom-remarks-textarea"
            />
            <div class="flex items-center justify-between">
              <span class="text-[11px] text-slate-500">
                {{ homeroomRemarksDraft.length }}/1000 chars
              </span>
              <Button
                type="button"
                label="Save Remarks"
                size="small"
                :loading="writeHomeroomRemarksMutation.isPending.value"
                :disabled="!isRemarksDirty || !isRemarksValid"
                data-testid="save-homeroom-remarks-btn"
                @click="handleSaveHomeroomRemarks"
              >
                <template #icon>
                  <Save class="mr-1.5 h-3.5 w-3.5" />
                </template>
              </Button>
            </div>
          </div>

          <div
            v-else
            class="rounded-md border border-slate-200 bg-white p-3 text-sm leading-relaxed text-slate-700"
            data-testid="homeroom-remarks-readonly"
          >
            {{ reportCard.generalRemarks || 'No homeroom remarks recorded yet.' }}
          </div>
        </section>
      </div>
    </template>

    <!-- Confirm Finalize Dialog (UC-HRM-03) -->
    <ConfirmActionDialog
      v-model:visible="showFinalizeDialog"
      title="Finalize & Lock Report Card"
      :message="`Are you sure you want to finalize the semester report card for ${reportCard?.studentName ?? 'this student'}? Once finalized, subject notes and homeroom remarks will be locked and the student's academic completion flag will be updated.`"
      confirm-label="Finalize Report Card"
      severity="primary"
      :loading="finalizeMutation.isPending.value"
      data-testid="finalize-confirm-dialog"
      @confirm="handleConfirmFinalize"
    />

    <!-- Administrative Override Dialog (UC-ADM-20, Tier 2 Confirmation with mandatory reason) -->
    <ConfirmActionDialog
      v-model:visible="showOverrideDialog"
      title="Unlock Finalized Report Card"
      :message="`Unlocking ${reportCard?.studentName ?? 'this student'}'s report card will revert its status from FINALIZED back to DRAFT so teachers and homeroom staff can apply corrections.`"
      confirm-label="Unlock to Draft"
      severity="warn"
      :require-reason="true"
      reason-label="Administrative Override Reason"
      reason-placeholder="Enter mandatory audit reason for unlocking this finalized report card..."
      :loading="overrideMutation.isPending.value"
      data-testid="override-confirm-dialog"
      @confirm="handleConfirmOverride"
    />
  </div>
</template>
