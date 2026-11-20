<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Select from 'primevue/select';
import { FilePlus2, Info, Users } from 'lucide-vue-next';

export interface BatchGenerateStudyGroupOption {
  id: string;
  name: string;
  studentCount: number;
  homeroomTeacherName?: string | null;
}

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studyGroups: BatchGenerateStudyGroupOption[];
    initialStudyGroupId?: string;
    operationalPeriodId: string;
    operationalPeriodLabel: string;
    existingCardsCount?: number;
    loading?: boolean;
  }>(),
  {
    initialStudyGroupId: '',
    existingCardsCount: 0,
    loading: false
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  confirm: [payload: { studyGroupId: string; operationalPeriodId: string }];
}>();

const selectedGroupId = ref<string>(props.initialStudyGroupId);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      selectedGroupId.value =
        props.initialStudyGroupId || props.studyGroups[0]?.id || '';
    }
  }
);

const selectedGroup = computed(
  () => props.studyGroups.find((g) => g.id === selectedGroupId.value) ?? null
);

const selectOptions = computed(() =>
  props.studyGroups.map((g) => ({
    label: `${g.name} (${g.studentCount} students)`,
    value: g.id
  }))
);

const isSameAsActiveGroup = computed(
  () => Boolean(props.initialStudyGroupId && selectedGroupId.value === props.initialStudyGroupId)
);

const totalEnrolled = computed(() => selectedGroup.value?.studentCount ?? 0);
const existingCount = computed(() =>
  isSameAsActiveGroup.value ? props.existingCardsCount : 0
);
const estimatedNewDrafts = computed(() =>
  Math.max(0, totalEnrolled.value - existingCount.value)
);

const canSubmit = computed(
  () =>
    !props.loading &&
    Boolean(selectedGroupId.value) &&
    Boolean(props.operationalPeriodId) &&
    totalEnrolled.value > 0
);

function handleClose(): void {
  if (props.loading) return;
  emit('update:visible', false);
}

function handleConfirm(): void {
  if (!canSubmit.value) return;
  emit('confirm', {
    studyGroupId: selectedGroupId.value,
    operationalPeriodId: props.operationalPeriodId
  });
}
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :closable="!loading"
    :draggable="false"
    class="w-full max-w-lg"
    data-testid="batch-generate-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-accent-50 text-brand-accent-600"
        >
          <FilePlus2 class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">
            Generate Draft Semester Report Cards
          </h3>
          <p class="text-xs text-slate-500">Pre-Flight Impact Summary</p>
        </div>
      </div>
    </template>

    <div class="space-y-4">
      <div class="space-y-1.5">
        <label
          for="batch-generate-group"
          class="block text-xs font-semibold uppercase tracking-wider text-slate-600"
        >
          Target Study Group <span class="text-rose-500">*</span>
        </label>
        <Select
          id="batch-generate-group"
          v-model="selectedGroupId"
          :options="selectOptions"
          option-label="label"
          option-value="value"
          placeholder="Select Study Group..."
          filter
          :disabled="loading"
          class="w-full text-sm"
          data-testid="batch-generate-group-select"
        />
      </div>

      <div class="rounded-lg border border-slate-200 bg-slate-50 p-4">
        <h4 class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-600">
          Batch Generation Impact Summary
        </h4>
        <dl class="grid grid-cols-2 gap-3 text-sm">
          <div>
            <dt class="text-xs text-slate-500">Academic Period</dt>
            <dd class="font-medium text-slate-900">{{ operationalPeriodLabel || '-' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-slate-500">Homeroom Teacher</dt>
            <dd class="font-medium text-slate-900">
              {{ selectedGroup?.homeroomTeacherName || 'Unassigned' }}
            </dd>
          </div>
          <div>
            <dt class="text-xs text-slate-500">Enrolled Students in Cohort</dt>
            <dd class="flex items-center gap-1.5 font-semibold text-slate-900">
              <Users class="h-4 w-4 text-slate-400" />
              <span>{{ totalEnrolled }}</span>
            </dd>
          </div>
          <div v-if="isSameAsActiveGroup">
            <dt class="text-xs text-slate-500">Estimated New Drafts</dt>
            <dd class="font-semibold text-brand-accent-600">
              {{ estimatedNewDrafts }} new
              <span v-if="existingCount > 0" class="text-xs font-normal text-slate-500">
                ({{ existingCount }} existing skipped)
              </span>
            </dd>
          </div>
        </dl>
      </div>

      <div
        class="flex items-start gap-2.5 rounded-lg border border-sky-200 bg-sky-50 p-3 text-xs leading-relaxed text-sky-900"
      >
        <Info class="mt-0.5 h-4 w-4 shrink-0 text-sky-600" />
        <p>
          Final grades are computed as the arithmetic mean of scorebook entries for this period.
          Students who already have a <strong>DRAFT</strong> or <strong>FINALIZED</strong> report
          card for this period will be safely skipped without overwriting existing notes.
        </p>
      </div>
    </div>

    <template #footer>
      <Button
        type="button"
        label="Cancel"
        severity="secondary"
        text
        :disabled="loading"
        data-testid="batch-generate-cancel-btn"
        @click="handleClose"
      />
      <Button
        type="button"
        label="Generate Draft Cards"
        :loading="loading"
        :disabled="!canSubmit"
        data-testid="batch-generate-confirm-btn"
        @click="handleConfirm"
      />
    </template>
  </Dialog>
</template>
