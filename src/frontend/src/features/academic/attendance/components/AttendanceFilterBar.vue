<script setup lang="ts">
import { computed } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Select from 'primevue/select';
import { BookOpen, Calendar, Users } from 'lucide-vue-next';
import {
  subjectsLookupQuery,
  studyGroupsLookupQuery
} from '@/shared/services/lookups.api';

const props = withDefaults(
  defineProps<{
    selectedSubjectId: string;
    selectedStudyGroupId: string;
    selectedDate: string;
    academicYearId?: string | null;
    disabled?: boolean;
  }>(),
  {
    academicYearId: null,
    disabled: false
  }
);

const emit = defineEmits<{
  'update:selectedSubjectId': [value: string];
  'update:selectedStudyGroupId': [value: string];
  'update:selectedDate': [value: string];
}>();

const subjectsQuery = useQuery(subjectsLookupQuery());
const studyGroupsQuery = useQuery(
  computed(() =>
    studyGroupsLookupQuery({
      academicYearId: props.academicYearId ?? undefined
    })
  )
);

const isLoadingSubjects = computed(() => subjectsQuery.isLoading.value);
const isLoadingStudyGroups = computed(() => studyGroupsQuery.isLoading.value);

const subjectOptions = computed(() =>
  (subjectsQuery.data.value ?? []).map((s) => ({
    label: s.code ? `${s.name} (${s.code})` : (s.name ?? 'Unnamed Subject'),
    value: s.id ?? ''
  }))
);

const studyGroupOptions = computed(() =>
  (studyGroupsQuery.data.value ?? []).map((g) => ({
    label: g.name ?? 'Unnamed Group',
    value: g.id ?? ''
  }))
);

function onSubjectChange(val: string | null) {
  emit('update:selectedSubjectId', val ?? '');
}

function onStudyGroupChange(val: string | null) {
  emit('update:selectedStudyGroupId', val ?? '');
}

function onDateChange(event: Event) {
  const target = event.target as HTMLInputElement;
  emit('update:selectedDate', target.value);
}
</script>

<template>
  <div
    class="rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:p-5"
    data-testid="attendance-filter-bar"
  >
    <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
      <!-- Subject Selector -->
      <div class="space-y-1.5">
        <label
          for="attendance-subject-select"
          class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600"
        >
          <BookOpen class="h-3.5 w-3.5 text-slate-400" />
          <span>Subject</span>
          <span class="text-rose-500">*</span>
        </label>
        <Select
          id="attendance-subject-select"
          :model-value="selectedSubjectId"
          :options="subjectOptions"
          option-label="label"
          option-value="value"
          placeholder="Select Subject..."
          filter
          :loading="isLoadingSubjects"
          :disabled="disabled"
          class="w-full text-sm"
          data-testid="subject-selector"
          @update:model-value="onSubjectChange"
        />
      </div>

      <!-- Study Group Selector -->
      <div class="space-y-1.5">
        <label
          for="attendance-study-group-select"
          class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600"
        >
          <Users class="h-3.5 w-3.5 text-slate-400" />
          <span>Study Group (Rombel)</span>
          <span class="text-rose-500">*</span>
        </label>
        <Select
          id="attendance-study-group-select"
          :model-value="selectedStudyGroupId"
          :options="studyGroupOptions"
          option-label="label"
          option-value="value"
          placeholder="Select Study Group..."
          filter
          :loading="isLoadingStudyGroups"
          :disabled="disabled"
          class="w-full text-sm"
          data-testid="study-group-selector"
          @update:model-value="onStudyGroupChange"
        />
      </div>

      <!-- Calendar Date Picker -->
      <div class="space-y-1.5">
        <label
          for="attendance-date-picker"
          class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600"
        >
          <Calendar class="h-3.5 w-3.5 text-slate-400" />
          <span>Session Date</span>
          <span class="text-rose-500">*</span>
        </label>
        <input
          id="attendance-date-picker"
          type="date"
          :value="selectedDate"
          :disabled="disabled"
          class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-xs transition-colors focus:border-brand-accent-500 focus:outline-hidden focus:ring-1 focus:ring-brand-accent-500 disabled:cursor-not-allowed disabled:bg-slate-100"
          data-testid="date-picker"
          @input="onDateChange"
        />
      </div>
    </div>
  </div>
</template>
