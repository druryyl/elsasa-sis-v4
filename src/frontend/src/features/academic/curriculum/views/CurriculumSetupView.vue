<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Button from 'primevue/button';
import {
  BookOpen,
  GraduationCap,
  Layers,
  Plus,
  RefreshCw,
  Users
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { useCurrentSession } from '@/shared/services/authSession';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import type {
  LearningUnitResponse,
  SubjectResponse,
  SubjectStageResponse
} from '../api/curriculum.api';
import {
  useDeleteLearningUnitMutation,
  useMapGradeMutation,
  useRemoveGradeMappingMutation
} from '../api/useCurriculumMutations';
import {
  useCurriculumStructureQuery,
  useSubjectStageParticipationsQuery
} from '../api/useCurriculumQueries';
import BatchEnrollModal from '../components/BatchEnrollModal.vue';
import CurriculumTree from '../components/CurriculumTree.vue';
import GradeMappingCanvas from '../components/GradeMappingCanvas.vue';
import LearningUnitFormModal from '../components/LearningUnitFormModal.vue';
import SubjectFormModal from '../components/SubjectFormModal.vue';
import SubjectStageFormModal from '../components/SubjectStageFormModal.vue';
import type { SchoolLevel } from '../schemas/curriculum.schema';

const session = useCurrentSession();
const { selectedPeriodId } = useAppContextBridge();

const canManage = computed(() =>
  session.hasPermission('Permissions.Academic.Curriculum.Manage')
);

// Queries
const curriculumQuery = useCurriculumStructureQuery();
const subjects = computed<SubjectResponse[]>(() => curriculumQuery.data.value ?? []);
const isLoading = computed(() => curriculumQuery.isLoading.value);

// Selected stage for the right configuration canvas
const selectedSubjectId = ref<string | null>(null);
const selectedStageId = ref<string | null>(null);

const selectedSubject = computed<SubjectResponse | null>(() => {
  if (!selectedSubjectId.value) return null;
  return subjects.value.find((s) => s.id === selectedSubjectId.value) ?? null;
});

const selectedStage = computed<SubjectStageResponse | null>(() => {
  if (!selectedSubject.value || !selectedStageId.value) return null;
  return (
    selectedSubject.value.stages?.find((st) => st.id === selectedStageId.value) ??
    null
  );
});

// Auto-select first stage if none selected and subjects exist
watch(
  subjects,
  (newSubjects) => {
    if (newSubjects.length > 0 && (!selectedSubjectId.value || !selectedStageId.value)) {
      const firstWithStages = newSubjects.find((s) => s.stages && s.stages.length > 0);
      if (firstWithStages && firstWithStages.stages && firstWithStages.stages.length > 0) {
        selectedSubjectId.value = firstWithStages.id ?? null;
        selectedStageId.value = firstWithStages.stages[0]?.id ?? null;
      } else {
        selectedSubjectId.value = newSubjects[0]?.id ?? null;
      }
    }
  },
  { immediate: true }
);

function handleSelectStage(stage: SubjectStageResponse, subject: SubjectResponse): void {
  selectedSubjectId.value = subject.id ?? null;
  selectedStageId.value = stage.id ?? null;
}

// Participations query for selected stage
const participationsQuery = useSubjectStageParticipationsQuery(
  computed(() => {
    if (!selectedStageId.value) return undefined;
    return {
      subjectStageId: selectedStageId.value,
      operationalPeriodId: selectedPeriodId.value ?? undefined
    };
  }),
  { enabled: computed(() => Boolean(selectedStageId.value)) }
);

const participations = computed(() => participationsQuery.data.value ?? []);

// Modals State
const showSubjectModal = ref(false);
const editingSubject = ref<SubjectResponse | null>(null);

const showStageModal = ref(false);
const targetSubjectForStage = ref<SubjectResponse | null>(null);
const editingStage = ref<SubjectStageResponse | null>(null);

const showUnitModal = ref(false);
const targetSubjectForUnit = ref<SubjectResponse | null>(null);
const targetStageForUnit = ref<SubjectStageResponse | null>(null);
const editingUnit = ref<LearningUnitResponse | null>(null);

const showBatchEnrollModal = ref(false);
const targetSubjectForEnroll = ref<SubjectResponse | null>(null);
const targetStageForEnroll = ref<SubjectStageResponse | null>(null);

// Tier 2 Confirmation Dialogs
const deleteUnitConfirmVisible = ref(false);
const unitToDelete = ref<LearningUnitResponse | null>(null);
const stageForUnitDelete = ref<SubjectStageResponse | null>(null);
const subjectForUnitDelete = ref<SubjectResponse | null>(null);

const removeMappingConfirmVisible = ref(false);
const mappingToRemove = ref<{ mappingId: string; schoolLevel: string; grade: string } | null>(
  null
);

// Mutations
const deleteUnitMutation = useDeleteLearningUnitMutation();
const mapGradeMutation = useMapGradeMutation();
const removeGradeMappingMutation = useRemoveGradeMappingMutation();

// Action Handlers
function openAddSubject(): void {
  editingSubject.value = null;
  showSubjectModal.value = true;
}

function openEditSubject(subject: SubjectResponse): void {
  editingSubject.value = subject;
  showSubjectModal.value = true;
}

function openAddStage(subject: SubjectResponse): void {
  targetSubjectForStage.value = subject;
  editingStage.value = null;
  showStageModal.value = true;
}

function openEditStage(stage: SubjectStageResponse, subject: SubjectResponse): void {
  targetSubjectForStage.value = subject;
  editingStage.value = stage;
  showStageModal.value = true;
}

function openAddUnit(stage: SubjectStageResponse, subject: SubjectResponse): void {
  targetSubjectForUnit.value = subject;
  targetStageForUnit.value = stage;
  editingUnit.value = null;
  showUnitModal.value = true;
}

function openEditUnit(
  unit: LearningUnitResponse,
  stage: SubjectStageResponse,
  subject: SubjectResponse
): void {
  targetSubjectForUnit.value = subject;
  targetStageForUnit.value = stage;
  editingUnit.value = unit;
  showUnitModal.value = true;
}

function promptDeleteUnit(
  unit: LearningUnitResponse,
  stage: SubjectStageResponse,
  subject: SubjectResponse
): void {
  unitToDelete.value = unit;
  stageForUnitDelete.value = stage;
  subjectForUnitDelete.value = subject;
  deleteUnitConfirmVisible.value = true;
}

async function confirmDeleteUnit(): Promise<void> {
  if (
    !unitToDelete.value?.id ||
    !stageForUnitDelete.value?.id ||
    !subjectForUnitDelete.value?.id
  )
    return;

  await deleteUnitMutation.mutateAsync({
    subjectId: subjectForUnitDelete.value.id,
    stageId: stageForUnitDelete.value.id,
    unitId: unitToDelete.value.id
  });

  deleteUnitConfirmVisible.value = false;
  unitToDelete.value = null;
}

function handleMapGrade(mapping: { schoolLevel: SchoolLevel; grade: string }): void {
  if (!selectedSubject.value?.id || !selectedStage.value?.id) return;

  mapGradeMutation.mutate({
    subjectId: selectedSubject.value.id,
    stageId: selectedStage.value.id,
    payload: {
      schoolLevel: mapping.schoolLevel,
      grade: mapping.grade
    }
  });
}

function promptRemoveMapping(mapping: {
  mappingId: string;
  schoolLevel: string;
  grade: string;
}): void {
  mappingToRemove.value = mapping;
  removeMappingConfirmVisible.value = true;
}

async function confirmRemoveMapping(): Promise<void> {
  if (
    !selectedSubject.value?.id ||
    !selectedStage.value?.id ||
    !mappingToRemove.value?.mappingId
  )
    return;

  await removeGradeMappingMutation.mutateAsync({
    subjectId: selectedSubject.value.id,
    stageId: selectedStage.value.id,
    mappingId: mappingToRemove.value.mappingId
  });

  removeMappingConfirmVisible.value = false;
  mappingToRemove.value = null;
}

function openEnrollStudyGroup(stage: SubjectStageResponse, subject: SubjectResponse): void {
  targetSubjectForEnroll.value = subject;
  targetStageForEnroll.value = stage;
  showBatchEnrollModal.value = true;
}

function onEnrollSuccess(): void {
  participationsQuery.refetch();
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6" data-testid="curriculum-setup-view">
      <!-- Breadcrumb -->
      <nav aria-label="Breadcrumb" class="flex items-center gap-1.5 text-xs text-slate-500">
        <span>Academic &amp; Curriculum</span>
        <span aria-hidden="true">/</span>
        <span class="font-medium text-slate-800">Curriculum &amp; Learning Structure</span>
      </nav>

      <!-- Page Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <BookOpen class="h-5 w-5 text-brand-accent-600" />
            <h1 class="text-xl font-bold tracking-tight text-slate-900">
              Curriculum &amp; Learning Structure
            </h1>
          </div>
          <p class="text-sm text-slate-500">
            Configure 3-level curriculum hierarchy (Subject &rarr; Stage &rarr; Learning Units), grade mappings, and batch student enrollments.
          </p>
        </div>

        <div class="flex items-center gap-2">
          <Button
            type="button"
            severity="secondary"
            outlined
            size="small"
            class="text-xs"
            :loading="isLoading"
            data-testid="refresh-curriculum-btn"
            @click="curriculumQuery.refetch()"
          >
            <RefreshCw class="h-3.5 w-3.5 mr-1.5" />
            <span>Refresh</span>
          </Button>

          <Button
            v-if="canManage"
            type="button"
            severity="primary"
            size="small"
            data-testid="add-subject-btn"
            @click="openAddSubject"
          >
            <Plus class="h-4 w-4 mr-1.5" />
            <span>Add Subject</span>
          </Button>
        </div>
      </div>

      <!-- Pattern 5: Planning Canvas (Dual-Region Layout) -->
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-12 items-start">
        <!-- Left Pane: Foundational Hierarchy Tree (5 cols) -->
        <div class="lg:col-span-5 space-y-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <Layers class="h-4 w-4 text-brand-accent-600" />
              <h2 class="text-sm font-semibold text-slate-900">Learning Structure Tree</h2>
            </div>
            <span class="text-xs text-slate-400">
              {{ subjects.length }} subjects configured
            </span>
          </div>

          <div v-if="isLoading" class="p-8 text-center text-sm text-slate-400">
            Loading curriculum structure...
          </div>

          <div
            v-else-if="subjects.length === 0"
            class="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center"
          >
            <EmptyState
              title="No subjects defined"
              description="Get started by creating your first academic subject."
              icon="pi pi-book"
            />
            <Button
              v-if="canManage"
              type="button"
              severity="primary"
              size="small"
              class="mt-4"
              @click="openAddSubject"
            >
              <Plus class="h-4 w-4 mr-1.5" />
              <span>Create Subject</span>
            </Button>
          </div>

          <CurriculumTree
            v-else
            :subjects="subjects"
            :selected-stage-id="selectedStageId"
            :can-manage="canManage"
            @select-stage="handleSelectStage"
            @add-subject="openAddSubject"
            @edit-subject="openEditSubject"
            @add-stage="openAddStage"
            @edit-stage="openEditStage"
            @add-unit="openAddUnit"
            @edit-unit="openEditUnit"
            @delete-unit="promptDeleteUnit"
            @enroll-study-group="openEnrollStudyGroup"
          />
        </div>

        <!-- Right Pane: Allocation Matrix & Configuration Canvas (7 cols) -->
        <div class="lg:col-span-7 space-y-6">
          <div v-if="!selectedStage || !selectedSubject" class="rounded-xl border border-dashed border-slate-200 bg-white p-12 text-center">
            <EmptyState
              title="No Stage Selected"
              description="Select a Subject Stage from the hierarchy on the left to configure grade mappings and enroll students."
              icon="pi pi-sitemap"
            />
          </div>

          <template v-else>
            <!-- Selected Stage Hero Card -->
            <div
              class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
              data-testid="selected-stage-hero"
            >
              <div class="space-y-1">
                <div class="flex items-center gap-2">
                  <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-brand-accent-100 text-brand-accent-800">
                    {{ selectedSubject.code }} &bull; Stage #{{ selectedStage.sequenceOrder }}
                  </span>
                  <h2 class="text-base font-bold text-slate-900">{{ selectedStage.name }}</h2>
                </div>
                <p v-if="selectedStage.description" class="text-xs text-slate-500">
                  {{ selectedStage.description }}
                </p>
                <div class="flex items-center gap-3 text-xs text-slate-500 pt-1">
                  <span>{{ selectedStage.units?.length ?? 0 }} Learning Units</span>
                  <span>&bull;</span>
                  <span>{{ selectedStage.gradeMappings?.length ?? 0 }} Administrative Grades</span>
                </div>
              </div>

              <div v-if="canManage" class="flex items-center gap-2 shrink-0">
                <Button
                  type="button"
                  severity="primary"
                  size="small"
                  data-testid="stage-enroll-btn"
                  @click="openEnrollStudyGroup(selectedStage, selectedSubject)"
                >
                  <Users class="h-4 w-4 mr-1.5" />
                  <span>Enroll Study Group</span>
                </Button>
              </div>
            </div>

            <!-- Grade Mapping Canvas -->
            <GradeMappingCanvas
              :subject-id="selectedSubject.id!"
              :stage-id="selectedStage.id!"
              :stage-name="selectedStage.name ?? ''"
              :grade-mappings="selectedStage.gradeMappings"
              :can-manage="canManage"
              :is-submitting="mapGradeMutation.isPending.value || removeGradeMappingMutation.isPending.value"
              @map-grade="handleMapGrade"
              @remove-mapping="promptRemoveMapping"
            />

            <!-- Active Enrolled Students Card -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs space-y-4" data-testid="participations-section">
              <div class="flex items-center justify-between border-b border-slate-100 pb-3">
                <div class="flex items-center gap-2">
                  <GraduationCap class="h-4 w-4 text-brand-accent-600" />
                  <h3 class="text-sm font-semibold text-slate-900">Enrolled Students (Academic Participations)</h3>
                </div>
                <div class="text-xs text-slate-500">
                  <span class="font-semibold text-slate-800">{{ participations.length }}</span> students enrolled
                </div>
              </div>

              <div v-if="participationsQuery.isLoading.value" class="text-center py-6 text-xs text-slate-400">
                Loading enrollments...
              </div>

              <div
                v-else-if="participations.length === 0"
                class="rounded-lg border border-dashed border-slate-200 bg-slate-50/50 p-6 text-center text-xs text-slate-500"
              >
                <p>No students enrolled in this stage yet.</p>
                <p v-if="canManage" class="text-2xs text-slate-400 mt-1">
                  Click "Enroll Study Group" above to batch-enroll placed students from an active class.
                </p>
              </div>

              <div v-else class="max-h-64 overflow-y-auto divide-y divide-slate-100 rounded-lg border border-slate-100">
                <div
                  v-for="p in participations"
                  :key="p.id"
                  class="flex items-center justify-between px-3 py-2 text-xs hover:bg-slate-50"
                  :data-testid="`participation-row-${p.id}`"
                >
                  <div class="space-y-0.5 truncate">
                    <div class="font-medium text-slate-800 truncate">{{ p.studentName }}</div>
                    <div class="text-2xs text-slate-400 font-mono">NIS: {{ p.studentNis || '-' }} &bull; {{ p.studyGroupName || 'No Study Group' }}</div>
                  </div>
                  <StatusPill
                    :status="p.status ?? 'ACTIVE'"
                    :label="p.status ?? 'ACTIVE'"
                    size="sm"
                  />
                </div>
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- Modals & Drawers -->
      <SubjectFormModal
        v-model:visible="showSubjectModal"
        :subject="editingSubject"
        @saved="curriculumQuery.refetch()"
      />

      <SubjectStageFormModal
        v-if="targetSubjectForStage"
        v-model:visible="showStageModal"
        :subject-id="targetSubjectForStage.id!"
        :subject-name="targetSubjectForStage.name ?? ''"
        :stage="editingStage"
        :default-sequence-order="(targetSubjectForStage.stages?.length ?? 0) + 1"
        @saved="curriculumQuery.refetch()"
      />

      <LearningUnitFormModal
        v-if="targetSubjectForUnit && targetStageForUnit"
        v-model:visible="showUnitModal"
        :subject-id="targetSubjectForUnit.id!"
        :stage-id="targetStageForUnit.id!"
        :stage-name="targetStageForUnit.name ?? ''"
        :unit="editingUnit"
        :default-sequence-order="(targetStageForUnit.units?.length ?? 0) + 1"
        @saved="curriculumQuery.refetch()"
      />

      <BatchEnrollModal
        v-if="targetSubjectForEnroll && targetStageForEnroll"
        v-model:visible="showBatchEnrollModal"
        :subject-id="targetSubjectForEnroll.id!"
        :subject-name="targetSubjectForEnroll.name ?? ''"
        :stage-id="targetStageForEnroll.id!"
        :stage-name="targetStageForEnroll.name ?? ''"
        @enrolled="onEnrollSuccess"
      />

      <!-- Tier 2 Confirmation Dialog: Delete Learning Unit -->
      <ConfirmActionDialog
        v-model:visible="deleteUnitConfirmVisible"
        title="Delete Learning Unit"
        :message="`Are you sure you want to delete unit '${unitToDelete?.name ?? ''}'? This action cannot be undone.`"
        confirm-label="Delete Unit"
        severity="danger"
        :loading="deleteUnitMutation.isPending.value"
        @confirm="confirmDeleteUnit"
        @cancel="deleteUnitConfirmVisible = false"
      />

      <!-- Tier 2 Confirmation Dialog: Remove Grade Mapping -->
      <ConfirmActionDialog
        v-model:visible="removeMappingConfirmVisible"
        title="Remove Grade Mapping"
        :message="`Are you sure you want to remove grade mapping ${mappingToRemove?.schoolLevel ?? ''} Grade ${mappingToRemove?.grade ?? ''} from '${selectedStage?.name ?? ''}'?`"
        confirm-label="Remove Mapping"
        severity="danger"
        :loading="removeGradeMappingMutation.isPending.value"
        @confirm="confirmRemoveMapping"
        @cancel="removeMappingConfirmVisible = false"
      />
    </div>
  </AppErrorBoundary>
</template>
