<script setup lang="ts">
import { ref } from 'vue';
import Button from 'primevue/button';
import {
  ChevronDown,
  ChevronRight,
  GraduationCap,
  Layers,
  Pencil,
  Plus,
  Trash2,
  Users
} from 'lucide-vue-next';
import type {
  LearningUnitResponse,
  SubjectResponse,
  SubjectStageResponse
} from '../api/curriculum.api';

withDefaults(
  defineProps<{
    subjects: SubjectResponse[];
    selectedStageId?: string | null;
    canManage?: boolean;
  }>(),
  {
    selectedStageId: null,
    canManage: true
  }
);

const emit = defineEmits<{
  'select-stage': [stage: SubjectStageResponse, subject: SubjectResponse];
  'add-subject': [];
  'edit-subject': [subject: SubjectResponse];
  'add-stage': [subject: SubjectResponse];
  'edit-stage': [stage: SubjectStageResponse, subject: SubjectResponse];
  'add-unit': [stage: SubjectStageResponse, subject: SubjectResponse];
  'edit-unit': [
    unit: LearningUnitResponse,
    stage: SubjectStageResponse,
    subject: SubjectResponse
  ];
  'delete-unit': [
    unit: LearningUnitResponse,
    stage: SubjectStageResponse,
    subject: SubjectResponse
  ];
  'enroll-study-group': [stage: SubjectStageResponse, subject: SubjectResponse];
}>();

// Expanded states for subjects and stages
const expandedSubjects = ref<Record<string, boolean>>({});
const expandedStages = ref<Record<string, boolean>>({});

function toggleSubject(subjectId: string): void {
  expandedSubjects.value[subjectId] = !expandedSubjects.value[subjectId];
}

function toggleStage(stageId: string): void {
  expandedStages.value[stageId] = !expandedStages.value[stageId];
}

function isSubjectExpanded(subjectId?: string): boolean {
  if (!subjectId) return false;
  // Expand first subject by default if not set
  if (expandedSubjects.value[subjectId] === undefined) {
    expandedSubjects.value[subjectId] = true;
  }
  return expandedSubjects.value[subjectId] ?? true;
}

function isStageExpanded(stageId?: string): boolean {
  if (!stageId) return false;
  if (expandedStages.value[stageId] === undefined) {
    expandedStages.value[stageId] = true;
  }
  return expandedStages.value[stageId] ?? true;
}
</script>

