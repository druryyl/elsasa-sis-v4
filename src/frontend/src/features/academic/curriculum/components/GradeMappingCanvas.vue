<script setup lang="ts">
import { computed } from 'vue';
import { Check, Plus, Tag, Trash2 } from 'lucide-vue-next';
import type { SubjectStageGradeResponse } from '../api/curriculum.api';
import {
  SCHOOL_LEVELS,
  VALID_GRADES_BY_LEVEL,
  type SchoolLevel
} from '../schemas/curriculum.schema';

const props = withDefaults(
  defineProps<{
    subjectId: string;
    stageId: string;
    stageName: string;
    gradeMappings?: SubjectStageGradeResponse[] | null;
    canManage?: boolean;
    isSubmitting?: boolean;
  }>(),
  {
    gradeMappings: () => [],
    canManage: true,
    isSubmitting: false
  }
);

const emit = defineEmits<{
  'map-grade': [{ schoolLevel: SchoolLevel; grade: string }];
  'remove-mapping': [{ mappingId: string; schoolLevel: string; grade: string }];
}>();

const safeMappings = computed(() => props.gradeMappings ?? []);

function findMapping(
  level: SchoolLevel,
  gradeNum: number
): SubjectStageGradeResponse | undefined {
  const targetGradeStr = String(gradeNum);
  return safeMappings.value.find(
    (m) =>
      m.schoolLevel?.toUpperCase() === level &&
      String(m.grade) === targetGradeStr
  );
}

function handleGradeClick(level: SchoolLevel, gradeNum: number): void {
  if (!props.canManage || props.isSubmitting) return;

  const existing = findMapping(level, gradeNum);
  if (existing && existing.id) {
    emit('remove-mapping', {
      mappingId: existing.id,
      schoolLevel: existing.schoolLevel ?? level,
      grade: existing.grade ?? String(gradeNum)
    });
  } else {
    emit('map-grade', {
      schoolLevel: level,
      grade: String(gradeNum)
    });
  }
}
</script>

<template>
  <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs" data-testid="grade-mapping-canvas">
    <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between mb-4 pb-3 border-b border-slate-100">
      <div>
        <div class="flex items-center gap-2">
          <Tag class="h-4 w-4 text-brand-accent-600" />
          <h3 class="text-sm font-semibold text-slate-900">Grade Mapping Matrix</h3>
        </div>
        <p class="text-xs text-slate-500 mt-0.5">
          Map {{ stageName }} to administrative grade levels. Click a grade pill to toggle its assignment.
        </p>
      </div>

      <div class="text-xs text-slate-500">
        <span class="font-semibold text-brand-accent-700">{{ safeMappings.length }}</span> grades mapped
      </div>
    </div>

    <!-- Matrix Rows -->
    <div class="space-y-4">
      <div
        v-for="level in SCHOOL_LEVELS"
        :key="level"
        class="flex flex-col gap-2.5 sm:flex-row sm:items-center p-3 rounded-lg bg-slate-50 border border-slate-100"
      >
        <div class="w-24 shrink-0">
          <span
            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold tracking-wide"
            :class="{
              'bg-blue-100 text-blue-800': level === 'SD',
              'bg-indigo-100 text-indigo-800': level === 'SMP',
              'bg-purple-100 text-purple-800': level === 'SMA',
              'bg-emerald-100 text-emerald-800': level === 'SMK'
            }"
          >
            {{ level }}
          </span>
          <span class="text-2xs text-slate-400 block mt-0.5">
            {{ level === 'SD' ? 'Primary' : level === 'SMP' ? 'Junior High' : 'Senior High' }}
          </span>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <button
            v-for="gradeNum in VALID_GRADES_BY_LEVEL[level]"
            :key="`${level}-${gradeNum}`"
            type="button"
            :disabled="!canManage || isSubmitting"
            :data-testid="`grade-pill-${level}-${gradeNum}`"
            class="group inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-all"
            :class="[
              findMapping(level, gradeNum)
                ? 'bg-brand-accent-600 text-white shadow-xs hover:bg-brand-accent-700'
                : 'bg-white text-slate-700 border border-slate-200 hover:border-brand-accent-300 hover:bg-brand-accent-50/50 hover:text-brand-accent-700',
              !canManage || isSubmitting ? 'cursor-not-allowed opacity-60' : 'cursor-pointer'
            ]"
            @click="handleGradeClick(level, gradeNum)"
          >
            <Check
              v-if="findMapping(level, gradeNum)"
              class="h-3 w-3 stroke-[2.5]"
            />
            <Plus
              v-else-if="canManage"
              class="h-3 w-3 opacity-0 group-hover:opacity-100 transition-opacity"
            />
            <span>Grade {{ gradeNum }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Active Mappings Tag List with Remove actions -->
    <div v-if="safeMappings.length > 0" class="mt-4 pt-3 border-t border-slate-100">
      <div class="text-xs font-medium text-slate-700 mb-2">Active Grade Mappings</div>
      <div class="flex flex-wrap gap-1.5">
        <span
          v-for="mapping in safeMappings"
          :key="mapping.id"
          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs bg-slate-100 text-slate-800 font-medium border border-slate-200"
          :data-testid="`active-mapping-${mapping.id}`"
        >
          <span>{{ mapping.schoolLevel }} Grade {{ mapping.grade }}</span>
          <button
            v-if="canManage"
            type="button"
            title="Remove mapping"
            :disabled="isSubmitting"
            class="text-slate-400 hover:text-rose-600 transition-colors p-0.5 rounded cursor-pointer"
            @click="emit('remove-mapping', {
              mappingId: mapping.id!,
              schoolLevel: mapping.schoolLevel ?? '',
              grade: mapping.grade ?? ''
            })"
          >
            <Trash2 class="h-3 w-3" />
          </button>
        </span>
      </div>
    </div>
  </div>
</template>
