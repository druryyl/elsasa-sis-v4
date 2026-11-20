<script setup lang="ts">
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import { Plus, RefreshCw, SlidersHorizontal } from 'lucide-vue-next';

interface StageOption {
  id: string;
  label: string;
  subjectId: string;
}

interface StudyGroupOption {
  id: string;
  name: string;
}

withDefaults(
  defineProps<{
    subjectStageId: string | null;
    studyGroupId: string | null;
    threshold: number;
    stageOptions: StageOption[];
    studyGroupOptions: StudyGroupOption[];
    canManage?: boolean;
    isLoading?: boolean;
    isDirty?: boolean;
  }>(),
  {
    canManage: false,
    isLoading: false,
    isDirty: false
  }
);

const emit = defineEmits<{
  'update:subjectStageId': [value: string | null];
  'update:studyGroupId': [value: string | null];
  'update:threshold': [value: number];
  'create-assessment': [];
  refresh: [];
}>();

function onThresholdChange(e: Event): void {
  const target = e.target as HTMLInputElement;
  const val = parseFloat(target.value);
  if (!isNaN(val)) {
    emit('update:threshold', Math.max(0, Math.min(100, val)));
  }
}
</script>

<template>
  <div
    class="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center sm:justify-between"
    data-testid="scorebook-filter-bar"
  >
    <div class="flex flex-1 flex-wrap items-center gap-3">
      <!-- Subject Stage Selector -->
      <div class="w-full sm:w-64">
        <label for="filter-stage" class="block text-[11px] font-medium text-slate-500 mb-1">
          Subject Stage
        </label>
        <Select
          id="filter-stage"
          :model-value="subjectStageId"
          :options="stageOptions"
          option-label="label"
          option-value="id"
          placeholder="Select Subject &amp; Stage"
          class="w-full text-xs"
          data-testid="scorebook-stage-select"
          @update:model-value="emit('update:subjectStageId', $event)"
        />
      </div>

      <!-- Study Group Selector -->
      <div class="w-full sm:w-52">
        <label for="filter-group" class="block text-[11px] font-medium text-slate-500 mb-1">
          Study Group (Rombel)
        </label>
        <Select
          id="filter-group"
          :model-value="studyGroupId"
          :options="studyGroupOptions"
          option-label="name"
          option-value="id"
          placeholder="Select Class"
          class="w-full text-xs"
          data-testid="scorebook-group-select"
          @update:model-value="emit('update:studyGroupId', $event)"
        />
      </div>

      <!-- Threshold Config -->
      <div class="w-full sm:w-36">
        <label for="filter-threshold" class="block text-[11px] font-medium text-slate-500 mb-1">
          Passing Threshold (KKM)
        </label>
        <div class="relative">
          <InputText
            id="filter-threshold"
            :value="threshold"
            type="number"
            min="0"
            max="100"
            step="1"
            class="w-full text-xs font-mono pr-7"
            data-testid="scorebook-threshold-input"
            @change="onThresholdChange"
          />
          <SlidersHorizontal class="absolute right-2 top-2.5 h-3.5 w-3.5 text-slate-400 pointer-events-none" />
        </div>
      </div>
    </div>

    <!-- Actions -->
    <div class="flex items-center gap-2 self-end sm:self-center">
      <Button
        severity="secondary"
        text
        size="small"
        :loading="isLoading"
        title="Refresh Gradebook Data"
        data-testid="scorebook-refresh-btn"
        @click="emit('refresh')"
      >
        <template #icon>
          <RefreshCw class="h-4 w-4" />
        </template>
      </Button>

      <Button
        v-if="canManage"
        severity="primary"
        size="small"
        :disabled="!subjectStageId || !studyGroupId"
        title="Add new assessment column"
        data-testid="scorebook-create-assessment-btn"
        @click="emit('create-assessment')"
      >
        <template #icon>
          <Plus class="h-4 w-4 mr-1" />
        </template>
        <span>Create Assessment</span>
      </Button>
    </div>
  </div>
</template>
