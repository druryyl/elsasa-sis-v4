<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import { AlertCircle, Info, Users } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { apiClient } from '@/shared/services/http';
import { studyGroupsLookupQuery } from '@/shared/services/lookups.api';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import type { components } from '@/shared/types/api/schema';
import { useBatchEnrollMutation } from '../api/useCurriculumMutations';

type StudyGroupRosterDto = components['schemas']['StudyGroupRosterDto'];

const props = withDefaults(
  defineProps<{
    visible: boolean;
    subjectId: string;
    subjectName: string;
    stageId: string;
    stageName: string;
  }>(),
  {}
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  enrolled: [{ enrolledCount: number; skippedCount: number }];
}>();

const { selectedAcademicYearId, selectedPeriodId, activeAcademicYearName, activePeriodName } =
  useAppContextBridge();

const selectedStudyGroupId = ref<string>('');
const conflictBanner = ref<string | null>(null);

// Fetch study groups for active academic year via shared lookups
const studyGroupsQuery = useQuery(
  computed(() =>
    studyGroupsLookupQuery({
      academicYearId: selectedAcademicYearId.value ?? undefined,
      pageSize: 100
    })
  )
);

const studyGroupOptions = computed(() => {
  const items = studyGroupsQuery.data.value ?? [];
  return items.map((sg) => ({
    label: `${sg.name} (${sg.schoolLevel} Gr.${sg.grade})`,
    value: sg.id ?? ''
  }));
});

// Fetch study group roster when a study group is selected to preview eligible students
const selectedGroupRosterQuery = useQuery<StudyGroupRosterDto>({
  queryKey: computed(() => ['curriculum', 'study-group-roster', selectedStudyGroupId.value]),
  queryFn: async () => {
    return (await apiClient.get(
      `/std/study-groups/${selectedStudyGroupId.value}/roster`
    )) as unknown as StudyGroupRosterDto;
  },
  enabled: computed(() => props.visible && Boolean(selectedStudyGroupId.value))
});

const eligibleStudentCount = computed(() => {
  if (!selectedStudyGroupId.value) return 0;
  return selectedGroupRosterQuery.data.value?.students?.length ?? 0;
});

const isRosterLoading = computed(() => selectedGroupRosterQuery.isLoading.value);

const selectedGroupName = computed(() => {
  const found = studyGroupOptions.value.find((opt) => opt.value === selectedStudyGroupId.value);
  return found?.label ?? 'None selected';
});

const batchEnrollMutation = useBatchEnrollMutation();
const isSubmitting = computed(() => batchEnrollMutation.isPending.value);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      conflictBanner.value = null;
      selectedStudyGroupId.value = '';
    }
  }
);

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

async function handleConfirm(): Promise<void> {
  conflictBanner.value = null;
  const periodId = selectedPeriodId.value;
  if (!periodId) {
    conflictBanner.value = 'No active operational period is currently configured.';
    return;
  }
  if (!selectedStudyGroupId.value) {
    conflictBanner.value = 'Please select a target study group.';
    return;
  }

  try {
    const result = await batchEnrollMutation.mutateAsync({
      operationalPeriodId: periodId,
      subjectStageId: props.stageId,
      studyGroupId: selectedStudyGroupId.value
    });

    emit('enrolled', {
      enrolledCount: result.enrolledCount ?? 0,
      skippedCount: result.skippedCount ?? 0
    });
    emit('update:visible', false);
  } catch (error) {
    conflictBanner.value =
      error instanceof Error ? error.message : 'Failed to complete batch enrollment.';
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :draggable="false"
    :closable="!isSubmitting"
    class="w-full max-w-xl"
    data-testid="batch-enroll-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-accent-50 text-brand-accent-600">
          <Users class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Batch Student Enrollment (Pre-Flight)</h3>
          <p class="text-xs text-slate-500">Tier 3 Impact Summary &bull; Stage Enrollment Workflow</p>
        </div>
      </div>
    </template>

    <div class="space-y-4">
      <div
        v-if="conflictBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="flex-1">{{ conflictBanner }}</div>
      </div>

      <!-- Target Selection -->
      <FormField label="Target Study Group" required field-id="study-group-select">
        <Select
          id="study-group-select"
          v-model="selectedStudyGroupId"
          :options="studyGroupOptions"
          option-label="label"
          option-value="value"
          placeholder="Select study group to enroll..."
          class="w-full"
          :loading="studyGroupsQuery.isLoading.value"
          data-testid="study-group-select"
        />
      </FormField>

      <!-- Tier 3 Pre-Flight Impact Summary Cards -->
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div class="rounded-lg border border-slate-200 bg-slate-50 p-3 space-y-1">
          <div class="text-2xs font-semibold uppercase tracking-wider text-slate-400">Target Curriculum Stage</div>
          <div class="text-sm font-semibold text-slate-900">{{ stageName }}</div>
          <div class="text-xs text-slate-500">{{ subjectName }}</div>
        </div>

        <div class="rounded-lg border border-slate-200 bg-slate-50 p-3 space-y-1">
          <div class="text-2xs font-semibold uppercase tracking-wider text-slate-400">Target Study Group</div>
          <div class="text-sm font-semibold text-slate-900">{{ selectedGroupName }}</div>
          <div class="text-xs text-slate-500">{{ activeAcademicYearName || 'Current Year' }} &bull; {{ activePeriodName || 'Current Period' }}</div>
        </div>

        <div class="rounded-lg border border-slate-200 bg-brand-accent-50/50 p-3 space-y-1 sm:col-span-2">
          <div class="flex items-center justify-between">
            <span class="text-2xs font-semibold uppercase tracking-wider text-brand-accent-700">Eligible Placed Students Preview</span>
            <span v-if="isRosterLoading" class="text-xs text-slate-500">Checking roster...</span>
            <span v-else class="text-base font-bold tabular-nums text-brand-accent-800" data-testid="eligible-count">
              {{ eligibleStudentCount }} Students
            </span>
          </div>
          <p class="text-xs text-slate-600">
            All students with active placement in this study group for the current academic year will be enrolled into {{ stageName }}.
          </p>
        </div>
      </div>

      <!-- Idempotency & Duplicate Skip Explanation Banner -->
      <div class="flex items-start gap-2.5 rounded-lg border border-blue-200 bg-blue-50 p-3 text-xs leading-relaxed text-blue-800" data-testid="skip-explanation">
        <Info class="mt-0.5 h-4 w-4 shrink-0 text-blue-600" />
        <div>
          <span class="font-semibold">Idempotent Duplicate Skip Behavior:</span>
          Students in this group who already hold an active participation in this subject stage for this period will be skipped automatically without duplicate records or errors.
        </div>
      </div>

      <!-- Action Footer -->
      <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeModal"
        />
        <Button
          type="button"
          severity="primary"
          :loading="isSubmitting"
          :disabled="!selectedStudyGroupId || isRosterLoading || isSubmitting"
          :label="eligibleStudentCount > 0 ? `Enroll ${eligibleStudentCount} Students` : 'Enroll Students'"
          data-testid="confirm-enroll-button"
          @click="handleConfirm"
        />
      </div>
    </div>
  </Dialog>
</template>