<template>
  <div class="space-y-3" data-testid="curriculum-tree">
    <div
      v-for="subject in subjects"
      :key="subject.id"
      class="rounded-xl border border-slate-200 bg-white overflow-hidden shadow-xs transition-shadow hover:shadow-sm"
      :data-testid="`subject-node-${subject.id}`"
    >
      <!-- Level 1: Subject Header -->
      <div
        class="flex items-center justify-between p-3.5 bg-slate-50/80 border-b border-slate-100 hover:bg-slate-100/70 transition-colors"
      >
        <div
          class="flex items-center gap-2.5 flex-1 min-w-0 cursor-pointer select-none"
          @click="toggleSubject(subject.id!)"
        >
          <button
            type="button"
            class="text-slate-400 hover:text-slate-700 p-0.5 rounded cursor-pointer"
            :aria-expanded="isSubjectExpanded(subject.id)"
            :data-testid="`toggle-subject-${subject.id}`"
            @click.stop="toggleSubject(subject.id!)"
          >
            <ChevronDown
              v-if="isSubjectExpanded(subject.id)"
              class="h-4 w-4"
            />
            <ChevronRight
              v-else
              class="h-4 w-4"
            />
          </button>

          <div class="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-brand-accent-100 text-brand-accent-700 font-bold text-xs">
            {{ subject.code || 'SUB' }}
          </div>

          <div class="truncate">
            <span class="text-sm font-semibold text-slate-900">{{ subject.name }}</span>
            <span class="ml-2 text-2xs font-medium text-slate-500 bg-slate-200/70 px-1.5 py-0.5 rounded-full">
              {{ subject.stages?.length ?? 0 }} stages
            </span>
          </div>
        </div>

        <!-- Subject Actions -->
        <div v-if="canManage" class="flex items-center gap-1 shrink-0 ml-2">
          <Button
            type="button"
            icon="pi pi-plus"
            severity="secondary"
            size="small"
            text
            title="Add Subject Stage"
            :data-testid="`add-stage-btn-${subject.id}`"
            @click="emit('add-stage', subject)"
          >
            <template #icon>
              <Plus class="h-3.5 w-3.5" />
            </template>
            <span class="text-xs hidden sm:inline ml-1">Add Stage</span>
          </Button>

          <Button
            type="button"
            severity="secondary"
            size="small"
            text
            title="Edit Subject"
            :data-testid="`edit-subject-btn-${subject.id}`"
            @click="emit('edit-subject', subject)"
          >
            <template #icon>
              <Pencil class="h-3.5 w-3.5 text-slate-500" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Subject Stages Container -->
      <div
        v-if="isSubjectExpanded(subject.id)"
        class="divide-y divide-slate-100"
      >
        <div
          v-if="!subject.stages || subject.stages.length === 0"
          class="p-4 text-xs text-slate-400 italic text-center"
        >
          No stages configured for this subject. Click "Add Stage" above to create one.
        </div>

        <div
          v-for="stage in subject.stages"
          :key="stage.id"
          class="pl-4 sm:pl-6 pr-3 py-3 transition-colors"
          :class="[
            selectedStageId === stage.id
              ? 'bg-brand-accent-50/40 border-l-3 border-brand-accent-600'
              : 'hover:bg-slate-50/60'
          ]"
          :data-testid="`stage-node-${stage.id}`"
        >
          <!-- Level 2: Stage Header -->
          <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div
              class="flex items-start gap-2.5 flex-1 min-w-0 cursor-pointer select-none"
              @click="emit('select-stage', stage, subject)"
            >
              <button
                type="button"
                class="mt-0.5 text-slate-400 hover:text-slate-700 p-0.5 rounded cursor-pointer"
                :aria-expanded="isStageExpanded(stage.id)"
                :data-testid="`toggle-stage-${stage.id}`"
                @click.stop="toggleStage(stage.id!)"
              >
                <ChevronDown
                  v-if="isStageExpanded(stage.id)"
                  class="h-3.5 w-3.5"
                />
                <ChevronRight
                  v-else
                  class="h-3.5 w-3.5"
                />
              </button>

              <div class="space-y-0.5 truncate">
                <div class="flex items-center gap-2">
                  <span class="inline-flex items-center px-1.5 py-0.2 rounded text-2xs font-bold bg-slate-100 text-slate-700 border border-slate-200">
                    #{{ stage.sequenceOrder }}
                  </span>
                  <span
                    class="text-xs font-semibold"
                    :class="selectedStageId === stage.id ? 'text-brand-accent-900' : 'text-slate-800'"
                  >
                    {{ stage.name }}
                  </span>
                </div>
                <p v-if="stage.description" class="text-2xs text-slate-500 truncate max-w-md">
                  {{ stage.description }}
                </p>

                <!-- Stage Badges -->
                <div class="flex items-center gap-1.5 pt-0.5 text-2xs text-slate-500">
                  <span class="inline-flex items-center gap-1">
                    <Layers class="h-2.5 w-2.5 text-slate-400" />
                    {{ stage.units?.length ?? 0 }} units
                  </span>
                  <span>&bull;</span>
                  <span class="inline-flex items-center gap-1">
                    <GraduationCap class="h-2.5 w-2.5 text-slate-400" />
                    {{ stage.gradeMappings?.length ?? 0 }} grades mapped
                  </span>
                </div>
              </div>
            </div>

            <!-- Stage Actions -->
            <div class="flex items-center gap-1 shrink-0 self-end sm:self-center">
              <Button
                v-if="canManage"
                type="button"
                severity="primary"
                size="small"
                outlined
                class="text-xs px-2 py-1"
                title="Enroll Study Group"
                :data-testid="`enroll-study-group-btn-${stage.id}`"
                @click.stop="emit('enroll-study-group', stage, subject)"
              >
                <Users class="h-3 w-3 mr-1" />
                <span>Enroll Group</span>
              </Button>

              <Button
                v-if="canManage"
                type="button"
                severity="secondary"
                size="small"
                text
                class="text-xs px-1.5 py-1"
                title="Add Learning Unit"
                :data-testid="`add-unit-btn-${stage.id}`"
                @click.stop="emit('add-unit', stage, subject)"
              >
                <Plus class="h-3 w-3 mr-1" />
                <span class="hidden md:inline">Unit</span>
              </Button>

              <Button
                v-if="canManage"
                type="button"
                severity="secondary"
                size="small"
                text
                title="Edit Stage"
                :data-testid="`edit-stage-btn-${stage.id}`"
                @click.stop="emit('edit-stage', stage, subject)"
              >
                <Pencil class="h-3 w-3 text-slate-500" />
              </Button>
            </div>
          </div>

          <!-- Level 3: Modular Learning Units Container -->
          <div
            v-if="isStageExpanded(stage.id)"
            class="mt-2.5 pl-6 sm:pl-8 space-y-1.5 border-l-2 border-slate-200/80 ml-2"
          >
            <div
              v-if="!stage.units || stage.units.length === 0"
              class="text-2xs text-slate-400 italic py-1"
            >
              No learning units in this stage. Click "+ Unit" to add modular learning units.
            </div>

            <div
              v-for="unit in stage.units"
              :key="unit.id"
              class="flex items-center justify-between p-2 rounded-lg bg-slate-50/70 border border-slate-100 hover:bg-white hover:border-slate-200 transition-colors group"
              :data-testid="`unit-node-${unit.id}`"
            >
              <div class="flex items-center gap-2 min-w-0 truncate">
                <span class="text-2xs font-mono text-slate-400">
                  {{ unit.sequenceOrder }}.
                </span>
                <span
                  class="inline-flex items-center px-1.5 py-0.2 rounded text-2xs font-semibold uppercase"
                  :class="{
                    'bg-emerald-100 text-emerald-800': unit.unitType === 'MODULE',
                    'bg-amber-100 text-amber-800': unit.unitType === 'PACE',
                    'bg-blue-100 text-blue-800': unit.unitType === 'LESSON',
                    'bg-purple-100 text-purple-800': unit.unitType === 'PRESENTATION',
                    'bg-rose-100 text-rose-800': unit.unitType === 'BOOK',
                    'bg-slate-100 text-slate-700': unit.unitType === 'OTHER'
                  }"
                >
                  {{ unit.unitType }}
                </span>
                <span class="text-xs font-medium text-slate-800 truncate">
                  {{ unit.name }}
                </span>
                <span v-if="unit.description" class="hidden lg:inline text-2xs text-slate-400 truncate max-w-xs">
                  &bull; {{ unit.description }}
                </span>
              </div>

              <!-- Unit Actions -->
              <div v-if="canManage" class="flex items-center gap-0.5 shrink-0 opacity-80 group-hover:opacity-100">
                <button
                  type="button"
                  title="Edit Unit"
                  class="text-slate-400 hover:text-slate-700 p-1 rounded hover:bg-slate-100 cursor-pointer"
                  :data-testid="`edit-unit-btn-${unit.id}`"
                  @click.stop="emit('edit-unit', unit, stage, subject)"
                >
                  <Pencil class="h-3 w-3" />
                </button>
                <button
                  type="button"
                  title="Delete Unit"
                  class="text-slate-400 hover:text-rose-600 p-1 rounded hover:bg-rose-50 cursor-pointer"
                  :data-testid="`delete-unit-btn-${unit.id}`"
                  @click.stop="emit('delete-unit', unit, stage, subject)"
                >
                  <Trash2 class="h-3 w-3" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
